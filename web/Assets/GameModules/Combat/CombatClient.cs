using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json.Linq;
using UnityEngine;
using 玩家数据结构;

namespace Dwsg.Combat
{
    public static class CombatClient
    {
        public static void Bind(Transform battlefieldParent)
        {
            var bridge = battlefieldParent.GetComponent<CombatClientBridge>();
            if (bridge == null) bridge = battlefieldParent.gameObject.AddComponent<CombatClientBridge>();
            bridge.Apply(GameNetwork.CurrentSnapshot);
        }
        public static string GeneralId(int legacyId)
        {
            WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
            JObject mappings = snapshot?.PrivatePlayer?["entityMappings"]?["generals"] as JObject;
            return mappings?.Properties().SingleOrDefault(entry => entry.Value.Value<int>("legacyId") == legacyId && entry.Value.Value<string>("playerId") == snapshot.PlayerId)?.Name;
        }
        public static void Withdraw(string battleId)
        {
            GameNetwork.SendCommand(CommandType(battleId, "withdraw"), new JObject { ["battleId"] = battleId }, result => {
                if (result.Code != GameCodes.Ok) 全局变量.提示类.显示信息(result.Message);
            });
        }
        public static void WithdrawGeneral(string battleId, string generalId, Action<GameResult> completed = null)
        {
            if (string.IsNullOrWhiteSpace(generalId)) { 全局变量.提示类.显示信息("请等待将领同步后撤退"); return; }
            GameNetwork.SendCommand(CommandType(battleId, "withdraw"), new JObject { ["battleId"] = battleId, ["generalId"] = generalId }, result => {
                if (result.Code != GameCodes.Ok) 全局变量.提示类.显示信息(result.Message);
                completed?.Invoke(result);
            });
        }
        public static string CommandType(string battleId, string action)
        {
            string kind = GameNetwork.CurrentSnapshot?.PrivatePlayer?["战斗运行"]?[battleId]?.Value<string>("Kind");
            return "combat." + (kind == "city" ? "city" : "bandit") + "." + action;
        }
    }

}
