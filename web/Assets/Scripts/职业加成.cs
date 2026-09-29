public class 职业加成
{
	public static double 攻击类加成(double 职业, double 兵种, double 对方兵种)
	{
		return Dwsg.Shared.Combat.战斗规则.职业攻击加成(职业, 兵种, 对方兵种);
	}

	public static double 防御类加成(double 职业, double 兵种, double 对方兵种)
	{
		return Dwsg.Shared.Combat.战斗规则.职业防御加成(职业, 兵种, 对方兵种);
	}

	public static double 生命类加成(double 职业, double 兵种)
	{
		return Dwsg.Shared.Combat.战斗规则.职业生命加成(职业, 兵种);
	}
}
