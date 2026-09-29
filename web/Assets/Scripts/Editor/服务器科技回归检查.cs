#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public static class 服务器科技回归检查
{
    // The audit-only 原书院同源对照 must be the frozen original script, renamed only for comparison.
    public static void 运行()
    {
        Check(Application.isBatchMode && !Application.isPlaying && !Dwsg.Network.GameNetwork.Enabled, "须在未联网的离屏原Unity编译副本执行");
        string seedPath = Argument("-dwsgSeedInput"), sourcePath = Argument("-dwsgOriginalBookInput");
        var baselineType = Type.GetType("原书院同源对照, Assembly-CSharp", true);
        var prices = Regex.Matches(File.ReadAllText(sourcePath), @"获取升级需要铜钱\(([0-9.]+),\s*(\w+)\)");
        Check(prices.Count == TechnologyRules.Count, "必须使用真实原21项书院源码");
        var world = new WorldState { Data = JObject.Parse(File.ReadAllText(seedPath)) };
        Check(LegacyWorldRules.CreatePlayer(world, "科技回归", "汉", 0, out int identity).Code == GameCodes.Ok, "实际原角色模板创建");
        var template = (JObject)world.Data["玩家列表"][identity]; var fief = (JObject)template["封地信息表"][0];
        Check(BuildingRules.Construct(template, fief, 1, 1).Code == GameCodes.Ok, "原模板建造书院");
        while (fief["建筑信息表"][0].Value<int>("等级") < 15) Check(BuildingRules.UpgradePlot(fief, 0).Code == GameCodes.Ok, "原大厅等级准备");
        while (fief["建筑信息表"][1].Value<int>("等级") < 15) Check(BuildingRules.UpgradePlot(fief, 1).Code == GameCodes.Ok, "原书院等级准备");
        var previousPlayers = 全局变量.所有玩家数据表; int previousIdentity = 全局变量.本机身份;
        var previousToast = 全局变量.提示类;
        var root = new GameObject("原科技按钮对照", typeof(RectTransform)); root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            全局变量.所有玩家数据表 = world.Data["玩家列表"].ToObject<System.Collections.Generic.List<玩家数据>>();
            全局变量.本机身份 = identity;
            全局变量.提示类 = Child(root.transform, "提示").gameObject.AddComponent<提示移动>();
            Child(全局变量.提示类.transform, "提示文本").gameObject.AddComponent<Text>();
            var current = root.AddComponent<书院脚本>(); var original = root.AddComponent(baselineType);
            var rows = Child(root.transform, "原21项列表");
            for (int i = 0; i < TechnologyRules.Count; i++)
            {
                var row = Child(rows, "原科技" + i);
                for (int cell = 0; cell < 6; cell++)
                {
                    var child = Child(row, "原节点" + cell);
                    if (cell >= 2 && cell <= 4) child.gameObject.AddComponent<Text>();
                    if (cell == 5) child.gameObject.SetActive(false);
                }
            }
            current.书院列表对象 = rows.gameObject; current.第几个玩家 = identity; current.第几个封地 = 0; current.第几个建筑 = 1;
            foreach (string field in new[] { "书院列表对象", "第几个玩家", "第几个封地", "第几个建筑" })
                baselineType.GetField(field).SetValue(original, typeof(书院脚本).GetField(field).GetValue(current));
            var oldCost = baselineType.GetMethod("获取升级需要铜钱", BindingFlags.NonPublic | BindingFlags.Instance);
            var oldUpgrade = baselineType.GetMethod("升级选中科技");
            var proof = new JArray(); int upgraded = 0, failed = 0;
            string zero = JsonConvert.SerializeObject(全局变量.所有玩家数据表[0]);
            void Compare(JObject input)
            {
                全局变量.所有玩家数据表[identity] = input.ToObject<玩家数据>();
                oldUpgrade.Invoke(original, null);
                var expected = JObject.FromObject(全局变量.所有玩家数据表[identity]);
                var player = input.ToObject<玩家数据>(); 全局变量.所有玩家数据表[identity] = player;
                var wallet = player.财产信息; var technology = player.科技信息; var first = player.封地信息表[0];
                current.升级选中科技();
                Check(JToken.DeepEquals(expected, JObject.FromObject(player)), "原21项按钮状态与共享升级不一致");
                Check(ReferenceEquals(wallet, player.财产信息) && ReferenceEquals(technology, player.科技信息) && ReferenceEquals(first, player.封地信息表[0]), "原科技钱包封地引用被替换");
            }
            for (int index = 0; index < TechnologyRules.Count; index++)
            {
                for (int i = 0; i < TechnologyRules.Count; i++) rows.GetChild(i).GetChild(5).gameObject.SetActive(i == index);
                string name = prices[index].Groups[2].Value;
                Check(name == TechnologyRules.NameAt(index), "原21项顺序或字段名不符");
                double basis = double.Parse(prices[index].Groups[1].Value, CultureInfo.InvariantCulture);
                for (int level = 0; level < TechnologyRules.Maximum(index); level++)
                {
                    double cost = (double)oldCost.Invoke(original, new object[] { basis, (double)level });
                    Check(cost == TechnologyRules.UpgradeCost(index, level), "原Mono费用边界不一致 " + name + " " + level);
                    var input = (JObject)template.DeepClone(); input["科技信息"][name] = (double)level;
                    input["封地信息表"][0]["建筑信息表"][1]["等级"] = level + (index >= 15 ? 11 : 1);
                    input["财产信息"][TechnologyRules.Currency(index)] = cost;
                    Compare(input); upgraded++;
                    proof.Add(new JObject { ["technology"] = name, ["level"] = level, ["cost"] = cost });
                    input["财产信息"][TechnologyRules.Currency(index)] = cost - 1; Compare(input); failed++;
                    input["财产信息"][TechnologyRules.Currency(index)] = cost;
                    input["封地信息表"][0]["建筑信息表"][1]["等级"] = Math.Max(1, level + (index >= 15 ? 10 : 0));
                    if (level > 0 || index >= 15) { Compare(input); failed++; }
                }
                var maximum = (JObject)template.DeepClone(); maximum["科技信息"][name] = (double)TechnologyRules.Maximum(index);
                maximum["财产信息"][TechnologyRules.Currency(index)] = 50000000.0; Compare(maximum); failed++;
            }
            Check(zero == JsonConvert.SerializeObject(全局变量.所有玩家数据表[0]), "科技按钮改动了玩家零");
            string[] args = Environment.GetCommandLineArgs(); int output = Array.IndexOf(args, "-dwsgTechnologyProof");
            if (output >= 0) File.WriteAllText(AuditPath(args[output + 1]), proof.ToString(Formatting.None));
            Debug.Log("TECHNOLOGY_UNITY_ORIGINAL_FEES_PASS " + proof.Count);
            Debug.Log("TECHNOLOGY_UNITY_ORIGINAL_BUTTONS_PASS " + upgraded);
            Debug.Log("TECHNOLOGY_UNITY_ORIGINAL_FAILURES_PASS " + failed);
        }
        finally
        {
            if (全局变量.提示类 != null) 全局变量.提示类.StopAllCoroutines();
            UnityEngine.Object.DestroyImmediate(root);
            全局变量.所有玩家数据表 = previousPlayers; 全局变量.本机身份 = previousIdentity; 全局变量.提示类 = previousToast;
        }
    }

    public static IEnumerator 在线原按钮()
    {
        Check(Application.isBatchMode && Application.isPlaying && Dwsg.Network.GameNetwork.HasRole, "须先真实原登录并进入原游戏场景");
        int identity = 全局变量.本机身份;
        var player = 全局变量.所有玩家数据表[identity]; var fief = player.封地信息表[0];
        int plot = fief.建筑信息表.FindIndex(b => b.类型 == 1);
        if (plot < 0)
        {
            plot = fief.建筑信息表.FindIndex(b => b.类型 == -1);
            Check(plot > 0, "原首封地没有空地建书院");
            var construction = UnityEngine.Object.FindObjectsByType<封地界面脚本>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            construction.第几个封地 = 0;
            typeof(封地界面脚本).GetField("已打开第几个建筑", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(construction, plot);
            var toggles = construction.建造界面UI对象.GetChild(1).GetChild(1);
            for (int i = 0; i < 7; i++) toggles.GetChild(i).GetComponent<Toggle>().isOn = i == 2;
            construction.建造建筑();
            float constructionLimit = Time.realtimeSinceStartup + 30;
            while (fief.建筑信息表[plot].类型 != 1) { Check(Time.realtimeSinceStartup < constructionLimit, "原在线建书院超时"); yield return null; }
        }
        int index = -1; var technologies = JObject.FromObject(player.科技信息);
        for (int i = 0; i < TechnologyRules.Count; i++)
        {
            int level = technologies.Value<int>(TechnologyRules.NameAt(i));
            double balance = JObject.FromObject(player.财产信息).Value<double>(TechnologyRules.Currency(i));
            if (level < TechnologyRules.Maximum(i) && fief.建筑信息表[plot].等级 > level + (i >= 15 ? 10 : 0) && balance >= TechnologyRules.UpgradeCost(i, level))
            { index = i; break; }
        }
        Check(index >= 0, "实际角色没有足够资源或书院等级研究原科技");
        var book = UnityEngine.Object.FindObjectsByType<书院脚本>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        book.第几个玩家 = identity; book.第几个封地 = 0; book.第几个建筑 = plot;
        for (int i = 0; i < TechnologyRules.Count; i++) book.书院列表对象.transform.GetChild(i).GetChild(5).gameObject.SetActive(i == index);
        string name = TechnologyRules.NameAt(index), currency = TechnologyRules.Currency(index);
        int beforeLevel = technologies.Value<int>(name); double beforeBalance = JObject.FromObject(player.财产信息).Value<double>(currency);
        string stableId = ((JObject)Dwsg.Network.GameNetwork.CurrentSnapshot.PrivatePlayer["entityMappings"]["fiefs"]).Properties()
            .Single(p => p.Value.Value<string>("playerId") == Dwsg.Network.GameNetwork.CurrentSnapshot.PlayerId && p.Value.Value<int>("legacyId") == fief.ID).Name;
        var originalTech = player.科技信息; var originalWallet = player.财产信息;
        book.升级选中科技();
        Check(JObject.FromObject(player.科技信息).Value<int>(name) == beforeLevel && JObject.FromObject(player.财产信息).Value<double>(currency) == beforeBalance, "原按钮提前升级或扣费");
        float limit = Time.realtimeSinceStartup + 30;
        while (JObject.FromObject(player.科技信息).Value<int>(name) == beforeLevel) { Check(Time.realtimeSinceStartup < limit, "实际Host科技升级超时"); yield return null; }
        Check(JObject.FromObject(player.科技信息).Value<int>(name) == beforeLevel + 1 &&
            JObject.FromObject(player.财产信息).Value<double>(currency) == beforeBalance - TechnologyRules.UpgradeCost(index, beforeLevel), "原在线科技费用或级别不符");
        Check(ReferenceEquals(originalTech, player.科技信息) && ReferenceEquals(originalWallet, player.财产信息) &&
            book.书院列表对象.transform.GetChild(index).GetChild(3).GetComponent<Text>().text == "(" + (beforeLevel + 1) + "级)", "原科技引用或确认后显示未刷新");
        GameResult stale = null;
        Dwsg.Network.GameNetwork.SendCommand("technology.upgrade", new JObject { ["fiefId"] = stableId, ["plot"] = plot, ["technology"] = name, ["expectedLevel"] = beforeLevel }, result => stale = result);
        limit = Time.realtimeSinceStartup + 30;
        while (stale == null) { Check(Time.realtimeSinceStartup < limit, "实际Host旧科技意图回执超时"); yield return null; }
        Check(stale.Code == GameCodes.Conflict && JObject.FromObject(player.科技信息).Value<int>(name) == beforeLevel + 1, "旧意图重复升级");
        Debug.Log("TECHNOLOGY_UNITY_ONLINE_BUTTON_PASS 4 " + name);
    }

    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("缺少原数据参数 " + name);
        return AuditPath(args[index + 1]);
    }
    private static string AuditPath(string path)
    {
        string full = Path.GetFullPath(path);
        if (!full.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p.Equals("audit", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("检查只访问audit");
        return full;
    }
    private static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform)); child.hideFlags = HideFlags.HideAndDontSave;
        child.transform.SetParent(parent, false); return child.transform;
    }
    private static void Check(bool success, string message) { if (!success) throw new InvalidOperationException(message); }
}
#endif
