using System;
using System.Collections.Generic;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Client.Chat
{
    public sealed class ChatInbox
    {
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> order = new Queue<string>();
        private string worldId, playerId, nation;

        public bool SetSession(WorldSnapshot snapshot)
        {
            if (snapshot == null) return false;
            string nextNation = snapshot.PrivatePlayer["基础信息"]?.Value<string>("国家");
            if (worldId == snapshot.WorldId && playerId == snapshot.PlayerId && nation == nextNation) return false;
            worldId = snapshot.WorldId; playerId = snapshot.PlayerId; nation = nextNation;
            seen.Clear(); order.Clear();
            return true;
        }

        public bool TryReceive(string messageWorldId, JObject message, out bool self)
        {
            self = false;
            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(playerId) || messageWorldId != worldId || message == null ||
                message["messageId"]?.Type != JTokenType.String || message["senderPlayerId"]?.Type != JTokenType.String ||
                message["senderName"]?.Type != JTokenType.String || message["content"]?.Type != JTokenType.String ||
                message["serverUtcMs"]?.Type != JTokenType.Integer) return false;
            string channel = message.Value<string>("channel");
            if (channel != "world" && (channel != "nation" || string.IsNullOrEmpty(nation) || message.Value<string>("nation") != nation))
                return false;
            string id = message.Value<string>("messageId"), content = message.Value<string>("content");
            if (string.IsNullOrEmpty(id) || content.Length == 0 || content.Length > 40 || !seen.Add(id)) return false;
            order.Enqueue(id);
            while (order.Count > 300) seen.Remove(order.Dequeue());
            self = message.Value<string>("senderPlayerId") == playerId;
            return true;
        }
    }
}
