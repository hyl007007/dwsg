using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Auxiliary;

// 原称号佩戴与赌场规则的权威执行入口，资金、开奖、冷却与回执同一事务保存。
public sealed class AuxiliaryModule : IGameModule, IGameTickModule
{
    public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "title.equip", "casino.bet", "casino.advance", "auxiliary.initialize" };
    private static IEnumerable<string> Humans(WorldState world) => (world.EntityMappings["humanPlayers"] as JObject ?? new JObject()).Properties()
        .Where(p => p.Value.Type == JTokenType.Boolean && p.Value.Value<bool>()).Select(p => p.Name);
    public static void InitializePlayer(WorldState world, string playerId, long utcMs)
    {
        if (!Humans(world).Contains(playerId)) throw new InvalidOperationException("Authenticated human role required");
        var all = world.EntityMappings["auxiliary"] as JObject;
        if (all == null) world.EntityMappings["auxiliary"] = all = new JObject();
        if (all[playerId] == null) all[playerId] = new JObject { ["titleReadyUtcMs"] = 0,
            ["casino"] = new JObject { ["roundId"] = Guid.NewGuid().ToString("N"), ["drawing"] = false,
                ["nextUtcMs"] = checked(utcMs + 60000), ["bets"] = new JArray() } };
    }
    public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs)
    {
        if (Humans(state).Any(id => state.EntityMappings["auxiliary"]?[id] == null))
            yield return new GameCommand { WorldId = state.WorldId, Type = "auxiliary.initialize", RequestId = "auxiliary-init:" + state.Revision };
        foreach (string id in Humans(state))
        {
            var casino = state.EntityMappings["auxiliary"]?[id]?["casino"];
            if (casino == null || casino.Value<long>("nextUtcMs") > serverUtcMs) continue;
            long due = casino.Value<long>("nextUtcMs");
            yield return new GameCommand { WorldId = state.WorldId, Type = "casino.advance", RequestId = "casino:" + id + ":" + due,
                Payload = new JObject { ["playerId"] = id, ["dueUtcMs"] = due } };
        }
    }
    public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
    {
        var actor = context?.Actor;
        if (actor == null || actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
            return GameResult.Reject(GameCodes.Forbidden, "角色或世界无效。");
        bool system = command.Type == "casino.advance" || command.Type == "auxiliary.initialize";
        if (actor.IsSystem != system || !system && !Humans(candidate).Contains(actor.PlayerId))
            return GameResult.Reject(GameCodes.Forbidden, "开奖和时钟只能由服务器推进。");
        if (command.Type == "auxiliary.initialize")
        {
            if (command.Payload.Count != 0) return Invalid();
            foreach (string id in Humans(candidate)) InitializePlayer(candidate, id, context.ServerUtcMs);
            return GameResult.Success();
        }
        if (command.Type == "casino.advance") return Advance(candidate, context, command.Payload);
        InitializePlayer(candidate, actor.PlayerId, context.ServerUtcMs);
        if (command.Type == "title.equip") return Equip(candidate, actor.PlayerId, context.ServerUtcMs, command.Payload);
        if (command.Type == "casino.bet") return Bet(candidate, actor.PlayerId, context.ServerUtcMs, command.Payload);
        return Invalid();
    }
    private static GameResult Invalid() => GameResult.Reject(GameCodes.InvalidArgument, "操作参数无效。");
    private static bool Keys(JObject value, params string[] keys) => value != null && value.Count == keys.Length && value.Properties().All(p => keys.Contains(p.Name));
    private static GameResult Equip(WorldState world, string id, long utc, JObject payload)
    {
        if (!Keys(payload, "name") || payload["name"]?.Type != JTokenType.String || payload.Value<string>("name").Length > 40) return Invalid();
        var player = world.RequirePlayer(id);
        var titles = player["称号信息表"] as JArray;
        var chosen = titles?.OfType<JObject>().FirstOrDefault(t => t.Value<string>("名字") == payload.Value<string>("name"));
        if (chosen == null || chosen.Value<int>("状态") == 0) return GameResult.Reject(GameCodes.Forbidden, "尚未获得此称号。");
        if (chosen.Value<int>("状态") == 2) return GameResult.Success(new JObject { ["name"] = chosen["名字"].DeepClone() });
        var clock = world.EntityMappings["auxiliary"][id];
        long ready = clock.Value<long>("titleReadyUtcMs");
        if (ready > utc) return GameResult.Reject(GameCodes.Conflict, "冷却中,剩余:" + Math.Ceiling((ready - utc) / 1000d));
        foreach (var title in titles.OfType<JObject>()) if (title.Value<int>("状态") != 0) title["状态"] = title == chosen ? 2 : 1;
        player["基础信息"]["称号名"] = chosen["名字"].DeepClone();
        clock["titleReadyUtcMs"] = checked(utc + 20000);
        return GameResult.Success(new JObject { ["name"] = chosen["名字"].DeepClone() });
    }
    private static GameResult Bet(WorldState world, string id, long utc, JObject payload)
    {
        if (!Keys(payload, "type", "amount") || payload["type"]?.Type != JTokenType.Integer || payload["amount"]?.Type != JTokenType.Integer ||
            !int.TryParse(payload["type"].ToString(), out int type) || type < 0 || type > 2 ||
            !int.TryParse(payload["amount"].ToString(), out int amount) || amount <= 0) return Invalid();
        var casino = (JObject)world.EntityMappings["auxiliary"][id]["casino"];
        if (casino.Value<bool>("drawing")) return GameResult.Reject(GameCodes.Conflict, "已经开始无法下注。");
        if (casino.Value<long>("nextUtcMs") <= utc) return GameResult.Reject(GameCodes.Conflict, "正在开局，请稍后再试。");
        var bets = (JArray)casino["bets"];
        if (bets.Any(b => b.Value<int>("type") == type)) return GameResult.Reject(GameCodes.Conflict, "本局该类别已下注，请等待结算。");
        var wallet = world.RequirePlayer(id)["财产信息"] as JObject;
        if (!ShopRules.TryNumber(wallet?["黄金"], out double gold) || gold < 0) return GameResult.Reject(GameCodes.Unavailable, "黄金数据无效。");
        if (gold < amount) return GameResult.Reject(GameCodes.InsufficientFunds, "余额不足下注失败。");
        wallet["黄金"] = gold - amount;
        bets.Add(new JObject { ["type"] = type, ["amount"] = amount });
        return GameResult.Success(new JObject { ["roundId"] = casino["roundId"].DeepClone(), ["amount"] = amount });
    }
    private static GameResult Advance(WorldState world, CommandContext context, JObject payload)
    {
        if (!Keys(payload, "playerId", "dueUtcMs") || payload["playerId"]?.Type != JTokenType.String || payload["dueUtcMs"]?.Type != JTokenType.Integer ||
            !long.TryParse(payload["dueUtcMs"].ToString(), out long due)) return Invalid();
        string id = payload.Value<string>("playerId");
        var casino = world.EntityMappings["auxiliary"]?[id]?["casino"] as JObject;
        if (!Humans(world).Contains(id) || casino == null || casino.Value<long>("nextUtcMs") != due || due > context.ServerUtcMs)
            return GameResult.Reject(GameCodes.Conflict, "开奖时钟已更新。");
        var bets = (JArray)casino["bets"];
        if (!casino.Value<bool>("drawing"))
        {
            if (bets.Count == 0) casino["nextUtcMs"] = checked(context.ServerUtcMs + 60000);
            else
            {
                casino["drawing"] = true;
                casino["dice"] = new JArray(RandomNumberGenerator.GetInt32(1,7),RandomNumberGenerator.GetInt32(1,7),RandomNumberGenerator.GetInt32(1,7));
                casino["nextUtcMs"] = checked(due + 10000);
            }
            return GameResult.Success();
        }
        var dice = (JArray)casino["dice"];
        int first=dice[0].Value<int>(), second=dice[1].Value<int>(), third=dice[2].Value<int>();
        if (new[] { first,second,third }.Any(d => d < 1 || d > 6)) return GameResult.Reject(GameCodes.Unavailable, "开奖结果无效。");
        int size = first+second+third < 11 ? 0 : 1;
        decimal payout = 0;
        var settled = (JArray)bets.DeepClone();
        foreach (var bet in settled.OfType<JObject>())
        {
            int type=bet.Value<int>("type");
            decimal amount = bet.Value<int>("amount");
            decimal returned = type == size ? amount * 1.2m : type == 2 && first == second && second == third ? amount * 1.5m : 0;
            payout += returned; bet["returned"] = returned;
        }
        var wallet = world.RequirePlayer(id)["财产信息"] as JObject;
        if (!ShopRules.TryNumber(wallet?["黄金"], out double gold) || gold < 0 || double.IsInfinity(gold + (double)payout))
            return GameResult.Reject(GameCodes.Unavailable, "黄金结算数据无效。");
        wallet["黄金"] = gold + (double)payout;
        casino["last"] = new JObject { ["roundId"] = casino["roundId"].DeepClone(), ["dice"] = dice.DeepClone(),
            ["bets"] = settled, ["payout"] = payout, ["settledUtcMs"] = due };
        casino["drawing"] = false; casino["bets"] = new JArray(); casino.Remove("dice");
        casino["roundId"] = Guid.NewGuid().ToString("N"); casino["nextUtcMs"] = checked(context.ServerUtcMs + 60000);
        return GameResult.Success();
    }
    public static JObject ProjectCasino(WorldState world, AuthenticatedActor actor)
    {
        if (actor == null || actor.IsSystem || actor.WorldId != world.WorldId || !Humans(world).Contains(actor.PlayerId)) return null;
        var casino = world.EntityMappings["auxiliary"]?[actor.PlayerId]?["casino"] as JObject;
        if (casino == null) return null;
        var view = (JObject)casino.DeepClone(); view.Remove("dice"); return view;
    }
    public static long TitleReadyUtcMs(WorldState world, AuthenticatedActor actor) => actor == null || actor.IsSystem || actor.WorldId != world.WorldId ? 0 :
        world.EntityMappings["auxiliary"]?[actor.PlayerId]?.Value<long>("titleReadyUtcMs") ?? 0;
}
