using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Generals
{
	public static partial class GeneralsClientAdapter
	{
		public static JArray CaptiveRows(int fiefIndex)
		{
			try
			{
				WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
				if (!GameNetwork.HasRole || snapshot == null) return new JArray();
				string fiefId = CaptiveFief(snapshot, fiefIndex);
				return new JArray((snapshot.PrivatePlayer["captives"] as JArray ?? new JArray()).OfType<JObject>()
					.Where(row => row.Value<string>("fiefId") == fiefId).Select(row => row.DeepClone()));
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); return new JArray(); }
		}

		static string CaptiveFief(WorldSnapshot snapshot, int fiefIndex)
		{
			int own = 全局变量.本机身份;
			if (own < 0 || own >= 全局变量.所有玩家数据表.Count || fiefIndex < 0 || fiefIndex >= 全局变量.所有玩家数据表[own].封地信息表.Count)
				throw new GeneralRuleException(GeneralFailure.NotFound, "请选择自己的封地");
			int legacyId = 全局变量.所有玩家数据表[own].封地信息表[fiefIndex].ID;
			return FindMapping(snapshot, "fiefs", binding => binding.Value<int>("legacyId") == legacyId);
		}

		public static void ManageCaptives(int fiefIndex, IEnumerable<string> generalIds, bool release, Action completed)
		{
			try
			{
				if (pending) { 提示("正在处理将领操作"); return; }
				WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
				if (!GameNetwork.Enabled || !GameNetwork.HasRole || snapshot == null)
					throw new GeneralRuleException(GeneralFailure.Conflict, "请先连接当前角色");
				string fiefId = CaptiveFief(snapshot, fiefIndex), playerId = snapshot.PlayerId, worldId = snapshot.WorldId;
				string[] ids = generalIds.ToArray();
				if (ids.Length == 0) return;
				if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
					throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择不同的俘虏");
				int index = 0;
				Action next = null;
				next = () =>
				{
					WorldSnapshot latest = GameNetwork.CurrentSnapshot;
					if (!GameNetwork.HasRole || latest == null || latest.PlayerId != playerId || latest.WorldId != worldId)
					{ pending = false; 提示("角色连接已改变，请重新刷新"); completed?.Invoke(); return; }
					if (index == ids.Length) { pending = false; completed?.Invoke(); return; }
					string id = ids[index++];
					GameNetwork.SendCommand(release ? "generals.releaseCaptive" : "generals.persuadeCaptive",
						new JObject { ["fiefId"] = fiefId, ["captiveGeneralId"] = id }, result =>
						{
							WorldSnapshot current = GameNetwork.CurrentSnapshot;
							if (current == null || current.PlayerId != playerId || current.WorldId != worldId)
							{ pending = false; 提示("角色连接已改变，请重新刷新"); completed?.Invoke(); return; }
							提示(result.Message);
							if (result.Code != GameCodes.Ok) { pending = false; completed?.Invoke(); return; }
							next();
						});
				};
				pending = true;
				next();
			}
			catch (GeneralRuleException failure) { pending = false; 提示(failure.Message); }
		}
	}
}
