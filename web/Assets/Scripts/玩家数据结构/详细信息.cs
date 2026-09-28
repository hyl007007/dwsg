namespace 玩家数据结构
{
	public class 详细信息
	{
		public double 忠诚 = 100.0;

		public double 所属封地 = 1.0;

		public double 经验;

		public double 升级需要经验 = 15.0;

		public double 俸禄;

		public double 剩余体力;

		public double 剩余兵力;

		public double 攻击模式 = 1.0;

		public double 状态;

		public double 俘虏玩家;

		public double 身份;

		public double 坑位颜色;

		public double 编队;

		public double 将领叛逃计时器;

		public int 判断身份()
		{
			if (身份 == 0.0)
			{
				return 0;
			}
			for (int i = 0; i < 全局变量.盟友列表.Count; i++)
			{
				if (身份 == 全局变量.盟友列表[i])
				{
					return 1;
				}
			}
			return 2;
		}
	}
}
