using System;
using System.Linq;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class EquipmentBoxRules
    {
        public static int Slot(string itemName)
        {
            switch (itemName)
            {
                case "70级兵盔箱": return 0;
                case "80级兵武箱": return 1;
                case "70级兵甲箱": return 2;
                case "80级兵骑箱": return 3;
                default: return -1;
            }
        }

        public static GameResult Use(JObject player, JArray itemDefinitions, JArray equipmentDefinitions, string itemName, int quantity, Func<int, int, int> range)
        {
            int slot = Slot(itemName);
            if (slot < 0) return GameResult.Reject(GameCodes.NotFound, "此装备箱的效果尚未接入");
            if (quantity <= 0) return GameResult.Reject(GameCodes.InvalidArgument, "使用数量无效");
            var definition = itemDefinitions?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("名字") == itemName);
            if (definition?.Value<string>("分类") != "宝箱" || definition.Value<string>("类型") != "装备箱子" || equipmentDefinitions == null || range == null)
                return GameResult.Reject(GameCodes.Unavailable, "装备箱原配置不完整");
            foreach (JToken equipment in equipmentDefinitions)
            {
                double level, value;
                if (!(equipment is JObject) || equipment["名称"]?.Type != JTokenType.String || string.IsNullOrEmpty(equipment.Value<string>("名称")) ||
                    equipment["类型"]?.Type != JTokenType.String || !ShopRules.TryNumber(equipment["等级"], out level) || level < 0 ||
                    !ShopRules.TryNumber(equipment["基础值"], out value) || value < 0)
                    return GameResult.Reject(GameCodes.Unavailable, "原装备配置无效");
            }
            string type = new[] { "头盔", "武器", "铠甲", "坐骑" }[slot];
            int cap = slot == 1 || slot == 3 ? 80 : 70;
            var eligible = equipmentDefinitions.OfType<JObject>().Where(item => item.Value<string>("类型") == type && item.Value<double>("等级") <= cap).ToArray();
            var inventory = player?["背包道具列表"] as JObject;
            var equipmentBag = player?["背包装备列表"] as JObject;
            if (eligible.Length == 0 || inventory == null || equipmentBag == null || !(equipmentBag[LegacyGenerals.EquipmentLists[slot]] is JArray))
                return GameResult.Reject(GameCodes.Unavailable, "原装备或背包数据不完整");
            var candidate = new JObject { ["背包道具列表"] = inventory.DeepClone(), ["背包装备列表"] = equipmentBag.DeepClone() };
            var consumed = InventoryRules.Consume(candidate, itemDefinitions, itemName, quantity);
            if (consumed.Code != GameCodes.Ok) return consumed;
            var list = (JArray)candidate["背包装备列表"][LegacyGenerals.EquipmentLists[slot]];
            var generated = new JArray();
            string message = null;
            for (int i = 0; i < quantity; i++)
            {
                var selected = eligible[range(0, eligible.Length)];
                // Original boxes draw the displayed quality separately, before the equipment's actual quality.
                int displayQuality = range(slot == 3 ? 3 : 2, 5), quality = range(2, 5);
                var equipment = new JObject { ["将领ID"] = -1, ["强化等级"] = 0d, ["品质"] = (double)quality, ["强化值"] = 0d,
                    ["保底次数"] = 0d, ["已强化次数"] = 0d, ["装备信息"] = selected.DeepClone(), ["炼魂属性"] = new JArray() };
                generated.Add(new JObject { ["slot"] = slot, ["legacyIndex"] = list.Count, ["displayQuality"] = displayQuality, ["equipment"] = equipment.DeepClone() });
                list.Add(equipment);
                message = "获得:" + new[] { "未知", "普通", "良好", "优秀", "卓越" }[displayQuality] + "的" + selected.Value<string>("名称") + "(" + selected.Value<double>("等级").ToString() + ")";
            }
            player["背包道具列表"] = candidate["背包道具列表"];
            player["背包装备列表"] = candidate["背包装备列表"];
            var result = GameResult.Success(new JObject { ["itemName"] = itemName, ["quantity"] = quantity, ["equipment"] = generated });
            result.Message = message;
            return result;
        }
    }
}
