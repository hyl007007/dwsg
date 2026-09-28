using System.Collections.Generic;
using 玩家数据结构;

public class 存档信息库类
{
	public int ID;

	public long 存档时间;

	public int 存档版本 = 1;

	public List<国家信息库类> 国家列表 = new List<国家信息库类>();

	public List<城池信息库类> 城池列表 = new List<城池信息库类>();

	public List<玩家数据> 玩家列表 = new List<玩家数据>();
}
