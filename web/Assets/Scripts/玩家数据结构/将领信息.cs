using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace 玩家数据结构
{
	public class 将领信息
	{
		public int ID;

		public 将领属性 将领属性;

		public 详细信息 详细信息;

		public 将领配兵 将领配兵;

		public List<将领装备> 将领装备表;

		public 将领培养 将领培养;

		public void 生成指定ID将领(double 要生成的将领ID)
		{
			将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(要生成的将领ID);
			if (将领属性库类 != null)
			{
				生成将领数据(将领属性库类);
			}
		}

		public void 生成将领数据(将领属性库类 要生成的将领信息)
		{
			将领信息 generated = Dwsg.Shared.Generals.GeneralCreationRules.Create(Newtonsoft.Json.Linq.JObject.FromObject(要生成的将领信息)).ToObject<将领信息>();
			将领属性 = generated.将领属性;
			详细信息 = generated.详细信息;
			将领装备表 = generated.将领装备表;
			将领配兵 = generated.将领配兵;
			将领培养 = generated.将领培养;
		}
		public void 将领重置等级()
		{
			详细信息.经验 = 0.0;
			详细信息.升级需要经验 = 15.0;
			将领属性.成长点数.武力分配点 = 0.0;
			将领属性.成长点数.智力分配点 = 0.0;
			将领属性.成长点数.统帅分配点 = 0.0;
			将领属性.成长点数.总分配点数 = 0.0;
			将领属性.成长点数.等级 = 1.0;
		}

		public void 将领获取经验值(double 获取的经验值)
		{
			JObject document = JObject.FromObject(this);
			Dwsg.Shared.Generals.GeneralExperienceRules.Add(document, 获取的经验值);
			将领属性.成长点数.等级 = document["将领属性"]["成长点数"]["等级"].Value<double>();
			将领属性.成长点数.总分配点数 = document["将领属性"]["成长点数"]["总分配点数"].Value<double>();
			详细信息.经验 = document["详细信息"]["经验"].Value<double>();
			详细信息.升级需要经验 = document["详细信息"]["升级需要经验"].Value<double>();
		}
		public double 计算将领升级需要经验()
		{
			return Dwsg.Shared.Generals.GeneralExperienceRules.RequiredForLevel(将领属性.成长点数.等级);
		}

		public double 获取升级需要经验(double 等级)
		{
			return Dwsg.Shared.Generals.GeneralExperienceRules.TotalForLevel(等级);
		}

		public double 获取当前等级升级需要经验(double 等级)
		{
			return Dwsg.Shared.Generals.GeneralExperienceRules.RequiredForLevel(等级);
		}
	}
}
