using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dwsg.Combat;
using Dwsg.Shared.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using 玩家数据结构;

public static class 服务器战斗演出回归检查
{
    private static int checks;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void Run()
    {
        GameObject parent = null;
        bool logging = Debug.unityLogger.logEnabled;
        try
        {
            JObject world = JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("DWSG_COMBAT_WORLD")));
            BanditBattle snapshot = JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("DWSG_COMBAT_BATTLE"))).ToObject<BanditBattle>();
            全局变量.所有玩家数据表 = world["玩家列表"].ToObject<List<玩家数据>>();
            全局变量.所有国家列表 = world["国家列表"].ToObject<List<国家信息库类>>();
            全局变量.所有城池列表 = world["城池列表"].ToObject<List<城池信息库类>>();
            所有城池界面脚本.地图H = 全局大地图库.大地图表.GetLength(0);
            所有城池界面脚本.地图W = 全局大地图库.大地图表.GetLength(1);
            全局变量.本机身份 = snapshot.Attackers[0].General["详细信息"].Value<int>("身份");
            全局兵种库.属性表.Clear(); 全局兵种库.初始化兵种库();
            全局将领库.属性表.Clear(); 全局将领库.初始化将领库();
            加载资源.头像资源(); 加载资源.预制体资源();
            string original = JsonConvert.SerializeObject(全局变量.所有玩家数据表);
            Debug.unityLogger.logEnabled = false;
            parent = new GameObject("ServerBattlePresentationCheck"); parent.SetActive(false);
            GameObject root = UnityEngine.Object.Instantiate(snapshot.Kind == "city" ? 全局变量.城池战斗场景pre : 全局变量.山贼战斗场景pre, parent.transform);
            战斗系统 system = root.GetComponentInChildren<战斗系统>(true);
            system.服务器战场ID = snapshot.BattleId; system.战场类型 = snapshot.Kind == "city" ? 1 : 0;
            system.坐标x = snapshot.X; system.坐标y = snapshot.Y;
            typeof(战斗系统).GetMethod("Start", Private).Invoke(system, null);
            Check(system.攻方坑位对象.transform.childCount == 15 && system.守方坑位对象.transform.childCount == 15, "original thirty pit prefabs");
            var view = root.AddComponent<BanditBattleView>(); view.System = system;
            view.Apply(snapshot); typeof(BanditBattleView).GetMethod("LateUpdate", Private).Invoke(view, null);
            将领功能[] models = root.GetComponentsInChildren<将领功能>(true);
            long utc = snapshot.StartedUtcMs + snapshot.Frame * 1000 / 60;
            var available = new HashSet<string>(snapshot.DefenseFormations.Where(group => group.AvailableUtcMs <= utc).Select(group => group.ArmyId));
            CombatUnit[] visible = snapshot.Attackers.Concat(snapshot.Defenders).Where(unit => !unit.Retired &&
                (snapshot.Kind != "city" || unit.Side == 0 || available.Contains(unit.ArmyId))).ToArray();
            Check(models.Length == visible.Length, "only arrived original general models are instantiated");
            var modelIds = (Dictionary<string, 将领功能>)typeof(BanditBattleView).GetField("models", Private).GetValue(view);
            foreach (CombatUnit unit in visible)
            {
                将领功能 model = unit.Slot >= 0
                    ? (unit.Side == 0 ? system.攻方坑位对象 : system.守方坑位对象).transform.GetChild(unit.Slot).GetComponentInChildren<将领功能>(true)
                    : modelIds[unit.CombatId];
                Check(model != null && model.战斗系统脚本对象 == system, "original hierarchy resolves server battlefield");
                Check(model.本将领信息.详细信息.剩余兵力 == unit.Remaining && model.本将领信息.将领配兵.数量 == unit.Remaining, "server quantity drives original model");
                Check(!(bool)typeof(将领功能).GetField("是否开始攻击", Private).GetValue(model), "local combat coroutine stays inactive");
                model.开始战斗(); typeof(将领功能).GetMethod("FixedUpdate", Private).Invoke(model, null);
            }
            Check(system.伤害显示缓存表.Count == 30, "original damage text cache");
            Check(system.攻方兵力 == snapshot.Attackers.Where(unit => !unit.Retired).Sum(unit => unit.Remaining) && system.守方兵力 == BanditBattleRules.DefendingForce(snapshot, utc), "side counters follow snapshot");
            if (snapshot.Kind == "city")
                Check(system.城墙信息显示.text == snapshot.Wall.ToString() && system.城墙血条对象.localPosition.x == 3f * (float)(snapshot.Wall / snapshot.WallMaximum), "original city wall display follows committed snapshot");
            typeof(战斗系统).GetMethod("FixedUpdate", Private).Invoke(system, null);
            Check(!system.战斗结束 && JsonConvert.SerializeObject(全局变量.所有玩家数据表) == original, "presentation cannot settle or mutate original player DTOs");
            CheckOriginalSlots(parent.transform, snapshot.Attackers[0].General, world["兵种配置"] as JArray);
            Debug.unityLogger.logEnabled = logging;
            Debug.Log("COMBAT_PRESENTATION_PASSED checks=" + checks + " models=" + models.Length);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.unityLogger.logEnabled = logging; Debug.LogException(error); EditorApplication.Exit(1);
        }
        finally { if (parent != null) UnityEngine.Object.DestroyImmediate(parent); Debug.unityLogger.logEnabled = logging; }
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
    }
    private static void CheckOriginalSlots(Transform parent, JObject source, JArray troops)
    {
        var random = new CombatRandom(11223344);
        var types = troops.OfType<JObject>().GroupBy(troop => troop.Value<int>("兵种")).ToDictionary(group => group.Key, group => group.First());
        MethodInfo place = typeof(BanditBattleRules).GetMethod("Place", BindingFlags.Static | BindingFlags.NonPublic);
        for (int trial = 0; trial < 64; trial++)
        {
            GameObject root = new GameObject("OriginalPitOrder"); root.transform.SetParent(parent);
            var system = root.AddComponent<战斗系统>();
            system.攻方坑位对象 = new GameObject("OriginalPits"); system.攻方坑位对象.transform.SetParent(root.transform);
            var legacy = root.AddComponent<编队将领上坑>(); legacy.设置攻守方 = 0;
            typeof(编队将领上坑).GetField("战斗系统脚本对象", Private).SetValue(legacy, system);
            var units = new List<CombatUnit>();
            var expected = new Dictionary<string, int>();
            int occupancy = trial % 16;
            for (int slot = 0; slot < 15; slot++)
            {
                GameObject pit = new GameObject("Pit" + slot); pit.transform.SetParent(system.攻方坑位对象.transform);
                if (slot >= occupancy) continue;
                CombatUnit unit = BanditBattleRules.CreateUnit("fixture" + slot, source, types[random.Next(1, 5)], 0);
                unit.General["将领配兵"]["ID"] = unit.Troop.Value<int>("ID"); unit.Slot = slot;
                units.Add(unit); expected.Add(unit.GeneralId, slot);
                GameObject model = new GameObject("OriginalGeneral"); model.transform.SetParent(pit.transform);
                model.AddComponent<将领功能>().本将领信息 = unit.General.ToObject<将领信息>();
            }
            for (int troopClass = 1; troopClass <= 4; troopClass++)
            {
                int free = (int)typeof(编队将领上坑).GetMethod("获取可上坑位", Private).Invoke(legacy, new object[] { (double)troopClass });
                int push = troopClass > 2 ? (int)typeof(编队将领上坑).GetMethod("获取可挤坑位", Private).Invoke(legacy, new object[] { (double)troopClass }) : -1;
                var candidate = units.Select(unit => new CombatUnit { GeneralId = unit.GeneralId, General = unit.General, Troop = unit.Troop, Slot = unit.Slot }).ToList();
                CombatUnit incoming = BanditBattleRules.CreateUnit("incoming", source, types[troopClass], 0); candidate.Add(incoming);
                int destination = free;
                string moved = null;
                if (free >= 0 && push >= 0 && ((free < 5 && push >= 5) || (free < 10 && push >= 10)))
                {
                    moved = units.FirstOrDefault(unit => unit.Slot == push)?.GeneralId; destination = push;
                }
                bool result = (bool)place.Invoke(null, new object[] { candidate, incoming });
                Check(result == (free >= 0) && incoming.Slot == destination, "pit entry matches actual original private methods");
                foreach (CombatUnit unit in candidate.Where(unit => unit != incoming))
                    Check(unit.Slot == (unit.GeneralId == moved ? free : expected[unit.GeneralId]), "original lower class displacement is preserved");
            }
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
