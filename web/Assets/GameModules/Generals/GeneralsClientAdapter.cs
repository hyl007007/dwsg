using System;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Generals
{
	public static class GeneralsClientAdapter
	{
		static bool pending;

		public static void Execute(int playerIndex, int? generalId, string type, JObject arguments, Action success)
		{
			try
			{
				if (playerIndex < 0 || playerIndex >= 全局变量.所有玩家数据表.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "角色数据不存在");
				玩家数据 player = 全局变量.所有玩家数据表[playerIndex];
				JObject payload = (JObject)(arguments ?? new JObject()).DeepClone();
				payload["generalId"] = generalId.HasValue ? new JValue(generalId.Value) : JValue.CreateNull();
				if (!GameNetwork.Enabled)
				{
					JObject candidate = GeneralRules.Execute(JObject.FromObject(player), type, payload, JArray.FromObject(全局兵种库.属性表), TIME.getTime());
					GeneralLegacyAdapter.Apply(player, candidate);
					success?.Invoke();
					return;
				}
				if (pending) { 提示("正在处理将领操作"); return; }
				WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
				if (playerIndex != 全局变量.本机身份 || snapshot == null || string.IsNullOrEmpty(snapshot.PlayerId))
					throw new GeneralRuleException(GeneralFailure.Conflict, "请先连接当前角色");
				if (generalId.HasValue) payload["generalId"] = FindMapping(snapshot, "generals", m => m.Value<int>("legacyId") == generalId.Value);
				if (type == "generals.equip")
				{
					int slot = LegacyGenerals.Integer(payload["equipmentSlot"]);
					int index = LegacyGenerals.Integer(payload["equipmentIndex"]);
					payload["equipmentId"] = FindMapping(snapshot, "equipment", m => m.Value<int>("slot") == slot && m.Value<int>("legacyIndex") == index);
					payload.Remove("equipmentSlot");
					payload.Remove("equipmentIndex");
				}
				pending = true;
				GameNetwork.SendCommand(type, payload, result =>
				{
					pending = false;
					if (result.Code != GameCodes.Ok) { 提示(result.Message); return; }
					WorldSnapshot latest = GameNetwork.CurrentSnapshot;
					if (latest == null || latest.PlayerId != snapshot.PlayerId || latest.WorldId != snapshot.WorldId) { 提示("角色连接已改变，请重新刷新"); return; }
					GeneralLegacyAdapter.Apply(player, latest.PrivatePlayer);
					success?.Invoke();
				});
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); }
		}

		public static bool PrepareDismissal(玩家数据 player, int generalId)
		{
			try
			{
				if (GameNetwork.Enabled) { 提示("联机将领解雇尚未接入，请保留当前将领"); return false; }
				GeneralLegacyAdapter.Apply(player, GeneralRules.PrepareDismissal(JObject.FromObject(player), generalId, TIME.getTime()));
				return true;
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); return false; }
		}

		static string FindMapping(WorldSnapshot snapshot, string name, Func<JObject, bool> predicate)
		{
			JObject mappings = snapshot.PrivatePlayer["entityMappings"]?[name] as JObject;
			JProperty found = mappings?.Properties().SingleOrDefault(p => p.Value is JObject && p.Value.Value<string>("playerId") == snapshot.PlayerId && predicate((JObject)p.Value));
			if (found == null) throw new GeneralRuleException(GeneralFailure.NotFound, "实例映射缺失，请重新连接");
			return found.Name;
		}

		static void 提示(string message)
		{
			if (全局变量.提示类 != null) 全局变量.提示类.显示信息(message ?? "将领操作失败");
		}
	}
}
