using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Client.Chat
{
    public sealed class ChatInbox
    {
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> order = new Queue<string>();
        private readonly HashSet<string> seenNotifications = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> permittedNotifications = new HashSet<string>(StringComparer.Ordinal);
        private string worldId, playerId, nation, nationId, cities, selectedCity;

        public bool SetSession(WorldSnapshot snapshot, int selectedFiefIndex = 0)
        {
            if (snapshot == null) return false;
            permittedNotifications.Clear();
            if (snapshot.PrivatePlayer["notifications"] is JObject notices)
                foreach (var entry in notices.Properties()) permittedNotifications.Add(entry.Name);
            string nextNation = snapshot.PrivatePlayer["基础信息"]?.Value<string>("国家");
            var currentNation = (snapshot.PublicWorld["国家列表"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(n => n.Value<string>("国号") == nextNation);
            if (!(currentNation?["成员列表"] is JArray members) || !members.Any(m => m.Type == JTokenType.String && m.Value<string>() == snapshot.PlayerId))
                nextNation = null;
            string nextNationId = nextNation == null ? null : currentNation.Value<string>("nationId");
            var locations = new SortedSet<string>(StringComparer.Ordinal);
            var permitted = snapshot.PrivatePlayer["chatCities"] as JArray;
            if (permitted != null) foreach (var city in permitted) { string key = CityKey(city as JObject, "x", "y"); if (key != null) locations.Add(key); }
            string nextCities = string.Join(";", locations), nextSelected = CityKey(SelectedCity(snapshot, selectedFiefIndex), "x", "y");
            if (nextSelected != null && !locations.Contains(nextSelected)) nextSelected = null;
            if (worldId == snapshot.WorldId && playerId == snapshot.PlayerId && nation == nextNation && nationId == nextNationId && cities == nextCities && selectedCity == nextSelected) return false;
            worldId = snapshot.WorldId; playerId = snapshot.PlayerId; nation = nextNation; nationId = nextNationId; cities = nextCities; selectedCity = nextSelected;
            seen.Clear(); order.Clear(); seenNotifications.Clear();
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
            bool notice = channel == "system" && message.Value<string>("notificationId") == message.Value<string>("messageId") &&
                message.Value<string>("senderPlayerId") == "server" && permittedNotifications.Contains(message.Value<string>("messageId"));
            bool rumor = channel == "rumor" && message.Value<string>("senderPlayerId") == "server";
            bool national = channel == "nation" && !string.IsNullOrEmpty(nation) && (nationId != null
                ? message["nationId"]?.Type == JTokenType.String && message.Value<string>("nationId") == nationId
                : message["nationId"] == null && message.Value<string>("nation") == nation);
            if (channel != "world" && !notice && !rumor && !national &&
                !(channel == "city" && selectedCity != null && CityKey(message, "cityX", "cityY") == selectedCity)) return false;
            string id = message.Value<string>("messageId"), content = message.Value<string>("content");
            if (string.IsNullOrEmpty(id) || content.Length == 0 || (!notice && content.Length > (rumor ? 512 : 40))) return false;
            if (notice) { if (!seenNotifications.Add(id)) return false; }
            else
            {
                if (!seen.Add(id)) return false;
                order.Enqueue(id);
                while (order.Count > 300) seen.Remove(order.Dequeue());
            }
            self = message.Value<string>("senderPlayerId") == playerId;
            return true;
        }

        public static JObject SelectedCity(WorldSnapshot snapshot, int selectedFiefIndex)
        {
            var fiefs = snapshot?.PrivatePlayer["封地信息表"] as JArray;
            return fiefs != null && selectedFiefIndex >= 0 && selectedFiefIndex < fiefs.Count
                ? fiefs[selectedFiefIndex]["所在城池"] as JObject : null;
        }

        public static bool CanSelectCity(WorldSnapshot snapshot, JObject location)
        {
            string selected = CityKey(location, "x", "y");
            var permitted = snapshot?.PrivatePlayer["chatCities"] as JArray;
            if (selected == null || permitted == null) return false;
            foreach (var city in permitted) if (CityKey(city as JObject, "x", "y") == selected) return true;
            return false;
        }

        private static string CityKey(JObject location, string xName, string yName)
        {
            int x, y;
            return location?[xName]?.Type == JTokenType.Integer && location[yName]?.Type == JTokenType.Integer &&
                int.TryParse(location[xName].ToString(), out x) && int.TryParse(location[yName].ToString(), out y) && x >= 0 && y >= 0
                ? x + "," + y : null;
        }
    }
}
