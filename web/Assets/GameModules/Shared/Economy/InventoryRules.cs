using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class InventoryRules
    {
        public static GameResult ConsumeOne(JObject player, JArray originalItemDefinitions, string itemName)
        {
            return Consume(player, originalItemDefinitions, itemName, 1);
        }

        // Original item-use consumes the minimum stack, unlike the reverse-stack sale operation.
        public static GameResult Consume(JObject player, JArray originalItemDefinitions, string itemName, int quantity)
        {
            if (quantity <= 0) return GameResult.Reject(GameCodes.InvalidArgument, "使用数量无效");
            var definition = originalItemDefinitions?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("名字") == itemName);
            if (definition == null) return GameResult.Reject(GameCodes.NotFound, "道具不存在");
            var inventory = player?["背包道具列表"] as JObject;
            string field = definition.Value<string>("分类") + "道具列表";
            var items = inventory?[field] as JArray;
            if (items == null) return GameResult.Reject(GameCodes.Unavailable, "背包道具分类不存在");
            double available = 0;
            foreach (JToken item in items)
            {
                double existing;
                if (!(item is JObject) || item["名字"]?.Type != JTokenType.String || !ShopRules.TryNumber(item["数量"], out existing) || existing < 0 || existing != Math.Truncate(existing))
                    return GameResult.Reject(GameCodes.Unavailable, "背包道具数据无效");
                if (item.Value<string>("名字") == itemName) available += existing;
            }
            if (available < quantity) return GameResult.Reject(GameCodes.Conflict, "使用失败，道具数量不足");
            var remaining = (JArray)items.DeepClone();
            for (int i = 0; i < quantity; i++)
                if (!ItemStackRules.ConsumeOne<JToken>(remaining, itemName, item => item.Value<string>("名字"), item => item.Value<double>("数量"),
                    (item, count) => item["数量"] = count))
                    return GameResult.Reject(GameCodes.Conflict, "使用失败，道具数量不足");
            inventory[field] = remaining;
            return GameResult.Success(new JObject { ["itemName"] = itemName, ["quantity"] = quantity });
        }
    }
}
