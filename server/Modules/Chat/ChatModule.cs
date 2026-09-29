using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Chat;

public sealed class ChatModule : IGameModule
{
    public const int MaximumLength = 40, HistoryLimit = 300;
    public const long MinimumIntervalMs = 1000;
    public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "chat.send" };

    public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
    {
        var actor = context?.Actor;
        if (actor == null || actor.IsSystem || actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
            return GameResult.Reject(GameCodes.Forbidden, "请使用本世界的已登录角色发言。");
        if (command.Type != "chat.send") return GameResult.Reject(GameCodes.InvalidArgument, "聊天命令无效。");
        var payload = command.Payload;
        if (payload == null ||
            payload["channel"]?.Type != JTokenType.String || payload["content"]?.Type != JTokenType.String)
            return GameResult.Reject(GameCodes.InvalidArgument, "聊天参数无效。");
        string channel = payload.Value<string>("channel"), content = payload.Value<string>("content");
        if (channel != "world" && channel != "nation" && channel != "city")
            return GameResult.Reject(GameCodes.InvalidArgument, "联机当前开放世界、国家和城池频道。");
        if (payload.Properties().Any(p => p.Name != "channel" && p.Name != "content" &&
            (channel != "city" || (p.Name != "cityX" && p.Name != "cityY"))))
            return GameResult.Reject(GameCodes.InvalidArgument, "聊天参数无效。");
        int cityX = 0, cityY = 0;
        if (channel == "city" && (!Coordinate(payload["cityX"], out cityX) || !Coordinate(payload["cityY"], out cityY)))
            return GameResult.Reject(GameCodes.InvalidArgument, "请选择当前本人封地所在城池。");
        if (content.Length > MaximumLength)
            return GameResult.Reject(GameCodes.InvalidArgument, "一条消息最多40字。");
        content = content.Replace("\r", "").Replace("\n", " ").Trim();
        if (content.Length == 0 || content.Any(char.IsControl))
            return GameResult.Reject(GameCodes.InvalidArgument, "请输入有效聊天内容。");
        JObject player;
        try { player = candidate.RequirePlayer(actor.PlayerId); }
        catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "角色不存在于此世界。"); }
        string name = player["基础信息"]?.Value<string>("名字"), nation = Nation(candidate, player);
        if (string.IsNullOrWhiteSpace(name)) return GameResult.Reject(GameCodes.RoleRequired, "请先创建角色。");
        if (channel == "nation" && nation == null) return GameResult.Reject(GameCodes.Forbidden, "加入国家后才能在国家频道发言。");
        if (channel == "city" && !LivesInCity(candidate, actor.PlayerId, cityX, cityY))
            return GameResult.Reject(GameCodes.Forbidden, "只能在本人现有封地所在城池发言。");

        // ponytail: keep the bounded chat log in the existing world transaction; no second store or broker.
        var history = candidate.Data["聊天消息"] as JArray;
        if (candidate.Data["聊天消息"] != null && history == null)
            return GameResult.Reject(GameCodes.Unavailable, "聊天记录无法读取。");
        history ??= new JArray();
        var previous = history.OfType<JObject>().LastOrDefault(m => m.Value<string>("senderPlayerId") == actor.PlayerId);
        if (previous != null && context.ServerUtcMs - previous.Value<long>("serverUtcMs") < MinimumIntervalMs)
            return GameResult.Reject(GameCodes.Conflict, "发言过于频繁，请稍后再发。");
        var message = new JObject { ["messageId"] = Guid.NewGuid().ToString("N"), ["channel"] = channel,
            ["content"] = content, ["senderPlayerId"] = actor.PlayerId, ["senderName"] = name,
            ["nation"] = nation, ["serverUtcMs"] = context.ServerUtcMs };
        if (channel == "city") { message["cityX"] = cityX; message["cityY"] = cityY; }
        history.Add(message);
        while (history.Count > HistoryLimit) history.RemoveAt(0);
        candidate.Data["聊天消息"] = history;
        var result = GameResult.Success(new JObject { ["chatMessage"] = message.DeepClone() });
        result.Events.Add(new GameEvent { EventId = message.Value<string>("messageId"), WorldId = candidate.WorldId,
            Type = "chat.message", ServerUtcMs = context.ServerUtcMs, Data = (JObject)message.DeepClone(),
            AudiencePlayerIds = channel == "world" ? null : ((JObject)candidate.EntityMappings["players"]).Properties()
                .Where(p => channel == "nation" ? Nation(candidate, candidate.RequirePlayer(p.Name)) == nation :
                    LivesInCity(candidate, p.Name, cityX, cityY)).Select(p => p.Name).ToArray() });
        return result;
    }

    // The Host adds this filtered view to the authenticated snapshot, never the raw national log.
    public static JArray ReadHistory(WorldState state, AuthenticatedActor actor)
    {
        if (actor != null && (actor.IsSystem || actor.WorldId != state.WorldId)) return new JArray();
        string nation = actor == null ? null : Nation(state, state.RequirePlayer(actor.PlayerId));
        var cities = ReadCities(state, actor);
        return new JArray((state.Data["聊天消息"] as JArray ?? new JArray()).OfType<JObject>()
            .Where(m => m.Value<string>("channel") == "world" || (nation != null && m.Value<string>("channel") == "nation" &&
                m.Value<string>("nation") == nation) || (m.Value<string>("channel") == "city" &&
                Coordinate(m["cityX"], out int x) && Coordinate(m["cityY"], out int y) &&
                cities.Any(c => c.Value<int>("x") == x && c.Value<int>("y") == y)))
            .Select(m => m.DeepClone()));
    }

    public static JArray ReadCities(WorldState state, AuthenticatedActor actor)
    {
        if (actor == null || actor.IsSystem || actor.WorldId != state.WorldId) return new JArray();
        var locations = new JArray();
        foreach (var fief in (state.RequirePlayer(actor.PlayerId)["封地信息表"] as JArray ?? new JArray()).OfType<JObject>())
            if (fief["所在城池"] is JObject location && Coordinate(location["x"], out int x) && Coordinate(location["y"], out int y) &&
                LivesInCity(state, actor.PlayerId, x, y) && !locations.Any(c => c.Value<int>("x") == x && c.Value<int>("y") == y))
                locations.Add(new JObject { ["x"] = x, ["y"] = y });
        return locations;
    }

    private static bool LivesInCity(WorldState state, string playerId, int x, int y)
    {
        var city = ((JArray)state.Data["城池列表"]).OfType<JObject>().FirstOrDefault(c => c.Value<int>("坐标x") == x && c.Value<int>("坐标y") == y);
        if (city == null) return false;
        int index = state.ResolvePlayerIndex(playerId);
        return (state.RequirePlayer(playerId)["封地信息表"] as JArray ?? new JArray()).OfType<JObject>().Any(f =>
            f["所在城池"] is JObject location && Coordinate(location["x"], out int fx) && Coordinate(location["y"], out int fy) && fx == x && fy == y &&
            (city["城池封地列表"] as JArray ?? new JArray()).OfType<JObject>().Any(r => r.Value<int>("第几个玩家") == index && JToken.DeepEquals(r["封地ID标识"], f["ID"])));
    }

    private static bool Coordinate(JToken value, out int position)
    {
        position = -1;
        return value?.Type == JTokenType.Integer && int.TryParse(value.ToString(), out position) && position >= 0;
    }

    private static string Nation(WorldState state, JObject player)
    {
        string nation = player["基础信息"]?.Value<string>("国家");
        return !string.IsNullOrEmpty(nation) && ((JArray)state.Data["国家列表"]).OfType<JObject>()
            .Any(n => n.Value<string>("国号") == nation) ? nation : null;
    }
}
