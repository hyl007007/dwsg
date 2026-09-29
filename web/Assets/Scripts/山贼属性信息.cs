using System.Collections.Generic;
using UnityEngine;
using 玩家数据结构;

public class 山贼属性信息
{
	public int 坐标x;

	public int 坐标y;

	public double 等级;

	public double 掉落宝物;

	public double 掉落宝箱;

	public double 掉落装备;

	public List<将领信息> 将领数据列表 = new List<将领信息>();

	public void 随机生成山贼(int x, int y)
	{
		Newtonsoft.Json.Linq.JObject 数据 = Dwsg.Shared.Combat.BanditGenerator.Generate(x, y, UnityEngine.Random.Range,
			配置ID =>
			{
				将领属性库类 配置 = 全局将领库.查询指定ID的将领数据(配置ID);
				配置.获取随机属性();
				将领信息 将领 = new 将领信息();
				将领.生成将领数据(配置);
				将领.将领属性.初始属性.名字 = 随机姓名.生成随机姓名();
				return Newtonsoft.Json.Linq.JObject.FromObject(将领);
			},
			(将领数据, 等级) =>
			{
				将领信息 将领 = 将领数据.ToObject<将领信息>();
				将领.将领获取经验值(将领.获取升级需要经验(等级));
				将领数据.ReplaceAll(Newtonsoft.Json.Linq.JObject.FromObject(将领).Properties());
			});
		山贼属性信息 山贼 = 数据.ToObject<山贼属性信息>();
		坐标x = 山贼.坐标x;
		坐标y = 山贼.坐标y;
		等级 = 山贼.等级;
		掉落宝物 = 山贼.掉落宝物;
		掉落宝箱 = 山贼.掉落宝箱;
		掉落装备 = 山贼.掉落装备;
		将领数据列表.Clear();
		将领数据列表.AddRange(山贼.将领数据列表);
		全局变量.所有玩家数据表[1].封地信息表[0].将领信息表.AddRange(将领数据列表);
	}
}
