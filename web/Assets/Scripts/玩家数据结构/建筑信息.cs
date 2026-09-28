namespace 玩家数据结构
{
	public class 建筑信息
	{
		public int 类型;

		public int 等级;

		public int 获取建筑头像索引()
		{
			if (等级 >= 10)
			{
				return 4;
			}
			if (等级 <= 2)
			{
				return 0;
			}
			if (等级 <= 4)
			{
				return 1;
			}
			if (等级 <= 6)
			{
				return 2;
			}
			if (等级 <= 9)
			{
				return 3;
			}
			return 0;
		}

		public string 获取建筑等级文本()
		{
			if (类型 == 0)
			{
				return "大厅" + 等级.ToString() + "级";
			}
			if (类型 == 1)
			{
				return "书院(" + 等级.ToString() + "级)";
			}
			if (类型 == 2)
			{
				return "房屋" + 等级.ToString() + "级";
			}
			if (类型 == 3)
			{
				return "农场" + 等级.ToString() + "级";
			}
			if (类型 == 4)
			{
				return "骑兵营(" + 等级.ToString() + "级)";
			}
			if (类型 == 5)
			{
				return "步兵营(" + 等级.ToString() + "级)";
			}
			if (类型 == 6)
			{
				return "弓兵营(" + 等级.ToString() + "级)";
			}
			if (类型 == 7)
			{
				return "战车营(" + 等级.ToString() + "级)";
			}
			return "未知" + 等级.ToString() + "级";
		}
	}
}
