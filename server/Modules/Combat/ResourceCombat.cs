using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Server.Modules.Generals;
using Dwsg.Window3;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        private const string ResourceTick = "resource.tick";
        private static 资源点存档 ResourceState(WorldState world) => world.Data["资源点"]?.ToObject<资源点存档>();
        private static void SaveResources(WorldState world, 资源点存档 state) { world.Data["资源点"] = JObject.FromObject(state); }

        private static IEnumerable<GameCommand> CollectResourceCommands(WorldState world, long now)
        {
            if (!(world.EntityMappings["humanPlayers"] is JObject humans) || !humans.Properties().Any(p => p.Value.Value<bool>())) yield break;
            long next = world.EntityMappings.Value<long?>("resourceNextUtcMs") ?? 0L;
            if (next <= now) yield return new GameCommand { WorldId = world.WorldId, Type = ResourceTick,
                RequestId = "resources:" + next, Payload = new JObject { ["dueUtcMs"] = next } };
        }

        private static 资源点存档 EnsureResources(WorldState world)
        {
            var state = ResourceState(world);
            if (state != null) return state;
            var humans = world.EntityMappings["humanPlayers"] as JObject;
            var owner = humans?.Properties().Where(p => p.Value.Value<bool>())
                .OrderBy(p => world.EntityMappings["players"].Value<int>(p.Name)).FirstOrDefault();
            if (owner == null) throw new InvalidOperationException("资源点需要已建立的角色");
            JObject fief = ((JArray)world.RequirePlayer(owner.Name)["封地信息表"]).OfType<JObject>().OrderBy(f => f.Value<int>("ID")).FirstOrDefault();
            if (fief == null) throw new InvalidOperationException("资源点需要出发封地");
            var camps = (JArray)world.Data["山贼列表"];
            int maxX = camps.Max(c => c.Value<int>("坐标x")), maxY = camps.Max(c => c.Value<int>("坐标y"));
            int[,] map = 全局大地图库.大地图表;
            var config = ResourcePointRules.Generate(map, fief["所在城池"].Value<int>("x"), fief["所在城池"].Value<int>("y"), (x, y) =>
                map[y - 1, x - 1] == 1 && !(x <= maxX && y <= maxY) &&
                !((JArray)world.Data["城池列表"]).Any(c => c.Value<int>("坐标x") == x && c.Value<int>("坐标y") == y) &&
                !camps.Any(c => c.Value<int>("坐标x") == x && c.Value<int>("坐标y") == y));
            if (config.Count != 2) throw new InvalidOperationException("资源点目录无法建立");
            state = new 资源点存档 { 世界标识 = world.WorldId };
            foreach (var point in config)
            {
                // 原一级大厅每小时15铜钱，一级农场每小时25粮食；资源点无科技加成。
                int rate = point.类型 == 资源点类型.铜矿 ? 15 : 25;
                state.点位.Add(new 资源点状态 { 标识 = point.标识, 类型 = point.类型, 坐标x = point.坐标x, 坐标y = point.坐标y, 时产 = rate, 剩余库存 = rate });
            }
            SaveResources(world, state);
            return state;
        }

        private static GameResult TickResources(WorldState world, CommandContext context, JObject payload)
        {
            long expected = world.EntityMappings.Value<long?>("resourceNextUtcMs") ?? 0L;
            if (!Keys(payload, "dueUtcMs") || payload["dueUtcMs"].Type != JTokenType.Integer || payload.Value<long>("dueUtcMs") != expected || context.ServerUtcMs < expected)
                return GameResult.Reject(GameCodes.Conflict, "资源结算尚未到期或已经处理");
            var result = AdvanceResources(world, EnsureResources(world), context.ServerUtcMs / 1000, null);
            if (result.Code == GameCodes.Ok) world.EntityMappings["resourceNextUtcMs"] = checked(context.ServerUtcMs + ResourcePointRules.Interval * 1000);
            return result;
        }

        private static GameResult AdvanceResources(WorldState world, 资源点存档 state, long now, string abandon)
        {
            var credits = new Dictionary<int, long[]>();
            foreach (var point in state.点位)
            {
                int owner = point.占领玩家ID;
                long amount = ResourcePointRules.Advance(point, now, point.标识 == abandon);
                if (owner < 0) continue;
                if (!credits.TryGetValue(owner, out long[] sums)) credits[owner] = sums = new long[2];
                sums[(int)point.类型] = checked(sums[(int)point.类型] + amount);
            }
            var players = (JArray)world.Data["玩家列表"];
            foreach (var credit in credits)
            {
                if (credit.Key < 0 || credit.Key >= players.Count) return GameResult.Reject(GameCodes.Unavailable, "资源点占领者不存在");
                for (int i = 0; i < 2; i++)
                {
                    double balance = players[credit.Key]["财产信息"].Value<double>(i == 0 ? "铜钱" : "粮食");
                    if (double.IsNaN(balance) || double.IsInfinity(balance) || balance < 0 || balance > 9007199254740991d - credit.Value[i])
                        return GameResult.Reject(GameCodes.Conflict, "资源收益无法入账，财产已达上限");
                }
            }
            foreach (var credit in credits)
                for (int i = 0; i < 2; i++)
                {
                    string key = i == 0 ? "铜钱" : "粮食";
                    players[credit.Key]["财产信息"][key] = players[credit.Key]["财产信息"].Value<double>(key) + credit.Value[i];
                }
            SaveResources(world, state);
            return GameResult.Success();
        }

        private static GameResult AbandonResource(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "resourceId") || !Id(payload["resourceId"], out string id)) return GameResult.Reject(GameCodes.InvalidArgument, "资源点参数无效");
            var state = EnsureResources(world);
            var point = state.点位.SingleOrDefault(p => p.标识 == id);
            if (point == null) return GameResult.Reject(GameCodes.NotFound, "资源点不存在");
            if (point.占领玩家ID != world.EntityMappings["players"].Value<int>(context.Actor.PlayerId))
                return GameResult.Reject(GameCodes.Forbidden, "只能放弃自己占领的资源点");
            if (context.ServerUtcMs / 1000 < point.结算时间)
                return GameResult.Reject(GameCodes.Conflict, "服务器时间尚未到上次结算时间，请稍后重试");
            return AdvanceResources(world, state, context.ServerUtcMs / 1000, id);
        }

        private static GameResult DispatchResource(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "resourceId", "generalIds") || !Id(payload["resourceId"], out string id) ||
                !(payload["generalIds"] is JArray ids) || ids.Count < 1 || ids.Count > 5 || ids.Any(v => !Id(v, out _)) ||
                ids.Values<string>().Distinct(StringComparer.Ordinal).Count() != ids.Count)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择1至5名不同将领和有效资源点");
            var state = EnsureResources(world);
            GameResult production = AdvanceResources(world, state, context.ServerUtcMs / 1000, null);
            if (production.Code != GameCodes.Ok) return production;
            var point = state.点位.SingleOrDefault(p => p.标识 == id);
            if (point == null) return GameResult.Reject(GameCodes.NotFound, "资源点不存在");
            if (point.占领玩家ID >= 0 || point.剩余库存 <= 0 || point.恢复时间 != 0 || ActiveAt(world, point.坐标x, point.坐标y, kind: "resource") != null)
                return GameResult.Reject(GameCodes.Conflict, "资源点已被占领、正在恢复或已有军队前往");
            int owner = world.EntityMappings["players"].Value<int>(context.Actor.PlayerId);
            int pending = (world.Data["战斗运行"] as JObject)?.Properties().Count(p => p.Value.Value<string>("Kind") == "resource" &&
                !p.Value.Value<bool>("SettlementApplied") && p.Value.Value<string>("PlayerId") == context.Actor.PlayerId) ?? 0;
            if (state.点位.Count(p => p.占领玩家ID == owner) + pending >= ResourcePointRules.Limit)
                return GameResult.Reject(GameCodes.Conflict, "资源点占领数量已达上限");
            JObject source = null;
            foreach (string generalId in ids.Values<string>())
            {
                GeneralsModule.ResolveGeneral(world, context.Actor.PlayerId, generalId, out JObject fief);
                if (source != null && source.Value<int>("ID") != fief.Value<int>("ID")) return GameResult.Reject(GameCodes.InvalidArgument, "资源部队必须来自同一封地");
                source = fief;
            }
            int sourceId = source.Value<int>("ID"), sx = source["所在城池"].Value<int>("x"), sy = source["所在城池"].Value<int>("y");
            JObject guard = ResourceGuard(world);
            if (guard == null) return GameResult.Reject(GameCodes.Unavailable, "现有一级守军尚未生成");
            string armyId = "army_" + Guid.NewGuid().ToString("N"), battleId = "battle_" + Guid.NewGuid().ToString("N");
            GameResult occupied = GeneralsModule.TryOccupy(world, context.Actor.PlayerId, armyId, ids.Values<string>().ToArray(), out List<JObject> generals);
            if (occupied.Code != GameCodes.Ok) return occupied;
            long arrival = checked(context.ServerUtcMs + Math.Max(10L, (Math.Abs((long)sx - point.坐标x) + Math.Abs((long)sy - point.坐标y)) * 10L) * 1000L);
            var battle = new BanditBattle { Kind = "resource", BattleId = battleId, ArmyId = armyId, PlayerId = context.Actor.PlayerId,
                NpcPlayerId = NpcPlayerId(world), ResourceId = id, ResourceBatch = point.批次, SourceFiefId = sourceId, SourceX = sx, SourceY = sy,
                X = point.坐标x, Y = point.坐标y, ArrivalUtcMs = arrival, NextTickUtcMs = arrival, RandomState = Seed() };
            AddArmy(world, battle, armyId, ids.Values<string>().ToArray(), generals, arrival);
            var defender = BanditBattleRules.CreateUnit(battleId + ":resource", guard, Troop(world, guard["将领配兵"].Value<int>("ID")), 1);
            defender.Ephemeral = true;
            battle.Defenders.Add(defender);
            Save(world, battle);
            return Updated(world, context, battle, "combat.resource.dispatched");
        }

        private static JObject ResourceGuard(WorldState world)
        {
            var camp = ((JArray)world.Data["山贼列表"]).OfType<JObject>().Where(c => c.Value<int>("等级") == 1 &&
                c["将领数据列表"] is JArray list && list.Count == 1).OrderBy(c => c.Value<int>("坐标y")).ThenBy(c => c.Value<int>("坐标x"))
                .Select(c => (JObject)c["将领数据列表"][0]).FirstOrDefault(g => g["详细信息"].Value<int>("状态") == 0 &&
                g["将领属性"]["成长点数"].Value<int>("等级") >= 1 && g["将领属性"]["成长点数"].Value<int>("等级") <= 3 &&
                g["将领属性"]["初始属性"].Value<int>("ID") >= 1 && g["将领属性"]["初始属性"].Value<int>("ID") <= 4 &&
                g["将领配兵"].Value<int>("ID") == 201 && g["将领配兵"].Value<double>("数量") >= 140 && g["将领配兵"].Value<double>("数量") < 400);
            if (camp == null) return null;
            var guard = (JObject)camp.DeepClone();
            guard["ID"] = 0; guard["详细信息"]["身份"] = 1; guard["详细信息"]["状态"] = 1;
            guard["详细信息"]["坑位颜色"] = 1; guard["详细信息"]["剩余兵力"] = guard["将领配兵"]["数量"].DeepClone();
            return guard;
        }

        private static GameResult SettleResource(WorldState world, BanditBattle battle, long utc)
        {
            if (battle.Phase == "won")
            {
                var state = ResourceState(world);
                var point = state?.点位.SingleOrDefault(p => p.标识 == battle.ResourceId && p.批次 == battle.ResourceBatch);
                if (point == null || point.占领玩家ID >= 0 || point.剩余库存 <= 0 || point.恢复时间 != 0)
                    return GameResult.Reject(GameCodes.Conflict, "资源目标已变化，战果未提交");
                point.占领玩家ID = world.EntityMappings["players"].Value<int>(battle.PlayerId);
                point.结算时间 = utc / 1000; point.节奏余秒 = 0;
                SaveResources(world, state);
            }
            foreach (var army in battle.Attackers.Where(u => !u.Retired).GroupBy(u => u.ArmyId))
            {
                var result = GeneralsModule.ApplyOutcome(world, battle.PlayerId, army.Key, army.Select(u => new GeneralOutcome {
                    GeneralId = u.GeneralId, General = u.General, Remaining = checked((int)u.Remaining), Wounded = checked((int)u.Wounded) }));
                if (result.Code != GameCodes.Ok) return result;
            }
            battle.Reward = new 战斗奖励(); battle.SettlementApplied = true; battle.SettledUtcMs = utc;
            return GameResult.Success();
        }

        public static JObject ProjectResources(WorldState world)
        {
            var state = ResourceState(world);
            var result = new JObject { ["worldId"] = world.WorldId, ["points"] = new JArray() };
            if (state == null) return result;
            var players = (JObject)world.EntityMappings["players"];
            foreach (var point in state.点位)
            {
                var view = JObject.FromObject(point); view.Remove("占领玩家ID");
                view["ownerId"] = point.占领玩家ID < 0 ? JValue.CreateNull() : new JValue(players.Properties().Single(p => p.Value.Value<int>() == point.占领玩家ID).Name);
                view["pending"] = ActiveAt(world, point.坐标x, point.坐标y, kind: "resource") != null;
                ((JArray)result["points"]).Add(view);
            }
            return result;
        }
    }
}
