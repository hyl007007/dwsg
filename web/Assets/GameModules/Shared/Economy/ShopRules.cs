using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class ShopRules
    {
        private static readonly string[] ItemLists = { "宝物道具列表", "加速道具列表", "生产道具列表", "宝箱道具列表", "强化道具列表", "任务道具列表" };
        private static readonly string[] EquipmentLists = { "武器装备列表", "头盔装备列表", "铠甲装备列表", "坐骑装备列表" };

        public static bool ValidQuantity(double quantity)
        {
            return !double.IsNaN(quantity) && !double.IsInfinity(quantity) && quantity >= 1 && quantity <= 100 && quantity == Math.Truncate(quantity);
        }

        public static bool TryNumber(JToken value, out double number)
        {
            number = 0;
            if (value == null || (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)) return false;
            try { number = value.Value<double>(); }
            catch (OverflowException) { return false; }
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        public static int UsedSlots(JObject player)
        {
            int count = CountLists(player?["背包道具列表"] as JObject, ItemLists);
            int equipment = CountLists(player?["背包装备列表"] as JObject, EquipmentLists);
            return count < 0 || equipment < 0 ? -1 : count + equipment;
        }

        private static int CountLists(JObject inventory, string[] fields)
        {
            int count = 0;
            foreach (string field in fields)
            {
                var items = inventory?[field] as JArray;
                if (items == null) return -1;
                count += items.Count;
            }
            return count;
        }

        public static GameResult Purchase(JObject player, JObject product, string category, string currency, double quantity)
        {
            if (!ValidQuantity(quantity)) return GameResult.Reject(GameCodes.InvalidArgument, "请选择 1 到 100 个整数数量");
            if (currency != "黄金" && currency != "白银") return GameResult.Reject(GameCodes.InvalidArgument, "该商品不支持此货币");
            string field = category + "道具列表";
            if (Array.IndexOf(ItemLists, field) < 0) return GameResult.Reject(GameCodes.NotFound, "道具分类不存在");
            var items = player?["背包道具列表"]?[field] as JArray;
            var wallet = player?["财产信息"] as JObject;
            double price, balance, capacity;
            if (product == null || items == null || wallet == null || product["道具名"]?.Type != JTokenType.String)
                return GameResult.Reject(GameCodes.Unavailable, "商城数据不完整");
            if (!TryNumber(product[currency + "售价"], out price) || price <= 0)
                return GameResult.Reject(GameCodes.InvalidArgument, "该商品不支持此货币");
            double stockValue;
            if (!TryNumber(product["限购数量"], out stockValue) || stockValue != Math.Truncate(stockValue) || stockValue < -1 || stockValue > int.MaxValue)
                return GameResult.Reject(GameCodes.Unavailable, "商城库存无效");
            int stock = (int)stockValue;
            int amount = (int)quantity;
            if (stock != -1 && stock < amount) return GameResult.Reject(GameCodes.Conflict, "购买失败，超出购买限制");
            double cost = price * amount;
            if (double.IsInfinity(cost) || !TryNumber(wallet[currency], out balance) || balance < cost)
                return GameResult.Reject(GameCodes.InsufficientFunds, "余额不足");
            string name = product.Value<string>("道具名");
            foreach (JToken item in items)
            {
                double existing;
                if (!(item is JObject) || item["名字"]?.Type != JTokenType.String || !TryNumber(item["数量"], out existing) || existing < 0)
                    return GameResult.Reject(GameCodes.Unavailable, "背包道具数据无效");
            }
            int usedSlots = UsedSlots(player);
            if (usedSlots < 0 || !TryNumber(player?["基础信息"]?["背包容量上限"], out capacity) || capacity < 0)
                return GameResult.Reject(GameCodes.Unavailable, "背包容量数据无效");
            int extraSlots = ItemStackRules.RequiredSlots<JToken>(items, name, amount, item => item.Value<string>("名字"), item => item.Value<double>("数量"));
            if (extraSlots > 0 && usedSlots + extraSlots > capacity)
                return GameResult.Reject(GameCodes.Conflict, "购买失败，背包容量不足");

            ItemStackRules.Add<JToken>(items, name, amount, item => item.Value<string>("名字"), item => item.Value<double>("数量"),
                (item, count) => item["数量"] = count, (itemName, count) => new JObject { ["名字"] = itemName, ["ID"] = 0, ["数量"] = count });
            wallet[currency] = balance - cost;
            if (stock != -1) product["限购数量"] = stock - amount;
            var result = GameResult.Success(new JObject { ["itemName"] = name, ["currency"] = currency, ["quantity"] = amount,
                ["amount"] = cost, ["remainingStock"] = product["限购数量"].DeepClone() });
            result.Message = "购买成功!";
            return result;
        }

        public static void RefreshListings<T>(IEnumerable<T> listings, Action<T, double> setGold,
            Action<T, int> setStock, Func<int, int, int> randomRange)
        {
            foreach (T listing in listings)
            {
                setGold(listing, randomRange(1011, 1326));
                setStock(listing, randomRange(1, 10));
            }
        }

        public static GameResult Sell(JObject player, JObject product, string category, string currency, double quantity)
        {
            if (!ValidQuantity(quantity)) return GameResult.Reject(GameCodes.InvalidArgument, "请选择 1 到 100 个整数数量");
            if (currency != "黄金" && currency != "白银") return GameResult.Reject(GameCodes.InvalidArgument, "该商品不支持此货币");
            string field = category + "道具列表";
            if (Array.IndexOf(ItemLists, field) < 0) return GameResult.Reject(GameCodes.NotFound, "道具分类不存在");
            var items = player?["背包道具列表"]?[field] as JArray;
            var wallet = player?["财产信息"] as JObject;
            if (product == null || items == null || wallet == null || product["道具名"]?.Type != JTokenType.String)
                return GameResult.Reject(GameCodes.Unavailable, "商城数据不完整");
            double price, balance;
            if (!TryNumber(product[currency + "售价"], out price) || price <= 0)
                return GameResult.Reject(GameCodes.InvalidArgument, "该商品不支持此货币");
            double proceeds = price * quantity;
            if (!TryNumber(wallet[currency], out balance) || double.IsInfinity(balance + proceeds))
                return GameResult.Reject(GameCodes.Unavailable, "资产数据无效");
            foreach (JToken item in items)
            {
                double existing;
                if (!(item is JObject) || item["名字"]?.Type != JTokenType.String || !TryNumber(item["数量"], out existing) || existing < 0 || existing != Math.Truncate(existing))
                    return GameResult.Reject(GameCodes.Unavailable, "背包道具数据无效");
            }
            string name = product.Value<string>("道具名");
            if (!ItemStackRules.Subtract<JToken>(items, name, (int)quantity, item => item.Value<string>("名字"), item => item.Value<double>("数量"),
                (item, count) => item["数量"] = count))
                return GameResult.Reject(GameCodes.Conflict, "卖出失败，数量不足");
            wallet[currency] = balance + proceeds;
            var result = GameResult.Success(new JObject { ["itemName"] = name, ["currency"] = currency, ["quantity"] = (int)quantity, ["amount"] = proceeds });
            result.Message = "卖出" + name + (int)quantity + "个成功，获得" + currency + proceeds;
            return result;
        }
    }
}
