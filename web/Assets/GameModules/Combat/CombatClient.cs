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
        public static bool HasActiveArmy(string battleId)
        {
            WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
            BanditBattle battle = snapshot?.PrivatePlayer?["战斗运行"]?[battleId]?.ToObject<BanditBattle>();
            return battle != null && !battle.SettlementApplied && (battle.PlayerId == snapshot.PlayerId
                ? battle.Attackers.Any(unit => !unit.Retired)
                : battle.GarrisonArmies.Any(army => army.PlayerId == snapshot.PlayerId && (army.Phase == "marching" || army.Phase == "fighting")));
        }
        public static bool CanWithdrawUnit(string battleId, string generalId)
        {
            WorldSnapshot snapshot = GameNetwork.CurrentSnapshot;
            BanditBattle battle = snapshot?.PrivatePlayer?["战斗运行"]?[battleId]?.ToObject<BanditBattle>();
            return battle != null && !battle.SettlementApplied && battle.Attackers.Concat(battle.Defenders).Any(unit => !unit.Retired
                && unit.GeneralId == generalId && (unit.GeneralOwnerId ?? (unit.Side == 0 ? battle.PlayerId : null)) == snapshot.PlayerId);
        }
        public static void Withdraw(string battleId)
        {
            BanditBattle battle = GameNetwork.CurrentSnapshot?.PrivatePlayer?["战斗运行"]?[battleId]?.ToObject<BanditBattle>();
            if (battle != null && battle.PlayerId != GameNetwork.CurrentSnapshot.PlayerId)
            {
                foreach (var army in battle.GarrisonArmies.Where(army => army.PlayerId == GameNetwork.CurrentSnapshot.PlayerId && (army.Phase == "marching" || army.Phase == "fighting")))
                    SendGarrisonWithdrawal(army.ArmyId, null, null);
                return;
            }
            GameNetwork.SendCommand(CommandType(battleId, "withdraw"), new JObject { ["battleId"] = battleId }, result => {
                if (result.Code != GameCodes.Ok) 全局变量.提示类.显示信息(result.Message);
            });
        }
        public static void WithdrawGeneral(string battleId, string generalId, Action<GameResult> completed = null)
        {
            if (string.IsNullOrWhiteSpace(generalId)) { 全局变量.提示类.显示信息("请等待将领同步后撤退"); return; }
            BanditBattle battle = GameNetwork.CurrentSnapshot?.PrivatePlayer?["战斗运行"]?[battleId]?.ToObject<BanditBattle>();
            CombatUnit garrison = battle?.Defenders.FirstOrDefault(unit => unit.PlayerGarrison && !unit.Retired && unit.GeneralId == generalId && unit.GeneralOwnerId == GameNetwork.CurrentSnapshot.PlayerId);
            if (garrison != null) { SendGarrisonWithdrawal(garrison.ArmyId, generalId, completed); return; }
            GameNetwork.SendCommand(CommandType(battleId, "withdraw"), new JObject { ["battleId"] = battleId, ["generalId"] = generalId }, result => {
                if (result.Code != GameCodes.Ok) 全局变量.提示类.显示信息(result.Message);
                completed?.Invoke(result);
            });
        }
        private static void SendGarrisonWithdrawal(string armyId, string generalId, Action<GameResult> completed)
        {
            var payload = new JObject { ["armyId"] = armyId };
            if (generalId != null) payload["generalId"] = generalId;
            GameNetwork.SendCommand("combat.city.garrison.withdraw", payload, result => {
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
