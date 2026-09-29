#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using 玩家数据结构;

public static class 军情战场回归检查
{
    // 离屏入口：-batchmode -nographics -executeMethod 军情战场回归检查.运行检查
    public static void 运行检查()
    {
        if (!Application.isBatchMode || Application.isPlaying)
        {
            throw new InvalidOperationException("军情战场回归检查只允许在编辑器离屏模式运行。");
        }
        List<军情信息> 原军情 = 全局变量.军情列表;
        List<山贼属性信息> 原山贼 = 全局变量.所有山贼数据列表;
        GameObject 原山贼预制件 = 全局变量.山贼战斗场景pre;
        GameObject 原城池预制件 = 全局变量.城池战斗场景pre;
        try
        {
            全局变量.山贼战斗场景pre = Resources.Load<GameObject>("战斗场景预制件/山贼战斗场景");
            全局变量.城池战斗场景pre = Resources.Load<GameObject>("战斗场景预制件/城池战斗场景");
            断言(全局变量.山贼战斗场景pre != null && 全局变量.城池战斗场景pre != null, "原战场预制件必须可加载");
            检查已有战场("两个战场，目标A", 2, 0);
            检查已有战场("两个战场，目标B", 2, 1);
            检查已有战场("单个战场", 1, 0);
            检查已有战场("同坐标，目标山贼战场", 2, 0, true);
            检查已有战场("同坐标，目标城池战场", 2, 1, true);
            检查已有战场("已经进入，不再追加", 2, 0, false, true);
            检查未命中新建();
            Debug.Log("军情战场回归：7个用例全部通过，调用原检测军情列表协程。");
        }
        finally
        {
            全局变量.军情列表 = 原军情;
            全局变量.所有山贼数据列表 = 原山贼;
            全局变量.山贼战斗场景pre = 原山贼预制件;
            全局变量.城池战斗场景pre = 原城池预制件;
        }
    }

    private static void 检查已有战场(string 名称, int 战场数, int 目标序号, bool 同坐标不同类型 = false, bool 已进入 = false)
    {
        GameObject 根 = 创建任务(out 全局任务脚本 任务);
        IEnumerator 检测 = null;
        try
        {
            List<战斗系统> 战场 = new List<战斗系统>();
            for (int i = 0; i < 战场数; i++)
            {
                战场.Add(创建战场(任务, 同坐标不同类型 ? 10 : 10 + i, 10, 同坐标不同类型 ? i : 0));
            }
            战斗系统 目标 = 战场[目标序号];
            军情信息 军情 = 创建军情(目标.坐标x, 目标.坐标y, 目标.战场类型);
            军情.已进入战场 = 已进入;
            全局变量.军情列表.Add(军情);
            检测 = 取得原检测协程(任务);
            断言(检测.MoveNext(), 名称 + "：原协程应完成一轮检测");
            Debug.Log(名称 + "：A队列=" + 战场[0].攻方要渲染的编队将领列表.Count + (战场数 > 1 ? "，B队列=" + 战场[1].攻方要渲染的编队将领列表.Count : "") + "，已进入=" + 军情.已进入战场);
            for (int i = 0; i < 战场.Count; i++)
            {
                int 期待队列数 = !已进入 && i == 目标序号 ? 1 : 0;
                断言(战场[i].攻方要渲染的编队将领列表.Count == 期待队列数, 名称 + "：战场" + i + "队列数应为" + 期待队列数);
            }
            if (!已进入)
            {
                断言(ReferenceEquals(目标.攻方要渲染的编队将领列表[0], 军情.队列将领列表), 名称 + "：必须追加原军情的将领列表");
            }
            断言(军情.已进入战场, 名称 + "：原已进入标记应保留");
            断言(检测.MoveNext(), 名称 + "：第二轮检测应继续运行");
            for (int i = 0; i < 战场.Count; i++)
            {
                int 期待队列数 = !已进入 && i == 目标序号 ? 1 : 0;
                断言(战场[i].攻方要渲染的编队将领列表.Count == 期待队列数, 名称 + "：第二轮不得重复追加");
            }
            断言(任务.战斗地图列表.transform.childCount == 战场数, 名称 + "：已有战场不得触发新建");
            Debug.Log("军情战场回归通过：" + 名称);
        }
        finally
        {
            (检测 as IDisposable)?.Dispose();
            UnityEngine.Object.DestroyImmediate(根);
        }
    }

    private static void 检查未命中新建()
    {
        GameObject 根 = 创建任务(out 全局任务脚本 任务);
        IEnumerator 检测 = null;
        try
        {
            战斗系统 A = 创建战场(任务, 10, 10, 0);
            战斗系统 B = 创建战场(任务, 11, 10, 0);
            将领信息 守将 = new 将领信息 { 详细信息 = new 详细信息() };
            全局变量.所有山贼数据列表.Add(new 山贼属性信息
            {
                坐标x = 12,
                坐标y = 10,
                将领数据列表 = new List<将领信息> { 守将 }
            });
            军情信息 军情 = 创建军情(12, 10, 0);
            全局变量.军情列表.Add(军情);
            long 检测前时间 = TIME.getTime();
            检测 = 取得原检测协程(任务);
            断言(检测.MoveNext(), "未命中：原协程应完成一轮检测");
            断言(任务.战斗地图列表.transform.childCount == 3, "未命中：应从原山贼预制件新建一个战场");
            战斗系统 新战场 = 任务.战斗地图列表.transform.GetChild(2).GetComponentInChildren<战斗系统>(true);
            断言(新战场 != null && 新战场.坐标x == 12 && 新战场.坐标y == 10 && 新战场.战场类型 == 0, "未命中：新战场目标与类型应正确");
            断言(新战场.创建时间 >= 检测前时间 && 新战场.创建时间 <= TIME.getTime(), "未命中：应保留原创建时间设置");
            断言(A.攻方要渲染的编队将领列表.Count == 0 && B.攻方要渲染的编队将领列表.Count == 0, "未命中：不得追加到已有战场");
            断言(新战场.攻方要渲染的编队将领列表.Count == 1 && ReferenceEquals(新战场.攻方要渲染的编队将领列表[0], 军情.队列将领列表), "未命中：原攻方队列应加入新战场");
            断言(新战场.守方要渲染的编队将领列表.Count == 1 && 新战场.守方要渲染的编队将领列表[0].Count == 1 && ReferenceEquals(新战场.守方要渲染的编队将领列表[0][0], 守将), "未命中：应使用实际山贼列表中的守将");
            断言(守将.详细信息.坑位颜色 == 1.0 && 军情.已进入战场, "未命中：原守将颜色与已进入标记应保留");
            断言(检测.MoveNext(), "未命中：第二轮检测应继续运行");
            断言(任务.战斗地图列表.transform.childCount == 3 && 新战场.攻方要渲染的编队将领列表.Count == 1, "未命中：第二轮不得重复新建或追加");
            Debug.Log("军情战场回归通过：未命中，原山贼预制件新建");
        }
        finally
        {
            (检测 as IDisposable)?.Dispose();
            UnityEngine.Object.DestroyImmediate(根);
        }
    }

    private static GameObject 创建任务(out 全局任务脚本 任务)
    {
        GameObject 根 = new GameObject("军情战场回归临时对象");
        根.SetActive(false);
        任务 = 根.AddComponent<全局任务脚本>();
        任务.战斗地图列表 = new GameObject("战斗地图列表");
        任务.战斗地图列表.transform.SetParent(根.transform, false);
        全局变量.军情列表 = new List<军情信息>();
        全局变量.所有山贼数据列表 = new List<山贼属性信息>();
        return 根;
    }

    private static 战斗系统 创建战场(全局任务脚本 任务, int x, int y, int 类型)
    {
        GameObject 对象 = UnityEngine.Object.Instantiate(类型 == 0 ? 全局变量.山贼战斗场景pre : 全局变量.城池战斗场景pre);
        对象.SetActive(false);
        对象.transform.SetParent(任务.战斗地图列表.transform, false);
        战斗系统 战场 = 对象.GetComponentInChildren<战斗系统>(true);
        断言(战场 != null, "原战场预制件必须包含真实战斗系统组件");
        战场.坐标x = x;
        战场.坐标y = y;
        战场.战场类型 = 类型;
        return 战场;
    }

    private static 军情信息 创建军情(int x, int y, int 类型)
    {
        return new 军情信息
        {
            坐标x = x,
            坐标y = y,
            战场类型 = 类型,
            到达时间 = TIME.getTime() - 1,
            队列将领列表 = new List<将领信息> { new 将领信息() }
        };
    }

    private static IEnumerator 取得原检测协程(全局任务脚本 任务)
    {
        MethodInfo 方法 = typeof(全局任务脚本).GetMethod("检测军情列表", BindingFlags.Instance | BindingFlags.NonPublic);
        断言(方法 != null, "原检测军情列表方法必须存在");
        return (IEnumerator)方法.Invoke(任务, null);
    }

    private static void 断言(bool 条件, string 信息)
    {
        if (!条件)
        {
            throw new InvalidOperationException("军情战场回归失败：" + 信息);
        }
    }
}
#endif
