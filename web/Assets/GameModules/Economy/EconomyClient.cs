using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

// Default Assembly-CSharp adapter: original DTOs never enter the pure Shared assembly.
public static class EconomyClient
{
    public static void ApplyCatalog(WorldSnapshot snapshot)
    {
        var catalog = snapshot?.PublicWorld?["商城商品"] as JArray;
        if (catalog == null) return;
        foreach (var listings in new[] { 全局商城库.热卖商品列表, 全局商城库.特价商品列表, 全局商城库.装备商品列表,
            全局商城库.生产商品列表, 全局商城库.加速商品列表, 全局商城库.宝物商品列表, 全局商城库.宝箱商品列表, 全局商城库.其他商品列表 })
            foreach (var listing in listings)
                foreach (JObject product in catalog)
                    if (product.Value<string>("道具名") == listing.道具名)
                    {
                        listing.黄金售价 = product.Value<double>("黄金售价");
                        listing.白银售价 = product.Value<double>("白银售价");
                        listing.限购数量 = product.Value<int>("限购数量");
                        break;
                    }
    }

    public static GameResult PurchaseOffline(玩家数据 player, 商品属性类 product, string category, string currency, double quantity)
    {
        return TradeOffline(player, product, category, currency, quantity, false);
    }

    public static GameResult SellOffline(玩家数据 player, 商品属性类 product, string category, string currency, double quantity)
    {
        return TradeOffline(player, product, category, currency, quantity, true);
    }

    private static GameResult TradeOffline(玩家数据 player, 商品属性类 product, string category, string currency, double quantity, bool sell)
    {
        var state = new JObject
        {
            ["基础信息"] = new JObject { ["背包容量上限"] = player.基础信息.背包容量上限 },
            ["财产信息"] = JObject.FromObject(player.财产信息),
            ["背包道具列表"] = JObject.FromObject(player.背包道具列表),
            ["背包装备列表"] = JObject.FromObject(player.背包装备列表)
        };
        var listing = JObject.FromObject(product);
        var result = sell ? ShopRules.Sell(state, listing, category, currency, quantity) : ShopRules.Purchase(state, listing, category, currency, quantity);
        if (result.Code != GameCodes.Ok) return result;
        // Apply the same shared stack rule through the original DTO delegate, preserving item references.
        if (sell)
        {
            if (!player.背包道具列表.扣除道具(product.道具名, (int)quantity))
                return GameResult.Reject(GameCodes.Conflict, "卖出失败，数量不足");
        }
        else player.背包道具列表.添加道具(product.道具名, (int)quantity);
        ApplyWallet(player, (JObject)state["财产信息"]);
        product.限购数量 = listing.Value<int>("限购数量");
        return result;
    }

    public static string UseStarterPack(玩家数据 player)
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            Dwsg.Network.GameNetwork.SendCommand("item.use", new JObject { ["itemName"] = "新手礼包" }, response =>
            {
                if (全局变量.提示类 != null) 全局变量.提示类.显示信息(response?.Message ?? "服务器未确认道具使用，请重试");
            });
            return "新手礼包使用请求已发送";
        }
        var state = new JObject { ["财产信息"] = JObject.FromObject(player.财产信息), ["背包道具列表"] = JObject.FromObject(player.背包道具列表) };
        var result = StarterPackRules.Use(state, JArray.FromObject(全局道具库.道具列表));
        if (result.Code != GameCodes.Ok) return "使用失败";
        if (!ItemStackRules.ConsumeOne(player.背包道具列表.获取道具分类列表("新手礼包"), "新手礼包", item => item.名字, item => item.数量,
            (item, count) => item.数量 = count)) return "使用失败";
        ApplyWallet(player, (JObject)state["财产信息"]);
        return result.Message;
    }

    public static GameResult GrantStarterRewardsOffline(玩家数据 player)
    {
        var wallet = JObject.FromObject(player.财产信息);
        var result = StarterPackRules.GrantRewards(wallet);
        if (result.Code == GameCodes.Ok) ApplyWallet(player, wallet);
        return result;
    }

    private static void ApplyWallet(玩家数据 player, JObject wallet)
    {
        player.财产信息.铜钱 = wallet.Value<double>("铜钱");
        player.财产信息.粮食 = wallet.Value<double>("粮食");
        player.财产信息.黄金 = wallet.Value<double>("黄金");
        player.财产信息.白银 = wallet.Value<double>("白银");
    }
}
