using System;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Economy
{
    public static class EquipmentBoxClient
    {
        public static bool Pending { get; private set; }

        public static void Use(string itemName, int quantity, Action<GameResult> completed)
        {
            if (MaterialPackClient.Pending) { completed(GameResult.Reject(GameCodes.Conflict, "正在使用宝箱，请稍候")); return; }
            if (EquipmentBoxRules.Slot(itemName) < 0 || quantity <= 0)
            { completed(GameResult.Reject(GameCodes.InvalidArgument, "请选择有效的装备箱和数量")); return; }
            if (GameNetwork.Enabled)
            {
                Pending = true;
                GameNetwork.SendCommand("item.use", new JObject { ["itemName"] = itemName, ["quantity"] = quantity }, result =>
                {
                    Pending = false;
                    completed(result);
                });
                return;
            }
            int index = 全局变量.本机身份;
            if (index < 0 || index >= 全局变量.所有玩家数据表.Count)
            { completed(GameResult.Reject(GameCodes.Forbidden, "请选择自己的角色")); return; }
            var player = 全局变量.所有玩家数据表[index];
            var candidate = new JObject { ["背包道具列表"] = JObject.FromObject(player.背包道具列表), ["背包装备列表"] = JObject.FromObject(player.背包装备列表) };
            var resultOffline = EquipmentBoxRules.Use(candidate, JArray.FromObject(全局道具库.道具列表), JArray.FromObject(全局装备库.属性表), itemName, quantity, UnityEngine.Random.Range);
            if (resultOffline.Code == GameCodes.Ok)
            {
                var boxes = player.背包道具列表.获取道具分类列表(itemName);
                var equipment = player.背包装备列表.获取指定部位列表(EquipmentBoxRules.Slot(itemName));
                foreach (JObject entry in resultOffline.Data["equipment"])
                {
                    ItemStackRules.ConsumeOne<道具信息>(boxes, itemName, item => item.名字, item => item.数量, (item, count) => item.数量 = count);
                    var generated = entry["equipment"].ToObject<将领装备>();
                    generated.装备信息 = 全局装备库.获取指定名字的装备(generated.装备信息.名称);
                    equipment.Add(generated);
                }
            }
            completed(resultOffline);
        }
    }
}
