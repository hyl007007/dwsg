using System.Collections.Generic;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Generals
{
	public static class GeneralLegacyAdapter
	{
		public static bool RecalculateLegacy(玩家数据 player)
		{
			JObject document = JObject.FromObject(player);
			bool abnormal = GeneralAttributeRules.Recalculate(document, TIME.getTime());
			for (int fief = 0; fief < player.封地信息表.Count; fief++)
			{
				foreach (将领信息 general in player.封地信息表[fief].将领信息表)
				{
					JObject ignored;
					JObject changed = LegacyGenerals.General(document, general.ID, out ignored);
					general.将领属性.成长点数.爆点数 = changed["将领属性"]["成长点数"].Value<double>("爆点数");
					JsonConvert.PopulateObject(changed["将领属性"]["最终属性"].ToString(Formatting.None), general.将领属性.最终属性);
					general.详细信息.身份 = changed["详细信息"].Value<double>("身份");
					general.详细信息.剩余体力 = changed["详细信息"].Value<double>("剩余体力");
				}
			}
			return abnormal;
		}

		public static void Apply(玩家数据 player, JObject document)
		{
			for (int fief = 0; fief < player.封地信息表.Count; fief++)
			{
				JObject source = (JObject)document["封地信息表"][fief];
				foreach (将领信息 general in player.封地信息表[fief].将领信息表)
				{
					JObject ignored;
					JObject changed = LegacyGenerals.General(document, general.ID, out ignored);
					JsonConvert.PopulateObject(changed["将领配兵"].ToString(Formatting.None), general.将领配兵);
					JsonConvert.PopulateObject(changed["详细信息"].ToString(Formatting.None), general.详细信息);
					JsonConvert.PopulateObject(changed["将领属性"]["成长点数"].ToString(Formatting.None), general.将领属性.成长点数);
					JsonConvert.PopulateObject(changed["将领属性"]["最终属性"].ToString(Formatting.None), general.将领属性.最终属性);
				}
				List<闲兵信息> pool = player.封地信息表[fief].闲兵信息表;
				JArray changedPool = (JArray)source["闲兵信息表"];
				for (int i = 0; i < changedPool.Count; i++)
				{
					if (i == pool.Count) pool.Add(changedPool[i].ToObject<闲兵信息>());
					else JsonConvert.PopulateObject(changedPool[i].ToString(Formatting.None), pool[i]);
				}
			}
			for (int team = 0; team < 5; team++)
				for (int slot = 0; slot < 5; slot++) player.编队信息表[team][slot] = document["编队信息表"][team][slot].Value<int>();
			for (int slot = 0; slot < 4; slot++)
			{
				List<将领装备> inventory = Equipment(player, slot);
				JArray source = LegacyGenerals.Equipment(document, slot);
				for (int i = 0; i < inventory.Count; i++) inventory[i].将领ID = source[i].Value<int>("将领ID");
			}
		}

		public static List<将领装备> Equipment(玩家数据 player, int slot)
		{
			switch (slot)
			{
				case 0: return player.背包装备列表.头盔装备列表;
				case 1: return player.背包装备列表.武器装备列表;
				case 2: return player.背包装备列表.铠甲装备列表;
				case 3: return player.背包装备列表.坐骑装备列表;
				default: throw new GeneralRuleException(GeneralFailure.InvalidArgument, "装备部位无效");
			}
		}
	}
}
