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

public static class 将领操作回归检查
{
	public static void 执行()
	{
		if (!Application.isBatchMode) throw new InvalidOperationException("请在离屏批处理副本执行");
		int failures = 0;
		try
		{
			全局将领库.属性表 = new List<将领属性库类>(); 全局将领库.初始化将领库();
			全局装备库.属性表 = new List<装备属性库类>(); 全局装备库.初始化装备库();
			全局兵种库.属性表 = new List<兵种属性库类>(); 全局兵种库.初始化兵种库();
			全局变量.本机身份 = 0; 全局变量.第几个封地 = 0;
			全局变量.将领状态图标资源表 = new Sprite[4];
			全局变量.将领编队图标资源表 = Resources.LoadAll<Sprite>("将领编队图标");
			Assert(全局变量.将领编队图标资源表.Length >= 5, "原编队图标资源未加载");
			Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/主场景.unity", OpenSceneMode.Single);
			将领列表显示 list = Find<将领列表显示>(scene);
			全局变量.提示类 = Find<提示移动>(scene);
			for (Transform parent = 全局变量.提示类.transform; parent != null; parent = parent.parent) parent.gameObject.SetActive(true);
			list.将领属性信息对象.SetActive(false); list.将领装备信息对象.SetActive(false);
			list.将领配兵信息对象.SetActive(false); list.将领培养信息对象.SetActive(false);
			MethodInfo prepare = typeof(将领解雇回归检查).GetMethod("准备将领", BindingFlags.Static | BindingFlags.NonPublic);
			Action<string, Action> check = (name, action) => { try { action(); Debug.Log("PASS M04 " + name); } catch (Exception error) { failures++; Debug.LogError("FAIL M04 " + name + " " + error); } };
			Func<double, 将领信息> ready = state => (将领信息)prepare.Invoke(null, new object[] { list, state, false, true });
			check("原补满按钮库存不足时自动配50兵并保留军情实例", () => {
				将领信息 general = ready(0); list.补满配兵();
				Assert(general.将领配兵.数量 == 50 && 全局变量.所有玩家数据表[0].封地信息表[0].闲兵信息表[1].数量 == 0, "兵力不守恒");
				Assert(ReferenceEquals(general, 全局变量.军情列表[0].队列将领列表[0]), "原军情引用被替换");
			});
			check("原换兵按钮返还旧兵并分配新兵", () => {
				将领信息 general = ready(0); list.点击第1个配兵();
				Assert(general.将领配兵.ID == 204 && general.将领配兵.数量 == 17, "新配兵错误");
				Assert(全局变量.所有玩家数据表[0].封地信息表[0].闲兵信息表[1].数量 == 50, "旧兵未归还");
			});
			check("原退兵按钮重复执行不重复返还", () => {
				将领信息 general = ready(0); list.解除配兵(); list.解除配兵();
				Assert(general.将领配兵.数量 == 0 && 全局变量.所有玩家数据表[0].封地信息表[0].闲兵信息表[1].数量 == 50, "退兵重复计入");
			});
			check("实际占用将领补满退兵穿卸全部拒绝且玩家军情全量不变", () => {
				ready(1); string before = Snapshot();
				list.补满配兵(); list.解除配兵(); list.全部穿戴装备(); list.全部卸载装备();
				Assert(before == Snapshot(), "被拒绝后数据变化");
			});
			check("原卸装按钮保留装备实例与原配兵", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0];
				将领装备 gear = player.背包装备列表.武器装备列表[0];
				general.将领属性.成长点数.等级 = gear.获取装备等级();
				list.全部卸载装备();
				Assert(ReferenceEquals(gear, player.背包装备列表.武器装备列表[0]) && gear.将领ID == -1 && general.将领配兵.数量 == 37, "原装备或配兵被更换");
				list.全部穿戴装备(); Assert(gear.将领ID == general.ID, "原自动穿装入口未接通");
			});
			check("原编队实际入口加入与移除，同一将领禁止重复", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0];
				general.详细信息.编队 = 0; player.编队信息表[0][0] = -1;
				将领编队 formation = Find<将领编队>(scene); formation.获取要显示的将领列表();
				for (int i = 0; i < 5; i++) formation.编队列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(i == 0);
				formation.点击列表将领(0); Assert(player.编队信息表[0][0] == general.ID && general.详细信息.编队 == 1, "编队未加入");
				string before = Snapshot(); formation.点击列表将领(0); Assert(before == Snapshot(), "将领重复加入编队");
				for (int i = 0; i < 5; i++) for (int j = 0; j < 5; j++) formation.编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j).GetChild(3).gameObject.SetActive(i == 0 && j == 0);
				formation.编队列表读写(); Assert(player.编队信息表[0][0] == -1 && general.详细信息.编队 == 0, "编队未移除");
			});
		}
		catch (Exception error) { failures++; Debug.LogError(error); }
		Debug.Log("M04 原将领操作回归结束，失败数=" + failures);
		EditorApplication.Exit(failures == 0 ? 0 : 1);
	}
	static T Find<T>(Scene scene) where T : Component { foreach (GameObject root in scene.GetRootGameObjects()) { T result = root.GetComponentInChildren<T>(true); if (result != null) return result; } throw new InvalidOperationException(typeof(T).Name); }
	static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
	static string Snapshot() { return JsonConvert.SerializeObject(new { players = 全局变量.所有玩家数据表, armies = 全局变量.军情列表 }); }
}
#endif
