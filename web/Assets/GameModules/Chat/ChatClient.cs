using System;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Client.Chat
{
    public static class ChatClient
    {
        private static readonly ChatInbox inbox = new ChatInbox();
        private static bool initialized;
        private static Action<JObject, bool> received;
        private static Action<string> rejected;
        private static Action reset;

        public static void Initialize(Action<JObject, bool> receive, Action<string> reject, Action clear)
        {
            received = receive; rejected = reject; reset = clear;
            if (!initialized)
            {
                initialized = true;
                GameNetwork.EventReceived += ReceiveEvent;
                GameNetwork.SnapshotReceived += ReceiveSnapshot;
            }
            if (GameNetwork.Enabled && GameNetwork.CurrentSnapshot != null) ReceiveSnapshot(GameNetwork.CurrentSnapshot);
        }

        public static bool Send(string channel, string content)
        {
            if (!GameNetwork.Enabled || !GameNetwork.Connected || !GameNetwork.HasRole)
            {
                rejected?.Invoke("联机连接未就绪，请连接后再发言。");
                return false;
            }
            GameNetwork.SendCommand("chat.send", new JObject { ["channel"] = channel, ["content"] = content }, result =>
            {
                if (result.Code == GameCodes.Ok) Receive(result.WorldId, result.Data["chatMessage"] as JObject);
                else rejected?.Invoke(string.IsNullOrEmpty(result.Message) ? "消息未确认，请稍后重试。" : result.Message);
            });
            return true;
        }

        private static void ReceiveEvent(GameEvent message)
        {
            if (GameNetwork.Enabled && message.Type == "chat.message") Receive(message.WorldId, message.Data);
        }

        private static void ReceiveSnapshot(WorldSnapshot snapshot)
        {
            if (!GameNetwork.Enabled || snapshot == null) return;
            if (inbox.SetSession(snapshot)) reset?.Invoke();
            if (snapshot.PublicWorld["chatMessages"] is JArray history)
                foreach (var item in history) Receive(snapshot.WorldId, item as JObject);
        }

        private static void Receive(string worldId, JObject message)
        {
            if (inbox.TryReceive(worldId, message, out bool self)) received?.Invoke(message, self);
        }
    }
}
