using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dwsg.Shared.Combat;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using 玩家数据结构;

public static class 城池同源回归检查
{
    private static int checks;
    public static void Run()
    {
        bool logging = Debug.unityLogger.logEnabled;
        GameObject root = null;
        try
        {
            string path = Environment.GetEnvironmentVariable("DWSG_WORLD_SEED");
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("需要真实Unity世界导出DWSG_WORLD_SEED");
            JObject seed = JObject.Parse(File.ReadAllText(path));
            全局变量.所有玩家数据表 = seed["玩家列表"].ToObject<List<玩家数据>>();
            全局变量.所有国家列表 = seed["国家列表"].ToObject<List<国家信息库类>>();
            全局变量.所有城池列表 = seed["城池列表"].ToObject<List<城池信息库类>>();
            // 原初始化城池列表设置这两个静态边界；本检查恢复真实导出，不能重生成942城。
            所有城池界面脚本.地图H = 全局大地图库.大地图表.GetLength(0);
            所有城池界面脚本.地图W = 全局大地图库.大地图表.GetLength(1);
            int cityIndex = 0;
            for (int y = 0; y < 所有城池界面脚本.地图H; y++)
            for (int x = 0; x < 所有城池界面脚本.地图W; x++)
            {
                if (全局大地图库.大地图表[y, x] < 2) continue;
                var city = 全局变量.所有城池列表[cityIndex++];
                if (city.坐标x != x + 1 || city.坐标y != y + 1 || !ReferenceEquals(city, 所有城池界面脚本.根据坐标获取指定城池(x + 1, y + 1)))
                    throw new InvalidOperationException("真实城池表与原地图逐行索引不符");
                checks++;
            }
            if (cityIndex != 全局变量.所有城池列表.Count) throw new InvalidOperationException("真实地图城池数量不符");
            全局兵种库.属性表.Clear(); 全局兵种库.初始化兵种库();
            全局将领库.属性表.Clear(); 全局将领库.初始化将领库();
            全局装备库.属性表.Clear(); 全局装备库.初始化装备库();
            加载资源.头像资源(); 加载资源.预制体资源();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/主场景.unity", OpenSceneMode.Additive);
            全局变量.提示类 = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<提示移动>(true)).First();
            for (Transform parent = 全局变量.提示类.transform; parent != null; parent = parent.parent) parent.gameObject.SetActive(true);
            root = new GameObject("OriginalCityRegression"); root.SetActive(false);
            var original = root.AddComponent<全局任务脚本>();
            MethodInfo militia = typeof(全局任务脚本).GetMethod("随机一个城池驻防将领", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo named = typeof(全局任务脚本).GetMethod("随机一个名将驻防城池", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.unityLogger.logEnabled = false;
            foreach (国家信息库类 nation in 全局变量.所有国家列表)
            {
                全局变量.本机身份 = nation.国王;
                var origins = 全局变量.所有城池列表.Where(city => city.国家 == nation.国号).Take(3).ToArray();
                foreach (var origin in origins)
                foreach (var destination in 全局变量.所有城池列表.Where((city, index) => index % 29 == 0))
                {
                    int expected = new A星寻路().开始寻路(origin.坐标x - 1, origin.坐标y - 1, destination.坐标x - 1, destination.坐标y - 1);
                    var extracted = new CityMarchRules(全局大地图库.大地图表, (x, y) => 所有城池界面脚本.根据坐标获取指定城池(x, y).国家 == nation.国号);
                    Equal(expected, extracted.Find(origin.坐标x - 1, origin.坐标y - 1, destination.坐标x - 1, destination.坐标y - 1), "真实城池寻路");
                }
            }
            foreach (int difficulty in new[] { 1, 2, 3, 4 })
            foreach (int level in new[] { 20, 29, 30, 49, 50, 51, 60, 69, 70, 71, 79, 90, 91, 98 })
            foreach (string nationName in new[] { 全局变量.所有国家列表[0].国号, "" })
            {
                全局变量.所有玩家数据表 = seed["玩家列表"].ToObject<List<玩家数据>>();
                全局变量.难度 = difficulty;
                int owner = 全局方法类.获取指定名字的国家(nationName)?.国王 ?? 2;
                JObject player = JObject.FromObject(全局变量.所有玩家数据表[owner]);
                long utc = TIME.getTime();
                int randomSeed = 100000 + difficulty * 1000 + level;
                UnityEngine.Random.InitState(randomSeed);
                var expected = (将领信息)militia.Invoke(original, new object[] { level, nationName });
                UnityEngine.Random.State expectedRandom = UnityEngine.Random.state;
                JObject expectedOwner = JObject.FromObject(全局变量.所有玩家数据表[owner]);
                UnityEngine.Random.InitState(randomSeed);
                JObject extracted = CityGarrisonRules.CreateMilitiaGeneral(level, player, (JArray)seed["将领配置"], (JObject)seed["姓名配置"], difficulty, utc, UnityEngine.Random.Range);
                JsonEqual(JObject.FromObject(expected), JObject.FromObject(extracted.ToObject<将领信息>()), "原驻防将领 level=" + level + ",difficulty=" + difficulty);
                JsonEqual(expectedOwner, JObject.FromObject(player.ToObject<玩家数据>()), "生成守军时重算原owner");
                if (!expectedRandom.Equals(UnityEngine.Random.state)) throw new InvalidOperationException("原民兵随机消费不一致");
                checks++;
            }
            foreach (int scale in new[] { 0, 1, 2, 3, 4 })
            {
                全局变量.所有玩家数据表 = seed["玩家列表"].ToObject<List<玩家数据>>();
                string nation = 全局变量.所有国家列表[0].国号;
                int owner = 全局变量.所有国家列表[0].国王;
                JObject player = JObject.FromObject(全局变量.所有玩家数据表[owner]);
                UnityEngine.Random.InitState(2000 + scale);
                var expected = (将领信息)named.Invoke(original, new object[] { nation, scale });
                var state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(2000 + scale);
                JObject selected = CityGarrisonRules.SelectNamedGuard((JArray)player["封地信息表"][0]["将领信息表"], scale, UnityEngine.Random.Range);
                JsonEqual(expected == null ? null : JObject.FromObject(expected), selected == null ? null : JObject.FromObject(selected.ToObject<将领信息>()), "原协防选将");
                JsonEqual(JObject.FromObject(全局变量.所有玩家数据表[owner]), JObject.FromObject(player.ToObject<玩家数据>()), "协防候选配兵副作用");
                if (!state.Equals(UnityEngine.Random.state)) throw new InvalidOperationException("原协防随机消费不一致");
                checks++;
                var city = new 城池信息库类 { 规模 = scale };
                Equal(city.获取城墙上限(), CityGarrisonRules.WallMaximum(scale), "原城墙上限");
                Equal(city.获取攻打战功(), CityGarrisonRules.WarReward(scale), "原占城战功");
            }
            foreach (var troop in 全局兵种库.属性表)
            foreach (double quantity in new[] { 1.0, 300.0, 12345.0 })
                Equal((double)Mathf.Round((float)(troop.攻击力 / 10.0)) * quantity, CityGarrisonRules.WallDamage(troop.攻击力, quantity), "原攻墙伤害");
            Debug.unityLogger.logEnabled = logging;
            Debug.Log("CITY_ORIGINAL_RULES_PASSED checks=" + checks);
            UnityEngine.Object.DestroyImmediate(root);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.unityLogger.logEnabled = logging;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            Debug.LogError(error); EditorApplication.Exit(1);
        }
    }
    private static void Equal(double expected, double actual, string name)
    {
        if (expected != actual) throw new InvalidOperationException(name + ":" + expected + " != " + actual);
        checks++;
    }
    private static void JsonEqual(JToken expected, JToken actual, string name)
    {
        if (!JToken.DeepEquals(expected, actual)) throw new InvalidOperationException(name + ":" + expected + " != " + actual);
        checks++;
    }
}
