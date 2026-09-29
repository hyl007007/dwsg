using System;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Generals
{
	public static class TroopTreatmentClientAdapter
	{
		static bool pending;
		public static void Heal(int playerIndex, int fiefIndex, int troopTypeId, double quantity, Action success)
		{
			try
			{
				if (playerIndex != 全局变量.本机身份 || playerIndex < 0 || playerIndex >= 全局变量.所有玩家数据表.Count)
					throw new GeneralRuleException(GeneralFailure.NotFound, "请选择自己的角色");
				玩家数据 player = 全局变量.所有玩家数据表[playerIndex];
				if (fiefIndex < 0 || fiefIndex >= player.封地信息表.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "请选择自己的封地");
				封地信息 fief = player.封地信息表[fiefIndex];
				int count = LegacyGenerals.Integer(new JValue(quantity));
				if (GameNetwork.Enabled)
				{
					if (pending) { 提示("正在治疗伤兵"); return; }
					WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
					if (!GameNetwork.HasRole || snapshot == null) throw new GeneralRuleException(GeneralFailure.Conflict, "角色尚未连接，请稍后重试");
					string fiefId = GeneralsClientAdapter.FindMapping(snapshot, "fiefs", m => m.Value<int>("legacyId") == fief.ID);
					pending = true;
					GameNetwork.SendCommand("generals.healWounded", new JObject { ["fiefId"] = fiefId, ["troopTypeId"] = troopTypeId, ["count"] = count }, result =>
					{
						pending = false;
						if (result.Code != GameCodes.Ok) { 提示(result.Message); return; }
						Completed(result.Data.Value<double>("copperCost"), result.Data.Value<double>("foodCost"), success);
					});
					return;
				}
				TroopTreatment change = TroopTreatmentRules.Execute(JObject.FromObject(player), fief.ID, troopTypeId, count, JArray.FromObject(全局兵种库.属性表));
				JObject updated = ((JArray)change.Player["封地信息表"]).OfType<JObject>().Single(f => f.Value<int>("ID") == fief.ID);
				player.财产信息.铜钱 = change.Player["财产信息"].Value<double>("铜钱");
				player.财产信息.粮食 = change.Player["财产信息"].Value<double>("粮食");
				JObject idle = ((JArray)updated["闲兵信息表"]).OfType<JObject>().Single(p => p.Value<int>("ID") == troopTypeId);
				闲兵信息 existing = fief.闲兵信息表.SingleOrDefault(p => p.ID == troopTypeId);
				if (existing == null) fief.闲兵信息表.Add(idle.ToObject<闲兵信息>());
				else existing.数量 = idle.Value<double>("数量");
				伤兵信息 wounded = fief.伤兵信息表.Single(p => p.ID == troopTypeId);
				JObject remaining = ((JArray)updated["伤兵信息表"]).OfType<JObject>().SingleOrDefault(p => p.Value<int>("ID") == troopTypeId);
				wounded.数量 = remaining == null ? 0.0 : remaining.Value<double>("数量");
				if (remaining == null) fief.伤兵信息表.Remove(wounded);
				Completed(change.CopperCost, change.FoodCost, success);
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); }
		}
		static void Completed(double copper, double food, Action success) { 提示("治疗成功!\n花费铜钱:" + copper + "\n花费粮食:" + food); success?.Invoke(); }
		static void 提示(string message) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(message); }
	}
}
