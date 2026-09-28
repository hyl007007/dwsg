using UnityEngine;

public static class 加载资源
{
	public static void 头像资源()
	{
		全局变量.所有头像资源表.Clear();
		Sprite[] collection = Resources.LoadAll<Sprite>("将领头像/君王");
		全局变量.所有头像资源表.AddRange(collection);
		Sprite[] collection2 = Resources.LoadAll<Sprite>("将领头像/尊将");
		全局变量.所有头像资源表.AddRange(collection2);
		Sprite[] collection3 = Resources.LoadAll<Sprite>("将领头像/战将");
		全局变量.所有头像资源表.AddRange(collection3);
		Sprite[] collection4 = Resources.LoadAll<Sprite>("将领头像/禧将");
		全局变量.所有头像资源表.AddRange(collection4);
		Sprite[] collection5 = Resources.LoadAll<Sprite>("将领头像/名将");
		全局变量.所有头像资源表.AddRange(collection5);
		Sprite[] collection6 = Resources.LoadAll<Sprite>("将领头像/普通");
		全局变量.所有头像资源表.AddRange(collection6);
		全局变量.书院头像资源表 = Resources.LoadAll<Sprite>("建筑头像/书院");
		全局变量.农田头像资源表 = Resources.LoadAll<Sprite>("建筑头像/农田");
		全局变量.大厅头像资源表 = Resources.LoadAll<Sprite>("建筑头像/大厅");
		全局变量.房屋头像资源表 = Resources.LoadAll<Sprite>("建筑头像/房屋");
		全局变量.弓兵营头像资源表 = Resources.LoadAll<Sprite>("建筑头像/弓兵营");
		全局变量.战车营头像资源表 = Resources.LoadAll<Sprite>("建筑头像/战车营");
		全局变量.步兵营头像资源表 = Resources.LoadAll<Sprite>("建筑头像/步兵营");
		全局变量.骑兵营头像资源表 = Resources.LoadAll<Sprite>("建筑头像/骑兵营");
		全局变量.称号头像资源表 = Resources.LoadAll<Sprite>("称号头像");
		全局变量.所有道具头像资源表 = Resources.LoadAll<Sprite>("道具头像");
		全局变量.所有国家头像资源表 = Resources.LoadAll<Sprite>("国家图片");
		全局变量.自建国头像 = Resources.Load<Sprite>("国家图片/自建国");
		全局变量.未知头像 = Resources.Load<Sprite>("将领头像/未知");
		全局变量.将领状态图标资源表 = Resources.LoadAll<Sprite>("将领状态图标");
		全局变量.将领编队图标资源表 = Resources.LoadAll<Sprite>("将领编队图标");
		Sprite[] collection7 = Resources.LoadAll<Sprite>("装备图片");
		全局变量.所有装备资源表.AddRange(collection7);
		全局变量.装备品质图片资源表 = Resources.LoadAll<Sprite>("装备品质图片");
		全局变量.装备初始图片 = Resources.LoadAll<Sprite>("装备初始图片");
		全局变量.所有兵种图片资源表 = Resources.LoadAll<Sprite>("兵种头像");
		全局变量.所有兵种图标资源表 = Resources.LoadAll<Sprite>("兵种小图标");
		全局变量.山贼头像资源表 = Resources.LoadAll<Sprite>("山贼头像");
		全局变量.城池归属图标资源表 = Resources.LoadAll<Sprite>("大地图资源/归属图标");
		全局变量.城池规模图片资源表 = Resources.LoadAll<Sprite>("大地图资源/城池图片");
		全局变量.城池规模头像资源表 = Resources.LoadAll<Sprite>("大地图资源/城池头像");
	}

	public static void 预制体资源()
	{
		全局变量.封地所有建筑模型.Clear();
		GameObject[] item = Resources.LoadAll<GameObject>("建筑模型/大厅");
		全局变量.封地所有建筑模型.Add(item);
		GameObject[] item2 = Resources.LoadAll<GameObject>("建筑模型/书院");
		全局变量.封地所有建筑模型.Add(item2);
		GameObject[] item3 = Resources.LoadAll<GameObject>("建筑模型/房屋");
		全局变量.封地所有建筑模型.Add(item3);
		GameObject[] item4 = Resources.LoadAll<GameObject>("建筑模型/农田");
		全局变量.封地所有建筑模型.Add(item4);
		GameObject[] item5 = Resources.LoadAll<GameObject>("建筑模型/骑兵营");
		全局变量.封地所有建筑模型.Add(item5);
		GameObject[] item6 = Resources.LoadAll<GameObject>("建筑模型/步兵营");
		全局变量.封地所有建筑模型.Add(item6);
		GameObject[] item7 = Resources.LoadAll<GameObject>("建筑模型/弓兵营");
		全局变量.封地所有建筑模型.Add(item7);
		GameObject[] item8 = Resources.LoadAll<GameObject>("建筑模型/战车营");
		全局变量.封地所有建筑模型.Add(item8);
		全局变量.封地所有建筑名字 = Resources.LoadAll<Sprite>("建筑模型/名字");
		全局变量.城池信息布局pre = (GameObject)Resources.Load("大地图资源/城池信息预制件");
		全局变量.所有城池路径pre = Resources.LoadAll<GameObject>("大地图资源/城池路径");
		全局变量.城池战斗场景pre = (GameObject)Resources.Load("战斗场景预制件/城池战斗场景");
		全局变量.山贼战斗场景pre = (GameObject)Resources.Load("战斗场景预制件/山贼战斗场景");
		全局变量.所有兵种模型 = Resources.LoadAll<GameObject>("兵种模型预制件");
		全局变量.选择出征将领列表pre = (GameObject)Resources.Load("选择出征将领预制件/列表将领");
		全局变量.编队对象pre = (GameObject)Resources.Load("挂载脚本预制体/编队对象");
		全局变量.将领对象pre = (GameObject)Resources.Load("挂载脚本预制体/将领对象");
		全局变量.红色坑位pre = (GameObject)Resources.Load("地图物件预制件/红色坑位");
		全局变量.蓝色坑位pre = (GameObject)Resources.Load("地图物件预制件/蓝色坑位");
		全局变量.模型阴影pre = (GameObject)Resources.Load("将领信息预制件/将领阴影");
		全局变量.骑兵模型pre = (GameObject)Resources.Load("兵种模型预制件/晓骑兵");
		全局变量.模型阴影pre = (GameObject)Resources.Load("将领信息预制件/将领阴影");
		全局变量.将领底部特效pre = (GameObject)Resources.Load("将领信息预制件/底部特效");
		全局变量.将领名字pre = (GameObject)Resources.Load("将领信息预制件/将领名字");
		全局变量.将领橙星pre = (GameObject)Resources.Load("将领信息预制件/将领橙星");
		全局变量.将领紫星pre = (GameObject)Resources.Load("将领信息预制件/将领紫星");
		全局变量.将领红星pre = (GameObject)Resources.Load("将领信息预制件/将领红星");
		全局变量.将领黄星pre = (GameObject)Resources.Load("将领信息预制件/将领黄星");
		全局变量.将领血条信息pre = (GameObject)Resources.Load("将领信息预制件/将领血条信息");
		全局变量.打击特效pre = (GameObject)Resources.Load("将领信息预制件/打击特效预制件");
		全局变量.伤害显示pre = (GameObject)Resources.Load("将领信息预制件/伤害显示预制件");
		全局变量.挡pre = (GameObject)Resources.Load("将领信息预制件/挡预制件");
		全局变量.闪pre = (GameObject)Resources.Load("将领信息预制件/闪预制件");
		全局变量.中pre = (GameObject)Resources.Load("将领信息预制件/中预制件");
		全局变量.加载动画pre = (GameObject)Resources.Load("通用界面/加载动画");
	}
}
