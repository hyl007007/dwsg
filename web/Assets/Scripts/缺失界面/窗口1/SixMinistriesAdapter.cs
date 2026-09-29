using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using 玩家数据结构;
using 缺失界面.窗口4;

namespace Dwsg.Window1
{
    // 只使用当前世界中已登记的玩家/将领引用；不复制资源或制造第二份将领。
    public sealed class SixMinistriesAdapter
    {
        private readonly List<玩家数据> players;
        private readonly 玩家数据[] playerAnchors;
        private readonly 基础信息[] infoAnchors;
        private readonly int[] playerIds;
        private readonly 基础信息 actorInfo;
        private readonly 财产信息 actorMoney;
        private readonly int actorId;
        private bool invalidated;
        public readonly 玩家数据 Actor;
        public SixMinistriesAdapter()
        {
            players = 全局变量.所有玩家数据表; Actor = ExistingWorldAdapter.CurrentPlayer;
            playerAnchors = players == null ? new 玩家数据[0] : players.ToArray();
            infoAnchors = playerAnchors.Select(p => p == null ? null : p.基础信息).ToArray();
            playerIds = infoAnchors.Select(p => p == null ? -1 : p.ID).ToArray();
            actorInfo = Actor == null ? null : Actor.基础信息; actorMoney = Actor == null ? null : Actor.财产信息;
            actorId = actorInfo == null ? -1 : actorInfo.ID;
        }
        public void Invalidate() { invalidated = true; }
        public static string Key(int id) { return "player:" + id.ToString(CultureInfo.InvariantCulture); }
        public string ActorKey { get { return Actor == null || Actor.基础信息 == null ? null : Key(Actor.基础信息.ID); } }
        public bool IsBoundWorld
        {
            get
            {
                return !invalidated && players != null && ReferenceEquals(players, 全局变量.所有玩家数据表) && players.Count == playerAnchors.Length &&
                    Enumerable.Range(0, players.Count).All(i => ReferenceEquals(players[i], playerAnchors[i]) && players[i] != null &&
                        ReferenceEquals(players[i].基础信息, infoAnchors[i]) && players[i].基础信息 != null && players[i].基础信息.ID == playerIds[i]) &&
                    Actor != null && Actor.基础信息 != null &&
                    ReferenceEquals(Actor.基础信息, actorInfo) && Actor.基础信息.ID == actorId && ReferenceEquals(Actor.财产信息, actorMoney) &&
                    players.All(p => p != null && p.基础信息 != null && p.基础信息.ID >= 0) &&
                    players.Select(p => p.基础信息.ID).Distinct().Count() == players.Count && players.Count(p => ReferenceEquals(p, Actor)) == 1;
            }
        }
        public bool IsCurrent { get { return IsBoundWorld && ReferenceEquals(Actor, ExistingWorldAdapter.CurrentPlayer); } }
        public ISet<string> PlayerKeys
        { get { return IsBoundWorld ? new HashSet<string>(players.Select(p => Key(p.基础信息.ID)), StringComparer.Ordinal) : new HashSet<string>(); } }
        public bool ContainsPlayer(玩家数据 player)
        { return IsBoundWorld && player != null && playerAnchors.Any(p => ReferenceEquals(p, player)); }
        public bool ResourcesValid()
        {
            if (!IsCurrent || Actor.财产信息 == null) return false;
            var m = Actor.财产信息;
            return new[] { m.铜钱, m.粮食, m.白银, m.黄金 }.All(SixMinistriesConfig.Number);
        }
        public bool CanPay(double copper, double grain)
        { return ResourcesValid() && SixMinistriesConfig.Number(copper) && SixMinistriesConfig.Number(grain) && Actor.财产信息.铜钱 >= copper && Actor.财产信息.粮食 >= grain; }
        public bool CanReceive(double copper, double grain)
        { return ResourcesValid() && SixMinistriesConfig.Number(copper) && SixMinistriesConfig.Number(grain) &&
            Actor.财产信息.铜钱 + copper <= Reward.ResourceLimit && Actor.财产信息.粮食 + grain <= Reward.ResourceLimit; }
        internal void Pay(double copper, double grain) { Actor.财产信息.铜钱 -= copper; Actor.财产信息.粮食 -= grain; }
        internal void Receive(double copper, double grain) { Actor.财产信息.铜钱 += copper; Actor.财产信息.粮食 += grain; }
        public List<将领信息> Generals()
        { return !IsCurrent || Actor.封地信息表 == null ? new List<将领信息>() : Actor.封地信息表.Where(l => l != null && l.将领信息表 != null).SelectMany(l => l.将领信息表).Where(g => g != null).ToList(); }
        private bool GeneralContext(将领信息 general, out 封地信息 land, out string error)
        {
            land = null; error = "将领引用、归属或状态已变更";
            if (!ResourcesValid() || general == null || general.详细信息 == null || general.将领属性 == null ||
                general.将领属性.初始属性 == null || general.将领属性.成长点数 == null || general.将领配兵 == null || general.详细信息.身份 != Actor.基础信息.ID ||
                general.ID <= 0 || general.详细信息.状态 != 0 || Actor.封地信息表 == null) return false;
            var roster = Generals();
            if (roster.Count(g => ReferenceEquals(g, general)) != 1 || roster.Count(g => g.ID == general.ID) != 1) return false;
            var matches = Actor.封地信息表.Where(l => l != null && l.将领信息表 != null && l.将领信息表.Contains(general)).ToList();
            if (matches.Count != 1) return false;
            bool marching = 全局变量.军情列表 != null && 全局变量.军情列表.Any(q => q != null && q.队列将领列表 != null && q.队列将领列表.Any(g => ReferenceEquals(g, general)));
            bool aiMarching = 全局变量.Ai军情列表 != null && 全局变量.Ai军情列表.Any(q => q != null && q.队列将领列表 != null && q.队列将领列表.Any(g => ReferenceEquals(g, general)));
            if (marching || aiMarching) { error = "出征中的将领不能训练"; return false; }
            double level = general.将领属性.成长点数.等级;
            if (!SixMinistriesConfig.Number(level) || level != Math.Floor(level) || level < 1 || level >= 99 ||
                !SixMinistriesConfig.Number(general.将领属性.成长点数.总分配点数) ||
                !SixMinistriesConfig.Number(general.详细信息.经验) || general.详细信息.经验 >= general.获取当前等级升级需要经验(level) ||
                !SixMinistriesConfig.Number(general.详细信息.剩余体力)) return false;
            land = matches[0]; error = null; return true;
        }
        public MinistryResult Train(将领信息 general)
        {
            封地信息 land; string error;
            if (!GeneralContext(general, out land, out error)) return MinistryResult.Fail(error);
            // 原窗口4规则同时验证体力与资源，再扣款并调用真实将领经验方法。
            var result = 军事本地规则.修炼(Actor, land, general);
            if (!result.成功) return MinistryResult.Fail(result.说明);
            return MinistryResult.Ok(result.说明);
        }
        public static double TrainingCopper(将领信息 general) { return 军事本地规则.修炼费用(general); }
        public static double TrainingExperience(将领信息 general) { return 军事本地规则.修炼经验(general); }
        public static int TrainingStamina { get { return 军事本地规则.修炼体力; } }
    }

    // 根窗口接入同槽位的可选字符串“六部政务”：
    // 新世界：在 Window1Module.Reset(id) 后调用 Reset(id)。
    // 捕获：Export(id)；恢复：先 TryDecode(id,json,out candidate,out error)，
    // 所有其他模块恢复成功后才 AttachValidated(candidate,out error)。
    // Decode不发布、不结算、不查旧全局军情，不发UI事件。json==null初始化该世界空玩家状态。
    // 外层存档事务仍由根窗口负责回滚世界与各模块快照，不另写文件/PlayerPrefs。
    public static class SixMinistriesModule
    {
        private static SixMinistriesService service;
        private static SixMinistriesAdapter adapter;
        private static readonly SixMinistriesConfig configuration = new SixMinistriesConfig();
        public static SixMinistriesService Service
        {
            get
            {
                string id = Window1Module.CurrentWorldId;
                if (ExistingWorldAdapter.CurrentPlayer == null || string.IsNullOrEmpty(id)) return null;
                if (service == null || adapter == null || !adapter.IsBoundWorld || service.WorldId != id) Reset(id);
                else if (!adapter.IsCurrent)
                {
                    var nextAdapter = new SixMinistriesAdapter();
                    Publish(new SixMinistriesService(service.Snapshot(), configuration, nextAdapter, TIME.getTime), nextAdapter);
                }
                return service;
            }
        }
        private static void Publish(SixMinistriesService next, SixMinistriesAdapter nextAdapter)
        { if (adapter != null) adapter.Invalidate(); adapter = nextAdapter; service = next; }
        public static void Reset(string worldId)
        {
            var nextAdapter = new SixMinistriesAdapter();
            var next = new SixMinistriesService(SixMinistriesState.Empty(worldId, configuration), configuration, nextAdapter, TIME.getTime);
            Publish(next, nextAdapter);
        }
        public static string Export(string expectedWorld)
        {
            // 不通过UI用的Service getter，不重置/结算/生成NPC/刷新其他模块。
            if (service == null || adapter == null || !adapter.IsBoundWorld || service.WorldId != expectedWorld)
                throw new InvalidOperationException("六部与当前世界标识不一致");
            return JsonConvert.SerializeObject(service.Snapshot());
        }
        public static bool TryDecode(string expectedWorld, string json, out SixMinistriesImport candidate, out string error)
        {
            candidate = null;
            error = "六部政务存档无效";
            if (!SixMinistriesConfig.Text(expectedWorld, 160) || ExistingWorldAdapter.CurrentPlayer == null || (json != null && json.Length > 4000000)) return false;
            try
            {
                var state = string.IsNullOrEmpty(json) ? SixMinistriesState.Empty(expectedWorld, configuration) :
                    JsonConvert.DeserializeObject<SixMinistriesState>(json, new JsonSerializerSettings { MaxDepth = 16, TypeNameHandling = TypeNameHandling.None });
                var nextAdapter = new SixMinistriesAdapter();
                if (state == null || !nextAdapter.IsCurrent || !state.Validate(expectedWorld, configuration, TIME.getTime(), nextAdapter.PlayerKeys, out error)) return false;
                var next = new SixMinistriesService(state, configuration, nextAdapter, TIME.getTime);
                candidate = new SixMinistriesImport(next, nextAdapter); error = null; return true;
            }
            catch (JsonException) { return false; }
            catch (ArgumentException) { return false; }
        }
        public static bool AttachValidated(SixMinistriesImport candidate, out string error)
        {
            error = "六部恢复候选或目标世界已变更";
            if (candidate == null || candidate.Attached || !candidate.Adapter.IsCurrent) return false;
            try { candidate.Service.Snapshot(); }
            catch (InvalidOperationException) { return false; }
            Publish(candidate.Service, candidate.Adapter); candidate.Attached = true; error = null; return true;
        }
        // 根窗口在 玩家数据.获取指定状态加成 的“攻击”/“防御”处 += BuffPercent(this,名字)。
        // 单位为百分数：有效buff返回5，叠加原道具10得到15；原将领计算仍统一除100。
        // 纯读取，不调用Service getter；必须是已绑定世界的真实玩家对象，不能仅以同ID冒领。
        public static double BuffPercent(玩家数据 player, string effect)
        { return BuffPercent(service == null ? null : service.WorldId, player, effect, TIME.getTime()); }
        public static double BuffPercent(string worldId, 玩家数据 player, string effect, long now)
        {
            if (service == null || adapter == null || service.WorldId != worldId || !adapter.ContainsPlayer(player)) return 0;
            return service.BuffPercent(player.基础信息.ID, effect, now);
        }
    }
    public sealed class SixMinistriesImport
    {
        internal readonly SixMinistriesService Service;
        internal readonly SixMinistriesAdapter Adapter;
        internal bool Attached;
        internal SixMinistriesImport(SixMinistriesService service, SixMinistriesAdapter adapter) { Service = service; Adapter = adapter; }
    }
}
