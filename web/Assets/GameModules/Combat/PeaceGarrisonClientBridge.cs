using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;
using UnityEngine;
using 缺失界面.窗口4;
using 玩家数据结构;

namespace Dwsg.Combat
{
    public sealed class PeaceGarrisonClientBridge : MonoBehaviour
    {
        private readonly Dictionary<string, 驻防军情信息> tasks = new Dictionary<string, 驻防军情信息>();
        private WorldSnapshot pending;
        private long revision = -1;
        private string role;
        private void OnEnable() { GameNetwork.SnapshotApplied += Apply; Apply(GameNetwork.CurrentSnapshot); }
        private void OnDisable() { GameNetwork.SnapshotApplied -= Apply; Clear(); }
        public void Apply(WorldSnapshot snapshot) { pending = snapshot; }
        private void Update()
        {
            if (!GameNetwork.Enabled || !GameNetwork.Connected || !GameNetwork.HasRole) { if (role != null) Clear(); return; }
            if (pending == null || 全局变量.本机身份 < 0 || 全局变量.本机身份 >= 全局变量.所有玩家数据表.Count) return;
            string identity = pending.WorldId + ":" + pending.PlayerId;
            if (role != identity) { Clear(); role = identity; }
            if (revision == pending.WorldRevision) return; revision = pending.WorldRevision;
            var player = 全局变量.所有玩家数据表[全局变量.本机身份]; var active = new HashSet<string>();
            foreach (JObject row in pending.PrivatePlayer["和平驻防运行"] as JArray ?? new JArray())
            {
                string id = row.Value<string>("ArmyId"); active.Add(id);
                驻防军情信息 task;
                if (!tasks.TryGetValue(id, out task)) { task = new 驻防军情信息 { 服务器任务ID = id }; tasks[id] = task; 全局变量.军情列表.Add(task); }
                string phase = row.Value<string>("Phase");
                task.服务器战场ID = row.Value<string>("BattleId");
                task.阶段 = phase == "returning" ? 驻防任务阶段.撤回 : phase == "fighting" ? 驻防任务阶段.参战 : phase == "stationed" ? 驻防任务阶段.驻守 : 驻防任务阶段.前往;
                task.任务用途 = phase == "returning" ? 军事任务用途.驻防撤回 : 军事任务用途.和平驻防;
                task.所属玩家 = 全局变量.本机身份; task.出发封地ID = row.Value<int>("SourceFiefId"); task.出发坐标x = row.Value<double>("FromX"); task.出发坐标y = row.Value<double>("FromY");
                task.坐标x = row.Value<int>("ToX"); task.坐标y = row.Value<int>("ToY"); task.驻防坐标x = row.Value<int>("X"); task.驻防坐标y = row.Value<int>("Y");
                task.出发时间 = row.Value<long>("StartedUtcMs") / 1000; task.到达时间 = row.Value<long>("ArrivalUtcMs") / 1000; task.目标国家 = row.Value<string>("Nation");
                task.目标城主 = Dwsg.Administration.AdministrationClient.PlayerIndex(row.Value<string>("Owner")); task.已进入战场 = phase == "fighting";
                var generalIds = new HashSet<string>(((JArray)row["GeneralIds"]).Values<string>());
                task.队列将领列表 = player.封地信息表.SelectMany(f => f.将领信息表).Where(g => generalIds.Contains(CombatClient.GeneralId(g.ID))).ToList();
            }
            foreach (string old in tasks.Keys.Where(id => !active.Contains(id)).ToArray()) { 全局变量.军情列表.Remove(tasks[old]); tasks.Remove(old); }
        }
        private void Clear()
        {
            foreach (var task in tasks.Values) 全局变量.军情列表.Remove(task);
            tasks.Clear(); revision = -1; role = null;
        }
        public static void Withdraw(驻防军情信息 task, Action<军事结果> done)
        {
            if (task == null || string.IsNullOrEmpty(task.服务器任务ID)) { done(军事结果.拒绝(军事错误.状态冲突, "驻防任务尚未同步。")); return; }
            GameNetwork.SendCommand("combat.city.garrison.withdraw", new JObject { ["armyId"] = task.服务器任务ID }, result =>
                done(result.Code == GameCodes.Ok ? 军事结果.通过(result.Message) : 军事结果.拒绝(军事错误.状态冲突, result.Message)));
        }
    }
}
