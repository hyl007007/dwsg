using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared
{
    public static class StableNationIds
    {
        // Nations retain their original ID after sorting, capital moves and destruction.
        // Keep retired bindings here; a reused 国号 must never inherit the old GUID.
        public static void EnsureMappings(WorldState candidate)
        {
            RequireWorld(candidate);
            JObject map;
            bool importing = candidate.EntityMappings["nationMappingVersion"] == null;
            if (importing)
            {
                if (candidate.EntityMappings["nations"] != null)
                    throw new InvalidOperationException("Legacy nation indexes require verified import identity evidence.");
                map = new JObject();
            }
            else map = (JObject)RequireMap(candidate).DeepClone();
            Reconcile(candidate, map, importing);
        }

        // Administrator-only migration: each value must be the original ID proved by
        // import metadata or a verified backup, never current 国家列表[oldIndex].ID.
        public static void MigrateLegacy(WorldState candidate, JObject verifiedLegacyIdsByGuid)
        {
            RequireWorld(candidate);
            if (candidate.EntityMappings["nationMappingVersion"] != null ||
                !(candidate.EntityMappings["nations"] is JObject old) || old.Count == 0 ||
                verifiedLegacyIdsByGuid == null || verifiedLegacyIdsByGuid.Count != old.Count)
                throw new InvalidOperationException("Verified legacy nation identity evidence is required.");
            var indexes = new HashSet<int>();
            var ids = new HashSet<int>();
            var map = new JObject();
            foreach (var binding in old.Properties())
            {
                int index = Integer(binding.Value, false);
                int id = Integer(verifiedLegacyIdsByGuid[binding.Name], true);
                if (!indexes.Add(index) || !ids.Add(id))
                    throw new InvalidOperationException("Legacy nation identity evidence is ambiguous.");
                map[binding.Name] = Binding(id, false);
            }
            Reconcile(candidate, map, true);
        }

        // Called only on the successful nation.create working candidate, before commit.
        public static void RegisterCreated(WorldState candidate, int legacyNationId)
        {
            var map = (JObject)RequireMap(candidate).DeepClone();
            var ids = ReadMappings(map);
            if (legacyNationId <= ids.Keys.DefaultIfEmpty(0).Max() ||
                Integer(candidate.Data["国家ID记录"], true) != legacyNationId ||
                !Nations(candidate).ContainsKey(legacyNationId))
                throw new InvalidOperationException("Only a newly allocated original nation can be registered.");
            map[Guid.NewGuid().ToString("N")] = Binding(legacyNationId, false);
            Reconcile(candidate, map, false);
        }

        public static string RequireId(WorldState state, int legacyNationId)
        {
            var map = RequireMap(state);
            var ids = ReadMappings(map);
            if (!Nations(state).ContainsKey(legacyNationId) || !ids.TryGetValue(legacyNationId, out var guid) ||
                map[guid].Value<bool>("deleted"))
                throw new InvalidOperationException("Active original nation identity is missing.");
            return guid;
        }

        public static JObject ResolveNation(WorldState state, string stableNationId)
        {
            var map = RequireMap(state);
            ReadMappings(map);
            var binding = map[stableNationId] as JObject;
            if (binding == null || binding.Value<bool>("deleted")) return null;
            return Nations(state).TryGetValue(Integer(binding["legacyId"], true), out var nation) ? nation : null;
        }

        private static void Reconcile(WorldState candidate, JObject map, bool allowNew)
        {
            var nations = Nations(candidate);
            var ids = ReadMappings(map);
            int greatest = ids.Keys.DefaultIfEmpty(0).Max();
            int maximum = Math.Max(greatest, nations.Keys.DefaultIfEmpty(0).Max());
            int counter = candidate.Data["国家ID记录"] == null ? maximum : Integer(candidate.Data["国家ID记录"], false);
            if (counter < maximum) throw new InvalidOperationException("Original nation counter is below an allocated ID.");
            foreach (var binding in map.Properties())
            {
                int id = Integer(binding.Value["legacyId"], true);
                bool present = nations.ContainsKey(id);
                if (present && binding.Value.Value<bool>("deleted"))
                    throw new InvalidOperationException("A retired original nation ID was reused.");
                binding.Value["deleted"] = !present;
            }
            foreach (int id in nations.Keys)
            {
                if (ids.ContainsKey(id)) continue;
                if (!allowNew || id <= greatest) throw new InvalidOperationException("Original nation identity requires an explicit creation or verified import.");
                map[Guid.NewGuid().ToString("N")] = Binding(id, false);
            }
            // Nothing reaches the candidate until every identity/counter check passed.
            candidate.EntityMappings["nations"] = map;
            candidate.EntityMappings["nationMappingVersion"] = 1;
            if (candidate.Data["国家ID记录"] == null) candidate.Data["国家ID记录"] = counter;
        }

        private static JObject Binding(int id, bool deleted)
        {
            return new JObject { ["legacyId"] = id, ["deleted"] = deleted };
        }

        private static Dictionary<int, string> ReadMappings(JObject map)
        {
            var ids = new Dictionary<int, string>();
            var guids = new HashSet<Guid>();
            foreach (var binding in map.Properties())
            {
                Guid guid;
                var value = binding.Value as JObject;
                if (!Guid.TryParseExact(binding.Name, "N", out guid) || !guids.Add(guid) || value == null ||
                    value["deleted"]?.Type != JTokenType.Boolean)
                    throw new InvalidOperationException("Stable nation mapping is invalid.");
                int id = Integer(value["legacyId"], true);
                if (ids.ContainsKey(id)) throw new InvalidOperationException("Duplicate original nation identity.");
                ids.Add(id, binding.Name);
            }
            return ids;
        }

        private static Dictionary<int, JObject> Nations(WorldState state)
        {
            RequireWorld(state);
            if (!(state.Data["国家列表"] is JArray nations)) throw new InvalidOperationException("Original nation list is missing.");
            var ids = new Dictionary<int, JObject>();
            foreach (var entry in nations)
            {
                if (!(entry is JObject nation)) throw new InvalidOperationException("Original nation record is invalid.");
                int id = Integer(nation["ID"], true);
                if (ids.ContainsKey(id)) throw new InvalidOperationException("Duplicate original nation ID.");
                ids.Add(id, nation);
            }
            return ids;
        }

        private static JObject RequireMap(WorldState state)
        {
            RequireWorld(state);
            if (Integer(state.EntityMappings["nationMappingVersion"], true) != 1 ||
                !(state.EntityMappings["nations"] is JObject map))
                throw new InvalidOperationException("Stable nation mappings are not initialized.");
            return map;
        }

        private static void RequireWorld(WorldState state)
        {
            if (state == null || state.Data == null || state.EntityMappings == null)
                throw new InvalidOperationException("World state is missing.");
        }

        private static int Integer(JToken value, bool positive)
        {
            int number;
            if (value == null || value.Type != JTokenType.Integer ||
                !int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ||
                number < (positive ? 1 : 0))
                throw new InvalidOperationException("Original nation identity is invalid.");
            return number;
        }
    }
}
