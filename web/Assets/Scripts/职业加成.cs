public class 职业加成
{
	public static double 攻击类加成(double 职业, double 兵种, double 对方兵种)
	{
		double num = 0.0;
		if (兵种 == 1.0 && 职业 == 1.0)
		{
			num += 15.0;
		}
		if (兵种 == 3.0 && 职业 == 4.0)
		{
			num += 30.0;
		}
		return num;
	}

	public static double 防御类加成(double 职业, double 兵种, double 对方兵种)
	{
		double num = 0.0;
		if (兵种 == 1.0 && 职业 == 1.0)
		{
			num += 15.0;
		}
		if (兵种 == 2.0)
		{
			if (职业 == 2.0)
			{
				num += 20.0;
			}
			if (对方兵种 == 3.0)
			{
				num += 50.0;
			}
		}
		return num;
	}

	public static double 生命类加成(double 职业, double 兵种)
	{
		double num = 0.0;
		if (职业 == 3.0)
		{
			num += 35.0;
		}
		else if (兵种 == 2.0 && 职业 == 2.0)
		{
			num += 15.0;
		}
		return num;
	}
}
