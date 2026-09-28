namespace 玩家数据结构
{
	public class 财产信息
	{
		public double 铜钱;

		public double 粮食;

		public double 白银;

		public double 黄金;

		public bool 扣除铜钱(double 要扣除的数量)
		{
			if (铜钱 >= 要扣除的数量)
			{
				铜钱 -= 要扣除的数量;
				return true;
			}
			return false;
		}

		public bool 扣除粮食(double 要扣除的数量)
		{
			if (粮食 >= 要扣除的数量)
			{
				粮食 -= 要扣除的数量;
				return true;
			}
			return false;
		}

		public bool 扣除白银(double 要扣除的数量)
		{
			if (白银 >= 要扣除的数量)
			{
				白银 -= 要扣除的数量;
				return true;
			}
			return false;
		}

		public bool 扣除黄金(double 要扣除的数量)
		{
			if (黄金 >= 要扣除的数量)
			{
				黄金 -= 要扣除的数量;
				return true;
			}
			return false;
		}
	}
}
