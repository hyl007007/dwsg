using System.Collections.Generic;
using UnityEngine;
using 玩家数据结构;

public class 战场信息
{
	public int 战场类型;

	public int 坐标x;

	public int 坐标y;

	public List<List<将领信息>> 攻方要渲染的编队将领列表 = new List<List<将领信息>>();

	public List<List<将领信息>> 守方要渲染的编队将领列表 = new List<List<将领信息>>();

	public List<GameObject> 将领血条缓存表 = new List<GameObject>();

	public List<GameObject> 伤害显示缓存表 = new List<GameObject>();

	public List<GameObject> 打击特效缓存表 = new List<GameObject>();

	public GameObject 战斗地图对象;

	public bool 开始检测战斗结果;

	public bool 战斗结束;

	public int 攻身份;

	public int 守身份 = 1;

	public double 攻方兵力;

	public double 守方兵力;
}
