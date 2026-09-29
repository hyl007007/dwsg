using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Economy
{
    public sealed class EconomyModule : IGameModule, IGameTickModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "shop.purchase", "shop.sell", "shop.refresh", "item.use" };
        private readonly Random random = new Random();

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "无权操作此世界");
            if (command.Type == "shop.refresh") return Refresh(candidate, context);
            if (command.Type != "shop.purchase" && command.Type != "shop.sell" && command.Type != "item.use") return GameResult.Reject(GameCodes.NotFound, "命令不存在");
            if (context.Actor.IsSystem || string.IsNullOrEmpty(context.Actor.PlayerId))
                return GameResult.Reject(GameCodes.Unauthenticated, "请先登录并创建角色");
            var payload = command.Payload;
            if (command.Type == "item.use")
            {
                if (payload == null || payload.Properties().Any(p => p.Name != "itemName") || payload["itemName"]?.Type != JTokenType.String)
                    return GameResult.Reject(GameCodes.InvalidArgument, "道具使用参数无效");
                if (payload.Value<string>("itemName") != "新手礼包")
                    return GameResult.Reject(GameCodes.NotFound, "此道具的联机效果尚未接入");
                JObject owner;
                try { owner = candidate.RequirePlayer(context.Actor.PlayerId); }
                catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "角色不存在于此世界"); }
                var used = StarterPackRules.Use(owner, candidate.Data["道具配置"] as JArray);
                if (used.Code == GameCodes.Ok) used.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "item.used",
                    ServerUtcMs = context.ServerUtcMs, Data = used.Data, AudiencePlayerIds = new[] { context.Actor.PlayerId } });
                return used;
            }
            if (payload == null || payload.Properties().Any(p => p.Name != "itemName" && p.Name != "currency" && p.Name != "quantity" && p.Name != "catalogVersion") ||
                payload["itemName"]?.Type != JTokenType.String || payload["currency"]?.Type != JTokenType.String || payload["catalogVersion"]?.Type != JTokenType.Integer)
                return GameResult.Reject(GameCodes.InvalidArgument, "购买参数无效");
            double quantity;
            if (!ShopRules.TryNumber(payload["quantity"], out quantity) || !ShopRules.ValidQuantity(quantity))
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择 1 到 100 个整数数量");
            if (!JToken.DeepEquals(payload["catalogVersion"], candidate.Data["商城配置版本"]))
                return GameResult.Reject(GameCodes.Conflict, "商城配置已更新，请刷新");
            string name = payload.Value<string>("itemName");
            var products = candidate.Data["商城商品"] as JArray;
            var items = candidate.Data["道具配置"] as JArray;
            var product = products?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("道具名") == name);
            var definition = items?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("名字") == name);
            if (product == null || definition == null) return GameResult.Reject(GameCodes.NotFound, "商品不存在");
            JObject player;
            try { player = candidate.RequirePlayer(context.Actor.PlayerId); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "角色不存在于此世界"); }
            var result = command.Type == "shop.sell"
                ? ShopRules.Sell(player, product, definition.Value<string>("分类"), payload.Value<string>("currency"), quantity)
                : ShopRules.Purchase(player, product, definition.Value<string>("分类"), payload.Value<string>("currency"), quantity);
            if (result.Code == GameCodes.Ok && command.Type == "shop.purchase")
                result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "shop.stockChanged", ServerUtcMs = context.ServerUtcMs,
                    Data = new JObject { ["itemName"] = name, ["remainingStock"] = product["限购数量"].DeepClone() } });
            return result;
        }

        public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs)
        {
            long due = state.Data.Value<long?>("商城下次刷新UTC") ?? 0;
            if (serverUtcMs >= due)
                yield return new GameCommand { WorldId = state.WorldId, Type = "shop.refresh", RequestId = "shop-refresh:" + due };
        }

        private GameResult Refresh(WorldState candidate, CommandContext context)
        {
            if (!context.Actor.IsSystem) return GameResult.Reject(GameCodes.Forbidden, "仅服务器可以刷新商城");
            long due = candidate.Data.Value<long?>("商城下次刷新UTC") ?? 0;
            if (context.ServerUtcMs < due) return GameResult.Reject(GameCodes.Conflict, "尚未到商城刷新时间");
            var names = candidate.Data["商城轮换商品"] as JArray;
            var catalog = candidate.Data["商城商品"] as JArray;
            if (names == null || catalog == null) return GameResult.Reject(GameCodes.Unavailable, "商城原表尚未载入");
            var listings = names.Select(name => catalog.OfType<JObject>().FirstOrDefault(item => item.Value<string>("道具名") == name.Value<string>())).ToArray();
            if (listings.Any(item => item == null)) return GameResult.Reject(GameCodes.Unavailable, "商城轮换原表不完整");
            lock (random) ShopRules.RefreshListings(listings, (item, price) => item["黄金售价"] = price, (item, stock) => item["限购数量"] = stock, random.Next);
            candidate.Data["商城下次刷新UTC"] = context.ServerUtcMs + 300000;
            var result = GameResult.Success();
            result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "shop.refreshed", ServerUtcMs = context.ServerUtcMs });
            return result;
        }
    }
}
