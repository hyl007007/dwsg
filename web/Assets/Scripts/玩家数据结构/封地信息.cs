using System.Collections.Generic;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace 玩家数据结构
{
	public class 封地信息
	{
		public int ID;

		public string 封地名字;

		public 坐标 所在城池;

		public double 产铜;

		public double 产粮;

		public List<建筑信息> 建筑信息表 = new List<建筑信息>();

		public List<将领信息> 将领信息表 = new List<将领信息>();

		public List<闲兵信息> 闲兵信息表 = new List<闲兵信息>();

		public List<伤兵信息> 伤兵信息表 = new List<伤兵信息>();

		public List<将领索引> 俘虏信息表 = new List<将领索引>();

		public List<将领索引> 驻防信息表 = new List<将领索引>();

		public void 初始化封地信息()
		{
			// 新封地的兵员列表由字段初始化为空；已有伤兵由战损与存档数据保留。
		}

		public void 添加一个俘虏到列表(将领索引 将领索引)
		{
			俘虏信息表.Add(将领索引);
		}

		public int 查找指定名字将领(string 要查找的名字)
		{
			int count = 将领信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (将领信息表[i].将领属性.初始属性.名字 == 要查找的名字)
				{
					return i;
				}
			}
			return -1;
		}

		public void 初始化建筑列表()
		{
			建筑信息表.Clear();
			建筑信息 建筑信息 = new 建筑信息();
			建筑信息.类型 = 0;
			建筑信息.等级 = 10;
			建筑信息表.Add(建筑信息);
			for (int i = 0; i < 12; i++)
			{
				建筑信息 建筑信息2 = new 建筑信息();
				建筑信息2.类型 = -1;
				建筑信息2.等级 = 0;
				建筑信息表.Add(建筑信息2);
			}
		}

		public bool 添加伤兵(int 兵种ID, double 添加数量)
		{
			int count = 伤兵信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (伤兵信息表[i].ID == 兵种ID)
				{
					伤兵信息表[i].数量 = 伤兵信息表[i].数量 + 添加数量;
					return true;
				}
			}
			伤兵信息 伤兵信息 = new 伤兵信息();
			伤兵信息.ID = 兵种ID;
			伤兵信息.数量 = 添加数量;
			伤兵信息表.Add(伤兵信息);
			return true;
		}

		public bool 删除伤兵(int 兵种ID, double 删除数量)
		{
			int count = 伤兵信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (伤兵信息表[i].ID == 兵种ID)
				{
					伤兵信息表[i].数量 = 伤兵信息表[i].数量 - 删除数量;
					if (伤兵信息表[i].数量 <= 0.0)
					{
						伤兵信息表.RemoveAt(i);
					}
					return true;
				}
			}
			return false;
		}

		public bool 添加闲兵(int 兵种ID, double 添加数量)
		{
			int count = 闲兵信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (闲兵信息表[i].ID == 兵种ID)
				{
					闲兵信息表[i].数量 = 闲兵信息表[i].数量 + 添加数量;
					return true;
				}
			}
			闲兵信息 闲兵信息 = new 闲兵信息();
			闲兵信息.ID = 兵种ID;
			闲兵信息.数量 = 添加数量;
			闲兵信息表.Add(闲兵信息);
			return true;
		}

		public bool 删除闲兵(int 兵种ID, double 删除数量)
		{
			int count = 闲兵信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (闲兵信息表[i].ID == 兵种ID)
				{
					闲兵信息表[i].数量 = 闲兵信息表[i].数量 - 删除数量;
					if (闲兵信息表[i].数量 <= 0.0)
					{
						闲兵信息表.RemoveAt(i);
					}
					return true;
				}
			}
			return false;
		}

		public int 获取书院等级()
		{
			int count = 建筑信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (建筑信息表[i].类型 == 1)
				{
					return 建筑信息表[i].等级;
				}
			}
			return -1;
		}

		public bool 升级建筑(int 第几个建筑)
		{
			var 数据 = 建筑规则数据();
			return 应用建筑结果(BuildingRules.UpgradePlot(数据, 第几个建筑), 数据, 第几个建筑);
		}

		public void 建造建筑(int 第几个建筑, int 建筑类型)
		{
			var 数据 = 建筑规则数据();
			应用建筑结果(BuildingRules.ConstructPlot(数据, 第几个建筑, 建筑类型), 数据, 第几个建筑);
		}

		public bool 拆除建筑(int 第几个建筑)
		{
			var 数据 = 建筑规则数据();
			return 应用建筑结果(BuildingRules.DemolishPlot(数据, 第几个建筑), 数据, 第几个建筑);
		}

		private JObject 建筑规则数据()
		{
			return new JObject { ["ID"] = ID, ["建筑信息表"] = JArray.FromObject(建筑信息表) };
		}

		private bool 应用建筑结果(GameResult 结果, JObject 数据, int 第几个建筑)
		{
			if (结果.Code != GameCodes.Ok) return false;
			建筑信息表[第几个建筑].类型 = 数据["建筑信息表"][第几个建筑].Value<int>("类型");
			建筑信息表[第几个建筑].等级 = 数据["建筑信息表"][第几个建筑].Value<int>("等级");
			return true;
		}
	}
}
