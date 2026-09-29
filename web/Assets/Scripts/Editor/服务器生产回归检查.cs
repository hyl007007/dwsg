#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

public static class 服务器生产回归检查
{
    // -executeMethod 服务器生产回归检查.运行 -dwsgSeedInput <actual audit world-seed.json>
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
        if (LegacyWorldRules.CreatePlayer(world, "农场回归", "汉", 0, out int playerIndex).Code != GameCodes.Ok)
            throw new InvalidOperationException("实际原世界角色创建失败");
        var player = world.Data["玩家列表"][playerIndex].ToObject<玩家数据>();
        var originalPlayers = 全局变量.所有玩家数据表;
        int originalIdentity = 全局变量.本机身份;
        var originalToast = 全局变量.提示类;
        var originalModels = 全局变量.封地所有建筑模型;
        var originalNames = 全局变量.封地所有建筑名字;
        var root = new GameObject("原封地生产按钮回归", typeof(RectTransform));
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            全局变量.所有玩家数据表 = new List<玩家数据> { player };
            全局变量.本机身份 = 0;
            全局变量.封地所有建筑模型 = new List<GameObject[]>();
            加载资源.预制体资源();
            全局变量.提示类 = Child(root.transform, "提示").gameObject.AddComponent<提示移动>();
            Child(全局变量.提示类.transform, "提示文本").gameObject.AddComponent<Text>();
            var script = root.AddComponent<封地界面脚本>();
            script.第几个封地 = 0;
            script.当前封地显示对象 = Child(root.transform, "当前封地").gameObject.AddComponent<Text>();
            script.封地建筑列表对象 = Child(root.transform, "建筑列表");
            for (int i = 0; i < 13; i++)
            {
                var cell = Child(script.封地建筑列表对象, "建筑" + i);
                Child(cell, "模型");
                var info = Child(cell, "信息");
                Child(info, "背景"); Child(info, "标签");
                Child(info, "等级").gameObject.AddComponent<Text>();
                Child(info, "名字").gameObject.AddComponent<Image>();
            }
            script.建造界面UI对象 = Child(root.transform, "建造界面");
            Child(script.建造界面UI对象, "标题");
            var options = Child(script.建造界面UI对象, "选项");
            Child(options, "标题");
            var toggles = Child(options, "类型");
            for (int i = 0; i < 7; i++) Child(toggles, "选项" + i).gameObject.AddComponent<Toggle>();
            typeof(封地界面脚本).GetField("已打开第几个建筑", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(script, 1);
            void ClearModels()
            {
                foreach (Transform cell in script.封地建筑列表对象)
                    while (cell.GetChild(0).childCount > 0) UnityEngine.Object.DestroyImmediate(cell.GetChild(0).GetChild(0).gameObject);
            }
            void Select(int toggle)
            {
                for (int i = 0; i < 7; i++) toggles.GetChild(i).GetComponent<Toggle>().isOn = i == toggle;
            }
            var fief = player.封地信息表[0]; var building = fief.建筑信息表[1];
            player.财产信息.铜钱 = player.财产信息.粮食 = 5000000;
            Select(1); script.建造建筑();
            Check(building.类型 == 3 && building.等级 == 1 && player.基础信息.粮食增加 == 1 && player.财产信息.铜钱 == 5000000,
                "原建造按钮须立即免费建一级农场");
            ClearModels(); script.升级建筑();
            Check(building.等级 == 2 && player.基础信息.粮食增加 == 2 && player.财产信息.铜钱 == 4999200 && player.财产信息.粮食 == 4998400,
                "原升级按钮费用和农场产量不符");
            double grain = player.财产信息.粮食;
            Check(ProductionClient.ProduceOffline(player, 50).Code == GameCodes.Ok && player.财产信息.粮食 == grain + 3,
                "原DTO每秒产量与所属国家科技加成不符");
            ClearModels(); script.拆除建筑();
            grain = player.财产信息.粮食;
            Check(building.类型 == -1 && building.等级 == 0 && player.基础信息.粮食增加 == 0 &&
                ProductionClient.ProduceOffline(player, 50).Code == GameCodes.Ok && player.财产信息.粮食 == grain,
                "升级后拆除农场仍产生粮食");
            Check(ReferenceEquals(fief, player.封地信息表[0]) && ReferenceEquals(building, fief.建筑信息表[1]), "原封地/建筑对象引用被替换");
            Debug.Log("PRODUCTION_UNITY_FARM_BUTTONS_PASS");
            var types = new[] { 2, 3, 1, 5, 6, 7, 4 };
            foreach (int toggle in new[] { 0, 2, 3, 4, 5, 6 })
            {
                ClearModels(); Select(toggle); script.建造建筑();
                Check(building.类型 == types[toggle] && building.等级 == 1 && player.基础信息.粮食增加 == 0, "非农田原建造按钮错误");
                ClearModels(); script.升级建筑();
                Check(building.等级 == 2 && player.基础信息.粮食增加 == 0, "非农田原升级按钮错误");
                ClearModels(); script.拆除建筑();
                Check(building.类型 == -1 && building.等级 == 0 && player.基础信息.粮食增加 == 0, "非农田原拆除按钮错误");
                Debug.Log("PRODUCTION_UNITY_OTHER_BUILDING_PASS " + types[toggle]);
            }
            ClearModels(); Select(1); script.建造建筑();
            player.财产信息.铜钱 = 800;
            string before = JsonConvert.SerializeObject(player);
            ClearModels(); script.升级建筑();
            Check(before == JsonConvert.SerializeObject(player), "原铜钱严格大于费用边界被改变");
            player.财产信息.铜钱 = 1000; player.财产信息.粮食 = 1600;
            before = JsonConvert.SerializeObject(player);
            script.升级建筑();
            Check(before == JsonConvert.SerializeObject(player), "原粮食严格大于费用边界被改变");
            Debug.Log("PRODUCTION_UNITY_STRICT_FUNDS_PASS");
            fief.建造建筑(2, 2);
            Check(fief.升级建筑(2) && fief.建筑信息表[2].等级 == 2 &&
                (bool)fief.GetType().GetMethod("拆除建筑").Invoke(fief, new object[] { 2 }), "原封地模型没有复用共享建筑规则");
            Debug.Log("PRODUCTION_UNITY_MODEL_PASS");
        }
        finally
        {
            if (全局变量.提示类 != null) 全局变量.提示类.StopAllCoroutines();
            UnityEngine.Object.DestroyImmediate(root);
            全局变量.所有玩家数据表 = originalPlayers; 全局变量.本机身份 = originalIdentity; 全局变量.提示类 = originalToast;
            全局变量.封地所有建筑模型 = originalModels; 全局变量.封地所有建筑名字 = originalNames;
        }
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.hideFlags = HideFlags.HideAndDontSave;
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
