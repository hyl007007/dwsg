using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Combat
{
    public static class BanditGenerator
    {
        // 原随机生成山贼的半开区间；等级7的将领为16..18，低级山贼固定民兵。
        private static readonly int[,] GeneralLevels = { { 1, 4 }, { 1, 4 }, { 5, 9 }, { 8, 11 }, { 10, 14 }, { 13, 16 }, { 16, 19 }, { 22, 25 }, { 26, 29 }, { 36, 39 } };
        private static readonly int[,] Minimum = {
            { 0, 140, 0, 0 }, { 0, 140, 0, 0 }, { 0, 700, 0, 0 },
            { 350, 1100, 350, 0 }, { 350, 700, 350, 150 }, { 500, 1000, 700, 300 },
            { 800, 1400, 1000, 500 }, { 600, 1100, 900, 300 }, { 900, 1500, 1100, 500 }, { 1500, 1800, 1800, 0 } };
        private static readonly int[,] Maximum = {
            { 0, 400, 0, 0 }, { 0, 400, 0, 0 }, { 0, 900, 0, 0 },
            { 450, 1300, 450, 0 }, { 450, 900, 450, 250 }, { 700, 1200, 800, 400 },
            { 1000, 1900, 1300, 600 }, { 900, 1300, 1100, 400 }, { 1100, 1800, 1400, 600 }, { 1700, 2300, 2000, 0 } };

        public static JObject Generate(int x, int y, Func<int, int, int> random,
            Func<int, JObject> createGeneral, Action<JObject, int> grantLevelExperience)
        {
            int level = random(1, 11);
            int drop = random(0, 1000);
            var generals = new JArray();
            var bandit = new JObject { ["坐标x"] = x, ["坐标y"] = y, ["等级"] = (double)level,
                ["掉落宝物"] = drop <= 40 * level ? 1.0 : 0.0,
                ["掉落宝箱"] = drop <= 25 * level ? 1.0 : 0.0,
                ["掉落装备"] = drop <= 10 * level ? 1.0 : 0.0, ["将领数据列表"] = generals };
            int count = level <= 3 ? 1 : level <= 5 ? 2 : level <= 7 ? 3 : level <= 9 ? 4 : 5;
            for (int i = 0; i < count; i++)
            {
                JObject general = createGeneral(random(1, 5));
                grantLevelExperience(general, random(GeneralLevels[level - 1, 0], GeneralLevels[level - 1, 1]));
                int troopClass = level <= 2 ? random(2, 3) : level == 3 ? 2 : random(1, level == 4 ? 4 : 5);
                int troopId = troopClass * 100 + (level <= 4 ? 1 : level <= 7 ? 2 : level <= 9 ? 3 : 4);
                int quantity;
                if (troopClass == 4 && level == 10)
                {
                    bool elephant = random(0, 100) < 50;
                    troopId = elephant ? 404 : 402;
                    quantity = elephant ? random(230, 450) : random(700, 800);
                }
                else
                {
                    if (troopClass == 4) troopId = level <= 7 ? 401 : 402;
                    quantity = random(Minimum[level - 1, troopClass - 1], Maximum[level - 1, troopClass - 1]);
                }
                general["将领配兵"] = new JObject { ["ID"] = (double)troopId, ["数量"] = (double)quantity };
                generals.Add(general);
            }
            return bandit;
        }

        public static string GenerateName(JObject names, Func<int, int, int> random)
        {
            var surnames = (JArray)names["姓"];
            var male = (JArray)names["男名"];
            var female = (JArray)names["女名"];
            string name = surnames[random(0, surnames.Count)].Value<string>();
            int length = random(1, 3);
            int gender = random(1, 3);
            JArray given = gender != 0 ? female : male;
            for (int i = 0; i < length; i++) name += given[random(0, given.Count)].Value<string>();
            return name;
        }
    }
}
