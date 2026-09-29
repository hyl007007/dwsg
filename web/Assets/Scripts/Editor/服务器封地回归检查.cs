#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public static class 服务器封地回归检查
{
    // -executeMethod 服务器封地回归检查.运行 -dwsgSeedInput <actual audit world-seed.json>
    public static void 运行()
    {
        if (!Application.isBatchMode || Application.isPlaying || Dwsg.Network.GameNetwork.Enabled)
            throw new InvalidOperationException("仅在未连接服务器的离屏编译副本执行");
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-dwsgSeedInput");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("缺少实际原世界-dwsgSeedInput");
        string path = Path.GetFullPath(args[index + 1]);
        if (!path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p.Equals("audit", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("检查只读取audit中的原世界种子");
        var world = new WorldState { Data = JObject.Parse(File.ReadAllText(path)) };
        Check(LegacyWorldRules.CreatePlayer(world, "封地回归", "汉", 0, out int playerIndex).Code == GameCodes.Ok, "原世界创建角色");
        var previousPlayers = 全局变量.所有玩家数据表;
        var previousCities = 全局变量.所有城池列表;
        var previousToast = 全局变量.提示类;
        int previousIdentity = 全局变量.本机身份;
        var root = new GameObject("原开辟封地按钮回归", typeof(RectTransform));
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            全局变量.所有玩家数据表 = world.Data["玩家列表"].ToObject<System.Collections.Generic.List<玩家数据>>();
            全局变量.所有城池列表 = world.Data["城池列表"].ToObject<System.Collections.Generic.List<城池信息库类>>();
            全局变量.本机身份 = playerIndex;
            全局变量.提示类 = new GameObject("提示", typeof(RectTransform), typeof(提示移动)).GetComponent<提示移动>();
            全局变量.提示类.transform.SetParent(root.transform, false);
            new GameObject("提示文本", typeof(RectTransform), typeof(Text)).transform.SetParent(全局变量.提示类.transform, false);
            var script = root.AddComponent<城池信息显示脚本>();
            script.开辟封地按钮 = new GameObject("开辟封地"); script.开辟封地按钮.transform.SetParent(root.transform);
            script.进入封地按钮 = new GameObject("进入封地"); script.进入封地按钮.transform.SetParent(root.transform);
            var player = 全局变量.所有玩家数据表[playerIndex];
            var first = player.封地信息表[0];
            var city = 全局变量.所有城池列表.First(c => c.国家 == "汉" && (c.坐标x != first.所在城池.x || c.坐标y != first.所在城池.y));
            typeof(城池信息显示脚本).GetField("显示第几个城池", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(script, 全局变量.所有城池列表.IndexOf(city));
            string originalZero = JsonConvert.SerializeObject(全局变量.所有玩家数据表[0]);
            string wallet = JsonConvert.SerializeObject(player.财产信息);
            int nextId = player.封地ID标识;
            script.进入封地按钮.SetActive(false);
            script.开辟封地();
            Check(player.封地信息表.Count == 2 && ReferenceEquals(first, player.封地信息表[0]) &&
                originalZero == JsonConvert.SerializeObject(全局变量.所有玩家数据表[0]) && wallet == JsonConvert.SerializeObject(player.财产信息), "原按钮免费开辟当前角色封地并保留原对象");
            var created = player.封地信息表[1];
            Check(created.ID == nextId && player.封地ID标识 == nextId + 1 && created.建筑信息表.Count == 13 &&
                created.建筑信息表[0].等级 == 10 && created.伤兵信息表.Count == 1 && created.伤兵信息表[0].ID == 104 && created.伤兵信息表[0].数量 == 1,
                "原封地编号建筑和伤兵模板");
            Check(created.所在城池.x == city.坐标x && created.所在城池.y == city.坐标y &&
                city.城池封地列表.Count(r => r.第几个玩家 == playerIndex && r.封地ID标识 == nextId) == 1 &&
                !script.开辟封地按钮.activeSelf && script.进入封地按钮.activeSelf, "原按钮同步位置登记和可见状态");
            string before = JsonConvert.SerializeObject(player);
            script.开辟封地();
            Check(before == JsonConvert.SerializeObject(player), "重复原按钮不会创建或扣费");
            Debug.Log("TERRITORY_UNITY_ORIGINAL_BUTTON_PASS 4");
        }
        finally
        {
            if (全局变量.提示类 != null) 全局变量.提示类.StopAllCoroutines();
            UnityEngine.Object.DestroyImmediate(root);
            全局变量.所有玩家数据表 = previousPlayers; 全局变量.所有城池列表 = previousCities;
            全局变量.提示类 = previousToast; 全局变量.本机身份 = previousIdentity;
        }
    }

    // Call after the original login, role entry and game scene have completed. No role or world is injected.
    public static IEnumerator 在线原按钮()
    {
        Check(Application.isBatchMode && Application.isPlaying && Dwsg.Network.GameNetwork.HasRole, "须使用真实离屏登录和原游戏场景");
        var player = 全局变量.所有玩家数据表[全局变量.本机身份];
        Check(player.封地信息表.Count <= 10, "实际验收角色已达原封地数量上限");
        var script = UnityEngine.Object.FindObjectsByType<城池信息显示脚本>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        var city = 全局变量.所有城池列表.First(c => c.国家 == player.基础信息.国家 &&
            !player.封地信息表.Any(f => f.所在城池.x == c.坐标x && f.所在城池.y == c.坐标y) &&
            Dwsg.Network.GameNetwork.GetCityFiefCount(全局变量.所有城池列表.IndexOf(c)) < c.获取封地上限());
        typeof(城池信息显示脚本).GetField("显示第几个城池", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(script, 全局变量.所有城池列表.IndexOf(city));
        var first = player.封地信息表[0];
        int count = player.封地信息表.Count, nextId = player.封地ID标识;
        double gold = player.财产信息.黄金, silver = player.财产信息.白银;
        script.开辟封地按钮.SetActive(true); script.进入封地按钮.SetActive(false);
        script.开辟封地();
        Check(player.封地信息表.Count == count && script.开辟封地按钮.activeSelf && !script.进入封地按钮.activeSelf, "原在线按钮须等待服务器确认");
        float limit = Time.realtimeSinceStartup + 30;
        while (player.封地信息表.Count == count)
        {
            Check(Time.realtimeSinceStartup < limit, "实际Host开辟封地超时");
            yield return null;
        }
        var created = player.封地信息表.Single(f => f.ID == nextId);
        string playerId = Dwsg.Network.GameNetwork.CurrentSnapshot.PlayerId;
        var mappings = (JObject)Dwsg.Network.GameNetwork.CurrentSnapshot.PrivatePlayer["entityMappings"]["fiefs"];
        Check(player.封地信息表.Count == count + 1 && ReferenceEquals(first, player.封地信息表[0]) &&
            mappings.Properties().Count(p => p.Value.Value<string>("playerId") == playerId && p.Value.Value<int>("legacyId") == nextId) == 1,
            "实际Host返回当前角色唯一稳定封地映射且保留原对象");
        Check(created.所在城池.x == city.坐标x && created.所在城池.y == city.坐标y && created.建筑信息表[0].等级 == 10 &&
            player.封地ID标识 == nextId + 1 && player.财产信息.黄金 == gold && player.财产信息.白银 == silver &&
            !script.开辟封地按钮.activeSelf && script.进入封地按钮.activeSelf, "原在线按钮应用原模板和服务器成功状态");
        GameResult duplicate = null;
        TerritoryClient.CreateFief(city, result => duplicate = result);
        limit = Time.realtimeSinceStartup + 30;
        while (duplicate == null) { Check(Time.realtimeSinceStartup < limit, "实际Host重复开辟回执超时"); yield return null; }
        Check(duplicate.Code == GameCodes.Conflict && player.封地信息表.Count == count + 1, "实际Host拒绝重复城池封地");
        Debug.Log("TERRITORY_UNITY_ONLINE_BUTTON_PASS 4");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
