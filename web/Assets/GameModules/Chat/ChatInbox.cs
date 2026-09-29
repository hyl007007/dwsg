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
        private string worldId, playerId, nation, cities, selectedCity;

        public bool SetSession(WorldSnapshot snapshot, int selectedFiefIndex = 0)
        {
            if (snapshot == null) return false;
            string nextNation = snapshot.PrivatePlayer["基础信息"]?.Value<string>("国家");
            var locations = new SortedSet<string>(StringComparer.Ordinal);
            var permitted = snapshot.PrivatePlayer["chatCities"] as JArray;
            if (permitted != null) foreach (var city in permitted) { string key = CityKey(city as JObject, "x", "y"); if (key != null) locations.Add(key); }
            string nextCities = string.Join(";", locations), nextSelected = CityKey(SelectedCity(snapshot, selectedFiefIndex), "x", "y");
            if (nextSelected != null && !locations.Contains(nextSelected)) nextSelected = null;
            if (worldId == snapshot.WorldId && playerId == snapshot.PlayerId && nation == nextNation && cities == nextCities && selectedCity == nextSelected) return false;
            worldId = snapshot.WorldId; playerId = snapshot.PlayerId; nation = nextNation; cities = nextCities; selectedCity = nextSelected;
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
            if (channel != "world" && !(channel == "nation" && !string.IsNullOrEmpty(nation) && message.Value<string>("nation") == nation) &&
                !(channel == "city" && selectedCity != null && CityKey(message, "cityX", "cityY") == selectedCity)) return false;
            string id = message.Value<string>("messageId"), content = message.Value<string>("content");
            if (string.IsNullOrEmpty(id) || content.Length == 0 || content.Length > 40 || !seen.Add(id)) return false;
            order.Enqueue(id);
            while (order.Count > 300) seen.Remove(order.Dequeue());
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
