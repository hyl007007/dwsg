using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public sealed class TroopTreatment
	{
		public JObject Player { get; set; }
		public double CopperCost { get; set; }
		public double FoodCost { get; set; }
	}

	public static class TroopTreatmentRules
	{
		public static TroopTreatment Execute(JObject player, int fiefId, int troopTypeId, int count, JArray troopConfigurations)
		{
			if (count <= 0) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "治疗数量必须为正整数");
			JObject candidate = (JObject)player.DeepClone();
			JObject fief = LegacyGenerals.Array(candidate["封地信息表"]).OfType<JObject>().SingleOrDefault(f => LegacyGenerals.Integer(f["ID"]) == fiefId);
			if (fief == null) throw new GeneralRuleException(GeneralFailure.NotFound, "封地不存在或不属于当前角色");
			JObject troop = LegacyGenerals.Array(troopConfigurations).OfType<JObject>().SingleOrDefault(t => LegacyGenerals.Integer(t["ID"]) == troopTypeId);
			if (troop == null) throw new GeneralRuleException(GeneralFailure.NotFound, "兵种配置不存在");
			JArray wounded = LegacyGenerals.Array(fief["伤兵信息表"]);
			JObject selected = wounded.OfType<JObject>().SingleOrDefault(w => LegacyGenerals.Integer(w["ID"]) == troopTypeId);
			if (selected == null || LegacyGenerals.Quantity(selected["数量"]) < count)
				throw new GeneralRuleException(GeneralFailure.Conflict, "伤兵数量不足，请重新选择");
			double copperPerUnit = LegacyGenerals.Number(troop["需要铜钱"]);
			double foodPerUnit = LegacyGenerals.Number(troop["需要粮食"]);
			if (copperPerUnit < 0 || foodPerUnit < 0) throw new GeneralRuleException(GeneralFailure.InvalidData, "治疗费用配置无效");
			double copperCost = count * (copperPerUnit / 2.0);
			double foodCost = count * (foodPerUnit / 2.0);
			LegacyGenerals.Number(new JValue(copperCost)); LegacyGenerals.Number(new JValue(foodCost));
			JObject property = LegacyGenerals.Object(candidate["财产信息"]);
			double copper = LegacyGenerals.Number(property["铜钱"]), food = LegacyGenerals.Number(property["粮食"]);
			if (copper < copperCost || food < foodCost)
				throw new GeneralRuleException(GeneralFailure.Conflict, "治疗失败!\n需要铜钱:" + copperCost + "\n需要粮食:" + foodCost);
			JArray idle = LegacyGenerals.Array(fief["闲兵信息表"]);
			JObject pool = idle.OfType<JObject>().SingleOrDefault(p => LegacyGenerals.Integer(p["ID"]) == troopTypeId);
			long merged = (pool == null ? 0L : LegacyGenerals.Quantity(pool["数量"])) + count;
			if (merged > int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "闲兵数量超出范围");
			property["铜钱"] = copper - copperCost;
			property["粮食"] = food - foodCost;
			selected["数量"] = (double)(LegacyGenerals.Quantity(selected["数量"]) - count);
			if (LegacyGenerals.Quantity(selected["数量"]) == 0) wounded.Remove(selected);
			if (pool == null) idle.Add(new JObject { ["ID"] = troopTypeId, ["数量"] = (double)merged });
			else pool["数量"] = (double)merged;
			return new TroopTreatment { Player = candidate, CopperCost = copperCost, FoodCost = foodCost };
		}
	}
}
