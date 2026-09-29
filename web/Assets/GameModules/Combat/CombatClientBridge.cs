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
    public sealed class CombatClientBridge : MonoBehaviour
    {
        private WorldSnapshot pending;
        private string role;
        private long revision = -1;
        private readonly Dictionary<string, BanditBattleView> views = new Dictionary<string, BanditBattleView>();
        private readonly Dictionary<string, 军情信息> armies = new Dictionary<string, 军情信息>();
        private readonly HashSet<string> known = new HashSet<string>();
        private readonly HashSet<string> notified = new HashSet<string>();
        private void OnEnable() { GameNetwork.SnapshotApplied += Apply; Apply(GameNetwork.CurrentSnapshot); }
        private void OnDisable() { GameNetwork.SnapshotApplied -= Apply; Clear(); }
        public void Apply(WorldSnapshot snapshot) { pending = snapshot; }
        private void Update()
        {
            if (!GameNetwork.Enabled || !GameNetwork.Connected || !GameNetwork.HasRole) { if (role != null) Clear(); return; }
            if (pending == null || 全局变量.山贼战斗场景pre == null) return;
            string key = pending.WorldId + ":" + pending.PlayerId;
            if (key != role) { Clear(); role = key; }
            if (revision == pending.WorldRevision) return;
            revision = pending.WorldRevision;
            var battles = pending.PrivatePlayer["战斗运行"] as JObject;
            if (battles == null) return;
            var activeArmies = new HashSet<string>();
            foreach (JProperty entry in battles.Properties())
            {
                BanditBattle battle = entry.Value.ToObject<BanditBattle>();
                if (battle.PlayerId != pending.PlayerId) continue;
                if (battle.SettlementApplied)
                {
                    if (views.TryGetValue(battle.BattleId, out BanditBattleView view)) { view.Finish(); views.Remove(battle.BattleId); }
                    if (battle.Phase != "joined" && known.Contains(battle.BattleId) && notified.Add(battle.BattleId))
                        全局变量.提示类.显示信息("战斗结束!\r\n声望+" + battle.Reward.声望 + "\r\n铜钱 + " + battle.Reward.国库铜钱 + "\r\n粮食 + " + battle.Reward.原提示粮食 + "\r\n黄金 + " + battle.Reward.原提示黄金);
                    continue;
                }
                known.Add(battle.BattleId);
                BanditBattleRules.EnsureFormations(battle);
                foreach (CombatFormation formation in battle.AttackFormations)
                {
                    var units = battle.Attackers.Where(unit => unit.ArmyId == formation.ArmyId && !unit.Retired).ToList();
                    if (units.Count == 0) continue;
                    activeArmies.Add(formation.ArmyId);
                    if (!armies.TryGetValue(formation.ArmyId, out 军情信息 march))
                    {
                        march = new 军情信息 { 战场类型 = 0, 坐标x = battle.X, 坐标y = battle.Y, 身份 = 全局变量.本机身份 };
                        armies.Add(formation.ArmyId, march); 全局变量.军情列表.Add(march);
                    }
                    march.到达时间 = formation.AvailableUtcMs / 1000;
                    march.已进入战场 = battle.Phase != "marching";
                    march.队列将领列表 = units.Select(unit => unit.General.ToObject<将领信息>()).ToList();
                }
                if (battle.Phase == "marching") continue;
                if (!views.TryGetValue(battle.BattleId, out BanditBattleView current))
                {
                    GameObject root = UnityEngine.Object.Instantiate(全局变量.山贼战斗场景pre, transform);
                    战斗系统 system = root.GetComponentInChildren<战斗系统>(true);
                    system.服务器战场ID = battle.BattleId;
                    system.战场类型 = 0; system.坐标x = battle.X; system.坐标y = battle.Y;
                    system.创建时间 = battle.StartedUtcMs / 1000; system.攻身份 = 全局变量.本机身份;
                    system.守身份 = battle.Defenders[0].General["详细信息"].Value<int>("身份");
                    current = root.AddComponent<BanditBattleView>(); current.System = system;
                    views.Add(battle.BattleId, current);
                }
                current.Apply(battle);
            }
            foreach (string id in armies.Keys.Where(id => !activeArmies.Contains(id)).ToArray())
            {
                全局变量.军情列表.Remove(armies[id]); armies.Remove(id);
            }
        }
        private void Clear()
        {
            foreach (var view in views.Values) if (view != null) view.Finish();
            foreach (var army in armies.Values) 全局变量.军情列表.Remove(army);
            views.Clear(); armies.Clear(); known.Clear(); notified.Clear(); revision = -1; role = null;
        }
    }

}
