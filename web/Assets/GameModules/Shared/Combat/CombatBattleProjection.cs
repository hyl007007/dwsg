using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Combat
{
    public static class CombatBattleProjection
    {
        public static IEnumerable<string> Participants(JObject storedBattle)
        {
            if (storedBattle == null) return Enumerable.Empty<string>();
            var owners = new List<string> { storedBattle.Value<string>("PlayerId"), storedBattle.Value<string>("CityOwnerPlayerId") };
            if (storedBattle["Defenders"] is JArray defenders)
                owners.AddRange(defenders.OfType<JObject>().Where(unit => !unit.Value<bool>("Ephemeral") && unit.Value<bool>("HumanOwner"))
                    .Select(unit => unit.Value<string>("GeneralOwnerId")));
            if (storedBattle["GarrisonArmies"] is JArray armies)
                owners.AddRange(armies.OfType<JObject>().Select(army => army.Value<string>("PlayerId")));
            return owners.Where(owner => !string.IsNullOrEmpty(owner)).Distinct(StringComparer.Ordinal);
        }

        public static JObject ProjectBattle(JObject storedBattle, string actorId)
        {
            if (string.IsNullOrEmpty(actorId) || !Participants(storedBattle).Contains(actorId, StringComparer.Ordinal)) return null;
            JObject view = (JObject)storedBattle.DeepClone();
            view.Remove("RandomState");
            string attacker = storedBattle.Value<string>("PlayerId");
            var armies = storedBattle["GarrisonArmies"] as JArray ?? new JArray();
            var pending = new HashSet<string>(armies.OfType<JObject>().Where(army => army.Value<long>("JoinedUtcMs") == 0
                && army.Value<string>("PlayerId") != actorId).Select(army => army.Value<string>("ArmyId")), StringComparer.Ordinal);
            foreach (string side in new[] { "Attackers", "Defenders" })
            {
                var units = view[side] as JArray;
                if (units == null) continue;
                foreach (JObject unit in units.OfType<JObject>().ToArray())
                {
                    if (pending.Contains(unit.Value<string>("ArmyId"))) { unit.Remove(); continue; }
                    string owner = unit.Value<string>("GeneralOwnerId") ?? (side == "Attackers" ? attacker : null);
                    if (owner == actorId || (side == "Defenders" && !unit.Value<bool>("PlayerGarrison") && !unit.Value<bool>("HumanOwner"))) continue;
                    JObject general = (JObject)unit["General"];
                    // 原服务器演出读取头像/名字/职业/成长星级、兵种、剩余兵力与颜色；不读取他人装备/培养/经验。
                    unit["General"] = new JObject {
                        ["ID"] = general["ID"]?.DeepClone(),
                        ["将领属性"] = new JObject { ["初始属性"] = Pick((JObject)general["将领属性"]["初始属性"], "ID", "名字", "职业", "成长") },
                        ["将领配兵"] = Pick((JObject)general["将领配兵"], "ID", "数量"),
                        ["详细信息"] = Pick((JObject)general["详细信息"], "身份", "坑位颜色", "状态", "剩余兵力")
                    };
                    unit.Remove("Wounded");
                }
            }
            view["GarrisonArmies"] = new JArray(armies.OfType<JObject>().Where(army => army.Value<string>("PlayerId") == actorId).Select(army => army.DeepClone()));
            if (view["DefenseFormations"] is JArray formations)
                foreach (JObject formation in formations.OfType<JObject>().Where(formation => pending.Contains(formation.Value<string>("ArmyId"))).ToArray()) formation.Remove();
            if (actorId != attacker) { view.Remove("Reward"); view.Remove("WarReward"); }
            return view;
        }

        private static JObject Pick(JObject source, params string[] fields)
        {
            var result = new JObject();
            foreach (string field in fields) if (source[field] != null) result[field] = source[field].DeepClone();
            return result;
        }
    }
}
