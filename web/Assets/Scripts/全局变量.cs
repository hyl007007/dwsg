using System.Collections.Generic;
using UnityEngine;
using 玩家数据结构;

public class 全局变量
{
	public static List<赌场信息> 当局赌场下注列表 = new List<赌场信息>();

	public static bool 赌场是否开始 = false;

	public static int 领取倒计时 = 300;

	public static string url = "https://xdda.lanzoum.com/imxPH11c92xe";

	public static int 验证变量 = 0;

	public static int 难度 = 0;

	public static List<存档信息库类> 所有存档列表 = new List<存档信息库类>();

	public static long 酒馆刷新时间 = 0L;

	public static GameObject 主相机;

	public static GameObject 战斗地图相机;

	public static GameObject 主界面UI对象;

	public static GameObject 战斗界面UI对象;

	public static bool 是否为登录=false;

	public static GameObject 大地图相机;

	public static GameObject 大地图布局对象;

	public static GameObject 封地布局对象;

	public static 提示移动 提示类;

	public static int 国家ID记录 = 0;

	public static List<国家信息库类> 所有国家列表 = new List<国家信息库类>();

	public static List<城池信息库类> 所有城池列表 = new List<城池信息库类>();

	public static List<将领信息> 所有名将列表 = new List<将领信息>();

	public static int 本机身份 = 0;

	public static int 第几个封地 = 0;

	public static List<玩家数据> 所有玩家数据表 = new List<玩家数据>();

	public static int 横向山贼数量 = 20;

	public static int 竖向山贼数量 = 20;

	public static List<山贼属性信息> 所有山贼数据列表 = new List<山贼属性信息>();

	public static List<double> 盟友列表 = new List<double>();

	public static List<战场信息> 战场列表 = new List<战场信息>();

	public static List<军情信息> 军情列表 = new List<军情信息>();

	public static List<Ai军情信息> Ai军情列表 = new List<Ai军情信息>();

	public static List<Sprite> 所有头像资源表 = new List<Sprite>();

	public static List<Sprite> 所有装备资源表 = new List<Sprite>();

	public static Sprite[] 所有兵种图片资源表;

	public static Sprite[] 所有兵种图标资源表;

	public static Sprite[] 将领状态图标资源表;

	public static Sprite[] 将领编队图标资源表;

	public static Sprite[] 装备品质图片资源表;

	public static Sprite[] 山贼头像资源表;

	public static Sprite[] 城池归属图标资源表;

	public static Sprite[] 城池规模图片资源表;

	public static Sprite[] 城池规模头像资源表;

	public static Sprite[] 书院头像资源表;

	public static Sprite[] 农田头像资源表;

	public static Sprite[] 大厅头像资源表;

	public static Sprite[] 弓兵营头像资源表;

	public static Sprite[] 战车营头像资源表;

	public static Sprite[] 房屋头像资源表;

	public static Sprite[] 步兵营头像资源表;

	public static Sprite[] 骑兵营头像资源表;

	public static Sprite[] 称号头像资源表;

	public static Sprite[] 所有道具头像资源表;

	public static Sprite[] 所有国家头像资源表;

	public static Sprite 未知头像;

	public static Sprite 自建国头像;

	public static Sprite[] 装备初始图片;

	public static GameObject[] 所有兵种模型;

	public static GameObject 城池信息布局pre;

	public static GameObject[] 所有城池路径pre;

	public static List<GameObject[]> 封地所有建筑模型 = new List<GameObject[]>();

	public static Sprite[] 封地所有建筑名字;

	public static GameObject 城池战斗场景pre;

	public static GameObject 山贼战斗场景pre;

	public static GameObject 加载动画pre;

	public static GameObject 骑兵模型pre;

	public static GameObject 模型阴影pre;

	public static GameObject 将领名字pre;

	public static GameObject 将领血条信息pre;

	public static GameObject 将领橙星pre;

	public static GameObject 将领紫星pre;

	public static GameObject 将领红星pre;

	public static GameObject 将领黄星pre;

	public static GameObject 将领底部特效pre;

	public static GameObject 红色坑位pre;

	public static GameObject 蓝色坑位pre;

	public static GameObject 将领对象pre;

	public static GameObject 编队对象pre;

	public static GameObject 打击特效pre;

	public static GameObject 伤害显示pre;

	public static GameObject 挡pre;

	public static GameObject 闪pre;

	public static GameObject 中pre;

	public static GameObject 选择出征将领列表pre;

	public static List<画册信息> 画册列表 = new List<画册信息>();

	public static void SetActive(GameObject go, bool state)
	{
		if (!(go == null) && go.activeSelf != state)
		{
			go.SetActive(state);
		}
	}
}
