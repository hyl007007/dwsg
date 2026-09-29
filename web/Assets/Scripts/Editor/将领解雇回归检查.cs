#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using 玩家数据结构;

public static class 将领解雇回归检查
{
	public static void 执行()
	{
		if (!Application.isBatchMode)
		{
			throw new InvalidOperationException("请在离屏批处理副本中执行将领解雇回归检查。");
		}
		List<玩家数据> 原玩家列表 = 全局变量.所有玩家数据表;
		List<军情信息> 原军情列表 = 全局变量.军情列表;
		List<将领属性库类> 原将领库 = 全局将领库.属性表;
		List<装备属性库类> 原装备库 = 全局装备库.属性表;
		提示移动 原提示 = 全局变量.提示类;
		Sprite[] 原状态图标 = 全局变量.将领状态图标资源表;
		int 原身份 = 全局变量.本机身份;
		int 原封地 = 全局变量.第几个封地;
		Scene 检查场景 = default(Scene);
		bool 新打开场景 = false;
		int 失败数 = 0;
		try
		{
			全局将领库.属性表 = new List<将领属性库类>();
			全局将领库.初始化将领库();
			全局装备库.属性表 = new List<装备属性库类>();
			全局装备库.初始化装备库();
			全局变量.本机身份 = 0;
			全局变量.第几个封地 = 0;
			全局变量.将领状态图标资源表 = new Sprite[4];
			检查场景 = SceneManager.GetSceneByPath("Assets/Scenes/主场景.unity");
			if (!检查场景.IsValid() || !检查场景.isLoaded)
			{
				检查场景 = EditorSceneManager.OpenScene("Assets/Scenes/主场景.unity", OpenSceneMode.Additive);
				新打开场景 = true;
			}
			将领列表显示 界面 = 查找组件<将领列表显示>(检查场景);
			全局变量.提示类 = 查找组件<提示移动>(检查场景);
			for (Transform 节点 = 全局变量.提示类.transform; 节点 != null; 节点 = 节点.parent)
			{
				节点.gameObject.SetActive(true);
			}
			界面.将领属性信息对象.SetActive(false);
			界面.将领装备信息对象.SetActive(false);
			界面.将领配兵信息对象.SetActive(false);
			界面.将领培养信息对象.SetActive(false);

			Action<string, Action> 检查 = (名字, 业务检查) =>
			{
				try
				{
					业务检查();
					Debug.Log("M04 PASS " + 名字);
				}
				catch (Exception 异常)
				{
					失败数++;
					Debug.LogError("M04 FAIL " + 名字 + "\n" + 异常);
				}
			};
			检查("出征带兵穿装拒绝后完整数据不变", () =>
			{
				准备将领(界面, 1.0, false, true);
				检查拒绝不变更(界面);
			});
			检查("其他不可解雇状态拒绝后完整数据不变", () =>
			{
				准备将领(界面, 2.0, false, true);
				检查拒绝不变更(界面);
			});
			检查("无选择不变更", () =>
			{
				准备将领(界面, 0.0, false, true);
				界面.将领列表对象[0].transform.GetChild(0).gameObject.SetActive(false);
				检查拒绝不变更(界面);
			});
			检查("过期封地索引不变更", () =>
			{
				准备将领(界面, 0.0, false, true);
				界面.要显示的将领列表[0] = new 将领索引信息(1, 0);
				检查拒绝不变更(界面);
			});
			检查("过期将领索引不变更", () =>
			{
				准备将领(界面, 0.0, false, true);
				界面.要显示的将领列表[0] = new 将领索引信息(0, 1);
				检查拒绝不变更(界面);
			});
			检查("空闲普通将领解雇并合并退兵", () => 检查正常解雇(界面, 0.0, false, true));
			检查("俘虏普通将领解雇并新增退兵记录", () => 检查正常解雇(界面, 3.0, false, false));
			检查("空闲名将按原规则回归", () => 检查正常解雇(界面, 0.0, true, true));
			检查("俘虏名将按原规则回归", () => 检查正常解雇(界面, 3.0, true, true));
			检查("名将回归封地缺失时不变更", () =>
			{
				准备将领(界面, 0.0, true, true);
				全局变量.所有玩家数据表[2].封地信息表.Clear();
				检查拒绝不变更(界面);
			});
		}
		catch (Exception 异常)
		{
			失败数++;
			Debug.LogError("M04 回归入口失败\n" + 异常);
		}
		finally
		{
			全局变量.所有玩家数据表 = 原玩家列表;
			全局变量.军情列表 = 原军情列表;
			全局将领库.属性表 = 原将领库;
			全局装备库.属性表 = 原装备库;
			全局变量.提示类 = 原提示;
			全局变量.将领状态图标资源表 = 原状态图标;
			全局变量.本机身份 = 原身份;
			全局变量.第几个封地 = 原封地;
			if (新打开场景 && 检查场景.IsValid()) EditorSceneManager.CloseScene(检查场景, true);
		}
		Debug.Log("M04 将领解雇回归结束，失败数=" + 失败数);
		EditorApplication.Exit(失败数 == 0 ? 0 : 1);
	}

	private static T 查找组件<T>(Scene 场景) where T : Component
	{
		foreach (GameObject 根 in 场景.GetRootGameObjects())
		{
			T 组件 = 根.GetComponentInChildren<T>(true);
			if (组件 != null) return 组件;
		}
		throw new InvalidOperationException("原主场景未找到 " + typeof(T).Name);
	}

	private static 将领信息 准备将领(将领列表显示 界面, double 状态, bool 是否名将, bool 已有同种闲兵)
	{
		全局变量.所有玩家数据表 = new List<玩家数据>();
		全局变量.军情列表 = new List<军情信息>();
		for (int i = 0; i < 3; i++)
		{
			玩家数据 玩家 = new 玩家数据();
			玩家.基础信息.ID = i;
			玩家.封地信息表.Add(new 封地信息());
			for (int j = 0; j < 5; j++) 玩家.编队信息表.Add(new List<int> { -1, -1, -1, -1, -1 });
			全局变量.所有玩家数据表.Add(玩家);
		}
		玩家数据 所有者 = 全局变量.所有玩家数据表[0];
		将领属性库类 模板 = 全局将领库.属性表.Find(条目 => 是否名将 ? 条目.系列 == "名将" : 条目.ID == 1.0);
		断言(模板 != null, "原将领库缺少检查所需模板");
		将领信息 将领 = new 将领信息();
		将领.生成将领数据(模板);
		所有者.添加将领信息到列表(0, 将领);
		将领.详细信息.状态 = 状态;
		将领.详细信息.忠诚 = 61.0;
		将领.详细信息.剩余体力 = 7.0;
		将领.详细信息.剩余兵力 = 29.0;
		将领.详细信息.编队 = 1.0;
		所有者.编队信息表[0][0] = 将领.ID;
		将领.将领配兵.ID = 104.0;
		将领.将领配兵.数量 = 37.0;
		所有者.封地信息表[0].闲兵信息表.Add(new 闲兵信息 { ID = 204, 数量 = 17.0 });
		if (已有同种闲兵) 所有者.封地信息表[0].闲兵信息表.Add(new 闲兵信息 { ID = 104, 数量 = 13.0 });
		for (int i = 0; i < 4; i++)
		{
			string 类型 = new[] { "头盔", "武器", "铠甲", "坐骑" }[i];
			装备属性库类 装备模板 = 全局装备库.属性表.Find(条目 => 条目.类型 == 类型);
			断言(装备模板 != null, "原装备库缺少 " + 类型);
			所有者.背包装备列表.添加指定装备数据到背包(装备模板, 1.0);
			所有者.背包装备列表.获取指定部位列表(i)[0].将领ID = 将领.ID;
		}
		全局变量.军情列表.Add(new 军情信息 { 队列将领列表 = new List<将领信息> { 将领 } });
		typeof(将领列表显示).GetField("第几个玩家", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(界面, 0);
		typeof(将领列表显示).GetField("第几页将领", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(界面, 0);
		界面.显示第几个封地 = -1;
		界面.要显示的将领列表.Clear();
		界面.要显示的将领列表.Add(new 将领索引信息(0, 0));
		for (int i = 0; i < 5; i++) 界面.将领列表对象[i].transform.GetChild(0).gameObject.SetActive(i == 0);
		return 将领;
	}

	private static void 检查拒绝不变更(将领列表显示 界面)
	{
		string 原数据 = 数据快照();
		界面.解雇将领();
		断言(原数据 == 数据快照(), "解雇被拒绝后玩家或军情数据发生变化");
	}

	private static void 检查正常解雇(将领列表显示 界面, double 状态, bool 是否名将, bool 已有同种闲兵)
	{
		将领信息 将领 = 准备将领(界面, 状态, 是否名将, 已有同种闲兵);
		玩家数据 所有者 = 全局变量.所有玩家数据表[0];
		int 回归ID = 全局变量.所有玩家数据表[2].将领ID标识;
		界面.解雇将领();
		断言(所有者.封地信息表[0].将领信息表.Count == 0, "原将领列表未移除目标");
		断言(将领.将领配兵.ID == 0.0 && 将领.将领配兵.数量 == 0.0, "配兵未解除");
		闲兵信息 退兵 = 所有者.封地信息表[0].闲兵信息表.Find(条目 => 条目.ID == 104);
		断言(退兵 != null && 退兵.数量 == (已有同种闲兵 ? 50.0 : 37.0), "原退兵数量不正确");
		断言(所有者.封地信息表[0].闲兵信息表.Count == 2, "退兵记录重复或其他兵种丢失");
		断言(所有者.封地信息表[0].闲兵信息表.Find(条目 => 条目.ID == 204).数量 == 17.0, "无关兵种发生变化");
		for (int i = 0; i < 4; i++)
		{
			List<将领装备> 装备列表 = 所有者.背包装备列表.获取指定部位列表(i);
			断言(装备列表.Count == 1 && 装备列表[0].将领ID == -1, "装备丢失或未卸下");
		}
		List<将领信息> 回归列表 = 全局变量.所有玩家数据表[2].封地信息表[0].将领信息表;
		断言(回归列表.Count == (是否名将 ? 1 : 0), "普通将领删除或名将回归规则发生变化");
		if (是否名将)
		{
			断言(ReferenceEquals(回归列表[0], 将领), "名将回归未复用原将领");
			断言(将领.详细信息.忠诚 == 100.0 && 将领.详细信息.身份 == 2.0, "名将回归忠诚或归属不正确");
			断言(将领.ID == 回归ID && 全局变量.所有玩家数据表[2].将领ID标识 == 回归ID + 1, "名将回归ID分配规则发生变化");
		}
	}

	private static string 数据快照()
	{
		return JsonConvert.SerializeObject(new { 玩家 = 全局变量.所有玩家数据表, 军情 = 全局变量.军情列表 });
	}

	private static void 断言(bool 条件, string 信息)
	{
		if (!条件) throw new InvalidOperationException(信息);
	}
}
#endif
