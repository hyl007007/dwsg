using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

namespace Dwsg.Combat
{
    public sealed class BanditBattleView : MonoBehaviour
    {
        public 战斗系统 System;
        private BanditBattle pending;
        private BanditBattle rendered;
        private long frame = -1;
        private readonly Dictionary<string, 将领功能> models = new Dictionary<string, 将领功能>();
        private readonly HashSet<string> dead = new HashSet<string>();
        private readonly Dictionary<string, Transform> formations = new Dictionary<string, Transform>();
        private Transform defending, attackRenderer, defenseRenderer;
        public void Apply(BanditBattle battle) { pending = battle; }
        private void LateUpdate()
        {
            foreach (将领功能 model in models.Values)
                if (model != null && (model.transform.parent.parent == System.攻方坑位对象.transform || model.transform.parent.parent == System.守方坑位对象.transform))
                    model.transform.localPosition = Vector2.MoveTowards(model.transform.localPosition, new Vector2(0f, 3.15f), 35f * Time.deltaTime);
            if (pending == null || System.攻方坑位对象.transform.childCount < 15 || ReferenceEquals(rendered, pending)) return;
            if (attackRenderer == null)
            {
                var renderers = System.GetComponentsInChildren<编队将领渲染>(true);
                attackRenderer = renderers.Single(renderer => renderer.设置攻守方 == 0).transform;
                defenseRenderer = renderers.Single(renderer => renderer.设置攻守方 == 1).transform;
                if (pending.Kind != "city") defending = UnityEngine.Object.Instantiate(全局变量.编队对象pre, defenseRenderer).transform;
            }
            BanditBattleRules.EnsureFormations(pending);
            foreach (CombatFormation formation in pending.AttackFormations)
            {
                if (!formations.TryGetValue(formation.ArmyId, out Transform model))
                {
                    model = UnityEngine.Object.Instantiate(全局变量.编队对象pre, attackRenderer).transform;
                    formations.Add(formation.ArmyId, model);
                }
                model.localPosition = new Vector2(formation.PositionX, 0f);
            }
            long utc = pending.StartedUtcMs + pending.Frame * 1000 / 60;
            if (pending.Kind == "city")
            {
                foreach (CombatFormation formation in pending.DefenseFormations.Where(group => group.AvailableUtcMs <= utc))
                {
                    if (!formations.TryGetValue(formation.ArmyId, out Transform model))
                    {
                        model = UnityEngine.Object.Instantiate(全局变量.编队对象pre, defenseRenderer).transform;
                        formations.Add(formation.ArmyId, model);
                    }
                    model.localPosition = new Vector2(formation.PositionX, 0f);
                }
                System.城墙信息显示.text = pending.Wall.ToString();
                float ratio = pending.WallMaximum > 0 ? (float)(pending.Wall / pending.WallMaximum) : 0;
                System.城墙血条对象.localPosition = new Vector2(3f * ratio, -0.15f);
            }
            else defending.localPosition = new Vector2(pending.DefenseFormationX, 0f);
            System.攻方兵力 = pending.Attackers.Where(unit => !unit.Retired).Sum(unit => unit.Remaining);
            System.守方兵力 = BanditBattleRules.DefendingForce(pending, utc);
            foreach (CombatUnit unit in pending.Attackers.Concat(pending.Defenders))
            {
                if (dead.Contains(unit.CombatId)) continue;
                if (pending.Kind == "city" && unit.Side == 1 && !formations.ContainsKey(unit.ArmyId)) continue;
                if (unit.Retired)
                {
                    if (models.TryGetValue(unit.CombatId, out 将领功能 retired))
                    { UnityEngine.Object.Destroy(retired.gameObject); models.Remove(unit.CombatId); }
                    continue;
                }
                if (!models.TryGetValue(unit.CombatId, out 将领功能 model))
                {
                    GameObject root = UnityEngine.Object.Instantiate(全局变量.将领对象pre, unit.Side == 0 || pending.Kind == "city" ? formations[unit.ArmyId] : defending);
                    root.transform.localPosition = Vector3.zero;
                    model = root.GetComponent<将领功能>();
                    model.本将领信息 = unit.General.ToObject<将领信息>();
                    model.本将领信息.将领配兵.数量 = unit.OriginalQuantity;
                    model.开始渲染将领(); model.设置将领模型图层(3 + models.Count % 5);
                    if ((unit.Side == 0 ? pending.Attackers.Where(entry => entry.ArmyId == unit.ArmyId)
                        : pending.Kind == "city" ? pending.Defenders.Where(entry => entry.ArmyId == unit.ArmyId) : pending.Defenders).Min(entry => entry.Troop.Value<float>("移动速度")) < 15f) model.设置走动状态();
                    models.Add(unit.CombatId, model);
                }
                if (unit.Slot >= 0)
                {
                    Transform pit = (unit.Side == 0 ? System.攻方坑位对象 : System.守方坑位对象).transform.GetChild(unit.Slot);
                    if (model.transform.parent != pit) { model.transform.SetParent(pit); model.设置将领模型图层(2 + unit.Slot % 5); model.设置等待状态(); }
                    model.将领显示星星(); model.将领显示血条();
                }
                model.应用服务器演出(unit);
                if (unit.Remaining <= 0) { dead.Add(unit.CombatId); UnityEngine.Object.Destroy(model.gameObject, 0.5f); }
            }
            foreach (CombatHit hit in pending.LastHits.Where(hit => hit.Frame > frame))
            {
                if (models.TryGetValue(hit.AttackerId, out 将领功能 actor) && actor != null) actor.服务器攻击演出(hit.Pierce);
                if (models.TryGetValue(hit.DefenderId, out 将领功能 target) && target != null) target.服务器伤害演出(hit.Damage, hit.Block, hit.Dodge);
                if (hit.DefenderId == "wall")
                {
                    GameObject damage = System.伤害显示缓存表.FirstOrDefault(item => !item.activeSelf);
                    if (damage != null)
                    {
                        damage.SetActive(true); damage.transform.position = System.城墙血条对象.position;
                        damage.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "-" + hit.Damage;
                    }
                }
            }
            frame = pending.Frame;
            rendered = pending;
        }
        public void Finish()
        {
            if (System != null && System.正在观战 && 全局变量.战斗界面UI对象 != null)
                全局变量.战斗界面UI对象.GetComponent<战斗界面UI脚本>().返回主界面();
            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
