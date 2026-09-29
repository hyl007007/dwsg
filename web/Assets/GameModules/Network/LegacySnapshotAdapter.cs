using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Network
{
    public static class LegacySnapshotAdapter
    {
        private static string equipmentOwner;
        private static readonly Dictionary<string, object> equipmentReferences = new Dictionary<string, object>(StringComparer.Ordinal);
        public static bool Apply(WorldSnapshot snapshot, JObject publicChanges, JObject privateChanges)
        {
            var players = snapshot.PublicWorld["玩家列表"] as JArray;
            if (players == null) return false;
            var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < players.Count; i++) indexes[players[i].Value<string>("playerId")] = i;
            if (publicChanges["玩家列表"] != null)
            {
                var visible = (JArray)players.DeepClone();
                for (var i = 0; i < visible.Count; i++) visible[i]["基础信息"]["ID"] = i;
                ApplyValue(全局变量.所有玩家数据表, 全局变量.所有玩家数据表.GetType(), visible);
            }
            if (publicChanges["国家列表"] is JArray nations)
            {
                var visible = (JArray)nations.DeepClone();
                for (var i = 0; i < visible.Count; i++)
                {
                    visible[i]["ID"] = i + 1;
                    foreach (var field in new[] { "国王", "大都督", "丞相", "奋武将军", "征东将军", "都尉", "侍郎" })
                        visible[i][field] = Index(indexes, visible[i][field]);
                    if (visible[i]["成员列表"] is JArray members)
                        for (var j = 0; j < members.Count; j++) members[j] = Index(indexes, members[j]);
                }
                ApplyValue(全局变量.所有国家列表, 全局变量.所有国家列表.GetType(), visible);
            }
            if (publicChanges["城池列表"] is JArray cities)
            {
                var visible = (JArray)cities.DeepClone();
                foreach (JObject city in visible) city["城主"] = Index(indexes, city["城主"]);
                ApplyValue(全局变量.所有城池列表, 全局变量.所有城池列表.GetType(), visible);
                // JsonIgnore omits this field when a city DTO is first created by ToObject.
                for (int i = 0; i < visible.Count; i++)
                    全局变量.所有城池列表[i].正在交战 = visible[i]["正在交战"]?.Type == JTokenType.Boolean && visible[i].Value<bool>("正在交战");
            }
            if (publicChanges["山贼列表"] is JArray bandits)
                ApplyValue(全局变量.所有山贼数据列表, 全局变量.所有山贼数据列表.GetType(), bandits);
            if (snapshot.PublicWorld["商城商品"] is JArray catalog)
                foreach (var category in new[] { 全局商城库.热卖商品列表, 全局商城库.特价商品列表, 全局商城库.装备商品列表,
                    全局商城库.生产商品列表, 全局商城库.加速商品列表, 全局商城库.宝物商品列表, 全局商城库.宝箱商品列表, 全局商城库.其他商品列表 })
                    foreach (var product in category)
                        foreach (var entry in catalog)
                            if (entry is JObject item && item.Value<string>("道具名") == product.道具名)
                            {
                                ApplyValue(product, typeof(商品属性类), item);
                                break;
                            }
            int own = -1;
            var hasOwner = snapshot.PlayerId != null && indexes.TryGetValue(snapshot.PlayerId, out own);
            if (!hasOwner) own = -1;
            var salaryReadyUtcMs = hasOwner ? snapshot.PrivatePlayer.Value<long?>("salaryReadyUtcMs") ?? 0L : 0L;
            全局变量.领取倒计时 = (int)Math.Min(int.MaxValue, Math.Ceiling(Math.Max(0m, (decimal)salaryReadyUtcMs - snapshot.ServerUtcMs) / 1000m));
            if (hasOwner)
            {
                全局变量.本机身份 = own;
                var changes = (JObject)privateChanges.DeepClone();
                if (changes["基础信息"] is JObject basic) basic["ID"] = own;
                if (changes["背包装备列表"] is JObject equipment)
                {
                    ApplyEquipment(全局变量.所有玩家数据表[own].背包装备列表, equipment,
                        snapshot.PrivatePlayer["entityMappings"]?["equipment"] as JObject ?? new JObject(), snapshot.WorldId + ":" + snapshot.PlayerId);
                    changes.Remove("背包装备列表");
                }
                ApplyValue(全局变量.所有玩家数据表[own], 全局变量.所有玩家数据表[own].GetType(), changes);
                foreach (var fief in 全局变量.所有玩家数据表[own].封地信息表)
                    foreach (var general in fief.将领信息表)
                        if (general.详细信息 != null) general.详细信息.身份 = own;
            }
            ApplyOwnFiefLinks(own);
            return true;
        }
        private static void ApplyOwnFiefLinks(int own)
        {
            foreach (var city in 全局变量.所有城池列表)
            {
                var updated = new List<封地索引>();
                if (own >= 0)
                    foreach (var fief in 全局变量.所有玩家数据表[own].封地信息表)
                    {
                        if (fief.所在城池 == null || fief.所在城池.x != city.坐标x || fief.所在城池.y != city.坐标y) continue;
                        var link = city.城池封地列表.Find(item => item.第几个玩家 == own && item.封地ID标识 == fief.ID);
                        updated.Add(link ?? new 封地索引(own, fief.ID));
                    }
                city.城池封地列表.Clear();
                city.城池封地列表.AddRange(updated);
            }
        }
        private static void ApplyEquipment(object backpack, JObject values, JObject mappings, string owner)
        {
            if (equipmentOwner != owner) { equipmentReferences.Clear(); equipmentOwner = owner; }
            var fields = new[] { "头盔装备列表", "武器装备列表", "铠甲装备列表", "坐骑装备列表" };
            for (var slot = 0; slot < fields.Length; slot++)
            {
                var field = backpack.GetType().GetField(fields[slot]);
                var list = (IList)field.GetValue(backpack);
                var itemType = field.FieldType.GetGenericArguments()[0];
                var array = values[fields[slot]] as JArray;
                if (array == null) continue;
                var stableIds = new Dictionary<int, string>();
                foreach (var entry in mappings.Properties())
                    if (entry.Value.Value<int>("slot") == slot) stableIds[entry.Value.Value<int>("legacyIndex")] = entry.Name;
                var updated = new List<object>();
                for (var i = 0; i < array.Count; i++)
                {
                    string id;
                    object previous = null;
                    if (stableIds.TryGetValue(i, out id)) equipmentReferences.TryGetValue(id, out previous);
                    var current = ApplyValue(previous, itemType, array[i]);
                    if (id != null) equipmentReferences[id] = current;
                    updated.Add(current);
                }
                list.Clear();
                foreach (var item in updated) list.Add(item);
            }
        }
        private static int Index(Dictionary<string, int> indexes, JToken id)
        {
            int value;
            return id != null && id.Type == JTokenType.String && indexes.TryGetValue(id.Value<string>(), out value) ? value : -1;
        }
        private static object ApplyValue(object existing, Type type, JToken value)
        {
            if (value.Type == JTokenType.Null) return type.IsValueType ? Activator.CreateInstance(type) : null;
            if (value is JArray array && typeof(IList).IsAssignableFrom(type))
            {
                var list = (IList)(existing ?? Activator.CreateInstance(type));
                var itemType = type.GetGenericArguments()[0];
                var useIdentity = itemType.Name == "将领信息" || itemType.Name == "封地信息" || itemType.Name == "玩家数据" || itemType.Name == "山贼属性信息" || itemType.Name == "国家信息库类";
                var incomingKeys = new HashSet<string>(StringComparer.Ordinal);
                if (useIdentity)
                    foreach (var item in array)
                    {
                        var key = Identity(item, itemType);
                        // Original bandit squads deliberately reuse ID 0; retain their distinct positional objects.
                        if (key == null || !incomingKeys.Add(key)) { useIdentity = false; break; }
                    }
                var identities = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var item in list)
                {
                    var key = useIdentity ? Identity(item, itemType) : null;
                    if (key != null) identities[key] = item;
                }
                var items = new List<object>();
                for (var i = 0; i < array.Count; i++)
                {
                    var key = useIdentity ? Identity(array[i], itemType) : null;
                    object previous = null;
                    if (key != null) identities.TryGetValue(key, out previous);
                    else if (i < list.Count) previous = list[i];
                    items.Add(ApplyValue(previous, itemType, array[i]));
                }
                list.Clear();
                foreach (var item in items) list.Add(item);
                return list;
            }
            if (value is JObject obj)
            {
                // The original DTOs include parameterized constructors; use their existing JSON codec.
                if (existing == null) return obj.ToObject(type);
                var target = existing;
                foreach (var fieldValue in obj.Properties())
                {
                    var field = type.GetField(fieldValue.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (field != null && !field.IsInitOnly) field.SetValue(target, ApplyValue(field.GetValue(target), field.FieldType, fieldValue.Value));
                }
                return target;
            }
            return value.ToObject(type);
        }
        private static string Identity(JToken value, Type type)
        {
            var obj = value as JObject;
            if (type.Name == "国家信息库类")
                return obj?["国号"]?.Type == JTokenType.String && !string.IsNullOrEmpty(obj.Value<string>("国号")) ? obj.Value<string>("国号") : null;
            if (type.Name == "山贼属性信息" && obj?["坐标x"] != null && obj["坐标y"] != null)
                return obj.Value<int>("坐标x").ToString(CultureInfo.InvariantCulture) + ":" + obj.Value<int>("坐标y").ToString(CultureInfo.InvariantCulture);
            var key = obj?["ID"] ?? obj?["基础信息"]?["ID"];
            return key != null && (key.Type == JTokenType.Integer || key.Type == JTokenType.Float)
                ? key.Value<long>().ToString(CultureInfo.InvariantCulture) : null;
        }
        private static string Identity(object value, Type type)
        {
            if (value == null) return null;
            if (type.Name == "国家信息库类")
                return type.GetField("国号").GetValue(value) as string;
            if (type.Name == "山贼属性信息")
                return Convert.ToInt32(type.GetField("坐标x").GetValue(value)).ToString(CultureInfo.InvariantCulture) + ":" +
                    Convert.ToInt32(type.GetField("坐标y").GetValue(value)).ToString(CultureInfo.InvariantCulture);
            var field = type.GetField("ID");
            if (field != null) return Convert.ToInt64(field.GetValue(value)).ToString(CultureInfo.InvariantCulture);
            var basic = type.GetField("基础信息");
            return basic == null ? null : Identity(basic.GetValue(value), basic.FieldType);
        }
    }
}
