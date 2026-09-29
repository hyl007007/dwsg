using System;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;
using 玩家数据结构;
using 缺失界面.窗口4;

namespace Dwsg.Progress
{
    public static class TrainingClient
    {
        public static void Train(将领信息 general, Action<GameResult> completed) { Execute(general, false, completed); }
        public static void RestoreStamina(将领信息 general, Action<GameResult> completed) { Execute(general, true, completed); }
        private static void Execute(将领信息 general, bool restore, Action<GameResult> completed)
        {
            int index = 全局变量.本机身份;
            var player = index >= 0 && index < 全局变量.所有玩家数据表.Count ? 全局变量.所有玩家数据表[index] : null;
            var land = player == null || player.封地信息表 == null ? null : player.封地信息表.FirstOrDefault(f => f != null && f.将领信息表 != null && f.将领信息表.Contains(general));
            if (general == null || land == null) { completed(GameResult.Reject(GameCodes.NotFound, "将领不属于当前君主")); return; }
            if (!GameNetwork.Enabled)
            {
                var result = restore ? 军事本地规则.恢复体力(player, land, general) : 军事本地规则.修炼(player, land, general);
                var answer = result.成功 ? GameResult.Success() : GameResult.Reject(GameCodes.Conflict, result.说明);
                answer.Message = result.说明; completed(answer); return;
            }
            var snapshot = GameNetwork.CurrentSnapshot;
            var mapping = (snapshot?.PrivatePlayer["entityMappings"]?["generals"] as JObject)?.Properties().SingleOrDefault(p => p.Value.Value<int>("legacyId") == general.ID && p.Value.Value<string>("playerId") == snapshot.PlayerId);
            if (mapping == null) { completed(GameResult.Reject(GameCodes.NotFound, "将领尚未同步，请重新连接")); return; }
            ProgressClient.Send(restore ? "generals.restoreStamina" : "generals.train", new JObject { ["generalId"] = mapping.Name }, completed);
        }
    }
}
