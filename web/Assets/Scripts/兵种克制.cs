public class 兵种克制
{
	public static double 攻击类加成(double 职业, double 兵种, double 对方兵种)
	{
		return Dwsg.Shared.Combat.战斗规则.兵种攻击加成(职业, 兵种, 对方兵种);
	}

	public static double 防御类加成(double 职业, double 兵种, double 对方兵种)
	{
		return Dwsg.Shared.Combat.战斗规则.兵种防御加成(职业, 兵种, 对方兵种);
	}
}
