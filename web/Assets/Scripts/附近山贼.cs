using UnityEngine;

public class 附近山贼
{
	private int 显示数量 = 5;

	private int 自身坐标x = 10;

	private int 自身坐标y = 10;

	public static void 生成山贼数据列表()
	{
		全局变量.所有山贼数据列表.Clear();
		全局变量.所有玩家数据表[1].封地信息表[0].将领信息表.Clear();
		UnityEngine.Debug.Log("生成山贼");
		for (int i = 0; i < 全局变量.横向山贼数量; i++)
		{
			for (int j = 0; j < 全局变量.竖向山贼数量; j++)
			{
				山贼属性信息 山贼属性信息 = new 山贼属性信息();
				山贼属性信息.随机生成山贼(i, j);
				全局变量.所有山贼数据列表.Add(山贼属性信息);
			}
		}
		全局变量.所有玩家数据表[1].计算最终属性();
		全局变量.所有玩家数据表[1].封地信息表[0].将领信息表.Clear();
		UnityEngine.Debug.Log("生成成功");
	}

	public static void 重新生成指定山贼(int x, int y)
	{
		山贼属性信息 山贼属性信息 = 获取指定坐标的山贼(x, y);
		if (山贼属性信息 != null)
		{
			山贼属性信息.随机生成山贼(x, y);
			全局变量.所有玩家数据表[1].计算最终属性();
			全局变量.所有玩家数据表[1].封地信息表[0].将领信息表.Clear();
		}
	}

	public static 山贼属性信息 获取指定坐标的山贼(int x, int y)
	{
		int count = 全局变量.所有山贼数据列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有山贼数据列表[i].坐标x == x && 全局变量.所有山贼数据列表[i].坐标y == y)
			{
				return 全局变量.所有山贼数据列表[i];
			}
		}
		return null;
	}
}
