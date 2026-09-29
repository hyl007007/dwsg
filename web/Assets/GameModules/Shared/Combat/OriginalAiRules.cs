using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Combat
{
    public static class OriginalAiRules
    {
        public const int MinimumIntervalSeconds = 120, MaximumIntervalSeconds = 300;
        public const int StaggerSeconds = 10, ArrivalSeconds = 30;
        public const int NearbyDistance = 4, MaximumPathAttempts = 12, MaximumReachable = 5;

        // 全局任务脚本.AI推城: 城池列表顺序、首个邻近起点、同距离先到者、12次寻路/5个可达。
        public static int SelectTarget(JArray cities, JObject nation, Func<int, int, int, int, int> find,
            Func<int, int, int> random)
        {
            var indices = new List<int>();
            var distances = new List<int>();
            var origins = new List<JObject>();
            // 原获取国家城池列表按城池原表顺序刷新；直接读当前权威表，避免导出时的空缓存。
            JObject[] ownCities = cities.OfType<JObject>().Where(city => city.Value<string>("国家") == nation.Value<string>("国号")).ToArray();
            for (int index = 0; index < cities.Count; index++)
            {
                JObject city = (JObject)cities[index];
                if (city.Value<int>("规模") >= 8 || city.Value<string>("国家") == nation.Value<string>("国号")
                    || city.Value<int>("城主") == nation.Value<int>("国王") || city.Value<bool>("正在交战")) continue;
                foreach (JObject origin in ownCities)
                {
                    int distance = Math.Max(Math.Abs(city.Value<int>("坐标x") - origin.Value<int>("坐标x")),
                        Math.Abs(city.Value<int>("坐标y") - origin.Value<int>("坐标y")));
                    if (distance > NearbyDistance) continue;
                    indices.Add(index); distances.Add(distance); origins.Add(origin); break;
                }
            }
            var reachable = new List<int>();
            for (int attempt = 0; attempt < MaximumPathAttempts && reachable.Count < MaximumReachable; attempt++)
            {
                int nearest = -1, distance = 999;
                for (int index = 0; index < indices.Count; index++)
                    if (distances[index] >= 0 && distances[index] < distance) { nearest = index; distance = distances[index]; }
                if (nearest < 0) break;
                distances[nearest] = -1;
                JObject city = (JObject)cities[indices[nearest]], origin = origins[nearest];
                int cost = find(origin.Value<int>("坐标x") - 1, origin.Value<int>("坐标y") - 1,
                    city.Value<int>("坐标x") - 1, city.Value<int>("坐标y") - 1);
                if (cost > 0 && cost < 999) reachable.Add(indices[nearest]);
            }
            return reachable.Count == 0 ? -1 : reachable[random(0, reachable.Count)];
        }

        public static int GeneralCount(int scale) => Math.Max(5, scale * 20);

        // 原生成99级临时将领后覆盖带兵；404机械兵再截断到原1/2.5。
        public static int TroopCount(int scale, double difficulty, double referenceLeadership, int troopId,
            Func<int, int, int> random)
        {
            int count = (int)((scale + difficulty + referenceLeadership / 20) * 2500 + random(scale * 100, scale * 500));
            return troopId == 404 ? (int)(count / 2.5) : count;
        }

        // Ai军情检测的新战场分支；规模1/2与现有玩家分支相同，3/4保持各自原数值。
        public static List<JObject> CreateMilitia(int scale, string referenceName, Func<int, int, int> random, Func<int, JObject> create)
        {
            if (scale == 1 || scale == 2) return CityGarrisonRules.CreateMilitia(scale, referenceName, random, create);
            var generals = new List<JObject>();
            if (scale != 3 && (scale != 4 || referenceName == "9977886")) return generals;
            int count = scale == 3 ? random(20, 35) : random(10, 30);
            for (int index = 0; index < count; index++) generals.Add(create(scale == 3 ? random(70, 80) : random(90, 99)));
            return generals;
        }
    }
}
