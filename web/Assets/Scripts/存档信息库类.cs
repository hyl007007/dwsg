using System.Collections.Generic;
using 玩家数据结构;

public class 存档信息库类
{
	public int ID;

	public long 存档时间;

	public int 存档版本 = 1;

	// 旧存档无此字段时初始化空扩展，保留原世界数据格式。
	public 界面扩展存档数据 界面扩展;

	// 旧档缺失此字段时为 null，读档方应替换为两份新的空军情列表。
	public 行军存档数据 行军;

	public List<国家信息库类> 国家列表 = new List<国家信息库类>();

	public List<城池信息库类> 城池列表 = new List<城池信息库类>();

	public List<玩家数据> 玩家列表 = new List<玩家数据>();
}
