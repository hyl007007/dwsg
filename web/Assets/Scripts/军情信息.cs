using System.Collections.Generic;
using 玩家数据结构;

public class 军情信息
{
	public int 战场类型;

	public int 坐标x;

	public int 坐标y;

	public long 到达时间;

	public List<将领信息> 队列将领列表 = new List<将领信息>();

	public bool 已进入战场;

	public int 身份;
}
