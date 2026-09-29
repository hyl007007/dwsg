using System;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;
using 玩家数据结构;
using System.Collections.Generic;

namespace Dwsg.Generals
{
	public static class TavernClientAdapter
	{
		static bool pending;
		public static void Refresh(int type, List<将领信息> candidates, Action refreshed)
		{
			if (GameNetwork.Enabled)
			{
				Send("generals.refreshTavern", new JObject { ["refreshType"] = type }, candidates, refreshed);
				return;
			}
			try
			{
				玩家数据 player = 全局变量.所有玩家数据表[全局变量.本机身份];
				string item = type == 1 ? "招贤令" : type == 2 ? "招贤金榜" : "皇榜";
				if (type != 0)
				{
					GameResult cost = InventoryRules.ConsumeOne(JObject.FromObject(player), JArray.FromObject(全局道具库.道具列表), item);
					if (cost.Code != GameCodes.Ok) { 提示(cost.Message); return; }
				}
				JObject names = new JObject { ["姓"] = JArray.FromObject(随机姓名.姓), ["男名"] = JArray.FromObject(随机姓名.男名), ["女名"] = JArray.FromObject(随机姓名.女名) };
				JArray generated = TavernRules.Generate(JArray.FromObject(全局将领库.属性表), type, UnityEngine.Random.Range,
					() => BanditGenerator.GenerateName(names, UnityEngine.Random.Range));
				if (type != 0 && !ItemStackRules.ConsumeOne(player.背包道具列表.获取道具分类列表(item), item, i => i.名字, i => i.数量, (i, count) => i.数量 = count))
				{ 提示("使用失败"); return; }
				candidates.Clear();
				candidates.AddRange(generated.ToObject<List<将领信息>>());
				全局变量.酒馆刷新时间 = TIME.getTime();
				refreshed?.Invoke();
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); }
		}

		public static void Recruit(int selectedIndex, List<将领信息> candidates, Action refreshed)
		{
			if (selectedIndex < 0 || selectedIndex >= candidates.Count) return;
			try
			{
				玩家数据 player = 全局变量.所有玩家数据表[全局变量.本机身份];
				int fiefIndex = 全局变量.第几个封地;
				if (fiefIndex < 0 || fiefIndex >= player.封地信息表.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "请先选择自己的封地");
				if (GameNetwork.Enabled)
				{
					WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
					JArray entries = snapshot?.PrivatePlayer["tavern"]?["candidates"] as JArray;
					if (entries == null || selectedIndex >= entries.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "酒馆已刷新，请重新选择");
					string fiefId = GeneralsClientAdapter.FindMapping(snapshot, "fiefs", m => m.Value<int>("legacyId") == player.封地信息表[fiefIndex].ID);
					Send("generals.recruit", new JObject { ["candidateId"] = entries[selectedIndex].Value<string>("id"), ["fiefId"] = fiefId }, candidates,
						() => { 提示("招募成功!"); refreshed?.Invoke(); });
					return;
				}
				将领信息 added = candidates[selectedIndex];
				int legacyId = player.将领ID标识;
				JObject candidate = TavernRules.AddGeneral(JObject.FromObject(player), player.封地信息表[fiefIndex].ID, JObject.FromObject(added), TIME.getTime());
				added.ID = legacyId;
				player.封地信息表[fiefIndex].将领信息表.Add(added);
				player.将领ID标识 = candidate.Value<int>("将领ID标识");
				GeneralLegacyAdapter.Apply(player, candidate);
				candidates.RemoveAt(selectedIndex);
				提示("招募成功!");
				refreshed?.Invoke();
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); }
		}

		public static void Apply(List<将领信息> candidates)
		{
			JObject tavern = GameNetwork.CurrentSnapshot?.PrivatePlayer["tavern"] as JObject;
			if (tavern == null) return;
			candidates.Clear();
			foreach (JToken entry in LegacyGenerals.Array(tavern["candidates"])) candidates.Add(entry["general"].ToObject<将领信息>());
			全局变量.酒馆刷新时间 = tavern.Value<long>("lastRefreshUtcMs") / 1000;
		}

		static void Send(string type, JObject payload, List<将领信息> candidates, Action refreshed)
		{
			if (pending) { 提示("正在处理招募操作"); return; }
			pending = true;
			GameNetwork.SendCommand(type, payload, result =>
			{
				pending = false;
				if (result.Code != GameCodes.Ok) { 提示(result.Message); return; }
				Apply(candidates);
				refreshed?.Invoke();
			});
		}
		static void 提示(string message) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(message); }
	}
}
