using UnityEngine;

namespace 玩家数据结构
{
	public class 基础信息
	{
		public int ID;

		public string 名字;

		public string 性别;

		public int 头像;

		public float 等级;

		public string 称号名;

		public string 国家;

		public double 贡献;

		public string 官阶;

		public double 战功;

		public 官职信息 官职 = 0;

		public double 声望;

		public double 将领数上限 = 25.0;

		public double 抓将几率 = 5.0;

		public double 将领数扩容数量;

		public double 背包容量上限 = 300.0;

		public double 粮食增加 = 0.0;

		public void 君主获得经验(double 获得的经验)
		{
			Dwsg.Shared.Economy.MonarchRules.GrantExperience(获得的经验, ref 声望, ref 等级);
		}

		public float 获取当前等级经验条比例()
		{
			double num = 获取当前等级升级所需经验();
			double num2 = 获取指定等级升级所需经验(等级 - 1f);
			double num3 = (声望 - num2) / (num - num2);
			if (num3 > 1.0)
			{
				num3 = 0.0;
			}
			return (float)num3;
		}

		public double 获取当前等级升级所需经验()
		{
			return 获取指定等级升级所需经验(等级);
		}

		public double 获取指定等级升级所需经验(float 等级)
		{
			return Dwsg.Shared.Economy.MonarchRules.RequiredExperience(等级);
		}

		public double 统兵类称号加成()
		{
			if (称号名 == "霸主")
			{
				return 10.0;
			}
			if (称号名 == "武圣")
			{
				return 20.0;
			}
			if (称号名 == "天子")
			{
				return 10.0;
			}
			if (称号名 == "暴君")
			{
				return 10.0;
			}
			return 0.0;
		}

		public double 移速类称号加成()
		{
			if (称号名 == "贪狼")
			{
				return 10.0;
			}
			if (称号名 == "善人")
			{
				return 10.0;
			}
			if (称号名 == "达人")
			{
				return 5.0;
			}
			if (称号名 == "神工")
			{
				return 10.0;
			}
			if (称号名 == "武圣")
			{
				return 10.0;
			}
			if (称号名 == "飞将")
			{
				return 10.0;
			}
			if (称号名 == "大神")
			{
				return 20.0;
			}
			return 0.0;
		}

		public double 攻速类称号加成()
		{
			return Dwsg.Shared.Combat.CombatModifiers.TitleBonus(称号名, "攻速");
		}

		public double 生命类称号加成()
		{
			return Dwsg.Shared.Combat.CombatModifiers.TitleBonus(称号名, "生命");
		}

		public double 防御类称号加成()
		{
			return Dwsg.Shared.Combat.CombatModifiers.TitleBonus(称号名, "防御");
		}

		public double 攻击类称号加成()
		{
			return Dwsg.Shared.Combat.CombatModifiers.TitleBonus(称号名, "攻击");
		}
	}

	public enum 官职信息
    {
		平民,
        校尉,
        监军,
        中郎将,
        卫将军,
        大将军,
        大都督,
		国王
    }

    //1、大都督，战功1.5w，俸禄30万（特殊功能，能够使用国库，领取铜梁资源）
    //2、大将军，战功8000，俸禄20万黄金
    //3、卫将军，战功7000，俸禄18万黄金
    //4、中郎将，战功5000，俸禄15万黄金
    //5、监军，战功3000，俸禄10万黄金
    //6、校尉，战功2000，俸禄5万黄金
}
