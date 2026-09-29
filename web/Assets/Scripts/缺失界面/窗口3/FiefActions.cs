using System;
using System.Linq;
using 玩家数据结构;

namespace Dwsg.Window3
{
    // All callers use the current local player; quotes never authorize a later spend.
    public static class FiefActions
    {
        public static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0; }
        public static double Whole(double value) { return Finite(value) ? Math.Min(16777215, Math.Floor(value)) : 0; }
        public static 玩家数据 Player(int index) { return index == 全局变量.本机身份 ? CityLocalAdapter.Player(index) : null; }
        public static 封地信息 Fief(int player, int index)
        {
            var p = Player(player);
            return p != null && index >= 0 && index < p.封地信息表.Count ? p.封地信息表[index] : null;
        }
        public static 建筑信息 Building(int player, int fief, int index)
        {
            var f = Fief(player, fief);
            return f != null && index >= 0 && index < f.建筑信息表.Count ? f.建筑信息表[index] : null;
        }
        public static double BuildingCost(int level, double basis) { return basis * Math.Pow(2, level) * 200; }
        public static string UpgradeError(封地信息 f, int index)
        {
            if (f == null || index < 0 || index >= f.建筑信息表.Count) return "请重新选择封地和建筑。";
            var b = f.建筑信息表[index];
            if (b.类型 < 0 || b.类型 > 7 || b.等级 < 1) return "请先在空地上建造建筑。";
            int cap = b.类型 >= 4 || f.ID != 1 ? 10 : 15;
            if (b.等级 >= cap) return "建筑已达" + cap + "级上限。";
            if (index != 0 && (f.建筑信息表[0].类型 != 0 || b.等级 >= f.建筑信息表[0].等级)) return "请先升级大厅，再升级此建筑。";
            return null;
        }
        private static void UpdateFarmIncome(玩家数据 p)
        {
            p.基础信息.粮食增加 = p.封地信息表.Sum(f => f.建筑信息表.Where(b => b.类型 == 3).Sum(b => (double)Math.Max(0, b.等级)));
        }
        public static CityResult Build(int player, int fief, int index, int type)
        {
            var f = Fief(player, fief); var b = Building(player, fief, index);
            if (b == null || index == 0 || type < 1 || type > 7) return CityResult.Fail("请重新选择空地和建筑。" );
            if (b.类型 != -1) return CityResult.Fail("这里已有建筑，请选择空地。" );
            if (type == 1 && f.获取书院等级() >= 0) return CityResult.Fail("每座封地只能建一座书院。" );
            f.建造建筑(index, type);
            if (b.类型 != type || b.等级 != 1) return CityResult.Fail("建造失败，请重新选择空地。" );
            UpdateFarmIncome(Player(player));
            return CityResult.Ok(b.获取建筑等级文本() + "建造完成。" );
        }
        public static CityResult Upgrade(int player, int fief, int index)
        {
            var p = Player(player); var f = Fief(player, fief);
            string error = UpgradeError(f, index); if (error != null) return CityResult.Fail(error);
            var b = f.建筑信息表[index]; double copper = BuildingCost(b.等级, 2), food = BuildingCost(b.等级, 4);
            if (!Finite(p.财产信息.铜钱) || !Finite(p.财产信息.粮食) || p.财产信息.铜钱 < copper || p.财产信息.粮食 < food)
                return CityResult.Fail("资源不足，需要铜钱" + copper.ToString("0") + "、粮食" + food.ToString("0") + "。" );
            if (!f.升级建筑(index)) return CityResult.Fail("等级条件已变化，请重新选择建筑。" );
            p.财产信息.铜钱 -= copper; p.财产信息.粮食 -= food; UpdateFarmIncome(p);
            return CityResult.Ok(b.获取建筑等级文本() + "升级完成。\n消耗铜钱" + copper.ToString("0") + "、粮食" + food.ToString("0") + "。" );
        }
        public static CityResult Demolish(int player, int fief, int index)
        {
            var b = Building(player, fief, index);
            if (b == null || b.类型 < 0) return CityResult.Fail("请先选择建筑。" );
            if (index == 0 || b.类型 == 0) return CityResult.Fail("大厅不能拆除。" );
            b.类型 = -1; b.等级 = 0; UpdateFarmIncome(Player(player));
            return CityResult.Ok("建筑已拆除，可以重新建造。" );
        }
        public static string RecruitmentError(int player, int fief, int building, int unit)
        {
            var b = Building(player, fief, building);
            int tier = unit % 100; int type = unit / 100 + 3;
            if (b == null || b.类型 < 4 || b.类型 > 7 || b.类型 != type || tier < 1 || tier > 4) return "兵营已变化，请重新选择兵种。";
            if (b.等级 < (tier - 1) * 3 + 1) return "该兵种需要" + ((tier - 1) * 3 + 1) + "级兵营。";
            var u = 全局兵种库.查询指定ID的数据(unit);
            return u == null || !Finite(u.需要铜钱) || !Finite(u.需要粮食) || !Finite(u.占用人口) || u.需要铜钱 <= 0 || u.需要粮食 <= 0 || u.占用人口 <= 0 ? "兵种资料无效。" : null;
        }
        public static double RecruitLimit(int player, int fief, int building, int unit)
        {
            if (RecruitmentError(player, fief, building, unit) != null) return 0;
            var p = Player(player); var u = 全局兵种库.查询指定ID的数据(unit);
            return Whole(Math.Min(Math.Min(p.财产信息.铜钱 / u.需要铜钱, p.财产信息.粮食 / u.需要粮食), (p.获取人口上限() - p.获取已占用人口()) / u.占用人口));
        }
        public static CityResult Recruit(int player, int fief, int building, int unit, double amount)
        {
            string error = RecruitmentError(player, fief, building, unit); if (error != null) return CityResult.Fail(error);
            if (amount <= 0 || amount != Whole(amount)) return CityResult.Fail("请输入正整数数量。" );
            if (amount > RecruitLimit(player, fief, building, unit)) return CityResult.Fail("资源或人口不足，请减少招募数量。" );
            var p = Player(player); var u = 全局兵种库.查询指定ID的数据(unit); var f = Fief(player, fief);
            double current = f.闲兵信息表.Where(x => x.ID == unit).Sum(x => x.数量);
            if (!Finite(current + amount)) return CityResult.Fail("兵员数量无效。" );
            p.财产信息.铜钱 -= u.需要铜钱 * amount; p.财产信息.粮食 -= u.需要粮食 * amount;
            f.添加闲兵(unit, amount);
            return CityResult.Ok("已招募" + u.名称 + " " + amount.ToString("0") + "，进入本封地闲兵。" );
        }
        public static double HealLimit(int player, int fief, int unit)
        {
            var p = Player(player); var f = Fief(player, fief); var u = 全局兵种库.查询指定ID的数据(unit);
            if (p == null || f == null || u == null || u.需要铜钱 <= 0 || u.需要粮食 <= 0) return 0;
            var wounded = f.伤兵信息表.FirstOrDefault(x => x.ID == unit);
            return wounded == null ? 0 : Whole(Math.Min(wounded.数量, Math.Min(p.财产信息.铜钱 / (u.需要铜钱 / 2), p.财产信息.粮食 / (u.需要粮食 / 2))));
        }
        public static CityResult Heal(int player, int fief, int unit, double amount)
        {
            if (amount <= 0 || amount != Whole(amount) || amount > HealLimit(player, fief, unit)) return CityResult.Fail("伤兵或资源不足，请重新选择治疗数量。" );
            var p = Player(player); var f = Fief(player, fief); var u = 全局兵种库.查询指定ID的数据(unit);
            if (!Finite(f.闲兵信息表.Where(x => x.ID == unit).Sum(x => x.数量) + amount)) return CityResult.Fail("兵员数量无效。" );
            p.财产信息.铜钱 -= amount * u.需要铜钱 / 2; p.财产信息.粮食 -= amount * u.需要粮食 / 2;
            f.删除伤兵(unit, amount); f.添加闲兵(unit, amount);
            return CityResult.Ok("已治疗" + u.名称 + " " + amount.ToString("0") + "，返回本封地闲兵。" );
        }
        public static double TradeRate(int player, int type)
        {
            var p = Player(player); if (p == null) return 0;
            if (type == 5) return p.基础信息.等级 * 10.0;
            if (type == 6) return p.基础信息.等级 * 30.0;
            return type == 7 ? 2.5 : type == 8 ? .3 : 0;
        }
        public static double TradeBalance(int player, int type)
        {
            var p = Player(player); if (p == null) return 0;
            return type == 5 || type == 6 ? p.财产信息.黄金 : type == 7 ? p.财产信息.铜钱 : type == 8 ? p.财产信息.粮食 : 0;
        }
        public static double TradeCost(int player, int type, double output)
        {
            double rate = TradeRate(player, type);
            return Finite(rate) && rate > 0 && Finite(output) ? Math.Ceiling(output / rate) : double.PositiveInfinity;
        }
        public static double TradeLimit(int player, int type) { return Whole(Math.Floor(TradeBalance(player, type)) * TradeRate(player, type)); }
        public static CityResult Trade(int player, int type, double output)
        {
            var p = Player(player); if (p == null) return CityResult.Fail("角色已变化，请重新打开市场。" );
            double cost = TradeCost(player, type, output), balance = TradeBalance(player, type);
            if (type < 5 || type > 8 || output <= 0 || output != Whole(output) || !Finite(cost) || cost <= 0 || !Finite(balance) || balance < cost)
                return CityResult.Fail("资源不足或数量无效，请重新选择交易数量。" );
            double target = type == 5 || type == 8 ? p.财产信息.铜钱 : p.财产信息.粮食;
            if (!Finite(target + output)) return CityResult.Fail("资源数量无效。" );
            if (type == 5 || type == 6) p.财产信息.黄金 -= cost;
            else if (type == 7) p.财产信息.铜钱 -= cost;
            else p.财产信息.粮食 -= cost;
            if (type == 5 || type == 8) p.财产信息.铜钱 += output; else p.财产信息.粮食 += output;
            return CityResult.Ok("交易完成，获得" + (type == 5 || type == 8 ? "铜钱" : "粮食") + output.ToString("0") + "，消耗" + (type == 5 || type == 6 ? "黄金" : type == 7 ? "铜钱" : "粮食") + cost.ToString("0") + "。" );
        }

        public static readonly string[] ResearchNames = { "工程设计", "征召技巧", "种植技术", "行军技巧", "市场贸易", "建筑学", "铸铁技术", "甲胄制造", "药草研究", "阵法技巧", "抛射技巧", "驾驭技巧", "战车设计", "统帅能力", "信仰", "仓储", "安置", "格斗", "精准", "驯马", "精工" };
        private static readonly double[] ResearchBases = { 2, 2, .24, 2.2, .24, .24, 2.8, 2.5, 2.5, 2, 2, 2, 2, 3, .4, 5, 5, 5, 5, 5, 5 };
        public static double ResearchLevel(科技信息 tech, int index)
        {
            if (tech == null) return double.NaN;
            switch (index)
            {
                case 0: return tech.工程设计;
                case 1: return tech.征召技巧;
                case 2: return tech.种植技术;
                case 3: return tech.行军技巧;
                case 4: return tech.市场贸易;
                case 5: return tech.建筑学;
                case 6: return tech.铸铁技术;
                case 7: return tech.甲胄制造;
                case 8: return tech.药草研究;
                case 9: return tech.阵法技巧;
                case 10: return tech.抛射技巧;
                case 11: return tech.驾驭技巧;
                case 12: return tech.战车设计;
                case 13: return tech.统帅能力;
                case 14: return tech.信仰;
                case 15: return tech.仓储;
                case 16: return tech.安置;
                case 17: return tech.格斗;
                case 18: return tech.精准;
                case 19: return tech.驯马;
                case 20: return tech.精工;
                default: return double.NaN;
            }
        }
        private static void SetResearchLevel(科技信息 tech, int index, double level)
        {
            switch (index)
            {
                case 0: tech.工程设计 = level; break;
                case 1: tech.征召技巧 = level; break;
                case 2: tech.种植技术 = level; break;
                case 3: tech.行军技巧 = level; break;
                case 4: tech.市场贸易 = level; break;
                case 5: tech.建筑学 = level; break;
                case 6: tech.铸铁技术 = level; break;
                case 7: tech.甲胄制造 = level; break;
                case 8: tech.药草研究 = level; break;
                case 9: tech.阵法技巧 = level; break;
                case 10: tech.抛射技巧 = level; break;
                case 11: tech.驾驭技巧 = level; break;
                case 12: tech.战车设计 = level; break;
                case 13: tech.统帅能力 = level; break;
                case 14: tech.信仰 = level; break;
                case 15: tech.仓储 = level; break;
                case 16: tech.安置 = level; break;
                case 17: tech.格斗 = level; break;
                case 18: tech.精准 = level; break;
                case 19: tech.驯马 = level; break;
                case 20: tech.精工 = level; break;
            }
        }
        public static int ResearchCap(int index) { return index == 0 ? 15 : index >= 15 ? 5 : 10; }
        public static double ResearchCost(int index, double level)
        {
            return index >= 0 && index < ResearchBases.Length && Finite(level) ? Math.Min(15000000, ResearchBases[index] * Math.Pow(2, level) * 10000) : double.PositiveInfinity;
        }
        public static int ResearchRequirement(int index, double level) { return (int)level + (index >= 15 ? 11 : 1); }
        public static string ResearchError(int player, int fief, int building, int index)
        {
            var p = Player(player); var b = Building(player, fief, building);
            if (p == null || b == null || b.类型 != 1 || index < 0 || index >= ResearchNames.Length) return "请重新选择书院和科技。";
            double level = ResearchLevel(p.科技信息, index);
            if (!Finite(level) || level != Math.Floor(level)) return "科技等级无效。";
            if (level >= ResearchCap(index)) return "科技已满级。";
            if (b.等级 < ResearchRequirement(index, level)) return "需要书院" + ResearchRequirement(index, level) + "级，请先升级书院。";
            double balance = index >= 15 ? p.财产信息.黄金 : p.财产信息.铜钱;
            double cost = ResearchCost(index, level);
            return !Finite(balance) || balance < cost ? (index >= 15 ? "黄金" : "铜钱") + "不足，需要" + cost.ToString("0") + "。" : null;
        }
        public static CityResult Research(int player, int fief, int building, int index)
        {
            string error = ResearchError(player, fief, building, index); if (error != null) return CityResult.Fail(error);
            var p = Player(player); double level = ResearchLevel(p.科技信息, index), cost = ResearchCost(index, level);
            if (index >= 15) p.财产信息.黄金 -= cost; else p.财产信息.铜钱 -= cost;
            SetResearchLevel(p.科技信息, index, level + 1);
            return CityResult.Ok(ResearchNames[index] + "升至" + (level + 1).ToString("0") + "级，消耗" + (index >= 15 ? "黄金" : "铜钱") + cost.ToString("0") + "。" );
        }
    }
}
