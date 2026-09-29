using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Economy
{
    public sealed class ProductionModule : IGameModule, IGameTickModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[]
            { "fief.construct", "fief.upgrade", "fief.demolish", "world.production.resume", "world.production.tick" };
        private readonly string instanceId = Guid.NewGuid().ToString("N");

        public static void InitializePlayer(WorldState candidate, string stablePlayerId, long serverUtcMs)
        {
            if (candidate.EntityMappings["humanPlayers"]?[stablePlayerId]?.Type != JTokenType.Boolean ||
                candidate.EntityMappings["humanPlayers"].Value<bool>(stablePlayerId) != true)
                throw new InvalidOperationException("Production requires an authoritative human role binding.");
            var player = candidate.RequirePlayer(stablePlayerId);
            player["基础信息"]["粮食增加"] = BuildingRules.FarmRate(player);
            var clocks = candidate.EntityMappings["production"] as JObject;
            if (clocks == null) candidate.EntityMappings["production"] = clocks = new JObject();
            if (clocks[stablePlayerId] == null) clocks[stablePlayerId] = NewClock(serverUtcMs);
        }

        private static JObject NewClock(long utc)
        {
            return new JObject { ["lastUtcMs"] = utc, ["nextClampUtcMs"] = checked(utc + 4000) };
        }

        private static IEnumerable<string> Humans(WorldState state)
        {
            var humans = state.EntityMappings["humanPlayers"] as JObject;
            return humans == null ? Enumerable.Empty<string>() : humans.Properties()
                .Where(p => p.Value.Type == JTokenType.Boolean && p.Value.Value<bool>()).Select(p => p.Name);
        }

        // A new server process starts clocks at its own UTC; shutdown time never grants extra production.
        private bool Resume(WorldState state, long utc)
        {
            if (state.Data.Value<string>("生产服务实例") == instanceId) return false;
            foreach (string id in Humans(state))
            {
                InitializePlayer(state, id, utc);
                state.EntityMappings["production"][id] = NewClock(utc);
            }
            state.Data["生产服务实例"] = instanceId;
            state.Data["生产下次结算UTC"] = checked(utc + 1000);
            return true;
        }

        public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs)
        {
            if (!Humans(state).Any()) yield break;
            if (state.Data.Value<string>("生产服务实例") != instanceId)
                yield return new GameCommand { WorldId = state.WorldId, Type = "world.production.resume", RequestId = "production-resume:" + instanceId };
            else
            {
                long due = state.Data.Value<long>("生产下次结算UTC");
                if (serverUtcMs >= due)
                    yield return new GameCommand { WorldId = state.WorldId, Type = "world.production.tick", RequestId = "production-tick:" + instanceId + ":" + due };
            }
        }

        private static GameResult Settle(WorldState state, string id, long utc)
        {
            InitializePlayer(state, id, utc);
            var player = state.RequirePlayer(id);
            var clock = (JObject)state.EntityMappings["production"][id];
            long last = clock.Value<long>("lastUtcMs");
            long clamp = clock.Value<long>("nextClampUtcMs");
            var nation = (state.Data["国家列表"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(item => item.Value<string>("国号") == player["基础信息"].Value<string>("国家"));
            double technology;
            if (nation == null || !ShopRules.TryNumber(nation["资源科技"], out technology))
                return GameResult.Reject(GameCodes.Unavailable, "角色所属国家的资源科技缺失");
            while (utc - last >= 1000)
            {
                var produced = ProductionRules.ProduceOneSecond(player, technology);
                if (produced.Code != GameCodes.Ok) return produced;
                last += 1000;
                if (last >= clamp)
                {
                    var capped = ProductionRules.ClampWallet((JObject)player["财产信息"]);
                    if (capped.Code != GameCodes.Ok) return capped;
                    clamp = checked(clamp + 4000);
                }
            }
            clock["lastUtcMs"] = last;
            clock["nextClampUtcMs"] = clamp;
            return GameResult.Success();
        }

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "无权操作此世界");
            if (command.Type == "world.production.resume" || command.Type == "world.production.tick")
            {
                if (!context.Actor.IsSystem) return GameResult.Reject(GameCodes.Forbidden, "仅服务器可以结算资源");
                if (command.Payload == null || command.Payload.HasValues)
                    return GameResult.Reject(GameCodes.InvalidArgument, "生产结算参数无效");
                bool resumed = Resume(candidate, context.ServerUtcMs);
                if (command.Type == "world.production.resume" || resumed) return GameResult.Success();
                if (context.ServerUtcMs < candidate.Data.Value<long>("生产下次结算UTC"))
                    return GameResult.Reject(GameCodes.Conflict, "尚未到资源结算时间");
                var result = GameResult.Success();
                foreach (string id in Humans(candidate))
                {
                    var settled = Settle(candidate, id, context.ServerUtcMs);
                    if (settled.Code != GameCodes.Ok) return settled;
                    result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "production.updated",
                        ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { id } });
                }
                candidate.Data["生产下次结算UTC"] = checked(context.ServerUtcMs + 1000);
                return result;
            }
            if (command.Type != "fief.construct" && command.Type != "fief.upgrade" && command.Type != "fief.demolish")
                return GameResult.Reject(GameCodes.NotFound, "命令不存在");
            string playerId = context.Actor.PlayerId;
            if (context.Actor.IsSystem || candidate.EntityMappings["humanPlayers"]?[playerId]?.Type != JTokenType.Boolean ||
                candidate.EntityMappings["humanPlayers"].Value<bool>(playerId) != true)
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可经营的角色");
            var payload = command.Payload;
            bool construct = command.Type == "fief.construct";
            if (payload == null || payload.Properties().Any(p => p.Name != "fiefId" && p.Name != "plot" && (!construct || p.Name != "buildingType")) ||
                payload["fiefId"]?.Type != JTokenType.String || payload["plot"]?.Type != JTokenType.Integer ||
                (construct && payload["buildingType"]?.Type != JTokenType.Integer))
                return GameResult.Reject(GameCodes.InvalidArgument, "建筑参数无效");
            double rawPlot, rawType = 0;
            if (!ShopRules.TryNumber(payload["plot"], out rawPlot) ||
                (construct && !ShopRules.TryNumber(payload["buildingType"], out rawType)) ||
                rawPlot < (command.Type == "fief.upgrade" ? 0 : 1) || rawPlot > 12 || (construct && (rawType < 1 || rawType > 7)))
                return GameResult.Reject(GameCodes.InvalidArgument, "建筑位置或类型无效");
            string fiefId = payload.Value<string>("fiefId");
            var mapping = candidate.EntityMappings["fiefs"]?[fiefId] as JObject;
            if (mapping == null || mapping.Value<string>("playerId") != playerId)
                return GameResult.Reject(GameCodes.Forbidden, "无权经营此封地");
            var player = candidate.RequirePlayer(playerId);
            var fief = (player["封地信息表"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(item => item.Value<int>("ID") == mapping.Value<int>("legacyId"));
            if (fief == null) return GameResult.Reject(GameCodes.NotFound, "封地不存在");
            Resume(candidate, context.ServerUtcMs);
            var accrued = Settle(candidate, playerId, context.ServerUtcMs);
            if (accrued.Code != GameCodes.Ok) return accrued;
            var changed = construct ? BuildingRules.Construct(player, fief, (int)rawPlot, (int)rawType)
                : command.Type == "fief.upgrade" ? BuildingRules.Upgrade(player, fief, (int)rawPlot) : BuildingRules.Demolish(player, fief, (int)rawPlot);
            if (changed.Code == GameCodes.Ok)
            {
                changed.Data = new JObject { ["fiefId"] = fiefId, ["plot"] = (int)rawPlot };
                changed.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "fief.buildingChanged",
                    ServerUtcMs = context.ServerUtcMs, Data = changed.Data, AudiencePlayerIds = new[] { playerId } });
            }
            return changed;
        }
    }
}
