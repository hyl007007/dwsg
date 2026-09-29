using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Generals
{
	public static partial class GeneralsClientAdapter
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
					if (type == "generals.cultivate")
					{
						GameResult result;
						JObject candidate = GeneralCultivationRules.Execute(JObject.FromObject(player), LegacyGenerals.Integer(payload["generalId"]),
							LegacyGenerals.Integer(payload["count"]), JArray.FromObject(全局道具库.道具列表), UnityEngine.Random.Range, TIME.getTime(), out result);
						GeneralLegacyAdapter.ApplyCultivation(player, generalId.Value, candidate, result.Data.Value<int>("consumedCount"));
						提示(result.Message);
					}
					else if (type == "generals.enhanceEquipment")
					{
						int slot = LegacyGenerals.Integer(payload["equipmentSlot"]), index = LegacyGenerals.Integer(payload["equipmentIndex"]);
						GameResult result;
						JObject candidate = EquipmentEnhancementRules.Execute(JObject.FromObject(player), slot, index, LegacyGenerals.Integer(payload["count"]),
							JArray.FromObject(全局道具库.道具列表), UnityEngine.Random.Range, TIME.getTime(), out result);
						GeneralLegacyAdapter.ApplyEnhancement(player, slot, index, candidate, result.Data.Value<string>("materialName"), result.Data.Value<int>("consumedCount"));
						提示(result.Message);
					}
					else if (type == "generals.refineEquipment" || type == "generals.setSoulLocks")
					{
						int slot = LegacyGenerals.Integer(payload["equipmentSlot"]), index = LegacyGenerals.Integer(payload["equipmentIndex"]);
						GameResult result = null;
						JObject candidate = type == "generals.setSoulLocks"
							? EquipmentSoulRules.Lock(JObject.FromObject(player), slot, index, payload["lockedIndices"] as JArray)
							: EquipmentSoulRules.Execute(JObject.FromObject(player), slot, index, LegacyGenerals.Integer(payload["mode"]), LegacyGenerals.Integer(payload["count"]),
								payload["lockedIndices"] as JArray, JArray.FromObject(全局道具库.道具列表), UnityEngine.Random.Range, UnityEngine.Random.Range, TIME.getTime(), out result);
						GeneralLegacyAdapter.ApplySoulChange(player, slot, index, candidate, result?.Data["materials"] as JObject);
						if (!string.IsNullOrEmpty(result?.Message)) 提示(result.Message);
					}
					else
					{
						JObject candidate = GeneralRules.Execute(JObject.FromObject(player), type, payload, JArray.FromObject(全局兵种库.属性表), TIME.getTime());
						GeneralLegacyAdapter.Apply(player, candidate);
					}
					success?.Invoke();
					return;
				}
				if (pending) { 提示("正在处理将领操作"); return; }
				WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
				if (playerIndex != 全局变量.本机身份 || snapshot == null || string.IsNullOrEmpty(snapshot.PlayerId))
					throw new GeneralRuleException(GeneralFailure.Conflict, "请先连接当前角色");
				if (generalId.HasValue) payload["generalId"] = FindMapping(snapshot, "generals", m => m.Value<int>("legacyId") == generalId.Value);
				if (type == "generals.equip" || type == "generals.enhanceEquipment" || type == "generals.refineEquipment" || type == "generals.setSoulLocks")
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
					if (type == "generals.dismiss") 提示(result.Data.Value<bool>("returnedToNature") ? "名将已回归大自然!" : "已解雇!");
					if ((type == "generals.cultivate" || type == "generals.enhanceEquipment" || type == "generals.refineEquipment") && !string.IsNullOrEmpty(result.Message)) 提示(result.Message);
					success?.Invoke();
				});
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); }
		}

		public static bool DismissLegacy(int playerIndex, int generalId)
		{
			try
			{
				if (GameNetwork.Enabled) { 提示("请通过联机解雇入口操作"); return false; }
				玩家数据 player = 全局变量.所有玩家数据表[playerIndex];
				将领信息 existing = null;
				List<将领信息> source = null;
				foreach (封地信息 fief in player.封地信息表)
					foreach (将领信息 general in fief.将领信息表)
						if (general.ID == generalId) { existing = general; source = fief.将领信息表; }
				GeneralDismissal change = DismissalRules.Execute(JArray.FromObject(全局变量.所有玩家数据表), playerIndex, generalId, TIME.getTime());
				JsonConvert.PopulateObject(change.RemovedGeneral["将领配兵"].ToString(), existing.将领配兵);
				JsonConvert.PopulateObject(change.RemovedGeneral["详细信息"].ToString(), existing.详细信息);
				JsonConvert.PopulateObject(change.RemovedGeneral["将领属性"]["成长点数"].ToString(), existing.将领属性.成长点数);
				JsonConvert.PopulateObject(change.RemovedGeneral["将领属性"]["最终属性"].ToString(), existing.将领属性.最终属性);
				source.Remove(existing);
				GeneralLegacyAdapter.Apply(player, (JObject)change.Players[playerIndex]);
				if (change.ReturnedToNature)
				{
					existing.ID = change.ReturnedLegacyId;
					玩家数据 nature = 全局变量.所有玩家数据表[2];
					nature.封地信息表[0].将领信息表.Add(existing);
					nature.将领ID标识 = change.Players[2].Value<int>("将领ID标识");
					GeneralLegacyAdapter.Apply(nature, (JObject)change.Players[2]);
				}
				提示(change.ReturnedToNature ? "名将已回归大自然!" : "已解雇!");
				return true;
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); return false; }
		}

		public static void EnhanceEquipment(将领装备 equipment, int count, Action success)
		{
			ExecuteEquipment(equipment, "generals.enhanceEquipment", new JObject { ["count"] = count }, success);
		}

		public static void RefineEquipment(将领装备 equipment, int mode, int count, IEnumerable<int> lockedIndices, Action success)
		{
			ExecuteEquipment(equipment, "generals.refineEquipment", new JObject { ["mode"] = mode, ["count"] = count, ["lockedIndices"] = JArray.FromObject(lockedIndices) }, success);
		}

		public static void SetSoulLocks(将领装备 equipment, IEnumerable<int> lockedIndices, Action success)
		{
			ExecuteEquipment(equipment, "generals.setSoulLocks", new JObject { ["lockedIndices"] = JArray.FromObject(lockedIndices) }, success);
		}

		static void ExecuteEquipment(将领装备 equipment, string type, JObject arguments, Action success)
		{
			int playerIndex = 全局变量.本机身份;
			if (equipment == null || playerIndex < 0 || playerIndex >= 全局变量.所有玩家数据表.Count) return;
			for (int slot = 0; slot < 4; slot++)
			{
				int index = GeneralLegacyAdapter.Equipment(全局变量.所有玩家数据表[playerIndex], slot).IndexOf(equipment);
				if (index < 0) continue;
				arguments["equipmentSlot"] = slot; arguments["equipmentIndex"] = index;
				Execute(playerIndex, null, type, arguments, success);
				return;
			}
			提示("装备不存在或不属于当前角色");
		}

		internal static string FindMapping(WorldSnapshot snapshot, string name, Func<JObject, bool> predicate)
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
