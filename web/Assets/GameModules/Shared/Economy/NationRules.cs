using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class NationRules
    {
        public const double FoundingCost = 10000000.0;
        private static readonly string[] Materials = { "玉玺", "虎符", "印绶", "令牌" };
        private static readonly int[] Quantities = { 1, 10, 100, 1000 };

        public static GameResult Donate(WorldState world, string playerId, string tag, long copper, long grain)
        {
            const long maximum = 9007199254740991L;
            if (copper < 0 || grain < 0 || copper > maximum || grain > maximum || (copper == 0 && grain == 0))
                return GameResult.Reject(GameCodes.InvalidArgument, "请输入正整数捐献数量；未填写的资源按0计算。");
            JObject player; int identity;
            try { player = world.RequirePlayer(playerId); identity = world.ResolvePlayerIndex(playerId); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unauthenticated, "当前角色数据未就绪。"); }
            var basic = player["基础信息"] as JObject;
            var nation = TerritoryRules.Nation(world, tag);
            double index;
            if (string.IsNullOrEmpty(tag) || nation == null || basic?.Value<string>("国家") != tag ||
                !ShopRules.TryNumber(basic?["ID"], out index) || index != identity ||
                (!(nation["成员列表"] is JArray members) || !members.Any(m => ShopRules.TryNumber(m, out index) && index == identity)) &&
                (!ShopRules.TryNumber(nation["国王"], out index) || index != identity))
                return GameResult.Reject(GameCodes.Forbidden, "只能向自己所属的国家捐献，请返回国家页。");
            var wallet = player["财产信息"] as JObject;
            string[] resources = { "铜钱", "粮食" };
            long[] amounts = { copper, grain };
            var personal = new double[2]; var national = new double[2];
            for (int i = 0; i < resources.Length; i++)
            {
                if (wallet == null || !ShopRules.TryNumber(wallet[resources[i]], out personal[i]) || personal[i] < 0 || personal[i] > maximum ||
                    !ShopRules.TryNumber(nation[resources[i]], out national[i]) || national[i] < 0 || national[i] > maximum - amounts[i])
                    return GameResult.Reject(GameCodes.Unavailable, "资源数量异常，未扣除资源。");
                if (personal[i] < amounts[i]) return GameResult.Reject(GameCodes.InsufficientFunds, "捐献失败，" + resources[i] + "不足。");
            }
            // 两种资源全部检查后一起更新，失败不得出现只扣一项的半笔捐献。
            for (int i = 0; i < resources.Length; i++)
            { wallet[resources[i]] = personal[i] - amounts[i]; nation[resources[i]] = national[i] + amounts[i]; }
            var result = GameResult.Success(new JObject { ["tag"] = tag, ["copper"] = copper, ["grain"] = grain });
            result.Message = "已捐献铜钱 " + copper + "、粮食 " + grain + "。";
            return result;
        }

        public static GameResult Create(WorldState candidate, string playerId, string name, string tag, string declaration, int x, int y, long utcSeconds)
        {
            // Membership, materials, fief migration and city ownership are one original-world transaction.
            var working = candidate.Clone();
            GameResult result;
            try { result = CreateOriginal(working, playerId, name, tag, declaration, x, y, utcSeconds); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "原国家、封地或将领数据不完整"); }
            catch (InvalidCastException) { return GameResult.Reject(GameCodes.Unavailable, "原国家数值数据无效"); }
            catch (OverflowException) { return GameResult.Reject(GameCodes.Unavailable, "原国家数值超出范围"); }
            catch (ArgumentException) { return GameResult.Reject(GameCodes.Unavailable, "原国家索引数据无效"); }
            if (result.Code == GameCodes.Ok) { candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings; }
            return result;
        }

        public static GameResult Join(WorldState candidate, string playerId, string tag)
        {
            var working = candidate.Clone();
            GameResult result;
            try { result = JoinOriginal(working, playerId, tag); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "原国家、封地或将领数据不完整"); }
            catch (InvalidCastException) { return GameResult.Reject(GameCodes.Unavailable, "原国家数值数据无效"); }
            catch (OverflowException) { return GameResult.Reject(GameCodes.Unavailable, "原国家数值超出范围"); }
            catch (ArgumentException) { return GameResult.Reject(GameCodes.Unavailable, "原国家索引数据无效"); }
            if (result.Code == GameCodes.Ok) { candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings; }
            return result;
        }

        private static GameResult JoinOriginal(WorldState world, string playerId, string tag)
        {
            if (string.IsNullOrEmpty(tag)) return GameResult.Reject(GameCodes.InvalidArgument, "请选择现有国家");
            var target = TerritoryRules.Nation(world, tag);
            if (target == null) return GameResult.Reject(GameCodes.NotFound, "国家不存在");
            var player = world.RequirePlayer(playerId); var basic = Object(player["基础信息"]);
            if (basic.Value<string>("国家") == tag) return GameResult.Reject(GameCodes.Conflict, "已在本国!");
            var former = TerritoryRules.Nation(world, basic.Value<string>("国家"));
            if (former == null) return GameResult.Reject(GameCodes.Unavailable, "原所属国家不存在");
            int index = world.ResolvePlayerIndex(playerId), formerKing = Integer(former["国王"]);
            if (formerKing == index) return GameResult.Reject(GameCodes.Conflict, "国王不能换国!");
            var players = Array(world.Data["玩家列表"]);
            if (Integer(basic["ID"]) != index || !Array(former["成员列表"]).Any(m => Integer(m) == index) ||
                formerKing < 0 || formerKing >= players.Count || Array(player["封地信息表"]).Count == 0 ||
                Array(target["成员列表"]).Any(m => Integer(m) == index))
                return GameResult.Reject(GameCodes.Unavailable, "原国家成员身份不一致");
            int x = Integer(target["国都x"]), y = Integer(target["国都y"]);
            var capital = TerritoryRules.City(world, x, y);
            if (capital == null || capital.Value<string>("国家") != tag) return GameResult.Reject(GameCodes.Unavailable, "原国家国都归属无效");
            if (HasOccupation(world, playerId, player)) return GameResult.Reject(GameCodes.Conflict, "角色正在交战，请结束出征后换国");
            var moved = MoveAndMergeFiefs(world, playerId, player, x, y);
            if (moved.Code != GameCodes.Ok) return moved;
            foreach (var owned in Array(world.Data["城池列表"]).OfType<JObject>().Where(c => c.Value<int>("城主") == index))
            { owned["城主"] = formerKing; owned["国家"] = players[formerKing]["基础信息"].Value<string>("国家"); }
            foreach (var member in Array(former["成员列表"]).Where(m => Integer(m) == index).ToArray()) member.Remove();
            Array(target["成员列表"]).Add(index);
            basic["国家"] = tag; basic["战功"] = 0.0;
            RefreshCityCaches(world);
            var result = GameResult.Success(new JObject { ["tag"] = tag, ["cityX"] = x, ["cityY"] = y });
            result.Message = "更换成功!";
            return result;
        }

        private static GameResult CreateOriginal(WorldState world, string playerId, string name, string tag, string declaration, int x, int y, long utc)
        {
            // Actual original Unity Mono Encoding.Default is UTF-8. Do not trim or add a new naming rule.
            if (string.IsNullOrEmpty(name) || Encoding.UTF8.GetByteCount(name) >= 17 ||
                string.IsNullOrEmpty(tag) || Encoding.UTF8.GetByteCount(tag) > 3 || string.IsNullOrEmpty(declaration))
                return GameResult.Reject(GameCodes.InvalidArgument, "国名、国号或宣言无效!");
            var nations = Array(world.Data["国家列表"]);
            if (nations.OfType<JObject>().Any(n => n.Value<string>("国号") == tag))
                return GameResult.Reject(GameCodes.Conflict, "国号已存在!");
            var player = world.RequirePlayer(playerId);
            int index = world.ResolvePlayerIndex(playerId);
            var basic = Object(player["基础信息"]);
            var former = TerritoryRules.Nation(world, basic.Value<string>("国家"));
            if (former == null) return GameResult.Reject(GameCodes.Unavailable, "原所属国家不存在");
            if (former.Value<int>("国王") == index) return GameResult.Reject(GameCodes.Conflict, "国王不能另建国家!");
            var city = TerritoryRules.City(world, x, y);
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "国都城池不存在");
            // Despite the original UI label 县以上, its actual city selector accepts every scale > 0.
            if (city.Value<int>("城主") != index || city.Value<int>("规模") <= 0)
                return GameResult.Reject(GameCodes.Forbidden, "请选择本人拥有的规模大于零的城池");
            if (Integer(basic["ID"]) != index || !Array(former["成员列表"]).Any(m => Integer(m) == index))
                return GameResult.Reject(GameCodes.Unavailable, "原国家成员身份不一致");
            int formerKing = Integer(former["国王"]);
            var players = Array(world.Data["玩家列表"]);
            if (formerKing < 0 || formerKing >= players.Count || !(players[formerKing] is JObject))
                return GameResult.Reject(GameCodes.Unavailable, "原国王身份无效");
            var fiefs = Array(player["封地信息表"]);
            if (fiefs.Count == 0) return GameResult.Reject(GameCodes.Unavailable, "原首封地不存在");
            var first = Object(fiefs[0]);
            if (HasOccupation(world, playerId, player)) return GameResult.Reject(GameCodes.Conflict, "角色正在交战，请结束出征后建国");
            var wallet = Object(player["财产信息"]);
            double balance;
            if (!ShopRules.TryNumber(wallet["铜钱"], out balance) || balance < 0)
                return GameResult.Reject(GameCodes.Unavailable, "角色资源数据无效");
            // Original founding is strictly > cost, unlike ordinary research's >= cost.
            if (balance <= FoundingCost) return GameResult.Reject(GameCodes.InsufficientFunds, "建国失败,材料不足!");
            for (int i = 0; i < Materials.Length; i++)
            {
                // These are founding materials, not individual commander buff uses.
                var consumed = InventoryRules.Consume(player, world.Data["道具配置"] as JArray, Materials[i], Quantities[i]);
                if (consumed.Code != GameCodes.Ok) return consumed;
            }
            int greatest = nations.OfType<JObject>().Select(n => Integer(n["ID"])).DefaultIfEmpty(0).Max();
            if (world.Data["国家ID记录"] != null) greatest = Math.Max(greatest, Integer(world.Data["国家ID记录"]));
            if (greatest < 0 || greatest == int.MaxValue) return GameResult.Reject(GameCodes.Unavailable, "原国家编号无效");
            int nationId = greatest + 1;
            var created = OriginalDefaults(nationId, name, tag, declaration, x, y, index, utc);
            nations.Add(created);
            // The actual button changes only scale; it never invokes 新建国家 or 生成指定规模城池数据.
            city["规模"] = 4;
            var migration = MoveAndMergeFiefs(world, playerId, player, x, y);
            if (migration.Code != GameCodes.Ok) return migration;
            // Fix the old 城主==0 helper by using the authenticated original player index.
            foreach (var owned in Array(world.Data["城池列表"]).OfType<JObject>().Where(c => c.Value<int>("城主") == index))
            { owned["城主"] = formerKing; owned["国家"] = players[formerKing]["基础信息"].Value<string>("国家"); }
            foreach (var member in Array(former["成员列表"]).Where(m => Integer(m) == index).ToArray()) member.Remove();
            created["成员列表"] = new JArray(index);
            basic["国家"] = tag; basic["官阶"] = "国王";
            city["城主"] = index; city["国家"] = tag;
            wallet["铜钱"] = balance - FoundingCost;
            world.Data["国家ID记录"] = nationId;
            RefreshCityCaches(world);
            var result = GameResult.Success(new JObject { ["nationId"] = nationId, ["name"] = name, ["tag"] = tag, ["cityX"] = x, ["cityY"] = y });
            result.Message = "建国成功!";
            return result;
        }

        private static GameResult MoveAndMergeFiefs(WorldState world, string playerId, JObject player, int x, int y)
        {
            int index = world.ResolvePlayerIndex(playerId);
            var fiefs = Array(player["封地信息表"]); var first = Object(fiefs[0]);
            var moved = TerritoryRules.MoveOriginalFief(world, index, Integer(first["ID"]), x, y);
            if (moved.Code != GameCodes.Ok) return moved;
            while (fiefs.Count > 1)
            {
                var removed = Object(fiefs[1]);
                var released = ReleaseReferences(world, removed);
                if (released.Code != GameCodes.Ok) return released;
                foreach (var general in Array(removed["将领信息表"]).ToArray()) Array(first["将领信息表"]).Add(general.DeepClone());
                MergeTroops(Array(first["闲兵信息表"]), Array(removed["闲兵信息表"]));
                MergeTroops(Array(first["伤兵信息表"]), Array(removed["伤兵信息表"]));
                var location = Object(removed["所在城池"]);
                var source = TerritoryRules.City(world, Integer(location["x"]), Integer(location["y"]));
                var registrations = Array(source?["城池封地列表"]);
                var registration = registrations.OfType<JObject>().SingleOrDefault(r => Integer(r["第几个玩家"]) == index && Integer(r["封地ID标识"]) == Integer(removed["ID"]));
                if (registration == null) return GameResult.Reject(GameCodes.Conflict, "原封地城池登记不一致");
                registration.Remove();
                var stableFiefs = world.EntityMappings["fiefs"] as JObject;
                if (stableFiefs != null)
                    foreach (var binding in stableFiefs.Properties().Where(p => p.Value.Value<string>("playerId") == playerId && p.Value.Value<int>("legacyId") == Integer(removed["ID"])).ToArray()) binding.Remove();
                fiefs.RemoveAt(1);
            }
            return GameResult.Success();
        }

        private static bool HasOccupation(WorldState world, string playerId, JObject player)
        {
            if ((world.EntityMappings["generalOccupancy"] as JObject)?.Properties().Any(p => p.Value.Value<string>("playerId") == playerId) == true) return true;
            return Array(player["封地信息表"]).OfType<JObject>().SelectMany(f => Array(f["将领信息表"]).OfType<JObject>()).Any(g => g["详细信息"].Value<double>("状态") == 1.0);
        }

        private static void RefreshCityCaches(WorldState world)
        {
            foreach (var nation in Array(world.Data["国家列表"]).OfType<JObject>())
                nation["城池列表"] = new JArray(Array(world.Data["城池列表"]).OfType<JObject>().Where(c => c.Value<string>("国家") == nation.Value<string>("国号"))
                    .Select(c => new JObject { ["x"] = c["坐标x"].DeepClone(), ["y"] = c["坐标y"].DeepClone() }));
        }

        private static GameResult ReleaseReferences(WorldState world, JObject removed)
        {
            var players = Array(world.Data["玩家列表"]);
            foreach (string field in new[] { "俘虏信息表", "驻防信息表" })
                foreach (var reference in Array(removed[field]).OfType<JObject>())
                {
                    int owner = Integer(reference["第几个玩家"]), legacyId = Integer(reference["将领ID标识"]);
                    if (owner < 0 || owner >= players.Count) throw new InvalidOperationException();
                    var general = Array(players[owner]["封地信息表"]).OfType<JObject>().SelectMany(f => Array(f["将领信息表"]).OfType<JObject>()).SingleOrDefault(g => Integer(g["ID"]) == legacyId);
                    if (general == null) return GameResult.Reject(GameCodes.Unavailable, "原俘虏或驻防实例不存在");
                    var ownerBinding = (world.EntityMappings["players"] as JObject)?.Properties().SingleOrDefault(p => Integer(p.Value) == owner);
                    var binding = (world.EntityMappings["generals"] as JObject)?.Properties().SingleOrDefault(p => p.Value.Value<string>("playerId") == ownerBinding?.Name && p.Value.Value<int>("legacyId") == legacyId);
                    if ((binding != null && world.EntityMappings["generalOccupancy"]?[binding.Name] != null) || general["详细信息"].Value<double>("状态") == 1.0)
                        return GameResult.Reject(GameCodes.Conflict, "原俘虏或驻防仍在交战，建国未扣费");
                    general["详细信息"]["状态"] = 0.0;
                }
            return GameResult.Success();
        }

        private static void MergeTroops(JArray target, JArray source)
        {
            foreach (var entry in source.OfType<JObject>())
            {
                int id = Integer(entry["ID"]), count = Integer(entry["数量"]);
                var existing = target.OfType<JObject>().SingleOrDefault(t => Integer(t["ID"]) == id);
                long total = count + (long)(existing == null ? 0 : Integer(existing["数量"]));
                if (id <= 0 || count < 0 || total < 0 || total > int.MaxValue) throw new InvalidOperationException();
                if (existing == null) target.Add(entry.DeepClone()); else existing["数量"] = (double)total;
            }
        }

        // Direct translation of all 27 original 国家信息库类 fields, after 初始化国家/刷新科技信息.
        public static JObject OriginalDefaults(int id, string name, string tag, string declaration, int x, int y, int king, long utc)
        {
            return new JObject { ["ID"] = id, ["国名"] = name, ["国号"] = tag, ["国都x"] = x, ["国都y"] = y, ["国王"] = king,
                ["城池列表"] = new JArray(), ["成员列表"] = new JArray(), ["效率"] = 120.0, ["科技等级"] = 0.0, ["科技积分"] = 1.0,
                ["攻击科技"] = 0.0, ["防御科技"] = 0.0, ["资源科技"] = 0.0, ["民生值"] = 5000.0, ["铜钱"] = 5000.0, ["粮食"] = 5000.0,
                ["公告"] = "", ["宣言"] = declaration, ["大都督"] = -1, ["丞相"] = -1, ["奋武将军"] = -1, ["征东将军"] = -1,
                ["都尉"] = -1, ["侍郎"] = -1, ["上次轮选时间"] = utc, ["轮选时间间隔"] = 300L };
        }
        private static JObject Object(JToken value) { return value as JObject ?? throw new InvalidOperationException(); }
        private static JArray Array(JToken value) { return value as JArray ?? throw new InvalidOperationException(); }
        private static int Integer(JToken value)
        {
            double number;
            if (!ShopRules.TryNumber(value, out number) || number != Math.Truncate(number) || number < int.MinValue || number > int.MaxValue) throw new InvalidOperationException();
            return (int)number;
        }
    }
}
