using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Dwsg.Shared.Notifications;
using Dwsg.Server.Modules.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule : IGameModule, IGameTickModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "combat.bandit.dispatch", "combat.bandit.reinforce", "combat.bandit.advance", "combat.bandit.withdraw",
            "combat.city.dispatch", "combat.city.reinforce", "combat.city.advance", "combat.city.withdraw", "combat.city.garrison.dispatch", "combat.city.garrison.withdraw", OriginalAiTick };

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "战斗角色或世界无效");
            if (context.Actor.IsSystem != (command.Type == "combat.bandit.advance" || command.Type == "combat.city.advance" || command.Type == OriginalAiTick))
                return GameResult.Reject(GameCodes.Forbidden, "战斗推进只能由服务器执行");
            try
            {
                WorldState working = candidate.Clone();
                if (command.Type != "combat.bandit.dispatch" && command.Type != "combat.city.dispatch")
                {
                    BanditBattle requested = Id(command.Payload?["battleId"], out string requestedId) ? Load(working, requestedId) : null;
                    if (requested != null && command.Type.StartsWith("combat.city.", StringComparison.Ordinal) != (requested.Kind == "city"))
                        return GameResult.Reject(GameCodes.InvalidArgument, "战场类型不匹配");
                }
                GameResult result = command.Type == OriginalAiTick ? ExecuteOriginalAi(working, context, command)
                    : command.Type == "combat.bandit.dispatch" ? Dispatch(working, context, command.Payload)
                    : command.Type == "combat.city.dispatch" ? DispatchCity(working, context, command.Payload)
                    : command.Type == "combat.city.garrison.dispatch" ? DispatchGarrison(working, context, command.Payload)
                    : command.Type == "combat.city.garrison.withdraw" ? WithdrawGarrison(working, context, command.Payload)
                    : command.Type == "combat.bandit.reinforce" || command.Type == "combat.city.reinforce" ? Reinforce(working, context, command.Payload)
                    : command.Type == "combat.bandit.advance" || command.Type == "combat.city.advance" ? Advance(working, context, command.Payload)
                    : command.Type == "combat.bandit.withdraw" || command.Type == "combat.city.withdraw" ? Withdraw(working, context, command.Payload)
                    : GameResult.Reject(GameCodes.InvalidArgument, "战斗命令无效");
                if (result.Code == GameCodes.Ok) { candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings; }
                return result;
            }
            catch (GeneralRuleException error)
            {
                return GameResult.Reject(error.Failure == GeneralFailure.Conflict ? GameCodes.Conflict : GameCodes.InvalidArgument, error.Message);
            }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "原战斗数据不完整，操作未提交"); }
        }

        public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs)
        {
            foreach (GameCommand command in CollectOriginalAiCommands(state, serverUtcMs)) yield return command;
            var battles = state.Data["战斗运行"] as JObject;
            if (battles == null) yield break;
            foreach (JProperty entry in battles.Properties().OrderBy(item => item.Value.Value<long>("NextTickUtcMs")).ThenBy(item => item.Name, StringComparer.Ordinal))
            {
                JObject battle = (JObject)entry.Value;
                if (battle.Value<bool>("SettlementApplied") || battle.Value<long>("NextTickUtcMs") > serverUtcMs) continue;
                long due = battle.Value<long>("NextTickUtcMs");
                yield return new GameCommand { WorldId = state.WorldId, Type = battle.Value<string>("Kind") == "city" ? "combat.city.advance" : "combat.bandit.advance",
                    RequestId = "combat:" + entry.Name + ":" + due,
                    Payload = new JObject { ["battleId"] = entry.Name, ["tickUtcMs"] = due } };
            }
        }

        private static GameResult Dispatch(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "x", "y", "generalIds") || !Coordinate(payload["x"], out int x) || !Coordinate(payload["y"], out int y)
                || !(payload["generalIds"] is JArray ids) || ids.Count < 1 || ids.Count > 5
                || ids.Any(id => id.Type != JTokenType.String || string.IsNullOrWhiteSpace(id.Value<string>()) || id.Value<string>().Length > 128)
                || ids.Select(id => id.Value<string>()).Distinct(StringComparer.Ordinal).Count() != ids.Count)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择1至5名不同将领和有效山贼坐标");
            JObject camp = Camp(world, x, y);
            if (camp == null) return GameResult.Reject(GameCodes.NotFound, "山贼不存在");
            BanditBattle existing = ActiveAt(world, x, y);
            if (existing != null && existing.PlayerId != context.Actor.PlayerId)
                return GameResult.Reject(GameCodes.Conflict, "该山贼已有出征军队");
            string battleId = "battle_" + Guid.NewGuid().ToString("N");
            string armyId = "army_" + Guid.NewGuid().ToString("N");
            string[] generalIds = ids.Values<string>().ToArray();
            GameResult occupied = GeneralsModule.TryOccupy(world, context.Actor.PlayerId, armyId, generalIds, out List<JObject> generals);
            if (occupied.Code != GameCodes.Ok) return occupied;
            string npcPlayerId = NpcPlayerId(world);
            var battle = new BanditBattle { BattleId = battleId, ArmyId = armyId, PlayerId = context.Actor.PlayerId, NpcPlayerId = npcPlayerId,
                X = x, Y = y, JoinBattleId = existing?.BattleId, ArrivalUtcMs = checked(context.ServerUtcMs + 10000),
                NextTickUtcMs = checked(context.ServerUtcMs + 10000), RandomState = Seed() };
            AddArmy(world, battle, armyId, generalIds, generals, battle.ArrivalUtcMs);
            var defenders = (JArray)Camp(world, x, y)["将领数据列表"];
            if (defenders.Count < 1 || defenders.Count > 5 || !defenders.Any(item => item["将领配兵"].Value<double>("数量") > 0))
                return GameResult.Reject(GameCodes.Unavailable, "原山贼部队数据无效");
            for (int i = 0; i < defenders.Count; i++)
                battle.Defenders.Add(BanditBattleRules.CreateUnit(battleId + ":npc:" + i, (JObject)defenders[i], Troop(world, defenders[i]["将领配兵"].Value<int>("ID")), 1));
            Save(world, battle);
            return Updated(world, context, battle, "combat.bandit.dispatched");
        }

        private static GameResult Reinforce(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "battleId", "generalIds") || !Id(payload["battleId"], out string id) ||
                !(payload["generalIds"] is JArray ids) || ids.Count < 1 || ids.Count > 5 || ids.Any(item => !Id(item, out _)))
                return GameResult.Reject(GameCodes.InvalidArgument, "增援参数无效");
            BanditBattle battle = Load(world, id);
            if (battle == null) return GameResult.Reject(GameCodes.NotFound, "战场不存在");
            if (battle.PlayerId != context.Actor.PlayerId) return GameResult.Reject(GameCodes.Forbidden, "只能增援自己的战场");
            if (battle.SettlementApplied || battle.Phase != "fighting") return GameResult.Reject(GameCodes.Conflict, "战场尚未到达或已经结束");
            if (battle.NextTickUtcMs <= context.ServerUtcMs)
            {
                GameResult advanced = Advance(world, context, new JObject { ["battleId"] = id, ["tickUtcMs"] = battle.NextTickUtcMs });
                if (advanced.Code != GameCodes.Ok) return advanced;
                battle = Load(world, id);
                if (battle.SettlementApplied) return GameResult.Reject(GameCodes.Conflict, "战场已经结束");
                if (battle.NextTickUtcMs <= context.ServerUtcMs) return GameResult.Reject(GameCodes.Conflict, "战场正在同步，请稍后增援");
            }
            string armyId = "army_" + Guid.NewGuid().ToString("N");
            string[] generalIds = ids.Values<string>().ToArray();
            GameResult occupied = GeneralsModule.TryOccupy(world, battle.PlayerId, armyId, generalIds, out List<JObject> generals);
            if (occupied.Code != GameCodes.Ok) return occupied;
            BanditBattleRules.EnsureFormations(battle);
            AddArmy(world, battle, armyId, generalIds, generals, context.ServerUtcMs);
            Save(world, battle);
            GameResult result = Updated(world, context, battle, "combat.bandit.reinforced");
            result.Data["armyId"] = armyId;
            return result;
        }

        private static void AddArmy(WorldState world, BanditBattle battle, string armyId, string[] ids, List<JObject> generals, long available)
        {
            for (int i = 0; i < generals.Count; i++)
            {
                CombatUnit unit = BanditBattleRules.CreateUnit(ids[i], generals[i], Troop(world, generals[i]["将领配兵"].Value<int>("ID")), 0);
                unit.ArmyId = armyId; unit.UnitId = armyId + ":" + ids[i]; unit.GeneralOwnerId = battle.PlayerId;
                battle.Attackers.Add(unit);
            }
            battle.AttackFormations.Add(new CombatFormation { ArmyId = armyId, AvailableUtcMs = available,
                PositionX = battle.Kind == "city" ? -40.25f : -24.25f });
        }

        private static GameResult Advance(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "battleId", "tickUtcMs") || !Id(payload["battleId"], out string id) || payload["tickUtcMs"]?.Type != JTokenType.Integer)
                return GameResult.Reject(GameCodes.InvalidArgument, "战场推进参数无效");
            BanditBattle battle = Load(world, id);
            if (battle == null) return GameResult.Reject(GameCodes.NotFound, "战场不存在");
            if (battle.SettlementApplied) return GameResult.Success(new JObject { ["battleId"] = id, ["phase"] = battle.Phase });
            if (payload.Value<long>("tickUtcMs") != battle.NextTickUtcMs || context.ServerUtcMs < battle.NextTickUtcMs)
                return GameResult.Reject(GameCodes.Conflict, "行军尚未到达或推进已处理");
            RefreshCurrentGenerals(world, battle);
            if (battle.Phase == "marching")
            {
                BanditBattle target = ActiveAt(world, battle.X, battle.Y, battle.BattleId, battle.Kind);
                if (JoinOriginalAiBattle(world, battle, target, context.ServerUtcMs))
                    return Updated(world, context, target, "combat.city.reinforced");
                if (target != null && target.PlayerId == battle.PlayerId && target.ArrivalUtcMs <= battle.ArrivalUtcMs)
                {
                    if (target.Phase == "marching") StartBattle(world, target);
                    if (target.IsTerminal)
                    {
                        GameResult returned = Settle(world, target, context.ServerUtcMs);
                        if (returned.Code != GameCodes.Ok) return returned;
                        Save(world, target);
                    }
                    else
                    {
                        BanditBattleRules.EnsureFormations(target);
                        target.Attackers.AddRange(battle.Attackers);
                        target.AttackFormations.AddRange(battle.AttackFormations);
                        battle.Phase = "joined"; battle.SettlementApplied = true; battle.SettledUtcMs = context.ServerUtcMs;
                        battle.JoinBattleId = target.BattleId; battle.CityOwnerPlayerId = target.CityOwnerPlayerId;
                        battle.Reward = new 战斗奖励();
                        Save(world, battle); Save(world, target);
                        return Updated(world, context, target, "combat.bandit.reinforced");
                    }
                }
                StartBattle(world, battle);
            }
            long cutoff = context.ServerUtcMs;
            var pending = world.Data["战斗运行"] as JObject;
            if (pending != null)
                foreach (JProperty entry in pending.Properties())
                    if (entry.Name != battle.BattleId && !entry.Value.Value<bool>("SettlementApplied") && entry.Value.Value<string>("Phase") == "marching"
                        && entry.Value.Value<int>("X") == battle.X && entry.Value.Value<int>("Y") == battle.Y)
                        if ((entry.Value.Value<string>("Kind") ?? "bandit") == battle.Kind)
                            cutoff = Math.Min(cutoff, entry.Value.Value<long>("ArrivalUtcMs"));
            var hits = new List<CombatHit>();
            // 限制一次离线补算长度；未补完的NextTick仍到期，下一轮Tick继续，不跳过战斗。
            for (int i = 0; i < 100 && cutoff >= battle.NextTickUtcMs && !battle.IsTerminal; i++)
            {
                GameResult garrisons = UpdateGarrisonArrivals(world, battle, battle.NextTickUtcMs);
                if (garrisons.Code != GameCodes.Ok) return garrisons;
                BanditBattleRules.Advance(battle, (unit, utc) => Profile(world, battle, unit, utc),
                    (unit, utc) => Recalculate(world, battle, unit, utc), GeneralExperienceRules.Add,
                    (actor, target, random, utc) => Capture(world, battle, actor, target, random, utc));
                hits.AddRange(battle.LastHits);
                battle.NextTickUtcMs = checked(battle.NextTickUtcMs + BanditBattleRules.TickMilliseconds);
            }
            battle.LastHits = hits;
            PublishCurrentGenerals(world, battle);
            GameResult settlement = null;
            if (battle.IsTerminal)
            {
                settlement = Settle(world, battle, context.ServerUtcMs);
                if (settlement.Code != GameCodes.Ok) return settlement;
            }
            else if (battle.Kind == "city") City(world, battle.X, battle.Y)["城墙"] = battle.Wall;
            else Camp(world, battle.X, battle.Y)["将领数据列表"] = new JArray(battle.Defenders.Select(unit => unit.General.DeepClone()));
            Save(world, battle);
            GameResult updated = Updated(world, context, battle, battle.SettlementApplied ? "combat.bandit.settled" : "combat.bandit.updated");
            if (settlement != null) updated.Events.AddRange(settlement.Events);
            return updated;
        }

        private static void StartBattle(WorldState world, BanditBattle battle)
        {
            if (battle.Kind == "city") { StartCityBattle(world, battle); return; }
            battle.Phase = "fighting";
            battle.StartedUtcMs = battle.ArrivalUtcMs;
            battle.NextTickUtcMs = checked(battle.ArrivalUtcMs + BanditBattleRules.TickMilliseconds);
            var defenders = (JArray)Camp(world, battle.X, battle.Y)["将领数据列表"];
            battle.Defenders.Clear();
            for (int i = 0; i < defenders.Count; i++)
                battle.Defenders.Add(BanditBattleRules.CreateUnit(battle.BattleId + ":npc:" + i, (JObject)defenders[i], Troop(world, defenders[i]["将领配兵"].Value<int>("ID")), 1));
        }

        private static GameResult Withdraw(WorldState world, CommandContext context, JObject payload)
        {
            bool single = payload?["generalId"] != null;
            if (!Keys(payload, single ? new[] { "battleId", "generalId" } : new[] { "battleId" }) || !Id(payload["battleId"], out string id)) return GameResult.Reject(GameCodes.InvalidArgument, "撤退参数无效");
            BanditBattle battle = Load(world, id);
            if (battle == null) return GameResult.Reject(GameCodes.NotFound, "战场不存在");
            if (battle.PlayerId != context.Actor.PlayerId) return GameResult.Reject(GameCodes.Forbidden, "只能撤退自己的军队");
            if (battle.SettlementApplied) return GameResult.Success(new JObject { ["battleId"] = id, ["phase"] = battle.Phase });
            RefreshCurrentGenerals(world, battle);
            if (single)
            {
                if (!Id(payload["generalId"], out string generalId)) return GameResult.Reject(GameCodes.InvalidArgument, "撤退将领无效");
                BanditBattleRules.EnsureFormations(battle);
                CombatUnit unit = battle.Attackers.SingleOrDefault(entry => !entry.Retired && entry.GeneralId == generalId);
                if (unit == null) return battle.Attackers.Any(entry => entry.GeneralId == generalId)
                    ? GameResult.Success(new JObject { ["battleId"] = id, ["generalId"] = generalId })
                    : GameResult.Reject(GameCodes.Forbidden, "只能撤退本军将领");
                var outcomes = new[] { new GeneralOutcome { GeneralId = generalId, General = unit.General, Remaining = checked((int)unit.Remaining), Wounded = checked((int)unit.Wounded) } };
                GameResult released = battle.Kind == "city"
                    ? GeneralsModule.ApplyCityDefenderOutcome(world, battle.PlayerId, unit.ArmyId, outcomes, false)
                    : GeneralsModule.ApplyOutcome(world, battle.PlayerId, unit.ArmyId, outcomes, false);
                if (released.Code != GameCodes.Ok) return released;
                unit.Retired = true;
                if (unit.General["详细信息"].Value<int>("状态") != 3) unit.General["详细信息"]["状态"] = 0.0;
                if (battle.Attackers.Any(entry => !entry.Retired && entry.Remaining > 0))
                {
                    Save(world, battle);
                    return Updated(world, context, battle, "combat.bandit.updated");
                }
            }
            battle.Phase = "withdrawn";
            GameResult settled = Settle(world, battle, context.ServerUtcMs);
            if (settled.Code != GameCodes.Ok) return settled;
            Save(world, battle);
            GameResult updated = Updated(world, context, battle, "combat.bandit.settled");
            updated.Events.AddRange(settled.Events);
            return updated;
        }

        private static GameResult Settle(WorldState world, BanditBattle battle, long utc)
        {
            if (battle.Kind == "city") return SettleCity(world, battle, utc);
            battle.Reward = BanditBattleRules.CalculateRewards(battle, CombatEconomy.GetActiveBonus(world.RequirePlayer(battle.PlayerId), "资源声望", utc));
            GameResult reward = CombatEconomy.ApplyCombatRewards(world, battle.PlayerId, battle.BattleId,
                battle.Reward.声望, battle.Reward.国库铜钱, battle.Reward.国库粮食);
            if (reward.Code != GameCodes.Ok) return reward;
            JObject camp = Camp(world, battle.X, battle.Y);
            if (battle.Phase == "won")
            {
                var random = new CombatRandom(battle.RandomState);
                var configurations = (JArray)world.Data["将领配置"];
                var generated = BanditGenerator.Generate(battle.X, battle.Y, random.Next, configurationId => {
                    JObject configuration = configurations.OfType<JObject>().Single(item => item.Value<int>("ID") == configurationId);
                    JObject general = GeneralCreationRules.CreateBanditGeneral(configuration, random.Next);
                    general["将领属性"]["初始属性"]["名字"] = BanditGenerator.GenerateName((JObject)world.Data["姓名配置"], random.Next);
                    return general;
                }, (general, level) => GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(level)));
                JObject npc = (JObject)world.RequirePlayer(battle.NpcPlayerId).DeepClone();
                var fiefs = (JArray)npc["封地信息表"];
                fiefs[0]["将领信息表"] = generated["将领数据列表"].DeepClone();
                GeneralAttributeRules.Recalculate(npc, utc / 1000);
                generated["将领数据列表"] = fiefs[0]["将领信息表"].DeepClone();
                camp.ReplaceAll(generated.Properties());
                battle.RandomState = random.State;
            }
            else if (battle.Phase == "lost" || (battle.Phase == "withdrawn" && battle.Frame > 0))
            {
                camp["将领数据列表"] = new JArray(battle.Defenders.Select(unit => {
                    JObject general = (JObject)unit.General.DeepClone(); general["详细信息"]["状态"] = 0.0; return general;
                }));
            }
            Save(world, battle);
            BanditBattleRules.EnsureFormations(battle);
            foreach (var army in battle.Attackers.Where(unit => !unit.Retired).GroupBy(unit => unit.ArmyId))
            {
                GameResult outcome = GeneralsModule.ApplyOutcome(world, battle.PlayerId, army.Key, army.Select(unit => new GeneralOutcome {
                    GeneralId = unit.GeneralId, General = unit.General, Remaining = checked((int)unit.Remaining), Wounded = checked((int)unit.Wounded) }));
                if (outcome.Code != GameCodes.Ok) return outcome;
            }
            battle.SettlementApplied = true;
            battle.SettledUtcMs = utc;
            return GameResult.Success();
        }

        private static void Recalculate(WorldState world, BanditBattle battle, CombatUnit actor, long utc)
        {
            if (battle.Kind == "city") { RecalculateCityOwner(world, battle, actor, utc); return; }
            if (actor.Side != 0) return; // 原NPC将领已从山贼玩家临时封地移走，不在玩家重算列表中。
            JObject player = (JObject)world.RequirePlayer(battle.PlayerId).DeepClone();
            foreach (CombatUnit unit in battle.Attackers.Where(unit => !unit.Retired))
            {
                JObject fief;
                JObject source = LegacyGenerals.General(player, unit.General.Value<int>("ID"), out fief);
                source.ReplaceAll(((JObject)unit.General.DeepClone()).Properties());
            }
            GeneralAttributeRules.Recalculate(player, utc / 1000);
            foreach (CombatUnit unit in battle.Attackers.Where(unit => !unit.Retired))
            {
                JObject fief;
                unit.General = (JObject)LegacyGenerals.General(player, unit.General.Value<int>("ID"), out fief).DeepClone();
            }
        }

        private static IEnumerable<CombatUnit> CurrentGenerals(WorldState world, BanditBattle battle)
        {
            foreach (CombatUnit unit in battle.Attackers.Concat(battle.Defenders))
            {
                if (unit.Retired || unit.Ephemeral || (unit.Side == 1 && battle.Kind != "city")) continue;
                JObject binding = world.EntityMappings["generalOccupancy"]?[unit.GeneralId] as JObject;
                string owner = unit.GeneralOwnerId ?? battle.PlayerId;
                string army = unit.Side == 1 && !unit.PlayerGarrison ? battle.BattleId : unit.ArmyId ?? battle.ArmyId;
                if (binding?.Value<string>("playerId") == owner && binding.Value<string>("armyId") == army) yield return unit;
            }
        }

        private static void RefreshCurrentGenerals(WorldState world, BanditBattle battle)
        {
            foreach (CombatUnit unit in CurrentGenerals(world, battle))
            {
                JObject fief;
                JObject source = GeneralsModule.ResolveGeneral(world, unit.GeneralOwnerId ?? battle.PlayerId, unit.GeneralId, out fief);
                unit.General["将领属性"] = source["将领属性"].DeepClone();
                unit.General["将领培养"] = source["将领培养"].DeepClone();
                foreach (string field in new[] { "经验", "升级需要经验", "剩余体力" })
                    unit.General["详细信息"][field] = source["详细信息"][field].DeepClone();
            }
        }

        private static void PublishCurrentGenerals(WorldState world, BanditBattle battle)
        {
            foreach (CombatUnit unit in CurrentGenerals(world, battle))
            {
                JObject fief;
                JObject source = GeneralsModule.ResolveGeneral(world, unit.GeneralOwnerId ?? battle.PlayerId, unit.GeneralId, out fief);
                source["将领属性"] = unit.General["将领属性"].DeepClone();
                foreach (string field in new[] { "经验", "升级需要经验", "剩余体力" })
                    source["详细信息"][field] = unit.General["详细信息"][field].DeepClone();
            }
        }

        private static CombatProfile Profile(WorldState world, BanditBattle battle, CombatUnit unit, long utc)
        {
            JObject player = world.RequirePlayer(unit.GeneralOwnerId ?? (unit.Side == 0 ? battle.PlayerId : battle.NpcPlayerId));
            string nation = player["基础信息"].Value<string>("国家");
            JObject country = ((JArray)world.Data["国家列表"]).OfType<JObject>().FirstOrDefault(item => item.Value<string>("国号") == nation);
            return CombatModifiers.Create(player, country, world.Data.Value<double>("难度"), utc, CombatEconomy.GetActiveBonus);
        }
        private static JObject Camp(WorldState world, int x, int y)
        {
            return (world.Data["山贼列表"] as JArray)?.OfType<JObject>().SingleOrDefault(camp => camp.Value<int>("坐标x") == x && camp.Value<int>("坐标y") == y);
        }
        private static BanditBattle ActiveAt(WorldState world, int x, int y, string except = null, string kind = "bandit")
        {
            var battles = world.Data["战斗运行"] as JObject;
            return battles?.Properties().Where(entry => entry.Name != except && !entry.Value.Value<bool>("SettlementApplied")
                && entry.Value.Value<int>("X") == x && entry.Value.Value<int>("Y") == y)
                .Where(entry => (entry.Value.Value<string>("Kind") ?? "bandit") == kind)
                .OrderBy(entry => entry.Value.Value<long>("ArrivalUtcMs")).ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .Select(entry => entry.Value.ToObject<BanditBattle>()).FirstOrDefault();
        }
        private static JObject Troop(WorldState world, int id)
        {
            return ((JArray)world.Data["兵种配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == id);
        }
        private static string NpcPlayerId(WorldState world, int legacyIndex = 1)
        {
            return ((JObject)world.EntityMappings["players"]).Properties().Single(entry =>
                entry.Value.Value<int>() == legacyIndex).Name;
        }
        private static BanditBattle Load(WorldState world, string id) { return world.Data["战斗运行"]?[id]?.ToObject<BanditBattle>(); }
        private static void Save(WorldState world, BanditBattle battle)
        {
            if (world.Data["战斗运行"] == null) world.Data["战斗运行"] = new JObject();
            world.Data["战斗运行"][battle.BattleId] = JObject.FromObject(battle);
        }
        private static GameResult Updated(WorldState world, CommandContext context, BanditBattle battle, string type)
        {
            if (battle.Kind == "city") type = type.Replace("combat.bandit.", "combat.city.");
            var data = new JObject { ["battleId"] = battle.BattleId, ["armyId"] = battle.ArmyId, ["phase"] = battle.Phase, ["arrivalUtcMs"] = battle.ArrivalUtcMs };
            GameResult result = GameResult.Success(data);
            GameResult notification = NotificationRules.RecordBattleLifecycle(world, battle.BattleId, type, context.ServerUtcMs);
            if (notification.Code != GameCodes.Ok) return notification;
            result.Events.AddRange(notification.Events);
            JObject state = JObject.FromObject(battle);
            foreach (string owner in CombatBattleProjection.Participants(state))
                result.Events.Add(new GameEvent { WorldId = world.WorldId,
                    Type = type, ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { owner }, Data = CombatBattleProjection.ProjectBattle(state, owner) });
            return result;
        }
        private static ulong Seed()
        {
            byte[] bytes = new byte[8]; ulong state;
            using (var random = RandomNumberGenerator.Create()) { do { random.GetBytes(bytes); state = BitConverter.ToUInt64(bytes, 0); } while (state == 0); }
            return state;
        }
        private static bool Keys(JObject value, params string[] allowed) { return value != null && value.Properties().Count() == allowed.Length && value.Properties().All(item => allowed.Contains(item.Name, StringComparer.Ordinal)); }
        private static bool Coordinate(JToken value, out int coordinate)
        {
            coordinate = -1;
            return value?.Type == JTokenType.Integer && int.TryParse(value.ToString(), out coordinate) && coordinate >= 0;
        }
        private static bool Id(JToken value, out string id)
        {
            id = value?.Type == JTokenType.String ? value.Value<string>() : null;
            return !string.IsNullOrWhiteSpace(id) && id.Length <= 128;
        }
    }
}
