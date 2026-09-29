#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using 玩家数据结构;

public static class 服务器原世界导出
{
	// 批处理编译副本：-executeMethod 服务器原世界导出.运行 -dwsgSeedOutput <audit内绝对路径>
	public static void 运行()
	{
		if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("仅在离屏编译副本执行");
		string[] 参数 = Environment.GetCommandLineArgs();
		int 索引 = Array.IndexOf(参数, "-dwsgSeedOutput");
		if (索引 < 0 || 索引 + 1 >= 参数.Length) throw new ArgumentException("缺少-dwsgSeedOutput");
		string 路径 = Path.GetFullPath(参数[索引 + 1]);
		if (!路径.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(目录 => 目录.Equals("audit", StringComparison.OrdinalIgnoreCase)))
			throw new ArgumentException("实际种子只能导出到audit目录");
		if (全局变量.所有玩家数据表.Count != 0 || 全局变量.所有城池列表.Count != 0 || 全局变量.所有国家列表.Count != 0)
			throw new InvalidOperationException("请使用尚未运行游戏的独立编译副本，不覆盖已有世界");

		全局将领库.初始化将领库();
		全局装备库.初始化装备库();
		全局兵种库.初始化兵种库();
		全局商城库.初始化商城库();
		全局道具库.初始化道具库();
		初始化脚本.初始化游戏数据();
		var 初始化对象 = new GameObject("原世界导出", typeof(初始化脚本));
		try { 初始化对象.GetComponent<初始化脚本>().初始化驻防将领(); }
		finally { UnityEngine.Object.DestroyImmediate(初始化对象); }
		附近山贼.生成山贼数据列表();

		var 原角色 = 全局变量.所有玩家数据表[0];
		var 商品列表 = new JArray();
		var 已导出 = new HashSet<string>();
		foreach (var 列表 in new[] { 全局商城库.热卖商品列表, 全局商城库.特价商品列表, 全局商城库.装备商品列表, 全局商城库.生产商品列表, 全局商城库.加速商品列表, 全局商城库.宝物商品列表, 全局商城库.宝箱商品列表, 全局商城库.其他商品列表 })
			foreach (var 商品 in 列表)
				if (已导出.Add(商品.道具名)) 商品列表.Add(JObject.FromObject(全局商城库.获取指定名字的道具(商品.道具名)));
		var 世界 = new JObject
		{
			["国家列表"] = JArray.FromObject(全局变量.所有国家列表),
			["城池列表"] = JArray.FromObject(全局变量.所有城池列表),
			["玩家列表"] = JArray.FromObject(全局变量.所有玩家数据表),
			["商城商品"] = 商品列表,
			["道具配置"] = JArray.FromObject(全局道具库.道具列表),
			["将领配置"] = JArray.FromObject(全局将领库.属性表),
			["装备配置"] = JArray.FromObject(全局装备库.属性表),
			["兵种配置"] = JArray.FromObject(全局兵种库.属性表),
			["山贼列表"] = JArray.FromObject(全局变量.所有山贼数据列表),
			["难度"] = 全局变量.难度,
			["姓名配置"] = new JObject { ["姓"] = JArray.FromObject(随机姓名.姓), ["男名"] = JArray.FromObject(随机姓名.男名), ["女名"] = JArray.FromObject(随机姓名.女名) },
			["商城轮换商品"] = JArray.FromObject(全局商城库.其他商品列表.Select(商品 => 商品.道具名)),
			["新角色模板"] = JObject.FromObject(原角色),
			["商城配置版本"] = 1,
			["原世界来源"] = new JObject { ["初始化入口"] = "初始化脚本.初始化游戏数据/初始化驻防将领", ["地图宽"] = 全局大地图库.大地图表.GetLength(1), ["地图高"] = 全局大地图库.大地图表.GetLength(0) }
		};
		// 首封地与角色创建流程一致，使用原国都、伤兵、建筑及名字规则生成模板。
		if (!原角色.加入指定国家("汉") || 原角色.封地信息表.Count != 1) throw new InvalidOperationException("原首封地生成失败");
		世界["初始封地模板"] = JObject.FromObject(原角色.封地信息表[0]);
		世界["城池容量配置"] = JArray.FromObject(全局变量.所有城池列表.Select(城池 => new { 城池.坐标x, 城池.坐标y, 城池.规模, 容量 = 城池.获取封地上限() }));
		世界["原玩家来源映射"] = JArray.FromObject(全局变量.所有玩家数据表.Select(玩家 => new { 原索引 = 玩家.基础信息.ID, 原名字 = 玩家.基础信息.名字, NPC = 玩家.基础信息.ID != 0 }));
		Directory.CreateDirectory(Path.GetDirectoryName(路径));
		File.WriteAllText(路径, 世界.ToString(Formatting.Indented), new System.Text.UTF8Encoding(false));
		Debug.Log("DWSG_ORIGINAL_WORLD_EXPORTED cities=" + ((JArray)世界["城池列表"]).Count + " countries=" + ((JArray)世界["国家列表"]).Count + " players=" + ((JArray)世界["玩家列表"]).Count + " products=" + 商品列表.Count + " path=" + 路径);
	}
}
#endif
