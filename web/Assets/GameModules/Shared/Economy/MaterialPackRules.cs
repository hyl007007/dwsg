using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class MaterialPackRules
    {
        public static string MaterialName(string itemName)
        {
            switch (itemName)
            {
                case "冰玉大材料包": return "冰玉";
                case "仙芝大材料包": return "仙芝";
                case "山海精华材料包": return "山海精华";
                case "地魄灵石材料包": return "地魄灵石";
                case "天魂灵石材料包": return "天魂灵石";
                case "昆仑玄铁材料包": return "昆仑玄铁";
                default: return null;
            }
        }

        private static string MaterialType(string material)
        {
            switch (material)
            {
                case "山海精华": return "炼魂材料_坐骑";
                case "地魄灵石": return "炼魂材料_铠甲";
                case "天魂灵石": return "炼魂材料_头盔";
                case "昆仑玄铁": return "炼魂材料_武器";
                default: return "强化材料";
            }
        }

        public static GameResult Use(JObject player, JArray originalItemDefinitions, string itemName, int quantity)
        {
            string material = MaterialName(itemName);
            if (material == null) return GameResult.Reject(GameCodes.NotFound, "此材料包的效果尚未接入");
            if (quantity <= 0) return GameResult.Reject(GameCodes.InvalidArgument, "使用数量无效");
            var packDefinition = originalItemDefinitions?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("名字") == itemName);
            var materialDefinition = originalItemDefinitions?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("名字") == material);
            string materialType = MaterialType(material);
            if (packDefinition?.Value<string>("分类") != "宝箱" || packDefinition.Value<string>("类型") != (materialType == "强化材料" ? "装备强化材料箱子" : "炼魂材料箱子") ||
                materialDefinition?.Value<string>("分类") != "强化" || materialDefinition.Value<string>("类型") != materialType)
                return GameResult.Reject(GameCodes.Unavailable, "材料包原道具定义不完整");
            var original = player?["背包道具列表"] as JObject;
            if (original == null) return GameResult.Reject(GameCodes.Unavailable, "背包道具数据不完整");
            var candidate = new JObject { ["背包道具列表"] = original.DeepClone() };
            var materials = candidate["背包道具列表"]["强化道具列表"] as JArray;
            if (materials == null) return GameResult.Reject(GameCodes.Unavailable, "强化道具分类不存在");
            foreach (JToken item in materials)
            {
                double count;
                if (!(item is JObject) || item["名字"]?.Type != JTokenType.String ||
                    !ShopRules.TryNumber(item["数量"], out count) || count < 0 || count != Math.Truncate(count))
                    return GameResult.Reject(GameCodes.Unavailable, "强化道具数据无效");
            }
            var consumed = InventoryRules.Consume(candidate, originalItemDefinitions, itemName, quantity);
            if (consumed.Code != GameCodes.Ok) return consumed;
            // The original batch button repeats a 99-material expansion, filling the minimum stack each time.
            for (int i = 0; i < quantity; i++)
                ItemStackRules.Add<JToken>(materials, material, 99, item => item.Value<string>("名字"), item => item.Value<double>("数量"),
                    (item, count) => item["数量"] = count, (name, count) => new JObject { ["名字"] = name, ["ID"] = 0, ["数量"] = count });
            player["背包道具列表"] = candidate["背包道具列表"];
            var result = GameResult.Success(new JObject { ["itemName"] = itemName, ["quantity"] = quantity,
                ["materialName"] = material, ["materialQuantity"] = 99d * quantity });
            result.Message = "获得:" + material + "x" + (99d * quantity);
            return result;
        }
    }
}
