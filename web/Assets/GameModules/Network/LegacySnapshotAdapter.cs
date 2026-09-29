using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Network
{
    public static class LegacySnapshotAdapter
    {
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
            }
            int own;
            if (snapshot.PlayerId != null && indexes.TryGetValue(snapshot.PlayerId, out own))
            {
                全局变量.本机身份 = own;
                var changes = (JObject)privateChanges.DeepClone();
                if (changes["基础信息"] is JObject basic) basic["ID"] = own;
                ApplyValue(全局变量.所有玩家数据表[own], 全局变量.所有玩家数据表[own].GetType(), changes);
            }
            return true;
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
                for (var i = 0; i < array.Count; i++)
                    if (i < list.Count) list[i] = ApplyValue(list[i], itemType, array[i]);
                    else list.Add(ApplyValue(null, itemType, array[i]));
                while (list.Count > array.Count) list.RemoveAt(list.Count - 1);
                return list;
            }
            if (value is JObject obj)
            {
                var target = existing ?? Activator.CreateInstance(type);
                foreach (var fieldValue in obj.Properties())
                {
                    var field = type.GetField(fieldValue.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (field != null && !field.IsInitOnly) field.SetValue(target, ApplyValue(field.GetValue(target), field.FieldType, fieldValue.Value));
                }
                return target;
            }
            return value.ToObject(type);
        }
    }
}
