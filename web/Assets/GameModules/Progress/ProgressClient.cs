using System;
using System.Collections.Generic;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Window1;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Dwsg.Progress
{
    // 服务器快照只作展示。所有领取、费用及倒计时成果都等待同一世界命令回执。
    public static class ProgressClient
    {
        public static JournalService Journal { get; private set; }
        public static SixMinistriesService Ministries { get; private set; }
        public static string WorldId { get; private set; }
        private static string playerId;
        private static long serverUtcMs;
        private static float receivedAt;
        private static JToken journalProjection, ministriesProjection;
        private static readonly HashSet<string> pending = new HashSet<string>();
        public static long NowSeconds { get { return (serverUtcMs + (long)(Math.Max(0, Time.realtimeSinceStartup - receivedAt) * 1000)) / 1000; } }
        public static void Apply(WorldSnapshot snapshot)
        {
            if (!GameNetwork.Enabled || snapshot == null || string.IsNullOrEmpty(snapshot.PlayerId))
            { Journal = null; Ministries = null; WorldId = null; playerId = null; journalProjection = ministriesProjection = null; pending.Clear(); return; }
            bool changedRole = WorldId != snapshot.WorldId || playerId != snapshot.PlayerId;
            if (changedRole) pending.Clear();
            WorldId = snapshot.WorldId; playerId = snapshot.PlayerId; serverUtcMs = snapshot.ServerUtcMs; receivedAt = Time.realtimeSinceStartup;
            var dto = snapshot.PrivatePlayer["progress"] as JObject;
            if (dto == null) { Journal = null; Ministries = null; journalProjection = ministriesProjection = null; return; }
            // 时钟始终同步；其他模块的快照不重建政务模型或通知原界面重新排版。
            if (!changedRole && Journal != null && Ministries != null &&
                JToken.DeepEquals(journalProjection, dto["journal"]) && JToken.DeepEquals(ministriesProjection, dto["ministries"])) return;
            var journal = dto["journal"]?.ToObject<Window1WorldState>();
            var ministries = dto["ministries"]?.ToObject<SixMinistriesState>();
            string error;
            if (journal == null || !journal.Validate(snapshot.WorldId, out error) || journal.Players.Count != 1 || journal.Players[0].PlayerKey != snapshot.PlayerId || ministries == null)
                throw new InvalidOperationException("服务器政务投影无效");
            var adapter = new ReadOnlyWorld(playerId);
            Journal = new JournalService(journal, adapter, () => DateTimeOffset.FromUnixTimeMilliseconds(serverUtcMs).ToOffset(TimeSpan.FromHours(8)).DateTime, true);
            Ministries = new SixMinistriesService(ministries, new SixMinistriesConfig(), adapter, () => NowSeconds);
            journalProjection = dto["journal"].DeepClone(); ministriesProjection = dto["ministries"].DeepClone();
            Window1Module.Signal();
        }
        public static void Send(string type, JObject payload, Action<GameResult> completed)
        {
            string key = type + ":" + payload.ToString(Newtonsoft.Json.Formatting.None);
            string expectedWorld = WorldId, expectedPlayer = playerId;
            if (!pending.Add(key)) { completed?.Invoke(GameResult.Reject(GameCodes.Conflict, "正在等待服务器确认，请勿重复点击")); return; }
            GameNetwork.SendCommand(type, payload, result =>
            {
                pending.Remove(key);
                if (expectedWorld != WorldId || expectedPlayer != playerId)
                { completed?.Invoke(GameResult.Reject(GameCodes.Forbidden, "角色已改变，请重新打开界面")); return; }
                completed?.Invoke(result);
            });
        }
        public static void MarkRead(string id, bool mail)
        {
            if (Journal == null || string.IsNullOrEmpty(id)) return;
            if (mail)
            { var item = Journal.Mails().Find(m => m.Id == id); if (item == null || item.Read) return; }
            else if (Journal.NoticeRead(id)) return;
            string type = mail ? "progress.readMail" : "progress.readNotice";
            string key = type + ":" + new JObject { ["id"] = id }.ToString(Newtonsoft.Json.Formatting.None);
            if (!pending.Contains(key)) Send(type, new JObject { ["id"] = id }, _ => { });
        }
        private sealed class ReadOnlyWorld : ILocalWorld, ISixMinistriesWorld
        {
            public string PlayerKey { get; private set; }
            public string ActorKey { get { return PlayerKey; } }
            public bool IsCurrent { get { return IsBoundWorld && ExistingWorldAdapter.CurrentPlayer != null; } }
            public bool IsBoundWorld { get { return GameNetwork.CurrentSnapshot != null && GameNetwork.CurrentSnapshot.PlayerId == PlayerKey; } }
            public ISet<string> PlayerKeys { get { return new HashSet<string> { PlayerKey }; } }
            public ReadOnlyWorld(string key) { PlayerKey = key; }
            public bool TryRead(out WorldProgress progress, out string error) { return new ExistingWorldAdapter().TryRead(out progress, out error); }
            public bool TryGrant(Reward reward, out string error) { error = "联机奖励需要服务器确认"; return false; }
            public bool ResourcesValid() { return IsCurrent && new SixMinistriesAdapter().ResourcesValid(); }
            public bool CanPay(double copper, double grain) { return false; }
            public bool CanReceive(double copper, double grain) { return false; }
            public void Pay(double copper, double grain) { throw new InvalidOperationException("联机客户端不能扣款"); }
            public void Receive(double copper, double grain) { throw new InvalidOperationException("联机客户端不能发奖"); }
            public MinistryResult Train(object general) { return MinistryResult.Fail("联机训练需要服务器确认"); }
        }
    }
}
