using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Economy
{
    public sealed class EconomyModule : IGameModule, IReadOnlyGameTickModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "shop.purchase", "shop.sell", "shop.refresh", "item.use" };
        private readonly Random random = new Random();
        private readonly Action<WorldState> initializeEntities;

        public EconomyModule(Action<WorldState> initializeEntities = null)
        {
            this.initializeEntities = initializeEntities;
        }

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
                if (payload == null || payload.Properties().Any(p => p.Name != "itemName" && p.Name != "quantity") || payload["itemName"]?.Type != JTokenType.String)
                    return GameResult.Reject(GameCodes.InvalidArgument, "道具使用参数无效");
                double useQuantity = 1;
                if (payload["quantity"] != null && (payload["quantity"].Type != JTokenType.Integer ||
                    !ShopRules.TryNumber(payload["quantity"], out useQuantity) || useQuantity < 1 || useQuantity > int.MaxValue))
                    return GameResult.Reject(GameCodes.InvalidArgument, "使用数量无效");
                string itemName = payload.Value<string>("itemName");
                if (itemName != "新手礼包" && MaterialPackRules.MaterialName(itemName) == null && EquipmentBoxRules.Slot(itemName) < 0)
                    return GameResult.Reject(GameCodes.NotFound, "此道具的联机效果尚未接入");
                if (itemName == "新手礼包" && useQuantity != 1)
                    return GameResult.Reject(GameCodes.InvalidArgument, "新手礼包请单次使用");
                JObject owner;
                try { owner = candidate.RequirePlayer(context.Actor.PlayerId); }
                catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "角色不存在于此世界"); }
                var used = EquipmentBoxRules.Slot(itemName) >= 0 ? UseEquipmentBox(candidate, context.Actor.PlayerId, itemName, (int)useQuantity)
                    : itemName == "新手礼包" ? StarterPackRules.Use(owner, candidate.Data["道具配置"] as JArray)
                    : MaterialPackRules.Use(owner, candidate.Data["道具配置"] as JArray, itemName, (int)useQuantity);
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

        private GameResult UseEquipmentBox(WorldState candidate, string playerId, string itemName, int quantity)
        {
            if (initializeEntities == null) return GameResult.Reject(GameCodes.Unavailable, "装备身份登记尚未就绪");
            var working = candidate.Clone();
            var result = EquipmentBoxRules.Use(working.RequirePlayer(playerId), working.Data["道具配置"] as JArray,
                working.Data["装备配置"] as JArray, itemName, quantity, RandomNumberGenerator.GetInt32);
            if (result.Code != GameCodes.Ok) return result;
            try { initializeEntities(working); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "装备身份登记失败"); }
            catch (GeneralRuleException) { return GameResult.Reject(GameCodes.Unavailable, "装备身份登记失败"); }
            var equipment = new JArray();
            foreach (JObject entry in result.Data["equipment"])
            {
                var matches = (working.EntityMappings["equipment"] as JObject)?.Properties().Where(p => p.Value is JObject &&
                    p.Value.Value<string>("playerId") == playerId && JToken.DeepEquals(p.Value["slot"], entry["slot"]) &&
                    JToken.DeepEquals(p.Value["legacyIndex"], entry["legacyIndex"])).ToArray();
                if (matches == null || matches.Length != 1 || string.IsNullOrEmpty(matches[0].Name))
                    return GameResult.Reject(GameCodes.Unavailable, "新装备身份未生成");
                var generated = entry["equipment"];
                equipment.Add(new JObject { ["equipmentId"] = matches[0].Name, ["slot"] = entry["slot"].DeepClone(),
                    ["name"] = generated["装备信息"]["名称"].DeepClone(), ["level"] = generated["装备信息"]["等级"].DeepClone(),
                    ["quality"] = generated["品质"].DeepClone(), ["displayQuality"] = entry["displayQuality"].DeepClone() });
            }
            result.Data["equipment"] = equipment;
            candidate.Data = working.Data;
            candidate.EntityMappings = working.EntityMappings;
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
