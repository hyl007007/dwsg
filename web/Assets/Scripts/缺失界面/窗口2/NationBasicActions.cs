using System;
using System.Collections.Generic;
using System.Globalization;
using 玩家数据结构;

namespace 缺失界面.窗口2
{
    public sealed class NationTechnologyCost
    {
        public double Gold, Grain, Copper;
        public string Description { get { return "黄金 " + NationDataSource.Number(Gold) + "  粮食 " + NationDataSource.Number(Grain) + "  铜钱 " + NationDataSource.Number(Copper); } }
    }

    // Commands share the existing world records; no additional player or nation save schema is required.
    public sealed class NationBasicActions
    {
        public static readonly NationBasicActions Current = new NationBasicActions(
            () => 全局变量.所有玩家数据表, () => 全局变量.所有国家列表,
            () => 全局变量.所有城池列表, () => 全局变量.本机身份);
        private const double ExactIntegerLimit = 9007199254740991d;
        private readonly Func<IList<玩家数据>> players;
        private readonly Func<IList<国家信息库类>> nations;
        private readonly Func<IList<城池信息库类>> cities;
        private readonly Func<int> actorIndex;

        private NationBasicActions(Func<IList<玩家数据>> players, Func<IList<国家信息库类>> nations,
            Func<IList<城池信息库类>> cities, Func<int> actorIndex)
        { this.players = players; this.nations = nations; this.cities = cities; this.actorIndex = actorIndex; }

        public 玩家数据 Actor
        {
            get { var all = players(); int index = actorIndex(); return all != null && index >= 0 && index < all.Count ? all[index] : null; }
        }

        public 国家信息库类 FindNation(string code)
        {
            if (string.IsNullOrEmpty(code) || nations() == null) return null;
            foreach (var nation in nations()) if (nation != null && nation.国号 == code) return nation;
            return null;
        }

        public 城池信息库类 FindCity(int x, int y)
        {
            if (cities() != null) foreach (var city in cities())
                if (city != null && city.坐标x == x && city.坐标y == y) return city;
            return null;
        }

        private static bool Valid(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= ExactIntegerLimit; }
        private static NationActionResult Fail(NationError error, string message) { return NationActionResult.Fail(error, message); }
        private NationActionResult ActorReady(int expectedActorId)
        {
            var actor = Actor;
            if (actor == null || actor.基础信息 == null || actor.财产信息 == null)
                return Fail(NationError.NoPlayer, "当前角色数据未就绪，请返回重试。");
            if (expectedActorId >= 0 && actor.基础信息.ID != expectedActorId)
                return Fail(NationError.Forbidden, "当前角色已变化，请返回重新操作。");
            return NationActionResult.Ok("");
        }

        private NationActionResult OwnNation(string code, int expectedActorId, out 国家信息库类 nation)
        {
            nation = null; var ready = ActorReady(expectedActorId); if (!ready.Success) return ready;
            nation = FindNation(code);
            if (nation == null) return Fail(NationError.NoNation, "请先加入国家。");
            if (Actor.基础信息.国家 != code || (nation.国王 != Actor.基础信息.ID &&
                (nation.成员列表 == null || !nation.成员列表.Contains(Actor.基础信息.ID))))
                return Fail(NationError.Forbidden, "只能操作自己所属的国家，请返回国家页。");
            return ready;
        }

        public static bool TryAmount(string text, out double amount)
        {
            amount = 0; text = (text ?? "").Trim(); if (text.Length == 0) return true;
            if (text.Length > 16) return false;
            foreach (char c in text) if (c < '0' || c > '9') return false;
            return double.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out amount) && Valid(amount);
        }

        public NationActionResult Donate(string code, string copperText, string grainText, int expectedActorId)
        {
            国家信息库类 nation; var ready = OwnNation(code, expectedActorId, out nation); if (!ready.Success) return ready;
            double copper, grain;
            if (!TryAmount(copperText, out copper) || !TryAmount(grainText, out grain) || (copper == 0 && grain == 0))
                return Fail(NationError.InvalidInput, "请输入正整数捐献数量；未填写的资源按0计算。");
            var money = Actor.财产信息;
            if (!Valid(money.铜钱) || !Valid(money.粮食) || !Valid(nation.铜钱) || !Valid(nation.粮食) ||
                !Valid(nation.铜钱 + copper) || !Valid(nation.粮食 + grain))
                return Fail(NationError.InvalidBalance, "资源数量异常，未扣除资源。");
            if (money.铜钱 < copper || money.粮食 < grain)
                return Fail(NationError.InvalidBalance, "捐献失败：现有铜钱 " + NationDataSource.Number(money.铜钱) + "，粮食 " + NationDataSource.Number(money.粮食) + "。");
            money.铜钱 -= copper; money.粮食 -= grain; nation.铜钱 += copper; nation.粮食 += grain;
            return NationActionResult.Ok("已捐献铜钱 " + NationDataSource.Number(copper) + "、粮食 " + NationDataSource.Number(grain) + "。");
        }

        public NationTechnologyCost TechnologyCost(string code)
        {
            var nation = FindNation(code); if (nation == null) return null;
            double total = nation.攻击科技 + nation.防御科技 + nation.资源科技;
            if (!Valid(nation.攻击科技) || !Valid(nation.防御科技) || !Valid(nation.资源科技) ||
                nation.攻击科技 != Math.Floor(nation.攻击科技) || nation.防御科技 != Math.Floor(nation.防御科技) ||
                nation.资源科技 != Math.Floor(nation.资源科技) || !Valid(total + 1)) return null;
            double gold, resource;
            if (total == 0) return new NationTechnologyCost { Gold = 5000, Grain = 10000, Copper = 10000 };
            if (total <= 20) { gold = 5000; resource = 10000; }
            else if (total <= 30) { gold = 10000; resource = 15000; }
            else if (total <= 50) { gold = 15000; resource = 20000; }
            else if (total <= 70) { gold = 20000; resource = 30000; }
            else if (total <= 80) { gold = 30000; resource = 50000; }
            else if (total <= 90) { gold = 50000; resource = 80000; }
            else if (total <= 100) { gold = 50000; resource = 90000; }
            else { gold = 200000; resource = 200000; }
            var cost = new NationTechnologyCost { Gold = total * gold, Grain = total * resource, Copper = total * resource };
            return Valid(cost.Gold) && Valid(cost.Grain) && Valid(cost.Copper) ? cost : null;
        }

        public NationActionResult UpgradeTechnology(string code, string kind, int expectedActorId)
        {
            国家信息库类 nation; var ready = OwnNation(code, expectedActorId, out nation); if (!ready.Success) return ready;
            if (kind != "攻击" && kind != "防御" && kind != "资源") return Fail(NationError.InvalidInput, "请选择要升级的科技。");
            var cost = TechnologyCost(code); var money = Actor.财产信息;
            if (cost == null || !Valid(money.黄金) || !Valid(money.粮食) || !Valid(money.铜钱))
                return Fail(NationError.InvalidBalance, "科技或资源数量异常，未扣除资源。");
            if (money.黄金 < cost.Gold || money.粮食 < cost.Grain || money.铜钱 < cost.Copper)
                return Fail(NationError.InvalidBalance, "升级失败，需要" + cost.Description + "。");
            money.黄金 -= cost.Gold; money.粮食 -= cost.Grain; money.铜钱 -= cost.Copper;
            if (kind == "攻击") nation.攻击科技++; else if (kind == "防御") nation.防御科技++; else nation.资源科技++;
            nation.刷新科技信息();
            return NationActionResult.Ok(kind + "科技提升1级，消耗" + cost.Description + "。");
        }

        public bool CanFound
        {
            get
            {
                var actor = Actor; if (actor == null || actor.基础信息 == null) return false;
                if (nations() != null) foreach (var nation in nations()) if (nation != null && nation.国王 == actor.基础信息.ID) return false;
                return true;
            }
        }

        public bool CanUseCapital(城池信息库类 city)
        {
            var actor = Actor;
            if (!CanFound || actor == null || city == null || city.城主 != actor.基础信息.ID || city.规模 < 1 || city.规模 > 3 || city.正在交战) return false;
            if (nations() != null) foreach (var nation in nations())
                if (nation != null && nation.国都x == city.坐标x && nation.国都y == city.坐标y) return false;
            return FindCity(city.坐标x, city.坐标y) == city;
        }

        private NationActionResult MembershipReady(国家信息库类 target, 城池信息库类 capital, bool founding)
        {
            var actor = Actor;
            if (!CanFound) return Fail(NationError.Forbidden, "国王不能换国或再次建国。");
            if (target == null || target.成员列表 == null || capital == null || capital.城池封地列表 == null ||
                actor.封地信息表 == null || cities() == null || nations() == null)
                return Fail(NationError.NoNation, "国家或国都数据未就绪，请返回重试。");
            if (!founding && capital.国家 != target.国号) return Fail(NationError.NoNation, "该国国都已失守，请重新选择国家。");
            foreach (var fief in actor.封地信息表)
                if (fief == null || fief.所在城池 == null || fief.将领信息表 == null || fief.闲兵信息表 == null ||
                    fief.伤兵信息表 == null || fief.俘虏信息表 == null || fief.驻防信息表 == null)
                    return Fail(NationError.InvalidInput, "封地数据异常，未更换国家。");
            if (HasActiveMilitary(actor))
                return Fail(NationError.Forbidden, "尚有出征、驻防或参战部队，请先撤回部队并结束战斗，再换国或建国。");
            // Validate references used by the existing base merge before changing any records.
            for (int i = 1; i < actor.封地信息表.Count; i++)
            {
                foreach (var reference in actor.封地信息表[i].俘虏信息表)
                    if (!ValidGeneralReference(reference)) return Fail(NationError.InvalidInput, "俘虏记录异常，请先处理该封地的俘虏。");
                foreach (var reference in actor.封地信息表[i].驻防信息表)
                    if (!ValidGeneralReference(reference)) return Fail(NationError.InvalidInput, "驻防记录异常，请先召回驻防将领。");
            }
            int arriving = 1;
            foreach (var fief in capital.城池封地列表) if (fief != null && fief.第几个玩家 == actorIndex()) arriving = 0;
            double capacity = founding ? 9000 : capital.获取封地上限();
            if (capital.城池封地列表.Count + arriving > capacity)
                return Fail(NationError.Forbidden, "该国国都封地已满，请选择其他国家。");
            return NationActionResult.Ok("");
        }

        private static bool HasActiveMilitary(玩家数据 actor)
        {
            var owned = new HashSet<将领信息>();
            foreach (var fief in actor.封地信息表)
                foreach (var general in fief.将领信息表)
                {
                    if (general == null) continue;
                    owned.Add(general);
                    if (general.详细信息 != null && (general.详细信息.状态 == 1 || general.详细信息.状态 == 2)) return true;
                }
            // Queues and pending battle reinforcements retain the real roster objects, even
            // before their rendered unit exists. A different player's AI does not block moving.
            if (全局变量.军情列表 != null)
                foreach (var army in 全局变量.军情列表)
                    if (army != null && ContainsOwnedGeneral(army.队列将领列表, owned)) return true;
            if (全局变量.Ai军情列表 != null)
                foreach (var army in 全局变量.Ai军情列表)
                    if (army != null && ContainsOwnedGeneral(army.队列将领列表, owned)) return true;
            if (全局变量.战场列表 != null)
                foreach (var battle in 全局变量.战场列表)
                {
                    if (battle == null || battle.战斗结束) continue;
                    if (ContainsOwnedFormation(battle.攻方要渲染的编队将领列表, owned) ||
                        ContainsOwnedFormation(battle.守方要渲染的编队将领列表, owned)) return true;
                }
            return false;
        }

        private static bool ContainsOwnedGeneral(IEnumerable<将领信息> generals, HashSet<将领信息> owned)
        {
            if (generals != null) foreach (var general in generals) if (general != null && owned.Contains(general)) return true;
            return false;
        }

        private static bool ContainsOwnedFormation(IEnumerable<List<将领信息>> formations, HashSet<将领信息> owned)
        {
            if (formations != null) foreach (var formation in formations) if (ContainsOwnedGeneral(formation, owned)) return true;
            return false;
        }

        private bool ValidGeneralReference(将领索引 reference)
        {
            var all = players(); if (reference == null || reference.第几个玩家 < 0 || reference.第几个玩家 >= all.Count) return false;
            var owner = all[reference.第几个玩家]; if (owner == null || owner.封地信息表 == null) return false;
            foreach (var fief in owner.封地信息表)
            {
                if (fief == null || fief.将领信息表 == null) return false;
                foreach (var general in fief.将领信息表)
                {
                    if (general == null || general.将领属性 == null || general.将领属性.初始属性 == null) return false;
                    if (general.将领属性.初始属性.ID == reference.将领ID标识) return general.详细信息 != null;
                }
            }
            return false;
        }

        // The original switch-country rule retains the base, gathers its units, returns other cities,
        // and clears merit. Use IDs for membership and indexes for the existing fief index structure.
        private void MoveMembership(国家信息库类 target, 城池信息库类 capital, 城池信息库类 foundingCapital = null)
        {
            var actor = Actor; int index = actorIndex(), id = actor.基础信息.ID;
            var old = FindNation(actor.基础信息.国家);
            foreach (var city in cities())
            {
                if (city == null) continue;
                if (city.城池封地列表 != null) city.城池封地列表.RemoveAll(item => item != null && item.第几个玩家 == index);
                if (city != foundingCapital && city.城主 == id)
                { city.城主 = old == null ? -1 : old.国王; city.国家 = old == null ? "" : old.国号; }
            }
            while (actor.封地信息表.Count > 1) actor.删除封地(1);
            actor.封地信息表[0].所在城池 = new 坐标(capital.坐标x, capital.坐标y);
            capital.城池封地列表.Add(new 封地索引(index, actor.封地信息表[0].ID));
            foreach (var nation in nations())
            {
                if (nation == null) continue;
                if (nation.成员列表 != null) nation.成员列表.RemoveAll(member => member == id);
                if (nation.大都督 == id) nation.大都督 = -1;
                if (nation.丞相 == id) nation.丞相 = -1;
                if (nation.奋武将军 == id) nation.奋武将军 = -1;
                if (nation.征东将军 == id) nation.征东将军 = -1;
                if (nation.都尉 == id) nation.都尉 = -1;
                if (nation.侍郎 == id) nation.侍郎 = -1;
            }
            target.成员列表.Add(id); actor.基础信息.国家 = target.国号;
            actor.基础信息.战功 = 0; actor.基础信息.官职 = 官职信息.平民; actor.基础信息.官阶 = "平民";
            if (old != null) old.获取国家城池列表(); target.获取国家城池列表();
        }

        public NationActionResult Join(string code, int expectedActorId)
        {
            var ready = ActorReady(expectedActorId); if (!ready.Success) return ready;
            var nation = FindNation(code); if (nation == null) return Fail(NationError.NoNation, "所选国家已不存在，请刷新列表。");
            if (Actor.基础信息.国家 == code) return Fail(NationError.InvalidInput, "已在本国。");
            var capital = FindCity(nation.国都x, nation.国都y);
            ready = MembershipReady(nation, capital, false); if (!ready.Success) return ready;
            if (Actor.封地信息表.Count == 0 && !capital.新建封地(actorIndex()))
                return Fail(NationError.Forbidden, "无法在该国国都建立基地，未更换国家。");
            MoveMembership(nation, capital);
            return NationActionResult.Ok("已加入" + nation.国名 + "（" + nation.国号 + "）。");
        }

        private static bool ValidText(string value, int limit)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit) return false;
            foreach (char c in value) if (char.IsControl(c)) return false;
            return true;
        }

        public NationActionResult Found(string name, string code, string declaration, int x, int y, int expectedActorId)
        {
            var ready = ActorReady(expectedActorId); if (!ready.Success) return ready;
            if (nations() == null) return Fail(NationError.NoNation, "国家数据未就绪，请返回重试。");
            name = (name ?? "").Trim(); code = (code ?? "").Trim(); declaration = (declaration ?? "").Trim();
            if (!ValidText(name, 16) || name.Length < 3) return Fail(NationError.InvalidInput, "国名须为3至16个字。");
            if (!ValidText(code, 1) || !((code[0] >= '\u3400' && code[0] <= '\u9fff') || (code[0] >= '\uf900' && code[0] <= '\ufaff')))
                return Fail(NationError.InvalidInput, "国号须为1个汉字。");
            if (string.IsNullOrWhiteSpace(declaration) || declaration.Length > 100)
                return Fail(NationError.InvalidInput, "请填写100字以内的建国宣言。");
            foreach (char c in declaration) if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                return Fail(NationError.InvalidInput, "建国宣言包含无效字符。");
            foreach (var nation in nations()) if (nation != null && (nation.国号 == code || nation.国名 == name))
                return Fail(NationError.InvalidInput, "国名或国号已被使用，请重新输入。");
            var capital = FindCity(x, y); if (!CanUseCapital(capital))
                return Fail(NationError.Forbidden, "请选择自己拥有、未交战的县城以上城池；现有国都不能用于建国。");
            var created = new 国家信息库类 { 国名 = name, 国号 = code, 国都x = x, 国都y = y, 宣言 = declaration };
            var previousNation = FindNation(Actor.基础信息.国家);
            ready = MembershipReady(created, capital, true); if (!ready.Success) return ready;
            var money = Actor.财产信息; var bag = Actor.背包道具列表;
            if (!Valid(money.铜钱) || money.铜钱 < 10000000 || bag == null)
                return Fail(NationError.InvalidBalance, "建国需要1000万铜钱、玉玺1、印绶100、虎符10、令牌1000。");
            string[] materials = { "玉玺", "印绶", "虎符", "令牌" }; int[] amounts = { 1, 100, 10, 1000 };
            var stacks = new List<道具信息>[materials.Length]; var copies = new List<道具信息>[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                stacks[i] = bag.获取道具分类列表(materials[i]); if (stacks[i] == null)
                    return Fail(NationError.InvalidBalance, materials[i] + "不足，需要" + amounts[i] + "。");
                double available = 0; copies[i] = new List<道具信息>();
                foreach (var item in stacks[i])
                {
                    if (item == null || !Valid(item.数量) || item.数量 != Math.Floor(item.数量) || item.数量 > int.MaxValue)
                        return Fail(NationError.InvalidBalance, "材料数量异常，未扣除资源。");
                    copies[i].Add(new 道具信息(item.名字, item.数量) { ID = item.ID });
                    if (item.名字 == materials[i]) available += item.数量;
                }
                if (available < amounts[i]) return Fail(NationError.InvalidBalance, materials[i] + "不足，需要" + amounts[i] + "。");
            }
            for (int i = 0; i < materials.Length; i++) if (!bag.扣除道具(materials[i], amounts[i]))
            {
                RestoreMaterials(stacks, copies); return Fail(NationError.InvalidBalance, "材料不足，建国取消，已退还材料。");
            }
            int previousScale = capital.规模; capital.规模 = 4;
            if (Actor.封地信息表.Count == 0 && !capital.新建封地(actorIndex()))
            {
                capital.规模 = previousScale; RestoreMaterials(stacks, copies);
                return Fail(NationError.Forbidden, "无法在所选国都建立基地，未扣除资源。");
            }
            money.铜钱 -= 10000000;
            created.初始化国家(name, code, x, y); created.国王 = Actor.基础信息.ID;
            nations().Add(created); MoveMembership(created, capital, capital);
            capital.城主 = Actor.基础信息.ID; capital.国家 = code; capital.生成指定规模城池数据(4); created.获取国家城池列表();
            if (previousNation != null) previousNation.获取国家城池列表();
            Actor.基础信息.官阶 = "国王"; Actor.基础信息.官职 = 官职信息.国王;
            return NationActionResult.Ok("建国成功：" + name + "（" + code + "），国都" + capital.名称 + "。");
        }

        private static void RestoreMaterials(List<道具信息>[] stacks, List<道具信息>[] copies)
        {
            for (int i = 0; i < stacks.Length; i++) { stacks[i].Clear(); stacks[i].AddRange(copies[i]); }
        }
    }
}
