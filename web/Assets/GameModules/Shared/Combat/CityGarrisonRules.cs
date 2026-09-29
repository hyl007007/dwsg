using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Combat
{
    public static class CityGarrisonRules
    {
        private static readonly int[,] Counts = { { 20, 30 }, { 30, 40 }, { 40, 60 }, { 100, 150 }, { 200, 230 } };
        private static readonly int[,] Levels = { { 20, 30 }, { 30, 50 }, { 60, 70 }, { 70, 80 }, { 90, 99 } };

        public static List<JObject> CreateMilitia(int scale, string attackerName, Func<int, int, int> random, Func<int, JObject> create)
        {
            if (scale < 0 || scale > 4) throw new InvalidOperationException("原城池规模无效");
            var generals = new List<JObject>();
            if (scale == 4 && attackerName == "997788") return generals;
            int count = random(Counts[scale, 0], Counts[scale, 1]);
            for (int index = 0; index < count; index++) generals.Add(create(random(Levels[scale, 0], Levels[scale, 1])));
            return generals;
        }

        public static JObject CreateMilitiaGeneral(int level, JObject player, JArray configurations, JObject names,
            double difficulty, long utcSeconds, Func<int, int, int> random)
        {
            double leadership = level > 90 ? (difficulty == 4 ? 65 : difficulty == 3 ? 45 : difficulty == 2 ? 35 : 25)
                : level > 50 ? (difficulty == 4 ? 55 : difficulty == 3 ? 45 : difficulty == 2 ? 35 : 25)
                : difficulty == 2 ? 15 : difficulty == 3 ? 25 : difficulty == 4 ? 35 : 5;
            player["科技信息"]["统帅能力"] = leadership;
            int configurationId = random(1, 7);
            JObject configuration = configurations.OfType<JObject>().Single(item => item.Value<int>("ID") == configurationId);
            JObject general = GeneralCreationRules.CreateBanditGeneral(configuration, random);
            general["将领属性"]["初始属性"]["名字"] = BanditGenerator.GenerateName(names, random);
            GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(level));
            general["详细信息"]["坑位颜色"] = 1.0;
            JArray ownerGenerals = (JArray)player["封地信息表"][0]["将领信息表"];
            int originalCount = ownerGenerals.Count;
            ownerGenerals.Add(general);
            GeneralAttributeRules.Recalculate(player, utcSeconds);
            // JObject属于ownerGenerals时移除它，再作为临时守军返回；原NPC真将领的重算也保留。
            general = (JObject)ownerGenerals[originalCount]; ownerGenerals.RemoveAt(originalCount);
            int troopClass = 100 * random(1, 5);
            random(1, 5); // 原方法的第一次阶级抽签随后被覆盖，仍消耗随机流。
            int grade = level <= 50 ? random(1, 4) : 4;
            if (troopClass == 400 && grade == 3) grade = 4;
            general["将领配兵"]["ID"] = (double)(troopClass + grade);
            general["将领配兵"]["数量"] = general["将领属性"]["最终属性"]["统兵"].DeepClone();
            player["科技信息"]["统帅能力"] = 8.0;
            return general;
        }

        public static JObject SelectNamedGuard(JArray generals, int scale, Func<int, int, int> random)
        {
            var eligible = new List<JObject>();
            foreach (JObject general in generals.OfType<JObject>())
            {
                if (general["详细信息"].Value<double>("状态") != 0) continue;
                string series = general["将领属性"]["初始属性"].Value<string>("系列");
                if (series != "名将" && scale < 3) continue;
                if ((series == "君王" || series == "尊将" || series == "战将" || series == "禧将") && scale < 4) continue;
                // 原方法在抽选前给每个合格候选满配104，非选中者也会变化。
                EquipNamedGuard(general);
                eligible.Add(general);
            }
            return eligible.Count == 0 ? null : eligible[random(0, eligible.Count)];
        }

        public static void EquipNamedGuard(JObject general)
        {
            general["将领配兵"]["ID"] = 104.0;
            general["将领配兵"]["数量"] = general["将领属性"]["最终属性"]["统兵"].DeepClone();
        }

        public static long[] FormationAvailability(int count, long arrivalUtcMs, Func<int, int, int> random)
        {
            int full = count / 5;
            var available = new long[full + (count % 5 == 0 ? 0 : 1)];
            for (int group = 0; group < full; group++)
                available[group] = group < 8 ? arrivalUtcMs : (arrivalUtcMs / 1000 + random(5, 20) + 1) * 1000;
            if (count % 5 != 0) available[full] = arrivalUtcMs;
            return available;
        }

        public static double WallMaximum(int scale) => scale == 0 ? 200000 : scale == 1 ? 400000 : scale == 2 ? 800000 : scale == 3 ? 1600000 : scale == 4 ? 2000000 : 1;
        public static double WarReward(int scale) => scale == 0 ? 500 : scale == 1 ? 1000 : scale == 2 ? 1500 : scale == 3 ? 2000 : scale == 4 ? 10000 : 1;
        public static double WallDamage(double troopAttack, double remaining) => Math.Round((float)(troopAttack / 10.0), MidpointRounding.ToEven) * remaining;
        public static int CaptureThreshold(double baseChance, double stateBonus, double breakout) => (int)baseChance + (int)stateBonus + (int)(100.0 - breakout);
        public static int Rank(double war) => (int)war >= 15000 ? 6 : (int)war >= 8000 ? 5 : (int)war >= 7000 ? 4 : (int)war >= 5000 ? 3 : (int)war >= 3000 ? 2 : (int)war >= 2000 ? 1 : 0;
    }
}
