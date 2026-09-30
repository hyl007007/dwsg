using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Dwsg.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Dwsg.Network
{
    public static class GameNetwork
    {
        private static string endpoint;
        private static string worldId = "main";
        private static string connectionId;
        private static JObject proof;
        private static long sequence;
        private static long snapshotId;
        private static long sessionExpires;
        private static bool connecting, workerRunning, reconnect, forceFull;
        private static readonly Queue<PendingCommand> pending = new Queue<PendingCommand>();
        public static WorldSnapshot CurrentSnapshot { get; private set; }
        public static int GetCityFiefCount(int legacyCityIndex)
        {
            var cities = CurrentSnapshot?.PublicWorld["城池列表"] as JArray;
            if (cities != null && legacyCityIndex >= 0 && legacyCityIndex < cities.Count)
                return cities[legacyCityIndex].Value<int>("封地数量");
            return legacyCityIndex >= 0 && legacyCityIndex < 全局变量.所有城池列表.Count
                ? 全局变量.所有城池列表[legacyCityIndex].城池封地列表.Count : 0;
        }
        public static bool Enabled { get { return !string.IsNullOrWhiteSpace(Endpoint); } }
        public static bool Connected { get { return connectionId != null; } }
        public static bool HasRole { get { return CurrentSnapshot != null && !string.IsNullOrEmpty(CurrentSnapshot.PlayerId); } }
        public static event Action<WorldSnapshot> SnapshotReceived;
        public static event Action<WorldSnapshot> SnapshotApplied { add { SnapshotReceived += value; } remove { SnapshotReceived -= value; } }
        public static event Action<GameEvent> EventReceived;
        public static event Action<GameResult> StatusChanged;
        private static string Endpoint
        {
            get
            {
                var configured = Environment.GetEnvironmentVariable("DWSG_GAME_URL");
                if (string.IsNullOrWhiteSpace(configured)) configured = PlayerPrefs.GetString("DWSG_GAME_URL", "");
                return string.IsNullOrWhiteSpace(configured) ? endpoint : configured.TrimEnd('/');
            }
        }
        public static void Configure(string address, string world)
        {
            endpoint = string.IsNullOrWhiteSpace(address) ? null : address.TrimEnd('/');
            worldId = string.IsNullOrWhiteSpace(world) ? "main" : world;
        }
        public static IEnumerator Connect(JObject phpProof, Action<GameResult> completed)
        {
            proof = (JObject)phpProof.DeepClone();
            yield return ConnectCore(null, null, completed);
            if (!workerRunning) GameNetworkRunner.StartWork(Work());
        }
        public static void CreateRole(string nickname, string nation, Action<GameResult> completed)
        {
            if (!Enabled || proof == null) { completed(GameResult.Reject(GameCodes.Unauthenticated, "请先登录。")); return; }
            GameNetworkRunner.StartWork(ConnectCore(nickname, nation, completed));
        }
        public static bool ApplyInitialSnapshot()
        {
            return CurrentSnapshot != null && LegacySnapshotAdapter.Apply(CurrentSnapshot, CurrentSnapshot.PublicWorld, CurrentSnapshot.PrivatePlayer);
        }
        public static void SendCommand(string type, JObject payload, Action<GameResult> completed)
        {
            if (!Enabled || !HasRole) { completed(GameResult.Reject(GameCodes.Unauthenticated, "请先连接并创建角色。")); return; }
            pending.Enqueue(new PendingCommand { PlayerId = CurrentSnapshot.PlayerId, Completed = completed,
                Command = new GameCommand { RequestId = Guid.NewGuid().ToString("N"), WorldId = CurrentSnapshot.WorldId, Type = type, Payload = (JObject)payload.DeepClone() } });
            if (!workerRunning) GameNetworkRunner.StartWork(Work());
        }
        public static void Disconnect()
        {
            var previous = connectionId;
            connectionId = null; reconnect = false;
            if (previous != null) GameNetworkRunner.StartWork(Fetch("disconnect", new JObject(), previous, _ => { }));
        }
        private static IEnumerator ConnectCore(string nickname, string nation, Action<GameResult> completed)
        {
            if (connecting) { completed(GameResult.Reject(GameCodes.Conflict, "正在连接，请稍候。")); yield break; }
            connecting = true;
            try
            {
                var input = new JObject { ["protocolVersion"] = 1, ["worldId"] = worldId, ["requestId"] = Guid.NewGuid().ToString("N"),
                    ["proof"] = proof.DeepClone(), ["nickname"] = nickname, ["nation"] = nation };
                JObject response = null;
                yield return Fetch("connect", input, null, value => response = value);
                var result = ReadResult(response);
                if (result.Code == GameCodes.Ok || result.Code == GameCodes.RoleRequired)
                {
                    connectionId = response.Value<string>("connectionId");
                    sequence = 0; snapshotId = 0; forceFull = false; reconnect = false;
                    ApplyResponse(response);
                }
                else if (result.Code == GameCodes.Unauthenticated || result.Code == GameCodes.SessionReplaced)
                { connectionId = null; reconnect = false; }
                Notify(StatusChanged, result);
                completed(result);
            }
            finally { connecting = false; }
        }
        private static IEnumerator Work()
        {
            workerRunning = true;
            try
            {
                while (Enabled && proof != null)
                {
                    if (connecting) { yield return null; continue; }
                    if (reconnect && !Connected)
                    {
                        yield return ConnectCore(null, null, _ => { });
                        if (!Connected) { yield return new WaitForSecondsRealtime(2); continue; }
                    }
                    if (!Connected) { yield return new WaitForSecondsRealtime(.25f); continue; }
                    var active = pending.Count == 0 ? null : pending.Peek();
                    if (active != null && active.PlayerId != CurrentSnapshot.PlayerId)
                    {
                        pending.Dequeue(); active.Completed(GameResult.Reject(GameCodes.Forbidden, "账号已变更，原操作未重发。")); continue;
                    }
                    var input = active == null ? new JObject() : JObject.FromObject(active.Command);
                    input["sinceRevision"] = forceFull ? -1 : CurrentSnapshot.WorldRevision;
                    input["sinceSnapshot"] = forceFull ? 0 : snapshotId;
                    input["afterSequence"] = sequence;
                    JObject response = null;
                    var sentConnection = connectionId;
                    bool interrupted = false;
                    Func<bool> interruptPoll = active == null ? (Func<bool>)(() => interrupted = pending.Count > 0) : null;
                    yield return Fetch(active == null ? "poll" : "command", input, sentConnection, value => response = value, interruptPoll);
                    // Responses from a connection superseded locally cannot overwrite its replacement.
                    if (sentConnection != connectionId) continue;
                    if (interrupted)
                    {
                        // New servers retain the exact acknowledged base across an abandoned poll.
                        // Older servers only know the last revision and need a full recovery reply.
                        if (snapshotId == 0) forceFull = true;
                        continue;
                    }
                    var result = ReadResult(response);
                    if (result.Code == GameCodes.Unavailable)
                    {
                        reconnect = true; connectionId = null; Notify(StatusChanged, result);
                        yield return new WaitForSecondsRealtime(2); continue;
                    }
                    if (result.Code == GameCodes.Unauthenticated)
                    {
                        // Host 重启或连接租约过期也会使旧连接失效。
                        // 通过 connect 重新核验 PHP 凭据；凭据确实失效时由 ConnectCore 停止重试。
                        connectionId = null; reconnect = true;
                        continue;
                    }
                    if (result.Code == GameCodes.SessionReplaced)
                    {
                        connectionId = null; reconnect = false; Notify(StatusChanged, result);
                        // Keep the exact pending request until this same player explicitly logs in again.
                        yield return new WaitForSecondsRealtime(.25f); continue;
                    }
                    if (!ApplyResponse(response))
                    {
                        forceFull = true;
                        yield return null;
                        continue;
                    }
                    if (active != null) { pending.Dequeue(); active.Completed(result); }
                    var delay = Math.Max(.1, Math.Min(.5, (sessionExpires - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 3000.0));
                    var nextPoll = Time.realtimeSinceStartup + (float)delay;
                    // Idle polling is throttled; queued player actions wake the worker next frame.
                    while (pending.Count == 0 && Connected && Time.realtimeSinceStartup < nextPoll)
                        yield return null;
                }
            }
            finally { workerRunning = false; }
        }
        private static bool ApplyResponse(JObject response)
        {
            var token = response["snapshot"] as JObject;
            if (token == null) return false;
            var snapshot = token.ToObject<WorldSnapshot>();
            if (response.Value<bool?>("snapshotDelta") == true)
            {
                if (CurrentSnapshot == null || CurrentSnapshot.WorldId != snapshot.WorldId || CurrentSnapshot.PlayerId != snapshot.PlayerId ||
                    CurrentSnapshot.WorldRevision != response.Value<long>("baseRevision") ||
                    response["baseSnapshotId"] != null && snapshotId != response.Value<long>("baseSnapshotId")) { forceFull = true; return false; }
                Merge(CurrentSnapshot.PublicWorld, snapshot.PublicWorld); Merge(CurrentSnapshot.PrivatePlayer, snapshot.PrivatePlayer);
                CurrentSnapshot.WorldRevision = snapshot.WorldRevision; CurrentSnapshot.ServerUtcMs = snapshot.ServerUtcMs;
            }
            else CurrentSnapshot = snapshot;
            snapshotId = response.Value<long?>("snapshotId") ?? 0;
            forceFull = false;
            LegacySnapshotAdapter.Apply(CurrentSnapshot, snapshot.PublicWorld, snapshot.PrivatePlayer);
            Notify(SnapshotReceived, CurrentSnapshot);
            if (response["events"] is JArray events)
                foreach (JObject item in events)
                {
                    var next = item.Value<long>("sequence");
                    if (next <= sequence) continue;
                    Notify(EventReceived, item["event"].ToObject<GameEvent>());
                    sequence = next;
                }
            sequence = Math.Max(sequence, response.Value<long?>("sequence") ?? sequence);
            sessionExpires = response.Value<long?>("sessionExpiresUtcMs") ?? sessionExpires;
            return true;
        }
        private static void Merge(JObject target, JObject changes)
        {
            foreach (var item in changes.Properties())
                if (item.Value.Type == JTokenType.Null) target.Remove(item.Name); else target[item.Name] = item.Value.DeepClone();
        }
        private static GameResult ReadResult(JObject response)
        {
            return response?["result"]?.ToObject<GameResult>() ?? GameResult.Reject(GameCodes.Unavailable, "连接中断，正在确认操作。");
        }
        private static void Notify<T>(Action<T> subscribers, T value)
        {
            if (subscribers == null) return;
            foreach (Action<T> subscriber in subscribers.GetInvocationList())
                try { subscriber(value); } catch (Exception ex) { Debug.LogError("联机视图更新失败: " + ex.GetType().Name); }
        }
        private static IEnumerator Fetch(string route, JObject body, string id, Action<JObject> completed, Func<bool> interrupt = null)
        {
            Uri uri;
            if (!Uri.TryCreate((Endpoint ?? "").TrimEnd('/') + "/" + route, UriKind.Absolute, out uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            { completed(null); yield break; }
            GameHttp.Reply reply = null;
            yield return GameHttp.Post(uri, Encoding.UTF8.GetBytes(body.ToString(Formatting.None)), "application/json", id, value => reply = value, interrupt);
            JObject response = null;
            if (reply != null && reply.Success)
                try { response = JObject.Parse(reply.Text); } catch (JsonException) { }
            completed(response);
        }
        private sealed class PendingCommand
        {
            public string PlayerId;
            public GameCommand Command;
            public Action<GameResult> Completed;
        }
    }
}
