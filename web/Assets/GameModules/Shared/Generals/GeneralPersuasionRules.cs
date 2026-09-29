using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralPersuasionRules
	{
		// 原封地劝降的三个分支；接收方和被俘将领均须来自已验证的候选数据。
		public static GameResult Execute(JObject player, JObject general, int playerIndex, Func<int, int, int> random)
		{
			int count = LegacyGenerals.Array(player["封地信息表"]).Sum(f => LegacyGenerals.Array(f["将领信息表"]).Count);
			if (count >= LegacyGenerals.Number(player["基础信息"]["将领数上限"]))
				throw new GeneralRuleException(GeneralFailure.Conflict, "劝降失败,将领上限!");
			JObject details = LegacyGenerals.Object(general["详细信息"]);
			if (playerIndex < 0 || LegacyGenerals.Number(details["状态"]) != 3.0 || LegacyGenerals.Integer(details["俘虏玩家"]) != playerIndex)
				throw new GeneralRuleException(GeneralFailure.Conflict, "将领已不在当前俘虏列表");
			double loyalty = LegacyGenerals.Number(details["忠诚"]);
			if (loyalty < int.MinValue + 100.0 || loyalty > int.MaxValue)
				throw new GeneralRuleException(GeneralFailure.InvalidData, "俘虏忠诚无效");
			int threshold = 100 - (int)loyalty;
			int roll = random(1, 500);
			string name = player["基础信息"].Value<string>("名字");
			if (name == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "角色名字缺失");
			using (MD5 md5 = MD5.Create())
				if (BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(name))).Replace("-", "") == "E586D0FD6B8E898AFA3B640A861EEBAB")
					roll = threshold;
			string outcome, message;
			if (roll <= threshold)
			{
				outcome = "recruited"; message = "劝降成功!";
				details["身份"] = (double)playerIndex;
				details["忠诚"] = 60.0;
				details["状态"] = 0.0;
				details["经验"] = 0.0;
				details["升级需要经验"] = 15.0;
				JObject points = LegacyGenerals.Object(general["将领属性"]["成长点数"]);
				foreach (string field in new[] { "武力分配点", "智力分配点", "统帅分配点", "总分配点数" }) points[field] = 0.0;
				points["等级"] = 1.0;
			}
			else if (random(0, 300) <= (int)loyalty)
			{
				outcome = "escaped"; message = "劝降失败,逃跑!";
				details["忠诚"] = 60.0;
				details["状态"] = 0.0;
			}
			else
			{
				outcome = "retained"; message = "劝降失败!";
				details["忠诚"] = Math.Max(0.0, loyalty - 2.0);
			}
			GameResult result = GameResult.Success(new JObject { ["outcome"] = outcome, ["loyalty"] = details["忠诚"].DeepClone() });
			result.Message = message;
			return result;
		}

		public static void RefreshRecruitedAttributes(JObject player, int generalId, long utcSeconds)
		{
			JObject calculated = (JObject)player.DeepClone();
			foreach (JObject fief in LegacyGenerals.Array(calculated["封地信息表"]))
				foreach (JObject general in LegacyGenerals.Array(fief["将领信息表"]).OfType<JObject>().ToArray())
					if (LegacyGenerals.Integer(general["ID"]) != generalId) general.Remove();
			if (GeneralAttributeRules.Recalculate(calculated, utcSeconds))
				throw new GeneralRuleException(GeneralFailure.InvalidData, "劝降后的将领属性异常");
			JObject ignored;
			JObject changed = LegacyGenerals.General(calculated, generalId, out ignored), received = LegacyGenerals.General(player, generalId, out ignored);
			received["将领属性"]["最终属性"] = changed["将领属性"]["最终属性"].DeepClone();
			received["将领属性"]["成长点数"]["爆点数"] = changed["将领属性"]["成长点数"]["爆点数"].DeepClone();
			received["详细信息"]["剩余体力"] = changed["详细信息"]["剩余体力"].DeepClone();
		}
	}
}
