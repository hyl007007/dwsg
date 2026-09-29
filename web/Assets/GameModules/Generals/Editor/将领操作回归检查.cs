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
			全局道具库.道具列表 = new List<道具信息库类>(); 全局道具库.初始化道具库();
			全局变量.本机身份 = 0; 全局变量.第几个封地 = 0;
			全局变量.将领状态图标资源表 = new Sprite[4];
			全局变量.将领编队图标资源表 = Resources.LoadAll<Sprite>("将领编队图标");
			全局变量.所有装备资源表 = new List<Sprite>(Resources.LoadAll<Sprite>("装备图片"));
			全局变量.装备初始图片 = Resources.LoadAll<Sprite>("装备初始图片");
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
			check("原酒馆免费生成五候选，实际招募到原封地无额外费用", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; player.财产信息.黄金 = 777;
				招募将领 tavern = Find<招募将领>(scene); tavern.自动刷新招募将领();
				Assert(tavern.将领列表.Count == 5, "未生成五候选");
				将领信息 selected = tavern.将领列表[0];
				for (int i = 0; i < 5; i++) tavern.将领列表对象.transform.GetChild(i).GetChild(12).gameObject.SetActive(i == 0);
				tavern.招募选中将领();
				Assert(tavern.将领列表.Count == 4 && player.封地信息表[0].将领信息表.Count == 2 && ReferenceEquals(selected, player.封地信息表[0].将领信息表[1]), "招募未保留实际候选实例");
				Assert(player.财产信息.黄金 == 777, "原免费招募被收费");
			});
			check("原三道具酒馆刷新按最小堆消费一次", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 招募将领 tavern = Find<招募将领>(scene);
				string[] names = { "招贤令", "招贤金榜", "皇榜" }; Action[] refresh = { tavern.招贤令刷新招募将领, tavern.金榜刷新招募将领, tavern.皇榜刷新招募将领 };
				for (int i = 0; i < 3; i++)
				{
					List<道具信息> stacks = player.背包道具列表.获取道具分类列表(names[i]);
					stacks.Clear(); stacks.Add(new 道具信息(names[i], 3)); stacks.Add(new 道具信息(names[i], 1));
					refresh[i](); Assert(stacks.Count == 1 && stacks[0].数量 == 3 && tavern.将领列表.Count == 5, "道具消费或候选数量错误");
				}
			});
			check("原治疗确认按当前身份治疗，半价扣费并保留原列表实例", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 封地信息 fief = player.封地信息表[0];
				List<闲兵信息> idle = fief.闲兵信息表; List<伤兵信息> wounded = fief.伤兵信息表;
				闲兵信息 existing = idle.Find(p => p.ID == 104); double previous = existing.数量;
				伤兵信息 healed = new 伤兵信息 { ID = 104, 数量 = 1 }; 伤兵信息 other = new 伤兵信息 { ID = 201, 数量 = 3 };
				wounded.Add(healed); wounded.Add(other); player.财产信息.铜钱 = 121; player.财产信息.粮食 = 350;
				调整数量脚本 treatment = Find<封地信息界面UI脚本>(scene).调整数量脚本对象; treatment.调整类型 = 4; treatment.第几个玩家 = 99; treatment.第几个封地 = 0; treatment.兵种ID = 104;
				typeof(调整数量脚本).GetField("调整数量", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(treatment, 1.0); treatment.gameObject.SetActive(true); treatment.确认调整();
				Assert(player.财产信息.铜钱 == 0 && player.财产信息.粮食 == 0 && existing.数量 == previous + 1, "原半价治疗扣费或闲兵不正确");
				Assert(ReferenceEquals(idle, fief.闲兵信息表) && ReferenceEquals(wounded, fief.伤兵信息表) && ReferenceEquals(existing, idle.Find(p => p.ID == 104)), "原兵力列表或条目被替换");
				Assert(wounded.Count == 1 && ReferenceEquals(wounded[0], other) && healed.数量 == 0 && !treatment.gameObject.activeSelf, "治疗后伤兵移除或原面板状态不正确");
			});
			check("原治疗确认余额不足无扣费退兵且面板保留", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; player.财产信息.铜钱 = 120; player.财产信息.粮食 = 350;
				player.封地信息表[0].伤兵信息表.Add(new 伤兵信息 { ID = 104, 数量 = 1 });
				调整数量脚本 treatment = Find<封地信息界面UI脚本>(scene).调整数量脚本对象; treatment.调整类型 = 4; treatment.第几个封地 = 0; treatment.兵种ID = 104;
				typeof(调整数量脚本).GetField("调整数量", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(treatment, 1.0); treatment.gameObject.SetActive(true); string before = Snapshot(); treatment.确认调整();
				Assert(before == Snapshot() && treatment.gameObject.activeSelf, "失败治疗改变原数据或关闭面板");
			});
			check("原三属性单点与全加守恒并保留成长点和军情引用", () => {
				将领信息 general = ready(0); 成长点数 points = general.将领属性.成长点数; points.等级 = 10; points.总分配点数 = 9;
				list.将领武力加点(); list.将领智力加点(); list.将领统帅加点(); list.将领武力全加();
				Assert(points.武力分配点 == 7 && points.智力分配点 == 1 && points.统帅分配点 == 1 && points.总分配点数 == 0, "原单点/全加点数不守恒");
				Assert(ReferenceEquals(points, general.将领属性.成长点数) && ReferenceEquals(general, 全局变量.军情列表[0].队列将领列表[0]), "原成长点或军情实例被更换");
			});
			check("原免费洗点归还等级减一点数且保留等级经验成长", () => {
				将领信息 general = ready(0); 成长点数 points = general.将领属性.成长点数; points.等级 = 10; points.总分配点数 = 0; points.武力分配点 = 3; points.智力分配点 = 4; points.统帅分配点 = 2;
				general.详细信息.经验 = 123; double growth = general.将领属性.初始属性.成长; 玩家数据 player = 全局变量.所有玩家数据表[0]; string inventory = JsonConvert.SerializeObject(player.背包道具列表); string money = JsonConvert.SerializeObject(player.财产信息);
				list.将领洗点();
				Assert(points.武力分配点 == 0 && points.智力分配点 == 0 && points.统帅分配点 == 0 && points.总分配点数 == 9 && points.等级 == 10 && general.详细信息.经验 == 123 && general.将领属性.初始属性.成长 == growth, "洗点改变原等级经验成长或归还点数错误");
				Assert(inventory == JsonConvert.SerializeObject(player.背包道具列表) && money == JsonConvert.SerializeObject(player.财产信息), "原免费洗点被收取额外道具或货币");
			});
			check("原无可分配点或出征将领加点洗点拒绝后数据不变", () => {
				ready(0); string before = Snapshot(); list.将领武力加点(); Assert(before == Snapshot(), "无点数仍消耗成负数");
				将领信息 occupied = ready(1); occupied.将领属性.成长点数.等级 = 10; occupied.将领属性.成长点数.总分配点数 = 9; before = Snapshot();
				list.将领智力全加(); list.将领洗点(); Assert(before == Snapshot(), "出征加点/洗点改变原军情或将领数据");
			});
			check("原培养次数增减受库存及1至100限制", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0];
				player.背包道具列表.宝物道具列表.Add(new 道具信息("将神魂", 150));
				list.培养减少次数(); Assert(general.将领培养.培养次数 == 1, "次数降到零");
				list.培养增加次数(); Assert(general.将领培养.培养次数 == 2, "原增加按钮未生效");
				general.将领培养.培养次数 = 100; list.培养增加次数(); Assert(general.将领培养.培养次数 == 100, "超过100次");
				player.背包道具列表.宝物道具列表[0].数量 = 1; general.将领培养.培养次数 = 1;
				list.培养增加次数(); Assert(general.将领培养.培养次数 == 1, "次数超过魂库存");
			});
			check("原联机培养增减只修改面板选择，不预写角色快照", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0];
				player.背包道具列表.宝物道具列表.Add(new 道具信息("将神魂", 3));
				FieldInfo endpoint = typeof(Dwsg.Network.GameNetwork).GetField("endpoint", BindingFlags.Static | BindingFlags.NonPublic); object previous = endpoint.GetValue(null);
				try
				{
					endpoint.SetValue(null, "ws://cultivation-check.invalid"); list.将领培养信息对象.SetActive(true);
					string before = Snapshot(); list.培养增加次数();
					string count = list.将领培养信息对象.transform.GetChild(4).GetChild(3).GetChild(1).GetComponent<UnityEngine.UI.Text>().text;
					Assert(before == Snapshot() && general.将领培养.培养次数 == 1 && count == "2", "联机增加次数预写候选数据或未显示");
					list.培养减少次数(); Assert(before == Snapshot(), "联机减少次数预写角色数据");
				}
				finally { endpoint.SetValue(null, previous); list.将领培养信息对象.SetActive(false); }
			});
			check("原培养按钮消耗最小魂堆并保留将领及培养实例", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 将领培养 cultivation = general.将领培养;
				general.将领属性.初始属性.成长 = 79; cultivation.保底次数 = cultivation.保底上限;
				道具信息 large = new 道具信息("将神魂", 3), small = new 道具信息("将神魂", 1);
				player.背包道具列表.宝物道具列表.Add(large); player.背包道具列表.宝物道具列表.Add(small);
				list.培养将领();
				Assert(general.将领属性.初始属性.成长 == 80 && cultivation.保底次数 == 0 && cultivation.保底上限 == 50, "原保底提升成长或档位错误");
				Assert(ReferenceEquals(cultivation, general.将领培养) && ReferenceEquals(general, 全局变量.军情列表[0].队列将领列表[0]), "培养替换原将领引用");
				Assert(player.背包道具列表.宝物道具列表.Count == 1 && ReferenceEquals(large, player.背包道具列表.宝物道具列表[0]) && large.数量 == 3 && small.数量 == 0, "魂未从最小堆消费");
			});
			check("原出征及被俘将领均允许培养且原状态配兵不变", () => {
				foreach (double state in new[] { 1.0, 3.0 })
				{
					将领信息 general = ready(state); 玩家数据 player = 全局变量.所有玩家数据表[0];
					general.将领属性.初始属性.成长 = 60; general.将领培养.保底次数 = general.将领培养.保底上限;
					player.背包道具列表.宝物道具列表.Add(new 道具信息("将神魂", 1)); list.培养将领();
					Assert(general.将领属性.初始属性.成长 == 61 && general.详细信息.状态 == state && general.将领配兵.数量 == 37, "原合法状态培养被拒绝或改变军队");
				}
			});
			check("原批量培养达到99立即停止且只消费已执行的一次", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0];
				general.将领属性.初始属性.成长 = 98; general.将领培养.培养次数 = 3; general.将领培养.保底次数 = general.将领培养.保底上限;
				player.背包道具列表.宝物道具列表.Add(new 道具信息("将神魂", 3)); list.培养将领();
				Assert(general.将领属性.初始属性.成长 == 99 && player.背包道具列表.宝物道具列表[0].数量 == 2 && general.将领培养.保底上限 == 200, "原成长上限仍继续消费");
				string before = Snapshot(); list.培养将领(); Assert(before == Snapshot(), "上限培养仍改写状态");
			});
			check("原批量培养缺魂保留已培养结果，零魂失败不改写数据", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0];
				general.将领属性.初始属性.成长 = 60; general.将领培养.培养次数 = 3; general.将领培养.保底次数 = general.将领培养.保底上限;
				player.背包道具列表.宝物道具列表.Add(new 道具信息("将神魂", 1)); list.培养将领();
				Assert(general.将领属性.初始属性.成长 == 61 && player.背包道具列表.宝物道具列表.Count == 0, "缺魂丢失已执行培养");
				string before = Snapshot(); list.培养将领(); Assert(before == Snapshot(), "零魂仍培养或刷新属性");
			});
			check("原将领装备强化入口及单次按钮使用真实装备实例", () => {
				将领信息 general = ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 将领装备 gear = player.背包装备列表.武器装备列表[0];
				gear.品质 = 4; gear.已强化次数 = gear.获取装备强化保底次数();
				道具信息 large = new 道具信息("冰玉", 3), small = new 道具信息("冰玉", 1); player.背包道具列表.强化道具列表.Add(large); player.背包道具列表.强化道具列表.Add(small);
				for (int i = 0; i < 4; i++) list.将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(2).gameObject.SetActive(i == 1);
				list.显示要强化的装备(); 强化脚本 forge = list.强化脚本对象; Assert(ReferenceEquals(gear, forge.装备对象), "原强化入口未选中真实穿戴装备"); forge.强化装备();
				Assert(gear.强化等级 == 1 && gear.强化值 == 10 && gear.已强化次数 == 0 && gear.将领ID == general.ID, "原强化等级值或穿戴关联不正确");
				Assert(ReferenceEquals(gear, player.背包装备列表.武器装备列表[0]) && small.数量 == 0 && large.数量 == 3 && player.背包道具列表.强化道具列表.Count == 1, "原强化更换装备或未从最小材料堆消费");
			});
			check("原强化随机失败仍消费材料并提交保底次数", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 将领装备 gear = player.背包装备列表.武器装备列表[0]; gear.品质 = 4;
				player.背包道具列表.强化道具列表.Add(new 道具信息("冰玉", 1)); 强化脚本 forge = list.强化脚本对象; forge.装备对象 = gear; forge.显示指定装备();
				UnityEngine.Random.State randomState = default(UnityEngine.Random.State); int roll = 0;
				for (int seed = 1; seed <= 100; seed++) { UnityEngine.Random.InitState(seed); randomState = UnityEngine.Random.state; roll = UnityEngine.Random.Range(1, 10000); if (roll >= 5000) break; }
				Assert(roll >= 5000, "无法取得原随机失败区间"); UnityEngine.Random.state = randomState; forge.强化装备();
				Assert(gear.强化等级 == 0 && gear.强化值 == 0 && gear.已强化次数 == 1 && player.背包道具列表.强化道具列表.Count == 0, "原失败未消费材料或未累加保底");
			});
			check("原十次强化按钮到30级即停止并保存已执行消耗", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 将领装备 gear = player.背包装备列表.武器装备列表[0]; gear.品质 = 4; gear.强化等级 = 29; gear.强化值 = 290; gear.已强化次数 = 100;
				player.背包道具列表.强化道具列表.Add(new 道具信息("冰玉", 100)); 强化脚本 forge = list.强化脚本对象; forge.装备对象 = gear; forge.显示指定装备(); forge.批量强化();
				Assert(gear.强化等级 == 30 && gear.强化值 == 300 && player.背包道具列表.强化道具列表[0].数量 == 70, "原十次上限仍继续消费");
				string before = Snapshot(); forge.强化装备(); Assert(before == Snapshot(), "满级装备仍消费或更新状态");
			});
			check("原强化缺材料及跨玩家装备拒绝后原数据不变", () => {
				ready(0); 玩家数据 player = 全局变量.所有玩家数据表[0]; 将领装备 gear = player.背包装备列表.武器装备列表[0]; gear.品质 = 4;
				强化脚本 forge = list.强化脚本对象; forge.装备对象 = gear; forge.显示指定装备(); string before = Snapshot(); forge.强化装备(); Assert(before == Snapshot(), "缺材料仍更改装备");
				player.背包道具列表.强化道具列表.Add(new 道具信息("冰玉", 3)); player.背包装备列表.武器装备列表.Remove(gear); 全局变量.所有玩家数据表[1].背包装备列表.武器装备列表.Add(gear);
				before = Snapshot(); forge.强化装备(); Assert(before == Snapshot(), "跨玩家装备强化被放行");
			});
			check("原出征被俘穿戴装备允许强化而军队兵力不重置", () => {
				foreach (double state in new[] { 1.0, 3.0 })
				{
					将领信息 general = ready(state); 玩家数据 player = 全局变量.所有玩家数据表[0]; 将领装备 gear = player.背包装备列表.武器装备列表[0]; gear.品质 = 4; gear.已强化次数 = 20;
					player.背包道具列表.强化道具列表.Add(new 道具信息("冰玉", 1)); 强化脚本 forge = list.强化脚本对象; forge.装备对象 = gear; forge.显示指定装备(); forge.强化装备();
					Assert(gear.强化等级 == 1 && gear.将领ID == general.ID && general.详细信息.状态 == state && general.将领配兵.数量 == 37 && general.详细信息.剩余兵力 == 29, "原合法状态强化被拒或重置军队兵力");
				}
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
