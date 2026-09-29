using System;
using System.Runtime.CompilerServices;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Generals
{
	public static partial class GeneralsClientAdapter
	{
		sealed class ExperienceBookTarget
		{
			public string WorldId, PlayerId, GeneralId;
		}
		static readonly ConditionalWeakTable<object, ExperienceBookTarget> experienceBookTargets = new ConditionalWeakTable<object, ExperienceBookTarget>();

		public static bool CaptureExperienceBookTarget(object source, int playerIndex, int fiefIndex, int generalIndex)
		{
			try
			{
				if (source == null) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "经验书入口不存在");
				experienceBookTargets.Remove(source);
				WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
				if (!GameNetwork.Enabled || !GameNetwork.HasRole || snapshot == null)
					throw new GeneralRuleException(GeneralFailure.Conflict, "请先连接当前角色");
				if (playerIndex != 全局变量.本机身份 || playerIndex < 0 || playerIndex >= 全局变量.所有玩家数据表.Count)
					throw new GeneralRuleException(GeneralFailure.NotFound, "请选择自己的将领");
				var player = 全局变量.所有玩家数据表[playerIndex];
				if (fiefIndex < 0 || fiefIndex >= player.封地信息表.Count || generalIndex < 0 || generalIndex >= player.封地信息表[fiefIndex].将领信息表.Count)
					throw new GeneralRuleException(GeneralFailure.NotFound, "将领不存在，请重新选择");
				int legacyId = player.封地信息表[fiefIndex].将领信息表[generalIndex].ID;
				string id = FindMapping(snapshot, "generals", mapping => mapping.Value<int>("legacyId") == legacyId);
				experienceBookTargets.Add(source, new ExperienceBookTarget { WorldId = snapshot.WorldId, PlayerId = snapshot.PlayerId, GeneralId = id });
				return true;
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); return false; }
		}

		public static void UseExperienceBook(object source, string itemName, double quantity, Action<GameResult> completed)
		{
			try
			{
				if (pending) { 提示("正在处理将领操作"); return; }
				if (itemName != GeneralExperienceBookRules.ItemName) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择原经验书");
				int count = LegacyGenerals.Integer(new JValue(quantity));
				if (count <= 0) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "使用数量必须为正整数");
				ExperienceBookTarget target;
				WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
				if (!GameNetwork.Enabled || !GameNetwork.HasRole || snapshot == null || source == null || !experienceBookTargets.TryGetValue(source, out target) ||
					target.PlayerId != snapshot.PlayerId || target.WorldId != snapshot.WorldId)
					throw new GeneralRuleException(GeneralFailure.Conflict, "请重新选择将领和经验书");
				JObject mapping = snapshot.PrivatePlayer["entityMappings"]?["generals"]?[target.GeneralId] as JObject;
				if (mapping == null || mapping.Value<string>("playerId") != target.PlayerId)
					throw new GeneralRuleException(GeneralFailure.NotFound, "将领已不属于当前角色，请重新选择");
				JObject fief;
				LegacyGenerals.General(snapshot.PrivatePlayer, LegacyGenerals.Integer(mapping["legacyId"]), out fief);
				pending = true;
				GameNetwork.SendCommand("generals.useExperienceBook", new JObject { ["generalId"] = target.GeneralId, ["itemName"] = itemName, ["count"] = count }, result =>
				{
					pending = false;
					WorldSnapshot latest = GameNetwork.CurrentSnapshot;
					if (!GameNetwork.HasRole || latest == null || latest.PlayerId != target.PlayerId || latest.WorldId != target.WorldId)
					{ 提示("角色连接已改变，请重新刷新"); return; }
					提示(result.Code == GameCodes.Ok ? "使用成功:\n" + result.Message : result.Message);
					completed?.Invoke(result);
				});
			}
			catch (GeneralRuleException failure) { 提示(failure.Message); }
		}
	}
}
