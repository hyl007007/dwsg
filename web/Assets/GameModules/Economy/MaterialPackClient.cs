using System;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Economy
{
    public static class MaterialPackClient
    {
        private static bool materialPending;
        public static bool Pending { get { return materialPending || EquipmentBoxClient.Pending; } }

        public static bool Supports(string itemName)
        {
            return MaterialPackRules.MaterialName(itemName) != null || EquipmentBoxRules.Slot(itemName) >= 0;
        }

        public static void Use(string itemName, int quantity, Action<GameResult> completed)
        {
            if (Pending) { completed(GameResult.Reject(GameCodes.Conflict, "正在使用宝箱，请稍候")); return; }
            if (EquipmentBoxRules.Slot(itemName) >= 0) { EquipmentBoxClient.Use(itemName, quantity, completed); return; }
            if (MaterialPackRules.MaterialName(itemName) == null || quantity <= 0)
            { completed(GameResult.Reject(GameCodes.InvalidArgument, "请选择有效的材料包和数量")); return; }
            if (GameNetwork.Enabled)
            {
                materialPending = true;
                GameNetwork.SendCommand("item.use", new JObject { ["itemName"] = itemName, ["quantity"] = quantity }, result =>
                {
                    materialPending = false;
                    completed(result);
                });
                return;
            }
            int index = 全局变量.本机身份;
            if (index < 0 || index >= 全局变量.所有玩家数据表.Count)
            { completed(GameResult.Reject(GameCodes.Forbidden, "请选择自己的角色")); return; }
            var player = 全局变量.所有玩家数据表[index];
            var candidate = new JObject { ["背包道具列表"] = JObject.FromObject(player.背包道具列表) };
            var resultOffline = MaterialPackRules.Use(candidate, JArray.FromObject(全局道具库.道具列表), itemName, quantity);
            if (resultOffline.Code == GameCodes.Ok)
            {
                var packs = player.背包道具列表.获取道具分类列表(itemName);
                var materials = player.背包道具列表.获取道具分类列表(MaterialPackRules.MaterialName(itemName));
                for (int i = 0; i < quantity; i++)
                {
                    ItemStackRules.ConsumeOne<道具信息>(packs, itemName, item => item.名字, item => item.数量, (item, count) => item.数量 = count);
                    ItemStackRules.Add<道具信息>(materials, MaterialPackRules.MaterialName(itemName), 99, item => item.名字, item => item.数量,
                        (item, count) => item.数量 = count, (name, count) => new 道具信息(name, count));
                }
            }
            completed(resultOffline);
        }
    }
}
