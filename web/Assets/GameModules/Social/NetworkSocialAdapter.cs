using System;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dwsg.Social
{
    public sealed class NetworkSocialAdapter : ISocialAdapter, IAsyncSocialAdapter, IDisposable
    {
        private SocialStateDto state = new SocialStateDto();
        private string fingerprint;
        private readonly string worldId;
        private bool disposed;
        public string CurrentPlayerId { get; private set; }
        public bool IsConnected { get { return !disposed && GameNetwork.Connected && Matches(GameNetwork.CurrentSnapshot); } }
        public bool IsBusy { get; private set; }
        public string ConnectionStatus { get { return IsConnected ? "联机 · 社交数据由服务器保存" : "连接中 · 等待服务器同步"; } }
        public event Action Changed;

        public NetworkSocialAdapter(WorldSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.PlayerId)) throw new ArgumentException("请先登录并创建角色");
            worldId = snapshot.WorldId; CurrentPlayerId = snapshot.PlayerId;
            Receive(snapshot);
            GameNetwork.SnapshotReceived += Receive;
        }
        private bool Matches(WorldSnapshot snapshot)
        { return snapshot != null && snapshot.WorldId == worldId && snapshot.PlayerId == CurrentPlayerId; }
        private void Receive(WorldSnapshot snapshot)
        {
            if (disposed || !Matches(snapshot)) return;
            var social = snapshot.PrivatePlayer["social"] as JObject;
            if (social == null) return;
            string next = social.ToString(Formatting.None);
            if (fingerprint == next) return;
            var incoming = social.ToObject<SocialStateDto>();
            if (incoming == null || incoming.OwnerId != CurrentPlayerId || incoming.WorldKey != worldId) return;
            state = incoming; fingerprint = next;
            if (Changed != null) Changed();
        }
        public SocialStateDto Snapshot() { return LocalSocialAdapter.Copy(state); }
        public SocialResult Execute(SocialCommand command)
        { return SocialResult.Fail("async", "联机操作需等待服务器回执"); }
        public void ExecuteAsync(SocialCommand command, Action<SocialResult> completed)
        {
            if (!IsConnected) { completed(SocialResult.Fail("offline", "尚未连接服务器，请等待重连")); return; }
            if (IsBusy) { completed(SocialResult.Fail("pending", "上一项操作正在提交，请稍候")); return; }
            if (command == null) { completed(SocialResult.Fail("invalid", "无效的社交操作")); return; }
            var payload = new JObject { ["kind"] = command.Kind.ToString(), ["accept"] = command.Accept };
            if (command.Target != null) payload["target"] = command.Target;
            if (command.Entity != null) payload["entity"] = command.Entity;
            if (command.Name != null) payload["name"] = command.Name;
            if (command.Text != null) payload["text"] = command.Text;
            IsBusy = true;
            if (Changed != null) Changed();
            GameNetwork.SendCommand("social.execute", payload, result => {
                IsBusy = false;
                if (disposed) return;
                Receive(GameNetwork.CurrentSnapshot);
                if (Changed != null) Changed();
                var socialResult = result.Code == GameCodes.Ok ? result.Data["socialResult"]?.ToObject<SocialResult>() : null;
                completed(socialResult ?? SocialResult.Fail(result.Code, result.Message ?? "服务器未确认此操作"));
            });
        }
        public string ExportJson() { return null; }
        public SocialResult ImportJson(string json) { return SocialResult.Fail("permission", "联机社交记录由服务器恢复"); }
        public void Reset() { }
        public void Dispose() { disposed = true; GameNetwork.SnapshotReceived -= Receive; Changed = null; }
    }
}
