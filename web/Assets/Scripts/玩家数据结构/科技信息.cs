namespace 玩家数据结构
{
	public class 科技信息
	{
		public double 工程设计;

		public double 征召技巧;

		public double 种植技术;

		public double 行军技巧;

		public double 市场贸易;

		public double 建筑学;

		public double 铸铁技术;

		public double 甲胄制造;

		public double 药草研究;

		public double 阵法技巧;

		public double 抛射技巧;

		public double 驾驭技巧;

		public double 战车设计;

		public double 统帅能力;

		public double 信仰;

		public double 仓储;

		public double 安置;

		public double 格斗;

		public double 精准;

		public double 驯马;

		public double 精工;

		public void 升级全部科技(int 升级到多少级)
		{
			工程设计 = 升级到多少级;
			征召技巧 = 升级到多少级;
			种植技术 = 升级到多少级;
			行军技巧 = 升级到多少级;
			市场贸易 = 升级到多少级;
			建筑学 = 升级到多少级;
			铸铁技术 = 升级到多少级;
			甲胄制造 = 升级到多少级;
			药草研究 = 升级到多少级;
			阵法技巧 = 升级到多少级;
			抛射技巧 = 升级到多少级;
			驾驭技巧 = 升级到多少级;
			战车设计 = 升级到多少级;
			统帅能力 = 升级到多少级;
		}

		public double 移速类科技(int 兵种)
		{
			double num = 0.0;
			if (兵种 == 4)
			{
				num = 战车设计 * 10.0;
			}
			return num + 行军技巧 * 5.0;
		}

		public double 攻速类科技(int 兵种)
		{
			return Dwsg.Shared.Combat.CombatModifiers.SpeedTechnology(战车设计, 兵种);
		}

		public double 生命类科技()
		{
			return Dwsg.Shared.Combat.CombatModifiers.LifeTechnology(药草研究);
		}

		public double 防御类科技(int 职业)
		{
			return Dwsg.Shared.Combat.CombatModifiers.DefenseTechnology(甲胄制造, 驾驭技巧, 阵法技巧, 职业);
		}

		public double 统兵类科技()
		{
			return 统帅能力 * 5.0;
		}

		public double 攻击类科技(int 职业)
		{
			return Dwsg.Shared.Combat.CombatModifiers.AttackTechnology(铸铁技术, 驾驭技巧, 抛射技巧, 职业);
		}

		public double 国家攻击科技加成(int 玩家)
		{
            if(全局变量.所有玩家数据表[玩家].基础信息.国家 == "野")
				return 10.0 * 全局变量.难度;
			else
			{
				if (全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[玩家].基础信息.国家) != null)
					return 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[玩家].基础信息.国家).攻击科技;
				else
					return 1;
			}
				
		}

		public double 国家防御科技加成(int 玩家)
		{
            if (全局变量.所有玩家数据表[玩家].基础信息.国家 == "野")
                return 10.0 * 全局变量.难度;
			else
			{
                if (全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[玩家].基础信息.国家) != null)
                    return 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[玩家].基础信息.国家).防御科技;
                else
                    return 1;
            }
               
        }

		public double 国家资源科技加成()
		{
			return 10.0;
		}
	}
}
