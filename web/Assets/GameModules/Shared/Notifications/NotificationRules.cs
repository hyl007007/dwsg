using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Notifications
{
    public static class NotificationRules
    {
        // Only trusted modules call this after saving the actual battle in their current candidate.
        public static GameResult RecordBattleLifecycle(WorldState candidate, string battleId, string eventType, long utcMs)
        {
            var battle = candidate.Data["战斗运行"]?[battleId] as JObject;
            if (battle == null || battle.Value<string>("BattleId") != battleId || string.IsNullOrEmpty(battle.Value<string>("PlayerId")))
                return GameResult.Reject(GameCodes.NotFound, "真实战场记录不存在。");
            if (eventType == null || (!eventType.StartsWith("combat.bandit.", StringComparison.Ordinal) && !eventType.StartsWith("combat.city.", StringComparison.Ordinal) && !eventType.StartsWith("combat.resource.", StringComparison.Ordinal)))
                return GameResult.Reject(GameCodes.InvalidArgument, "战场通知类型无效。");
            var result = GameResult.Success();
            if (battle.Value<string>("Phase") == "joined") return result;
            var player = candidate.RequirePlayer(battle.Value<string>("PlayerId"));
            var notices = player["notifications"] as JObject;
            if (notices == null) { notices = new JObject(); player["notifications"] = notices; }
            string phase = battle.Value<string>("Phase");
            if (eventType.EndsWith(".dispatched", StringComparison.Ordinal))
                Add(candidate, battle, notices, "march:" + battleId, "combat.march", "军队已出征，前往" + Target(candidate, battle)["name"] + Position(battle) + "。", utcMs, result, null, Target(candidate, battle));
            if (phase == "fighting" && battle.Value<long>("StartedUtcMs") > 0)
                Add(candidate, battle, notices, "start:" + battleId, "combat.started", "军队已抵达" + Target(candidate, battle)["name"] + Position(battle) + "，开始战斗。", utcMs, result);
            if (battle.Value<bool>("SettlementApplied") && (phase == "won" || phase == "lost" || phase == "withdrawn") && notices["report:" + battleId] == null)
            {
                var report = Report(candidate, battle, notices);
                Add(candidate, battle, notices, "report:" + battleId, "battle.report", FormatReport(report), battle.Value<long>("SettledUtcMs"), result, report);
            }
            RecordGarrisons(candidate, battle, eventType, utcMs, result);
            RecordCityOwner(candidate, battle, eventType, utcMs, result);
            return result;
        }

        private static void RecordCityOwner(WorldState state, JObject battle, string eventType, long utcMs, GameResult result)
        {
            string owner = battle.Value<string>("CityOwnerPlayerId");
            if (battle.Value<string>("Kind") != "city" || string.IsNullOrEmpty(owner) || owner == battle.Value<string>("PlayerId") ||
                state.EntityMappings["humanPlayers"]?[owner]?.Type != JTokenType.Boolean || !state.EntityMappings["humanPlayers"][owner].Value<bool>()) return;
            var player = state.RequirePlayer(owner);
            var notices = player["notifications"] as JObject;
            if (notices == null) { notices = new JObject(); player["notifications"] = notices; }
            string phase = battle.Value<string>("Phase"), location = Target(state, battle)["name"] + Position(battle);
            if (eventType == "combat.city.dispatched" && phase == "marching")
                Add(state, battle, notices, "owner-march:" + battle.Value<string>("BattleId"), "combat.city.defense.march", "敌军正在前往本人城池" + location + "。", utcMs, result, null, Target(state, battle), owner, true);
            if (phase == "fighting" && battle.Value<long>("StartedUtcMs") > 0)
                Add(state, battle, notices, "owner-start:" + battle.Value<string>("BattleId"), "combat.city.defense.started", "本人城池" + location + "正遭受进攻。", battle.Value<long>("StartedUtcMs"), result, null, Target(state, battle), owner, true);
            if (battle.Value<bool>("SettlementApplied") && (phase == "won" || phase == "lost" || phase == "withdrawn"))
                Add(state, battle, notices, "owner-settled:" + battle.Value<string>("BattleId"), "combat.city.defense.settled", "本人城池" + location + "，" +
                    (phase == "won" ? "城池失守" : phase == "lost" ? "攻城军队败退" : "攻城军队已撤退") + "。", battle.Value<long>("SettledUtcMs"), result, null, Target(state, battle), owner, true);
        }

        private static void RecordGarrisons(WorldState state, JObject battle, string eventType, long utcMs, GameResult result)
        {
            if (battle.Value<string>("Kind") != "city" || !(battle["GarrisonArmies"] is JArray armies)) return;
            foreach (var army in armies.OfType<JObject>())
            {
                string owner = army.Value<string>("PlayerId"), armyId = army.Value<string>("ArmyId"), phase = army.Value<string>("Phase");
                var player = state.RequirePlayer(owner);
                var notices = player["notifications"] as JObject;
                if (notices == null) { notices = new JObject(); player["notifications"] = notices; }
                string suffix = battle.Value<string>("BattleId") + ":" + armyId;
                if (phase == "marching" && eventType == "combat.city.garrison.dispatched")
                    Add(state, battle, notices, "march:" + suffix, "combat.garrison.march", "驻防军队已出发，前往" + Target(state, battle)["name"] + Position(battle) + "。", utcMs, result, null, Target(state, battle), owner);
                if (phase == "fighting" && army.Value<long>("JoinedUtcMs") > 0)
                    Add(state, battle, notices, "start:" + suffix, "combat.garrison.started", "驻防军队已抵达" + Target(state, battle)["name"] + Position(battle) + "，加入守城。", army.Value<long>("JoinedUtcMs"), result, null, null, owner);
                if ((phase == "marching" || phase == "fighting") && eventType == "combat.city.garrison.withdrawn")
                    foreach (var unit in GarrisonUnits(battle, army).Where(u => u.Value<bool>("Retired")))
                    {
                        string text = unit["General"]?["将领属性"]?["初始属性"]?.Value<string>("名字") ?? "将领";
                        text += unit["General"]?["详细信息"]?.Value<int>("状态") == 3 ? "已被俘，已退出原驻防军队" : "已从驻防军队撤回";
                        text += "，剩余兵力" + Number(unit["General"]?["详细信息"]?["剩余兵力"]) + "，伤兵" + Number(unit["Wounded"]) + "。";
                        Add(state, battle, notices, "return:" + suffix + ":" + unit.Value<string>("UnitId"), "combat.garrison.returned", text, utcMs, result, null, null, owner);
                    }
                if (army.Value<long>("SettledUtcMs") > 0 && (phase == "won" || phase == "lost" || phase == "withdrawn" || phase == "returned") && notices["report:" + suffix] == null)
                {
                    var report = Report(state, battle, notices, army);
                    Add(state, battle, notices, "report:" + suffix, "battle.report", FormatReport(report), army.Value<long>("SettledUtcMs"), result, report, null, owner);
                }
            }
        }

        private static IEnumerable<JObject> GarrisonUnits(JObject battle, JObject army)
        {
            return ((JArray)battle["Defenders"]).OfType<JObject>().Where(unit => unit.Value<bool>("PlayerGarrison") &&
                unit.Value<string>("GeneralOwnerId") == army.Value<string>("PlayerId") && unit.Value<string>("ArmyId") == army.Value<string>("ArmyId"));
        }

        public static int Unread(JObject privatePlayer)
        {
            return (privatePlayer?["notifications"] as JObject)?.Properties().Count(p => p.Value.Value<long>("readUtcMs") == 0) ?? 0;
        }

        public static GameResult MarkRead(WorldState candidate, AuthenticatedActor actor, JObject payload, long utcMs)
        {
            if (actor == null || actor.IsSystem || actor.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "请使用本人角色读取通知。");
            if (payload == null || payload.Properties().Count() != 1 || !(payload["notificationIds"] is JArray ids) || ids.Count < 1 || ids.Count > 100 ||
                ids.Any(id => id.Type != JTokenType.String || string.IsNullOrEmpty(id.Value<string>()) || id.Value<string>().Length > 256))
                return GameResult.Reject(GameCodes.InvalidArgument, "通知参数无效。");
            var player = candidate.RequirePlayer(actor.PlayerId);
            var notices = player["notifications"] as JObject;
            var selected = ids.Values<string>().Distinct(StringComparer.Ordinal).ToArray();
            if (notices == null || selected.Any(id => !(notices[id] is JObject)))
                return GameResult.Reject(GameCodes.NotFound, "本人通知不存在。");
            foreach (string id in selected) if (notices[id].Value<long>("readUtcMs") == 0) notices[id]["readUtcMs"] = utcMs;
            return GameResult.Success(new JObject { ["notificationIds"] = new JArray(selected), ["unreadCount"] = Unread(player) });
        }

        private static void Add(WorldState state, JObject battle, JObject notices, string id, string kind, string content, long utcMs,
            GameResult result, JObject report = null, JObject target = null, string owner = null, bool cityOwnerStatus = false)
        {
            if (notices[id] != null) return;
            var entry = new JObject { ["notificationId"] = id, ["messageId"] = id, ["kind"] = kind, ["channel"] = "system",
                ["battleId"] = battle["BattleId"].DeepClone(), ["senderPlayerId"] = "server", ["senderName"] = report == null ? "系统" : "战报",
                ["content"] = content, ["serverUtcMs"] = utcMs, ["readUtcMs"] = 0 };
            if (report != null) entry["report"] = report;
            if (target != null) entry["target"] = target;
            if (cityOwnerStatus) { entry["playerId"] = owner; entry["side"] = "cityOwner"; entry["sourcePhase"] = battle["Phase"].DeepClone(); }
            notices[id] = entry;
            result.Events.Add(new GameEvent { EventId = id, WorldId = state.WorldId, Type = "system.notification", ServerUtcMs = utcMs,
                AudiencePlayerIds = new[] { owner ?? battle.Value<string>("PlayerId") }, Data = (JObject)entry.DeepClone() });
        }

        private static JObject Target(WorldState state, JObject battle)
        {
            if (battle.Value<string>("Kind") == "resource") return new JObject {
                ["kind"] = "resource", ["x"] = battle["X"]?.DeepClone(), ["y"] = battle["Y"]?.DeepClone(),
                ["name"] = "资源点", ["resourceId"] = battle["ResourceId"]?.DeepClone() };
            bool city = battle.Value<string>("Kind") == "city";
            var target = new JObject { ["kind"] = city ? "city" : "bandit", ["x"] = battle["X"]?.DeepClone(), ["y"] = battle["Y"]?.DeepClone(),
                ["name"] = city ? (battle.Value<string>("CityName") ?? "城池") : "山贼" };
            if (city)
            {
                target["nation"] = battle["CityNation"]?.DeepClone(); target["scale"] = battle["CityScale"]?.DeepClone();
            }
            else if (!battle.Value<bool>("SettlementApplied"))
            {
                var camp = (state.Data["山贼列表"] as JArray)?.OfType<JObject>().FirstOrDefault(c => c.Value<int>("坐标x") == battle.Value<int>("X") && c.Value<int>("坐标y") == battle.Value<int>("Y"));
                if (camp != null) { target["level"] = camp["等级"].DeepClone(); target["name"] = camp["等级"] + "级山贼"; }
            }
            return target;
        }

        private static JObject Report(WorldState state, JObject battle, JObject notices, JObject garrison = null)
        {
            string id = battle.Value<string>("BattleId");
            string marchId = "march:" + id + (garrison == null ? "" : ":" + garrison.Value<string>("ArmyId"));
            var target = notices[marchId]?["target"] as JObject ?? Target(state, battle);
            var armies = new JArray();
            var ownUnits = garrison == null ? ((JArray)battle["Attackers"]).OfType<JObject>() : GarrisonUnits(battle, garrison);
            foreach (var group in ownUnits.GroupBy(u => u.Value<string>("ArmyId") ?? battle.Value<string>("ArmyId")))
            {
                var units = new JArray(group.Select(unit => {
                    var details = new JObject {
                    ["generalId"] = unit["GeneralId"]?.DeepClone(), ["unitId"] = unit["UnitId"]?.DeepClone(),
                    ["name"] = unit["General"]?["将领属性"]?["初始属性"]?["名字"]?.DeepClone(),
                    ["troopId"] = unit["General"]?["将领配兵"]?["ID"]?.DeepClone(),
                    ["originalQuantity"] = unit["OriginalQuantity"].DeepClone(),
                    ["remaining"] = unit["General"]["详细信息"]["剩余兵力"].DeepClone(),
                    ["loss"] = unit.Value<double>("OriginalQuantity") - unit["General"]["详细信息"].Value<double>("剩余兵力"),
                    ["wounded"] = unit["Wounded"].DeepClone(), ["retired"] = unit["Retired"].DeepClone() };
                    if (garrison != null) details["status"] = unit["General"]["详细信息"]["状态"].DeepClone();
                    return details;
                }));
                armies.Add(new JObject { ["armyId"] = group.Key, ["units"] = units });
            }
            var report = new JObject { ["battleId"] = id, ["worldId"] = state.WorldId, ["playerId"] = (garrison ?? battle)["PlayerId"].DeepClone(),
                ["phase"] = (garrison ?? battle)["Phase"].DeepClone(), ["target"] = target.DeepClone(), ["startedUtcMs"] = (garrison == null ? battle["StartedUtcMs"] : garrison["JoinedUtcMs"]).DeepClone(),
                ["settledUtcMs"] = (garrison ?? battle)["SettledUtcMs"].DeepClone(), ["armies"] = armies };
            if (garrison == null)
            {
                report["reward"] = battle["Reward"]?.DeepClone();
                if (battle["WarReward"] != null) report["warReward"] = battle["WarReward"].DeepClone();
            }
            else { report["side"] = "defender"; report["armyId"] = garrison["ArmyId"].DeepClone(); }
            return report;
        }

        private static string Position(JObject battle) => "(" + battle.Value<int>("X") + "," + battle.Value<int>("Y") + ")";
        private static string Number(JToken value) => (value?.Value<double>() ?? 0).ToString("0.##", CultureInfo.InvariantCulture);

        private static string FormatReport(JObject report)
        {
            string phase = report.Value<string>("phase");
            var text = new StringBuilder(report.Value<string>("side") == "defender" ? "驻防战报：" : "战报：").Append(report["target"].Value<string>("name")).Append('(').Append(report["target"]["x"]).Append(',').Append(report["target"]["y"]).Append(")，")
                .Append(phase == "won" ? "胜利" : phase == "lost" ? "战败" : phase == "returned" ? "返回" : "撤退");
            int number = 0;
            foreach (var army in (JArray)report["armies"])
            {
                text.Append("\n第").Append(++number).Append("军队：");
                foreach (var unit in (JArray)army["units"])
                {
                    double loss = unit.Value<double>("originalQuantity") - unit.Value<double>("remaining");
                    text.Append(unit.Value<string>("name") ?? "将领").Append(" 兵力").Append(Number(unit["originalQuantity"])).Append("→").Append(Number(unit["remaining"]))
                        .Append("，兵损").Append(loss.ToString("0.##", CultureInfo.InvariantCulture)).Append("，伤兵").Append(Number(unit["wounded"]));
                    if (unit.Value<int>("status") == 3) text.Append("，已被俘");
                    if (unit.Value<bool>("retired")) text.Append(report.Value<string>("side") == "defender" ? "，已退出队列" : "，已撤退归队");
                    text.Append('；');
                }
            }
            var reward = report["reward"] as JObject;
            if (reward != null)
            {
                text.Append("\n声望").Append(Number(reward["声望"])).Append("，国库铜钱").Append(Number(reward["国库铜钱"])).Append("，国库粮食").Append(Number(reward["国库粮食"]));
                text.Append("\n战场提示：粮食").Append(Number(reward["原提示粮食"])).Append("，黄金").Append(Number(reward["原提示黄金"]));
            }
            if (report["warReward"] != null) text.Append("\n战功").Append(Number(report["warReward"]));
            return text.ToString();
        }
    }
}
