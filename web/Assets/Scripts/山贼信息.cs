using System.Collections.Generic;

public class 山贼信息
{
	public List<山贼属性信息> 山贼列表 = new List<山贼属性信息>();

	public void 生成山贼(int x)
	{
		山贼列表.Clear();
		for (int i = 0; i < 20; i++)
		{
			山贼属性信息 山贼属性信息 = new 山贼属性信息();
			山贼属性信息.随机生成山贼(x, i);
			山贼列表.Add(山贼属性信息);
		}
		全局变量.所有玩家数据表[1].计算最终属性();
		全局变量.所有玩家数据表[1].封地信息表[0].将领信息表.Clear();
	}
}
