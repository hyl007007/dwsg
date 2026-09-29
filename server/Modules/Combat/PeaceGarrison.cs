using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Server.Modules.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        private const string PeaceGarrisonTick = "combat.city.peace.tick";
        private static JObject PeaceTasks(WorldState world)
        {
            var tasks = world.Data["和平驻防运行"] as JObject;
            if (tasks == null) world.Data["和平驻防运行"] = tasks = new JObject();
            return tasks;
        }
        private static bool PeaceFriendly(WorldState world, JObject task)
        {
            var city = City(world, task.Value<int>("X"), task.Value<int>("Y"));
            var player = world.RequirePlayer(task.Value<string>("PlayerId"));
            return city != null && city.Value<int>("城主") == task.Value<int>("Owner") && city.Value<string>("国家") == task.Value<string>("Nation") &&
                (city.Value<int>("城主") == world.ResolvePlayerIndex(task.Value<string>("PlayerId")) || !string.IsNullOrEmpty(city.Value<string>("国家")) && player["基础信息"].Value<string>("国家") == city.Value<string>("国家"));
        }
        private static GameResult DispatchPeaceGarrison(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "x", "y", "generalIds") || !Coordinate(payload["x"], out int x) || !Coordinate(payload["y"], out int y) ||
                !(payload["generalIds"] is JArray ids) || ids.Count < 1 || ids.Count > 5 || ids.Any(v => !Id(v, out _)) || ids.Values<string>().Distinct().Count() != ids.Count)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择同一封地的1至5名驻防将领。");
            string playerId = context.Actor.PlayerId;
            if (!IsHumanCityPlayer(world, playerId)) return GameResult.Reject(GameCodes.Forbidden, "此连接没有可派遣驻防的真人角色。");
            var city = City(world, x, y); var player = world.RequirePlayer(playerId); string nation = player["基础信息"].Value<string>("国家");
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "城池不存在。");
            if (city.Value<int>("城主") != world.ResolvePlayerIndex(playerId) && (string.IsNullOrEmpty(nation) || city.Value<string>("国家") != nation))
                return GameResult.Reject(GameCodes.Forbidden, "只能驻防本人或本国城池。");
            var active = ActiveAt(world, x, y, kind: "city");
            if (active != null && active.Phase == "fighting") return GameResult.Reject(GameCodes.Conflict, "城池正在交战，请使用守方援军。");
            string[] generalIds = ids.Values<string>().ToArray(); JObject source = null; double troops = 0;
            foreach (string id in generalIds)
            {
                var general = GeneralsModule.ResolveGeneral(world, playerId, id, out JObject fief);
                if (source != null && source.Value<int>("ID") != fief.Value<int>("ID")) return GameResult.Reject(GameCodes.InvalidArgument, "请从同一有效封地选择驻防将领。");
                source = fief; troops += general["将领配兵"].Value<double>("数量");
            }
            double occupied = PeaceOccupied(world, x, y);
            if (troops <= 0 || double.IsNaN(troops) || double.IsInfinity(troops) || occupied + troops > PeaceGarrisonRules.Capacity(city.Value<int>("规模")))
                return GameResult.Reject(GameCodes.Conflict, "驻防兵力超过本城容量（含在途部队）。");
            int sourceX = source["所在城池"].Value<int>("x"), sourceY = source["所在城池"].Value<int>("y");
            long seconds = PeaceGarrisonRules.TravelSeconds(sourceX, sourceY, x, y);
            if (seconds < 0) return GameResult.Reject(GameCodes.InvalidArgument, "行军坐标无效。");
            string armyId = "peace_" + Guid.NewGuid().ToString("N"); int fiefId = source.Value<int>("ID");
            var reserved = GeneralsModule.TryOccupy(world, playerId, armyId, generalIds, out List<JObject> generals);
            if (reserved.Code != GameCodes.Ok) return reserved;
            foreach (var general in generals) general["详细信息"]["状态"] = 2.0;
            var task = new JObject { ["ArmyId"] = armyId, ["PlayerId"] = playerId, ["GeneralIds"] = new JArray(generalIds), ["X"] = x, ["Y"] = y,
                ["Nation"] = city.Value<string>("国家"), ["Owner"] = city.Value<int>("城主"), ["SourceFiefId"] = fiefId, ["FromX"] = sourceX, ["FromY"] = sourceY,
                ["ToX"] = x, ["ToY"] = y, ["StartedUtcMs"] = context.ServerUtcMs, ["ArrivalUtcMs"] = checked(context.ServerUtcMs + seconds * 1000), ["Phase"] = "marching" };
            PeaceTasks(world)[armyId] = task;
            var result = GameResult.Success(new JObject { ["armyId"] = armyId, ["arrivalUtcMs"] = task["ArrivalUtcMs"].DeepClone() });
            result.Message = "驻防部队已出发，预计" + seconds + "秒后到达。";
            return result;
        }
        private static double PeaceOccupied(WorldState world, int x, int y)
        {
            double total = 0;
            foreach (var task in (world.Data["和平驻防运行"] as JObject ?? new JObject()).Properties().Select(p => p.Value).OfType<JObject>())
                if (task.Value<int>("X") == x && task.Value<int>("Y") == y && task.Value<string>("Phase") != "returning")
                    foreach (string id in ((JArray)task["GeneralIds"]).Values<string>())
                        total += GeneralsModule.ResolveGeneral(world, task.Value<string>("PlayerId"), id, out _)["将领配兵"].Value<double>("数量");
            return total;
        }
        private static IEnumerable<GameCommand> CollectPeaceGarrisonCommands(WorldState world, long now)
        {
            var tasks = world.Data["和平驻防运行"] as JObject;
            if (tasks == null) yield break;
            foreach (var task in tasks.Properties().Select(p => p.Value).OfType<JObject>())
            {
                string phase = task.Value<string>("Phase");
                bool due = phase == "returning" ? task.Value<long>("ArrivalUtcMs") <= now || ReturnFiefMoved(world, task)
                    : phase != "fighting" && (!PeaceFriendly(world, task) || phase == "marching" && task.Value<long>("ArrivalUtcMs") <= now);
                if (due) yield return new GameCommand { WorldId = world.WorldId, Type = PeaceGarrisonTick, RequestId = "peace:" + task.Value<string>("ArmyId") + ":" + world.Revision + ":" + now / 1000,
                    Payload = new JObject { ["armyId"] = task.Value<string>("ArmyId") } };
            }
        }
        private static JObject ReturnFief(WorldState world, JObject task)
        {
            return (world.RequirePlayer(task.Value<string>("PlayerId"))["封地信息表"] as JArray)?.OfType<JObject>().SingleOrDefault(f => f.Value<int>("ID") == task.Value<int>("SourceFiefId"));
        }
        private static bool ReturnFiefMoved(WorldState world, JObject task)
        {
            var fief = ReturnFief(world, task);
            return fief != null && (fief["所在城池"].Value<int>("x") != task.Value<int>("ToX") || fief["所在城池"].Value<int>("y") != task.Value<int>("ToY"));
        }
        private static GameResult BeginPeaceReturn(WorldState world, JObject task, long now)
        {
            var fief = ReturnFief(world, task);
            if (fief == null) return GameResult.Reject(GameCodes.Conflict, "没有有效返回封地，未释放将领。");
            long start = task.Value<long>("StartedUtcMs"), arrival = task.Value<long>("ArrivalUtcMs");
            double progress = arrival <= start ? 1 : Math.Min(1, Math.Max(0, (double)(now - start) / (arrival - start)));
            double x = task.Value<double>("FromX") + (task.Value<int>("ToX") - task.Value<double>("FromX")) * progress;
            double y = task.Value<double>("FromY") + (task.Value<int>("ToY") - task.Value<double>("FromY")) * progress;
            int targetX = fief["所在城池"].Value<int>("x"), targetY = fief["所在城池"].Value<int>("y");
            long seconds = PeaceGarrisonRules.TravelSeconds(x, y, targetX, targetY);
            if (seconds < 0) return GameResult.Reject(GameCodes.InvalidArgument, "返回坐标无效。");
            task["FromX"] = x; task["FromY"] = y; task["ToX"] = targetX; task["ToY"] = targetY;
            task["StartedUtcMs"] = now; task["ArrivalUtcMs"] = checked(now + seconds * 1000); task["Phase"] = "returning";
            var result = GameResult.Success(); result.Message = "驻防部队已撤回，到达封地后恢复空闲；保留现有配兵。"; return result;
        }
        private static GameResult WithdrawPeaceGarrison(WorldState world, CommandContext context, JObject task)
        {
            if (task.Value<string>("PlayerId") != context.Actor.PlayerId) return GameResult.Reject(GameCodes.Forbidden, "只能撤回本人的驻防军队。");
            if (task.Value<string>("Phase") == "fighting") return GameResult.Reject(GameCodes.Conflict, "驻防部队正在参战，请在战场撤退。");
            if (task.Value<string>("Phase") == "returning") return GameResult.Reject(GameCodes.Conflict, "部队已在返回途中。");
            return BeginPeaceReturn(world, task, context.ServerUtcMs);
        }
        private static GameResult TickPeaceGarrison(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "armyId") || !Id(payload["armyId"], out string id)) return GameResult.Reject(GameCodes.InvalidArgument, "驻防推进参数无效。");
            var task = PeaceTasks(world)[id] as JObject;
            if (task == null) return GameResult.Success();
            string phase = task.Value<string>("Phase");
            if (phase == "fighting") return GameResult.Success();
            if (phase != "returning" && !PeaceFriendly(world, task)) return BeginPeaceReturn(world, task, context.ServerUtcMs);
            if (phase == "returning" && ReturnFiefMoved(world, task)) return BeginPeaceReturn(world, task, context.ServerUtcMs);
            if (context.ServerUtcMs < task.Value<long>("ArrivalUtcMs")) return GameResult.Reject(GameCodes.Conflict, "行军尚未到达。");
            if (phase == "returning")
            {
                string playerId = task.Value<string>("PlayerId");
                var outcomes = ((JArray)task["GeneralIds"]).Values<string>().Select(g => { var original = GeneralsModule.ResolveGeneral(world, playerId, g, out _); return new GeneralOutcome { GeneralId = g, General = (JObject)original.DeepClone(), Remaining = original["将领配兵"].Value<int>("数量"), Wounded = 0 }; }).ToArray();
                var released = GeneralsModule.ApplyCityDefenderOutcome(world, playerId, id, outcomes);
                if (released.Code != GameCodes.Ok) return released;
                PeaceTasks(world).Remove(id); return GameResult.Success();
            }
            var active = ActiveAt(world, task.Value<int>("X"), task.Value<int>("Y"), kind: "city");
            if (active != null && active.Phase == "fighting") return BeginPeaceReturn(world, task, context.ServerUtcMs);
            task["Phase"] = "stationed";
            return GameResult.Success();
        }
        private static void AttachPeaceGarrisons(WorldState world, BanditBattle battle)
        {
            if (battle.Phase != "fighting") return;
            var tasks = world.Data["和平驻防运行"] as JObject;
            if (tasks == null) return;
            foreach (var task in tasks.Properties().Select(p => p.Value).OfType<JObject>().Where(t => t.Value<string>("Phase") == "stationed" && t.Value<int>("X") == battle.X && t.Value<int>("Y") == battle.Y))
            {
                if (!PeaceFriendly(world, task)) continue;
                string armyId = task.Value<string>("ArmyId"), playerId = task.Value<string>("PlayerId");
                if (battle.GarrisonArmies.Any(a => a.ArmyId == armyId)) continue;
                var ids = ((JArray)task["GeneralIds"]).Values<string>().ToList();
                foreach (string id in ids)
                {
                    var general = GeneralsModule.ResolveGeneral(world, playerId, id, out _);
                    if (world.EntityMappings["generalOccupancy"]?[id]?.Value<string>("armyId") != armyId) throw new InvalidOperationException("和平驻防将领占用不一致。");
                    general["详细信息"]["状态"] = 1.0; general["详细信息"]["坑位颜色"] = 1.0;
                    var unit = BanditBattleRules.CreateUnit(id, general, Troop(world, general["将领配兵"].Value<int>("ID")), 1);
                    unit.GeneralOwnerId = playerId; unit.PlayerGarrison = true; unit.HumanOwner = true; unit.ArmyId = armyId; unit.UnitId = armyId + ":" + id;
                    battle.Defenders.Add(unit);
                }
                battle.GarrisonArmies.Add(new CombatGarrisonArmy { ArmyId = armyId, PlayerId = playerId, Nation = task.Value<string>("Nation"), GeneralIds = ids, ArrivalUtcMs = battle.StartedUtcMs, JoinedUtcMs = battle.StartedUtcMs, Phase = "fighting" });
                battle.DefenseFormations.Add(new CombatFormation { ArmyId = armyId, AvailableUtcMs = battle.StartedUtcMs, PositionX = -24f });
                task["Phase"] = "fighting"; task["BattleId"] = battle.BattleId;
            }
        }
        private static GameResult RestorePeaceAfterBattle(WorldState world, CombatGarrisonArmy army, CombatUnit[] units, bool withdrawn, long now)
        {
            var task = PeaceTasks(world)[army.ArmyId] as JObject;
            if (task == null) return GameResult.Success();
            var survivors = new List<string>(); var roster = (JArray)task["GeneralIds"];
            foreach (var unit in units)
            {
                var general = GeneralsModule.ResolveGeneral(world, army.PlayerId, unit.GeneralId, out _);
                if (general["详细信息"].Value<double>("状态") != 3 && general["将领配兵"].Value<double>("数量") > 0) survivors.Add(unit.GeneralId);
                if (withdrawn || !survivors.Contains(unit.GeneralId)) foreach (var id in roster.Where(v => v.Value<string>() == unit.GeneralId).ToArray()) id.Remove();
            }
            var target = task;
            if (withdrawn && survivors.Count > 0)
            {
                target = (JObject)task.DeepClone(); string id = "peace_" + Guid.NewGuid().ToString("N");
                target["ArmyId"] = id; target["GeneralIds"] = new JArray(survivors); target["FromX"] = target["ToX"] = task["X"].DeepClone(); target["FromY"] = target["ToY"] = task["Y"].DeepClone();
                target["StartedUtcMs"] = target["ArrivalUtcMs"] = now;
                var returned = BeginPeaceReturn(world, target, now); if (returned.Code != GameCodes.Ok) return returned;
                PeaceTasks(world)[id] = target;
            }
            else if (!withdrawn) { task["Phase"] = "stationed"; task.Remove("BattleId"); }
            foreach (string id in survivors)
            {
                var general = GeneralsModule.ResolveGeneral(world, army.PlayerId, id, out _); general["详细信息"]["状态"] = 2.0;
                world.EntityMappings["generalOccupancy"][id] = new JObject { ["playerId"] = army.PlayerId, ["armyId"] = target.Value<string>("ArmyId") };
            }
            if (roster.Count == 0) PeaceTasks(world).Remove(army.ArmyId);
            return GameResult.Success();
        }
        public static void EnrichPeaceGarrisonProjection(WorldState world, AuthenticatedActor actor, JObject publicWorld, JObject privatePlayer)
        {
            var own = new JArray(); var tasks = (world.Data["和平驻防运行"] as JObject ?? new JObject()).Properties().Select(p => p.Value).OfType<JObject>().ToArray();
            if (actor != null)
                foreach (var task in tasks.Where(t => t.Value<string>("PlayerId") == actor.PlayerId))
                {
                    var projected = (JObject)task.DeepClone();
                    projected["Owner"] = ((JObject)world.EntityMappings["players"]).Properties().FirstOrDefault(p => p.Value.Value<int>() == task.Value<int>("Owner"))?.Name;
                    own.Add(projected);
                }
            privatePlayer["和平驻防运行"] = own;
            string nation = actor == null ? null : world.RequirePlayer(actor.PlayerId)["基础信息"].Value<string>("国家");
            foreach (JObject city in (JArray)publicWorld["城池列表"])
            {
                if (actor == null || city.Value<string>("国家") != nation) continue;
                int x = city.Value<int>("坐标x"), y = city.Value<int>("坐标y");
                city["和平驻防兵力"] = PeaceOccupied(world, x, y);
                var summaries = city["驻防摘要"] as JArray; if (summaries == null) city["驻防摘要"] = summaries = new JArray();
                foreach (var task in tasks.Where(t => t.Value<int>("X") == x && t.Value<int>("Y") == y && t.Value<string>("Phase") == "stationed"))
                    foreach (string id in ((JArray)task["GeneralIds"]).Values<string>())
                    {
                        var general = GeneralsModule.ResolveGeneral(world, task.Value<string>("PlayerId"), id, out _);
                        summaries.Add(new JObject { ["name"] = general["将领属性"]["初始属性"].Value<string>("名字"), ["troops"] = general["将领配兵"].Value<double>("数量"), ["owner"] = world.RequirePlayer(task.Value<string>("PlayerId"))["基础信息"].Value<string>("名字") });
                    }
            }
        }
    }
}
