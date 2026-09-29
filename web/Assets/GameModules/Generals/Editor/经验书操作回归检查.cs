#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dwsg.Generals;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using 玩家数据结构;

public static class 经验书操作回归检查
{
	static FieldInfo Field(Type type, string name) { return type.GetField(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
	static object Queue { get { return Field(typeof(GameNetwork), "pending").GetValue(null); } }
	static object[] Commands() { return ((IEnumerable)Queue).Cast<object>().ToArray(); }
	static GameCommand Command(object pending) { return (GameCommand)Field(pending.GetType(), "Command").GetValue(pending); }
	static void ClearQueue() { Queue.GetType().GetMethod("Clear").Invoke(Queue, null); Field(typeof(GeneralsClientAdapter), "pending").SetValue(null, false); }
	static string Data() { return JsonConvert.SerializeObject(new { players = 全局变量.所有玩家数据表, armies = 全局变量.军情列表 }); }
	static void Assert(bool value, string name) { if (!value) throw new InvalidOperationException(name); }

	public static void 执行()
	{
		if (!Application.isBatchMode) throw new InvalidOperationException("请在唯一临时工程离屏批处理执行");
		int failures = 0;
		FieldInfo endpoint = Field(typeof(GameNetwork), "endpoint"), worker = Field(typeof(GameNetwork), "workerRunning"), pending = Field(typeof(GeneralsClientAdapter), "pending");
		MethodInfo snapshotSetter = typeof(GameNetwork).GetProperty("CurrentSnapshot").GetSetMethod(true);
		object previousEndpoint = endpoint.GetValue(null), previousWorker = worker.GetValue(null), previousPending = pending.GetValue(null);
		WorldSnapshot previousSnapshot = GameNetwork.CurrentSnapshot;
		Assert(Commands().Length == 0, "已有命令不能用于fixture检查");
		try
		{
			全局将领库.属性表 = new List<将领属性库类>(); 全局将领库.初始化将领库();
			全局装备库.属性表 = new List<装备属性库类>(); 全局装备库.初始化装备库();
			全局兵种库.属性表 = new List<兵种属性库类>(); 全局兵种库.初始化兵种库();
			全局道具库.道具列表 = new List<道具信息库类>(); 全局道具库.初始化道具库();
			加载资源.头像资源();
			全局变量.本机身份 = 0; 全局变量.第几个封地 = 0; 全局变量.将领状态图标资源表 = new Sprite[4];
			全局变量.将领编队图标资源表 = Resources.LoadAll<Sprite>("将领编队图标");
			Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/主场景.unity", OpenSceneMode.Single);
			将领列表显示 list = Find<将领列表显示>(scene); 使用道具脚本 chooser = list.使用道具脚本对象;
			显示背包物品 bag = Find<显示背包物品>(scene); 调整数量脚本 quantity = bag.调整数量脚本对象;
			全局变量.提示类 = Find<提示移动>(scene);
			for (Transform parent = 全局变量.提示类.transform; parent != null; parent = parent.parent) parent.gameObject.SetActive(true);
			list.将领属性信息对象.SetActive(false); list.将领装备信息对象.SetActive(false); list.将领配兵信息对象.SetActive(false); list.将领培养信息对象.SetActive(false);
			endpoint.SetValue(null, "http://experience-book-check.invalid"); worker.SetValue(null, true);
			Button open = ButtonFor(scene, list, "将领使用经验书"), use = ButtonFor(scene, chooser, "使用选中道具"), one = ButtonFor(scene, bag, "使用道具"), batch = ButtonFor(scene, bag, "批量使用道具"), confirm = ButtonFor(scene, quantity, "确认调整");
			Action<string, Action> check = (name, action) => { try { ClearQueue(); action(); Debug.Log("PASS M04 ExperienceBook " + name); } catch (Exception error) { failures++; Debug.LogError("FAIL M04 ExperienceBook " + name + " " + error); } };
			Func<int, WorldSnapshot> prepare = selected => Prepare(list, chooser, bag, snapshotSetter, selected);
			check("原五个persistent按钮与唯一原经验书配置", () => { Assert(open && use && one && batch && confirm, "原按钮缺失"); Assert(全局道具库.获取指定类型的道具列表("经验书").Count == 1, "经验书配置不是原唯一一本"); });
			check("chooser打开后移除前将领仍发送原选中stable目标", () => {
				WorldSnapshot snapshot = prepare(1); open.onClick.Invoke();
				全局变量.所有玩家数据表[0].封地信息表[0].将领信息表.RemoveAt(0); ((JArray)snapshot.PrivatePlayer["封地信息表"][0]["将领信息表"]).RemoveAt(0);
				string before = Data(); UnityEngine.Random.State rng = UnityEngine.Random.state; use.onClick.Invoke();
				Assert(Commands().Length == 1 && Command(Commands()[0]).Payload.Value<string>("generalId") == "book-general-b", "选择器移位串目标");
				Assert(Command(Commands()[0]).Payload.Count == 3 && Command(Commands()[0]).Payload.Value<int>("count") == 1 && before == Data() && rng.Equals(UnityEngine.Random.state), "联机预扣库存经验或改RNG");
			});
			check("三原使用按钮pending时无本地XP库存重算或重复queue", () => {
				prepare(1); open.onClick.Invoke(); batch.onClick.Invoke(); pending.SetValue(null, true);
				string before = Data(); UnityEngine.Random.State rng = UnityEngine.Random.state; use.onClick.Invoke(); one.onClick.Invoke(); confirm.onClick.Invoke();
				Assert(Commands().Length == 0 && before == Data() && rng.Equals(UnityEngine.Random.state), "pending仍执行原本地道具效果");
			});
			check("chooser连续点击仅发送同一个原book请求", () => { prepare(1); open.onClick.Invoke(); use.onClick.Invoke(); string nonce = Command(Commands()[0]).RequestId; use.onClick.Invoke(); Assert(Commands().Length == 1 && Command(Commands()[0]).RequestId == nonce, "连点再次扣书"); });
			check("背包单用按原默认首将且不复用旧chooser目标", () => { prepare(1); open.onClick.Invoke(); string before = Data(); one.onClick.Invoke(); Assert(Commands().Length == 1 && Command(Commands()[0]).Payload.Value<string>("generalId") == "book-general-a" && before == Data(), "背包复用了另一选择器旧目标"); });
			check("原批用打开冻结首将后列表重排仍为同stableID并支持101", () => {
				WorldSnapshot snapshot = prepare(1); batch.onClick.Invoke();
				var owned = 全局变量.所有玩家数据表[0].封地信息表[0].将领信息表; owned.Reverse();
				var generals = (JArray)snapshot.PrivatePlayer["封地信息表"][0]["将领信息表"]; snapshot.PrivatePlayer["封地信息表"][0]["将领信息表"] = new JArray(generals.Reverse().Select(g => g.DeepClone()));
				quantity.数量滑条对象.value = 101; quantity.滑条改变购买数量(); string before = Data(); confirm.onClick.Invoke();
				Assert(Commands().Length == 1 && Command(Commands()[0]).Payload.Value<string>("generalId") == "book-general-a" && Command(Commands()[0]).Payload.Value<int>("count") == 101 && before == Data(), "原批用重排或101限制错误");
			});
			check("批用原首将已消失不会消费在替补首将", () => {
				WorldSnapshot snapshot = prepare(0); batch.onClick.Invoke(); 全局变量.所有玩家数据表[0].封地信息表[0].将领信息表.RemoveAt(0); ((JArray)snapshot.PrivatePlayer["封地信息表"][0]["将领信息表"]).RemoveAt(0);
				string before = Data(); confirm.onClick.Invoke(); Assert(Commands().Length == 0 && before == Data(), "被移除目标由替补继承");
			});
			check("原批用非整数负数零与int32溢出全拒且不发nonce", () => {
				prepare(0); batch.onClick.Invoke(); string before = Data();
				foreach (double count in new[] { 0.0, -1.0, 0.5, 2147483648.0, double.NaN, double.PositiveInfinity }) { Field(typeof(调整数量脚本), "调整数量").SetValue(quantity, count); confirm.onClick.Invoke(); }
				Assert(Commands().Length == 0 && before == Data(), "非法数量仍发命令或扣书");
			});
			check("原数量输入无效书数量不退出且旧回执不关闭重开对话框", () => {
				prepare(0); batch.onClick.Invoke(); string before = Data();
				foreach (string value in new[] { "0", "-1", "1.5", "2147483648", "abc" }) { quantity.输入数量对象.text = value; quantity.输入改变购买数量(); }
				Assert(Commands().Length == 0 && before == Data(), "原数量输入非法值执行本地效果");
				quantity.数量滑条对象.value = 1; quantity.滑条改变购买数量(); confirm.onClick.Invoke(); object queued = Commands()[0];
				batch.onClick.Invoke(); Assert(quantity.gameObject.activeSelf, "原批用未重开"); Queue.GetType().GetMethod("Dequeue").Invoke(Queue, null);
				((Action<GameResult>)Field(queued.GetType(), "Completed").GetValue(queued))(GameResult.Success(new JObject()));
				Assert(quantity.gameObject.activeSelf && before == Data(), "旧回执关闭了新打开的数量窗或重复写经验");
			});
			check("chooser连接角色变化后旧目标不能发送", () => { WorldSnapshot snapshot = prepare(1); open.onClick.Invoke(); snapshot.PlayerId = "different-player"; string before = Data(); use.onClick.Invoke(); Assert(Commands().Length == 0 && before == Data(), "旧角色目标泄漏"); });
			check("外人映射以及已消失映射不能消费首将", () => {
				WorldSnapshot snapshot = prepare(1); open.onClick.Invoke(); snapshot.PrivatePlayer["entityMappings"]["generals"]["book-general-b"]["playerId"] = "foreign-player"; string before = Data(); use.onClick.Invoke(); Assert(Commands().Length == 0 && before == Data(), "外人stable目标仍能使用");
			});
			check("fixture原快照回执应用保留军情将领引用且callback不再次经验重算", () => {
				WorldSnapshot snapshot = prepare(1); open.onClick.Invoke(); use.onClick.Invoke(); object queued = Commands()[0]; GameCommand command = Command(queued);
				将领信息 held = 全局变量.所有玩家数据表[0].封地信息表[0].将领信息表[1]; JObject canonical = GeneralExperienceBookRules.Execute(snapshot.PrivatePlayer, held.ID, "太平要术", 1, JArray.FromObject(全局道具库.道具列表), snapshot.ServerUtcMs / 1000, out GameResult result);
				snapshot.PrivatePlayer = canonical; LegacySnapshotAdapter.Apply(snapshot, new JObject(), canonical);
				Assert(ReferenceEquals(held, 全局变量.所有玩家数据表[0].封地信息表[0].将领信息表[1]) && ReferenceEquals(held, 全局变量.军情列表[0].队列将领列表[0]), "快照替换了原军情引用");
				string after = Data(); Queue.GetType().GetMethod("Dequeue").Invoke(Queue, null); ((Action<GameResult>)Field(queued.GetType(), "Completed").GetValue(queued))(result);
				Assert(Data() == after && !(bool)pending.GetValue(null) && held.将领属性.成长点数.等级 > 1 && 全局变量.所有玩家数据表[0].背包道具列表.获取指定道具数量("太平要术") == 100, "callback二次经验或扣书/未释放pending");
				use.onClick.Invoke(); Assert(Commands().Length == 1 && Command(Commands()[0]).RequestId != command.RequestId, "已确认回执后正常新nonce不能使用");
			});
			check("重开原将领chooser重新冻结当前目标", () => { prepare(1); open.onClick.Invoke(); Select(list, 0); open.onClick.Invoke(); use.onClick.Invoke(); Assert(Command(Commands()[0]).Payload.Value<string>("generalId") == "book-general-a", "重开继承旧将领"); });
		}
		catch (Exception error) { failures++; Debug.LogError(error); }
		finally { ClearQueue(); endpoint.SetValue(null, previousEndpoint); worker.SetValue(null, previousWorker); pending.SetValue(null, previousPending); snapshotSetter.Invoke(null, new object[] { previousSnapshot }); }
		Debug.Log("M04 原经验书操作回归结束，失败数=" + failures);
		EditorApplication.Exit(failures == 0 ? 0 : 1);
	}

	static WorldSnapshot Prepare(将领列表显示 list, 使用道具脚本 chooser, 显示背包物品 bag, MethodInfo snapshotSetter, int selected)
	{
		// 最小原配置2将领/101本书/私有映射展示fixture；不认证、不购买、不授予真实账号。
		玩家数据 own = new 玩家数据(); own.基础信息.ID = 0; own.封地信息表.Add(new 封地信息 { ID = 1 });
		for (int i = 0; i < 5; i++) own.编队信息表.Add(new List<int> { -1, -1, -1, -1, -1 });
		for (int i = 0; i < 2; i++) { 将领信息 general = new 将领信息(); general.生成将领数据(全局将领库.属性表[0]); general.ID = i + 1; general.详细信息.身份 = 0; own.封地信息表[0].将领信息表.Add(general); }
		own.背包道具列表.宝物道具列表.Add(new 道具信息("太平要术", 101.0));
		全局变量.所有玩家数据表 = new List<玩家数据> { own }; 全局变量.本机身份 = 0; 全局变量.军情列表 = new List<军情信息> { new 军情信息 { 队列将领列表 = new List<将领信息> { own.封地信息表[0].将领信息表[1] } } };
		JObject privatePlayer = JObject.FromObject(own); privatePlayer["entityMappings"] = new JObject { ["generals"] = new JObject {
			["book-general-a"] = new JObject { ["playerId"] = "book-own", ["legacyId"] = 1 }, ["book-general-b"] = new JObject { ["playerId"] = "book-own", ["legacyId"] = 2 } } };
		JObject publicPlayer = JObject.FromObject(own); publicPlayer["playerId"] = "book-own";
		WorldSnapshot snapshot = new WorldSnapshot { WorldId = "book-ui-fixture", PlayerId = "book-own", ServerUtcMs = 1790660000000,
			PrivatePlayer = privatePlayer, PublicWorld = new JObject { ["玩家列表"] = new JArray(publicPlayer) } };
		snapshotSetter.Invoke(null, new object[] { snapshot });
		Field(typeof(将领列表显示), "第几个玩家").SetValue(list, 0); Field(typeof(将领列表显示), "第几页将领").SetValue(list, 0); list.显示第几个封地 = -1;
		list.要显示的将领列表.Clear(); list.要显示的将领列表.Add(new 将领索引信息(0, 0)); list.要显示的将领列表.Add(new 将领索引信息(0, 1)); Select(list, selected);
		chooser.要显示的列表 = 全局道具库.获取指定类型的道具列表("经验书"); chooser.刷新显示(); chooser.显示列表对象.transform.GetChild(0).GetComponent<Toggle>().isOn = true;
		bag.已选择道具名字.text = "太平要术"; bag.已选中道具.text = "0"; bag.要显示的物品列表 = own.背包道具列表.宝物道具列表;
		Field(typeof(显示背包物品), "显示类型").SetValue(bag, 1); Field(typeof(显示背包物品), "显示第几页").SetValue(bag, 1);
		return snapshot;
	}
	static void Select(将领列表显示 list, int selected) { for (int i = 0; i < 5; i++) list.将领列表对象[i].transform.GetChild(0).gameObject.SetActive(i == selected); }
	static Button ButtonFor(Scene scene, Component target, string method) { foreach (GameObject root in scene.GetRootGameObjects()) foreach (Button button in root.GetComponentsInChildren<Button>(true)) for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) if (button.onClick.GetPersistentTarget(i) == target && button.onClick.GetPersistentMethodName(i) == method) return button; throw new InvalidOperationException("原persistent按钮未绑定 " + method); }
	static T Find<T>(Scene scene) where T : Component { foreach (GameObject root in scene.GetRootGameObjects()) { T result = root.GetComponentInChildren<T>(true); if (result != null) return result; } throw new InvalidOperationException(typeof(T).Name); }
}
#endif
