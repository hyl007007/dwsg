namespace 玩家数据结构
{
	public class 状态信息
	{
		public string 名字 = "";

		public double 初始加成;

		public double 加成;

		public long 到期时间;

		public 状态信息(string str, double jc)
		{
			名字 = str;
			初始加成 = jc;
			加成 = jc;
		}

		public void 增加持续时间()
		{
			long num = 86400L;
			if (到期时间 == 0L)
			{
				到期时间 = TIME.getTime();
			}
			if (到期时间 <= TIME.getTime())
			{
				到期时间 = TIME.getTime() + num;
			}
			else
			{
				到期时间 += num;
			}
		}

		public string 获取状态显示文本()
		{
			string str = "";
			if (名字 == "休战")
			{
				str = "休战";
			}
			else if (名字 == "攻击" || 名字 == "防御")
			{
				str = "军队" + 名字 + "增加" + 加成.ToString() + "%";
			}
			else if (名字 == "抓将几率")
			{
				str = "增加抓将的几率";
			}
			else if (名字 == "资源声望")
			{
				str = "战斗后资源声望的获取增加" + 加成.ToString() + "%";
			}
			else if (名字 == "将领经验")
			{
				str = "将领获取的经验增加" + 加成.ToString() + "%";
			}
			else if (名字 == "封地破坏")
			{
				str = "加强封地的破坏威力";
			}
			else if (名字 == "掠夺降忠")
			{
				str = "增加夺取降忠效果";
			}
			else if (名字 == "掠夺收益")
			{
				str = "增加夺取收益效果";
			}
			else if (名字 == "俘虏玩家")
			{
				str = "增加俘虏玩家将领的几率";
			}
			else if (名字 == "攻击速度")
			{
				str = "军队攻击速度增加" + 加成.ToString() + "%";
			}
			long num = 获取状态剩余时间();
			if (num > 0)
			{
				return str + "  剩余时间:" + TIME.ToTimeFormat(num);
			}
			return str + "(未开启)";
		}

		public long 获取状态剩余时间()
		{
			if (到期时间 > 0)
			{
				long num = 到期时间 - TIME.getTime();
				if (num > 0)
				{
					return num;
				}
			}
			return 0L;
		}
	}
}
