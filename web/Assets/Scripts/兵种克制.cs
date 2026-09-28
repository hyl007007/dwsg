public class 兵种克制
{
	public static double 攻击类加成(double 职业, double 兵种, double 对方兵种)
	{
		double num = 0.0;
		if (兵种 == 1.0 && 对方兵种 == 2.0)
		{
			num += 50.0;
		}
		if (兵种 == 3.0 && 对方兵种 == 1.0)
		{
			num += 50.0;
		}
		return num;
	}

	public static double 防御类加成(double 职业, double 兵种, double 对方兵种)
	{
		double num = 0.0;
		if (兵种 == 2.0 && 对方兵种 == 3.0)
		{
			num += 50.0;
		}
		return num;
	}
}
