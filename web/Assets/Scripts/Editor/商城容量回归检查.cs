#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

// 仅在离屏编译副本中用 -executeMethod 调用，不保存场景或玩家数据。
public static class 商城容量回归检查
{
	private static int 通过数;
	private static readonly FieldInfo 数量字段 = typeof(购买道具脚本).GetField("购买数量", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo 消息字段 = typeof(聊天系统).GetField("全部消息", BindingFlags.Static | BindingFlags.NonPublic);

	public static void 运行()
	{
		使用隔离数据(delegate
		{
			通过数 = 0;
			using (var 数据 = new 购买检查数据("交易品1", 300))
				检查购买("满包无同名堆", 数据, 1, false, "购买失败，背包容量不足");
			using (var 数据 = new 购买检查数据("将神魂", 300, 300, 0, 998))
			{
				检查购买("满包998堆买1", 数据, 1, true);
				检查堆(数据, 999);
				检查(数据.玩家.获取背包物品数量() == 300, "合堆不应增加槽位");
			}
			using (var 数据 = new 购买检查数据("将神魂", 300, 300, 0, 998))
				检查购买("满包998堆买2", 数据, 2, false, "购买失败，背包容量不足");
			using (var 数据 = new 购买检查数据("将神魂", 299, 300, 0, 998))
			{
				检查购买("有余位998堆买2", 数据, 2, true);
				检查堆(数据, 999, 1);
				检查(数据.玩家.获取背包物品数量() == 300, "拆堆应增加一个槽位");
			}
			using (var 数据 = new 购买检查数据("将神魂", 300, 300, 0, 999, 998))
			{
				检查购买("多个同名堆选择最小堆", 数据, 1, true);
				检查堆(数据, 999, 999);
				检查(数据.玩家.获取背包物品数量() == 300, "最小堆合并不应增加槽位");
			}
			using (var 数据 = new 购买检查数据("将神魂", 300, 300, 0, 999, 999, 300))
			{
				检查购买("100个合并到第三堆", 数据, 100, true);
				检查堆(数据, 999, 999, 400);
			}
			using (var 数据 = new 购买检查数据("将神魂", 300, 301))
			{
				检查购买("已扩展容量允许新堆", 数据, 1, true);
				检查(数据.玩家.获取背包物品数量() == 301, "应使用玩家当前扩展容量");
			}
			using (var 数据 = new 购买检查数据("将神魂", 301, 301))
				检查购买("扩展容量已满", 数据, 1, false, "购买失败，背包容量不足");
			using (var 数据 = new 购买检查数据("将神魂", 300, 300, 4))
				检查购买("四类装备均占槽位", 数据, 1, false, "购买失败，背包容量不足");
			using (var 数据 = new 购买检查数据("将神魂", 299, 300, 4))
			{
				检查购买("装备占位但有余位", 数据, 1, true);
				检查(数据.玩家.获取背包物品数量() == 300, "装备及新道具应合计300槽");
			}
			using (var 数据 = new 购买检查数据("将神魂", 0))
			{
				数据.玩家.财产信息.黄金 = 数据.商品.黄金售价;
				检查购买("黄金余额恰好足够", 数据, 1, true);
				检查(数据.玩家.财产信息.黄金 == 0, "足额购买后余额应为0");
			}
			using (var 数据 = new 购买检查数据("将神魂", 299))
			{
				检查购买("数量上界100及无限库存", 数据, 100, true);
				检查堆(数据, 100);
				检查(数据.玩家.获取背包物品数量() == 300, "100件道具只占一槽");
			}
			using (var 数据 = new 购买检查数据("疾风符", 0))
			{
				数据.玩家.财产信息.白银 = 数据.商品.白银售价;
				检查购买("白银余额恰好足够", 数据, 1, true, null, true);
			}
			using (var 数据 = new 购买检查数据("疾风符", 300, 300, 0, 998))
			{
				检查购买("白银满包合堆", 数据, 1, true, null, true);
				检查堆(数据, 999);
			}
			using (var 数据 = new 购买检查数据("疾风符", 300))
				检查购买("白银满包新堆拒绝", 数据, 1, false, "购买失败，背包容量不足", true);
			using (var 数据 = new 购买检查数据("将神魂", 0))
				检查购买("原商品不支持白银", 数据, 1, false, "该商品不支持此货币", true);
			using (var 数据 = new 购买检查数据("交易品1", 0))
			{
				数据.玩家.财产信息.黄金 = 数据.商品.黄金售价 * 数据.商品.限购数量;
				检查购买("购买全部有限库存", 数据, 数据.商品.限购数量, true);
				检查购买("库存售罄后拒绝", 数据, 1, false, "购买失败，超出购买限制");
			}
			using (var 数据 = new 购买检查数据("将神魂", 0))
			{
				数据.玩家.财产信息.黄金 = 数据.商品.黄金售价 - 1;
				检查购买("黄金余额不足", 数据, 1, false, "余额不足");
			}
			foreach (double 数量 in new[] { 0.0, -1.0, 101.0, 1.5, double.NaN, double.PositiveInfinity })
				using (var 数据 = new 购买检查数据("将神魂", 0))
					检查购买("非法数量" + 数量, 数据, 数量, false, "请选择 1 到 100 个整数数量");
			Debug.Log("SHOP_CAPACITY_REGRESSION_PASS cases=" + 通过数);
		});
	}

	// 同一检查文件配合修复前的两个生产脚本执行，记录原方法的真实错误行为。
	public static void 复现原缺陷()
	{
		使用隔离数据(delegate
		{
			using (var 数据 = new 购买检查数据("交易品1", 300))
			{
				数据.玩家.财产信息.黄金 = 数据.商品.黄金售价;
				double 原余额 = 数据.玩家.财产信息.黄金;
				int 原库存 = 数据.商品.限购数量;
				数量字段.SetValue(数据.脚本, 1.0);
				数据.脚本.黄金购买();
				检查(数据.玩家.获取背包物品数量() == 301, "修复前应复现新增第301槽");
				检查(数据.玩家.财产信息.黄金 == 0, "修复前应复现满包仍扣费");
				检查(数据.商品.限购数量 == 原库存 - 1, "修复前应复现满包仍减库存");
				检查(数据.玩家.背包道具列表.获取指定道具数量("交易品1") == 1, "修复前应复现满包仍发货");
				Debug.Log("SHOP_CAPACITY_BASELINE_REPRO slots=300->301 gold=" + 原余额 + "->0 stock=" + 原库存 + "->" + 数据.商品.限购数量 + " items=0->1");
			}
		});
	}

	private static void 检查购买(string 名称, 购买检查数据 数据, double 数量, bool 应成功, string 失败提示 = null, bool 使用白银 = false)
	{
		string 原状态 = 状态(数据);
		double 原黄金 = 数据.玩家.财产信息.黄金;
		double 原白银 = 数据.玩家.财产信息.白银;
		double 原数量 = 数据.玩家.背包道具列表.获取指定道具数量(数据.脚本.道具名字);
		int 原库存 = 数据.商品.限购数量;
		if (!double.IsNaN(数量) && !double.IsInfinity(数量) && 数量 >= 1 && 数量 <= 100 && 数量 == Math.Truncate(数量))
		{
			数据.脚本.数量滑条对象.value = (float)数量;
			数据.脚本.改变购买数量();
		}
		else
			数量字段.SetValue(数据.脚本, 数量);
		if (使用白银) 数据.脚本.白银购买();
		else 数据.脚本.黄金购买();
		var 消息 = (List<聊天消息>)消息字段.GetValue(null);
		检查(消息.Count > 0 && 消息[消息.Count - 1].内容 == (应成功 ? "购买成功!" : 失败提示), 名称 + "：交易提示不符合预期");
		if (应成功)
		{
			double 金额 = (使用白银 ? 数据.商品.白银售价 : 数据.商品.黄金售价) * 数量;
			检查(数据.玩家.财产信息.黄金 == 原黄金 - (使用白银 ? 0 : 金额), 名称 + "：黄金扣费错误");
			检查(数据.玩家.财产信息.白银 == 原白银 - (使用白银 ? 金额 : 0), 名称 + "：白银扣费错误");
			检查(数据.玩家.背包道具列表.获取指定道具数量(数据.脚本.道具名字) == 原数量 + 数量, 名称 + "：发货数量错误");
			检查(数据.商品.限购数量 == (原库存 == -1 ? -1 : 原库存 - (int)数量), 名称 + "：库存扣减错误");
		}
		else
			检查(状态(数据) == 原状态, 名称 + "：失败后余额、物品或库存发生变化");
		通过数++;
		Debug.Log("SHOP_CAPACITY_PASS " + 名称);
	}

	private static string 状态(购买检查数据 数据)
	{
		return JsonConvert.SerializeObject(new { 数据.玩家.财产信息, 数据.玩家.背包道具列表, 数据.玩家.背包装备列表, 数据.商品.限购数量 });
	}

	private static void 检查堆(购买检查数据 数据, params int[] 数量)
	{
		var 堆 = 数据.玩家.背包道具列表.获取道具分类列表(数据.脚本.道具名字).FindAll(道具 => 道具.名字 == 数据.脚本.道具名字);
		检查(堆.Count == 数量.Length, "同名堆数错误");
		for (int i = 0; i < 数量.Length; i++) 检查(堆[i].数量 == 数量[i], "第" + i + "堆数量错误");
	}

	private static void 检查(bool 条件, string 说明)
	{
		if (!条件) throw new InvalidOperationException(说明);
	}

	private static void 使用隔离数据(Action 操作)
	{
		检查(Application.isBatchMode && !Application.isPlaying, "请仅在离屏编译副本的批处理编辑模式执行");
		var 原玩家 = 全局变量.所有玩家数据表;
		int 原身份 = 全局变量.本机身份;
		var 原提示 = 全局变量.提示类;
		var 原头像 = 全局变量.所有道具头像资源表;
		var 原表 = new Dictionary<FieldInfo, object>();
		try
		{
			foreach (var 类型 in new[] { typeof(全局商城库), typeof(全局道具库), typeof(全局装备库) })
				foreach (var 字段 in 类型.GetFields(BindingFlags.Public | BindingFlags.Static))
				{
					原表.Add(字段, 字段.GetValue(null));
					字段.SetValue(null, Activator.CreateInstance(字段.FieldType));
				}
			全局商城库.初始化商城库();
			全局道具库.初始化道具库();
			全局装备库.初始化装备库();
			全局变量.所有道具头像资源表 = new Sprite[0];
			全局变量.本机身份 = 0;
			操作();
		}
		finally
		{
			foreach (var 表 in 原表) 表.Key.SetValue(null, 表.Value);
			全局变量.所有玩家数据表 = 原玩家;
			全局变量.本机身份 = 原身份;
			全局变量.提示类 = 原提示;
			全局变量.所有道具头像资源表 = 原头像;
		}
	}

	private sealed class 购买检查数据 : IDisposable
	{
		public readonly 玩家数据 玩家;
		public readonly 商品属性类 商品;
		public readonly 购买道具脚本 脚本;
		private readonly GameObject 根对象;

		public 购买检查数据(string 商品名, int 已占格数, double 容量 = 300, int 装备数 = 0, params int[] 同名堆)
		{
			玩家 = new 玩家数据();
			玩家.初始化一个玩家("商城容量回归", "汉");
			玩家.基础信息.背包容量上限 = 容量;
			全局变量.所有玩家数据表 = new List<玩家数据> { 玩家 };
			商品 = 全局商城库.获取指定名字的道具(商品名);
			检查(商品 != null, "必须使用原商城商品");
			玩家.财产信息.黄金 = Math.Max(玩家.财产信息.黄金, 商品.黄金售价 * 5);
			foreach (int 数量 in 同名堆) 玩家.背包道具列表.添加道具(商品名, 数量);
			string[] 装备名 = { "精钢剑", "青铜盔", "青铜铠", "战马" };
			for (int i = 0; i < 装备数; i++)
				玩家.背包装备列表.添加指定装备数据到背包(全局装备库.获取指定名字的装备(装备名[i]), 1);
			while (玩家.获取背包物品数量() < 已占格数) 玩家.背包道具列表.添加道具("皇榜", 999);
			检查(玩家.获取背包物品数量() == 已占格数, "检查数据槽位数错误");

			根对象 = new GameObject("商城容量回归", typeof(购买道具脚本));
			根对象.hideFlags = HideFlags.HideAndDontSave;
			脚本 = 根对象.GetComponent<购买道具脚本>();
			脚本.道具名字 = 商品名;
			脚本.道具头像对象 = 子对象("道具头像").AddComponent<Image>();
			脚本.道具名字对象 = 文本("道具名字");
			脚本.道具说明对象 = 文本("道具说明");
			脚本.黄金售价对象 = 文本("黄金售价");
			脚本.黄金资产对象 = 文本("黄金资产");
			脚本.白银资产对象 = 文本("白银资产");
			脚本.购买数量对象 = 文本("购买数量");
			脚本.限购数量 = 文本("限购数量");
			脚本.数量滑条对象 = 子对象("数量滑条").AddComponent<Slider>();
			脚本.数量滑条对象.minValue = 0;
			脚本.数量滑条对象.maxValue = 100;
			脚本.白银售价对象 = 子对象("白银售价");
			for (int i = 0; i < 3; i++)
				子对象("白银售价" + i, 脚本.白银售价对象.transform).AddComponent<Text>();
			全局变量.提示类 = 子对象("提示").AddComponent<提示移动>();
			子对象("提示文本", 全局变量.提示类.transform).AddComponent<Text>();
		}

		private GameObject 子对象(string 名字, Transform 父对象 = null)
		{
			var 对象 = new GameObject(名字, typeof(RectTransform));
			对象.hideFlags = HideFlags.HideAndDontSave;
			对象.transform.SetParent(父对象 != null ? 父对象 : 根对象.transform, false);
			return 对象;
		}

		private Text 文本(string 名字) { return 子对象(名字).AddComponent<Text>(); }

		public void Dispose()
		{
			全局变量.提示类.StopAllCoroutines();
			UnityEngine.Object.DestroyImmediate(根对象);
		}
	}
}
#endif
