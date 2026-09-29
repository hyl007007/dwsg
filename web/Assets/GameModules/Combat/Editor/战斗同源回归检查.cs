using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dwsg.Shared.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using 玩家数据结构;

public static class 战斗同源回归检查
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    public static void Run()
    {
        bool logging = Debug.unityLogger.logEnabled;
        GameObject root = null;
        try
        {
            string source = Environment.GetEnvironmentVariable("DWSG_WORLD_SEED");
            if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("需要原初始化世界导出 DWSG_WORLD_SEED");
            JObject seed = JObject.Parse(File.ReadAllText(source));
            全局变量.所有玩家数据表 = seed["玩家列表"].ToObject<List<玩家数据>>();
            全局变量.所有国家列表 = seed["国家列表"].ToObject<List<国家信息库类>>();
            全局变量.难度 = seed.Value<int>("难度");
            全局兵种库.属性表.Clear();
            全局兵种库.初始化兵种库();
            全局将领库.属性表.Clear();
            全局将领库.初始化将领库();
            root = new GameObject("CombatRuleRegression");
            root.SetActive(false);
            战斗系统 battle = root.AddComponent<战斗系统>();
            var first = new GameObject("Attacker"); first.transform.SetParent(root.transform);
            var second = new GameObject("Defender"); second.transform.SetParent(root.transform);
            将领功能 actor = first.AddComponent<将领功能>();
            将领功能 target = second.AddComponent<将领功能>();
            actor.战斗系统脚本对象 = battle;
            target.战斗系统脚本对象 = battle;
            typeof(将领功能).GetField("被攻击的将领脚本", Private).SetValue(actor, target);
            全局变量.所有玩家数据表[0].科技信息.升级全部科技(7);
            全局变量.所有玩家数据表[1].科技信息.升级全部科技(11);
            Debug.unityLogger.logEnabled = false;
            foreach (string title in new[] { "无", "学士", "先锋", "财主", "达人", "宗师", "神工", "霸主", "天子", "暴君", "君王", "帝王", "护军", "劳模", "猛将", "名士", "无双", "破军", "地主", "飞将", "武圣", "贪狼" })
            {
                var basic = 全局变量.所有玩家数据表[0].基础信息;
                basic.称号名 = title;
                Equal(basic.攻击类称号加成(), CombatModifiers.TitleBonus(title, "攻击"));
                Equal(basic.防御类称号加成(), CombatModifiers.TitleBonus(title, "防御"));
                Equal(basic.生命类称号加成(), CombatModifiers.TitleBonus(title, "生命"));
                Equal(basic.攻速类称号加成(), CombatModifiers.TitleBonus(title, "攻速"));
            }
            全局变量.所有玩家数据表[0].基础信息.称号名 = "帝王";
            全局变量.所有玩家数据表[1].基础信息.称号名 = "无双";
            for (int career = 1; career <= 4; career++)
            for (int enemyCareer = 1; enemyCareer <= 4; enemyCareer++)
            for (int attackIndex = 0; attackIndex < 全局兵种库.属性表.Count; attackIndex++)
            for (int defenseIndex = 0; defenseIndex < 全局兵种库.属性表.Count; defenseIndex++)
            {
                actor.本将领信息 = General(全局兵种库.属性表[attackIndex].ID, career, 0);
                target.本将领信息 = General(全局兵种库.属性表[defenseIndex].ID, enemyCareer, 1);
                typeof(将领功能).GetField("兵种索引", Private).SetValue(actor, attackIndex);
                typeof(将领功能).GetField("兵种索引", Private).SetValue(target, defenseIndex);
                var a = BanditBattleRules.CreateUnit("a", JObject.FromObject(actor.本将领信息), JObject.FromObject(全局兵种库.属性表[attackIndex]), 0);
                var d = BanditBattleRules.CreateUnit("d", JObject.FromObject(target.本将领信息), JObject.FromObject(全局兵种库.属性表[defenseIndex]), 1);
                CombatProfile ap = Profile(0), dp = Profile(1);
                double attack = Call(actor, "获取最终攻击力");
                double defense = Call(actor, "获取最终防御力");
                double life = Call(actor, "获取守方血量");
                Equal(attack, BanditBattleRules.Attack(a, d, ap));
                Equal(defense, BanditBattleRules.Defense(a, d, ap, dp));
                Equal(life, BanditBattleRules.Life(d, dp));
                Equal(Call(actor, "计算最终伤害", attack, defense, life), 战斗规则.最终伤害(attack, defense, a.Remaining, life));
            }
            for (int i = 0; i < 200; i++)
            {
                UnityEngine.Random.InitState(9000 + i);
                全局变量.所有玩家数据表[1].封地信息表[0].将领信息表.Clear();
                var original = new 山贼属性信息(); original.随机生成山贼(2, 3);
                UnityEngine.Random.InitState(9000 + i);
                JObject shared = BanditGenerator.Generate(2, 3, UnityEngine.Random.Range, Create, (general, level) => {
                    var unit = general.ToObject<将领信息>();
                    unit.将领获取经验值(unit.获取升级需要经验(level));
                    general.ReplaceAll(JObject.FromObject(unit).Properties());
                });
                if (!JToken.DeepEquals(JObject.FromObject(original), JObject.FromObject(shared.ToObject<山贼属性信息>())))
                    throw new InvalidOperationException("原山贼生成序列不同 seed=" + (9000 + i));
                checks++;
            }
            Debug.unityLogger.logEnabled = logging;
            Debug.Log("COMBAT_RULES_PASSED checks=" + checks);
            UnityEngine.Object.DestroyImmediate(root);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.unityLogger.logEnabled = logging;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            Debug.LogError(error);
            EditorApplication.Exit(1);
        }
    }

    private static JObject Create(int id)
    {
        var configuration = 全局将领库.查询指定ID的将领数据(id);
        configuration.获取随机属性();
        var general = new 将领信息(); general.生成将领数据(configuration);
        general.将领属性.初始属性.名字 = 随机姓名.生成随机姓名();
        return JObject.FromObject(general);
    }
    private static 将领信息 General(double troopId, int career, int identity)
    {
        var general = new 将领信息(); general.生成指定ID将领(1);
        general.将领属性.初始属性.职业 = career;
        general.将领属性.最终属性.攻击 = 123.5;
        general.将领属性.最终属性.防御 = 72.5;
        general.将领属性.最终属性.生命值 = 250;
        general.将领配兵.ID = troopId; general.将领配兵.数量 = 875;
        general.详细信息.剩余兵力 = 875; general.详细信息.身份 = identity;
        return general;
    }
    private static CombatProfile Profile(int index)
    {
        玩家数据 player = 全局变量.所有玩家数据表[index];
        国家信息库类 country = 全局方法类.获取指定名字的国家(player.基础信息.国家);
        return CombatModifiers.Create(JObject.FromObject(player), country == null ? null : JObject.FromObject(country), 全局变量.难度,
            TIME.getTime() * 1000, (_, name, utc) => player.获取指定状态加成(name));
    }
    private static double Call(将领功能 unit, string method, params object[] values)
    {
        return (double)typeof(将领功能).GetMethod(method, Private).Invoke(unit, values);
    }
    private static void Equal(double original, double shared)
    {
        if (original != shared) throw new InvalidOperationException("原战斗公式不同 original=" + original + " shared=" + shared);
        checks++;
    }
}
