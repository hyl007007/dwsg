using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Combat
{
    public static class CombatModifiers
    {
        public static double AttackTechnology(double iron, double cavalry, double archer, int career)
        {
            return iron * 4.0 + (career == 1 ? cavalry * 3.0 : career == 4 ? archer * 6.0 : 0.0);
        }
        public static double DefenseTechnology(double armor, double cavalry, double infantry, int career)
        {
            return armor * 3.0 + (career == 1 ? cavalry * 3.0 : career == 2 ? infantry * 5.0 : 0.0);
        }
        public static double LifeTechnology(double herb) { return herb * 5.0; }
        public static double SpeedTechnology(double machine, int troopClass) { return troopClass == 4 ? machine * 3.0 : 0.0; }
        public static double TitleBonus(string title, string attribute)
        {
            if (attribute == "攻击")
                return title == "学士" || title == "达人" ? 5.0 : title == "宗师" ? 20.0 : title == "先锋" || title == "财主" || title == "神工" || title == "霸主" || title == "天子" || title == "暴君" || title == "君王" || title == "帝王" ? 10.0 : 0.0;
            if (attribute == "防御")
                return title == "学士" || title == "达人" ? 5.0 : title == "护军" || title == "劳模" || title == "神工" || title == "霸主" || title == "天子" || title == "君王" || title == "帝王" ? 10.0 : 0.0;
            if (attribute == "生命")
                return title == "达人" ? 5.0 : title == "无双" ? 20.0 : title == "猛将" || title == "名士" || title == "天子" || title == "君王" || title == "帝王" ? 10.0 : 0.0;
            if (attribute == "攻速")
                return title == "飞将" ? 20.0 : title == "达人" ? 5.0 : title == "破军" || title == "地主" || title == "天子" || title == "暴君" || title == "君王" || title == "帝王" ? 10.0 : 0.0;
            throw new ArgumentOutOfRangeException(nameof(attribute));
        }

        public static CombatProfile Create(JObject player, JObject country, double difficulty, long utc,
            Func<JObject, string, long, double> activeBonus)
        {
            var technology = (JObject)player["科技信息"];
            var basic = (JObject)player["基础信息"];
            string title = basic.Value<string>("称号名");
            bool wild = basic.Value<string>("国家") == "野";
            return new CombatProfile {
                AttackTechnology = technology.Value<double>("铸铁技术"), CavalryTechnology = technology.Value<double>("驾驭技巧"),
                DefenseTechnology = technology.Value<double>("甲胄制造"), InfantryTechnology = technology.Value<double>("阵法技巧"),
                ArcherTechnology = technology.Value<double>("抛射技巧"), LifeTechnology = technology.Value<double>("药草研究"),
                MachineTechnology = technology.Value<double>("战车设计"), BlockTechnology = technology.Value<double>("格斗"),
                PierceTechnology = technology.Value<double>("精准"), DodgeTechnology = technology.Value<double>("驯马"),
                NationAttack = wild ? difficulty * 10.0 : country == null ? 1.0 : country.Value<double>("攻击科技"),
                NationDefense = wild ? difficulty * 10.0 : country == null ? 1.0 : country.Value<double>("防御科技"),
                AttackState = activeBonus(player, "攻击", utc), DefenseState = activeBonus(player, "防御", utc),
                SpeedState = activeBonus(player, "攻击速度", utc), ExperienceState = activeBonus(player, "将领经验", utc),
                AttackTitle = TitleBonus(title, "攻击"), DefenseTitle = TitleBonus(title, "防御"),
                LifeTitle = TitleBonus(title, "生命"), SpeedTitle = TitleBonus(title, "攻速"), Emperor = title == "帝王" };
        }
    }
}
