using System;
using System.Collections.Generic;
using System.Linq;
using 玩家数据结构;

namespace Dwsg.Window1
{
    public sealed class MinistryResult
    {
        public bool Success;
        public string Message;
        public long JobId;
        public static MinistryResult Ok(string text, long jobId = 0) { return new MinistryResult { Success = true, Message = text, JobId = jobId }; }
        public static MinistryResult Fail(string text) { return new MinistryResult { Message = text }; }
    }

    // 模型无Unity组件/协程。时间由调用方提供；只有显式领取才发放奖励。
    // 每次事务先验证世界、权限、对象引用、数值和结果上限，再同步提交。
    public sealed class SixMinistriesService
    {
        private readonly SixMinistriesState state;
        private readonly SixMinistriesConfig rules;
        private readonly SixMinistriesAdapter world;
        private readonly Func<long> clock;
        public string WorldId { get { return state.WorldId; } }
        public string ActorKey { get { return world.ActorKey; } }
        public long Now { get { return clock(); } }
        public SixMinistriesConfig Rules { get { return rules.Copy(); } }
        public SixMinistriesAdapter World { get { return world; } }
        public IReadOnlyList<MinistryOfficer> Roster { get { return state.Officers.Where(o => o.Owner == ActorKey).OrderBy(o => o.Id).ToArray(); } }
        public IReadOnlyList<MinistryOfficer> NpcTargets { get { return state.Officers.Where(o => rules.NpcOwner(o.Owner)).OrderBy(o => o.Id).ToArray(); } }
        public IReadOnlyList<MinistryJob> PendingJobs { get { return state.Jobs.Where(j => j.Owner == ActorKey).OrderBy(j => j.Id).ToArray(); } }
        public SixMinistriesService(SixMinistriesState source, SixMinistriesConfig configuration, SixMinistriesAdapter adapter, Func<long> time)
        {
            if (source == null || configuration == null || adapter == null || time == null) throw new ArgumentException("六部模型参数缺失");
            rules = configuration.Copy(); world = adapter; clock = time;
            string error;
            if (!world.IsCurrent || !source.Validate(source.WorldId, rules, clock(), world.PlayerKeys, out error))
                throw new ArgumentException("六部模型或当前世界无效");
            state = source.Copy();
        }
        public SixMinistriesState Snapshot()
        {
            string error;
            if (!world.IsBoundWorld || !state.Validate(state.WorldId, rules, clock(), world.PlayerKeys, out error))
                throw new InvalidOperationException("六部状态或当前世界已变更");
            return state.Copy();
        }
        private bool Guard(out long now, out string error)
        {
            now = clock(); error = "当前世界或玩家引用已变更，请重新打开六部";
            if (!world.IsCurrent) return false;
            if (!state.Validate(state.WorldId, rules, now, world.PlayerKeys, out error)) return false;
            if (!world.ResourcesValid()) { error = "资源数值无效，本次未扣款或发奖"; return false; }
            error = null; return true;
        }
        private bool Owned(MinistryOfficer officer)
        { return officer != null && state.Officers.Any(o => ReferenceEquals(o, officer)) && officer.Owner == ActorKey; }
        public bool IsBusy(MinistryOfficer officer)
        { return officer != null && state.Jobs.Any(j => j.OfficerId == officer.Id || j.TargetId == officer.Id); }
        private bool Pending(MinistryJobKind kind, int slot = -1, int candidate = -1)
        { return state.Jobs.Any(j => j.Owner == ActorKey && j.Kind == kind && j.Slot == slot && j.Candidate == candidate); }
        private int ReservedSlots(long except = 0)
        { return state.Jobs.Count(j => j.Owner == ActorKey && j.Id != except && (j.Kind == MinistryJobKind.Recruit ||
            j.Kind == MinistryJobKind.Persuasion && j.ExpectedProgress + rules.PersuasionStep >= rules.PersuasionGoal)); }
        private bool HasIdentity(int count = 1) { return state.NextId <= rules.MaxIdentity - 1 - count; }
        private MinistryResult Start(MinistryJobKind kind, long now, double copper, double grain,
            int slot = -1, int candidate = -1, MinistryOfficer officer = null, MinistryOfficer target = null)
        {
            int duration = rules.Duration(kind);
            if (!HasIdentity() || state.Jobs.Count >= rules.MaxJobs || duration < 1 || now > rules.MaxTimestamp - duration)
                return MinistryResult.Fail("待办数量、标识或计时达到上限");
            if (!world.CanPay(copper, grain)) return MinistryResult.Fail("资源不足，需要铜钱" + copper + "、粮食" + grain);
            var job = new MinistryJob { Id = state.NextId, Owner = ActorKey, Kind = kind, StartedAt = now,
                ReadyAt = now + duration, Slot = slot, Candidate = candidate,
                OfficerId = officer == null ? 0 : officer.Id, TargetId = target == null ? 0 : target.Id,
                TargetOwner = target == null ? null : target.Owner, ExpectedProgress = target == null ? 0 : target.Persuasion };
            world.Pay(copper, grain); state.NextId++; state.LastTimestamp = now; state.Jobs.Add(job);
            return MinistryResult.Ok("已开始，" + duration + "秒后可领取", job.Id);
        }
        public MinistryResult StartRecruit(int candidate)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            if (candidate < 0 || candidate >= rules.Candidates.Length) return MinistryResult.Fail("候选文官不存在");
            if (Pending(MinistryJobKind.Recruit, candidate: candidate)) return MinistryResult.Fail("该候选正在招募，请勿重复支付");
            if (Roster.Count + ReservedSlots() >= rules.RosterLimit || state.Officers.Count >= rules.MaxOfficers)
                return MinistryResult.Fail("文官名册已满，待领取招募也占用名额");
            return Start(MinistryJobKind.Recruit, now, rules.RecruitCopper, 0, candidate: candidate);
        }
        public MinistryResult Dismiss(MinistryOfficer officer)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            if (!Owned(officer)) return MinistryResult.Fail("文官引用或归属已变更");
            if (IsBusy(officer)) return MinistryResult.Fail("文官正在执行政务，不能解雇");
            state.Officers.Remove(officer); state.LastTimestamp = now;
            return MinistryResult.Ok("已解雇文官" + officer.Name);
        }
        public MinistryResult StartPlant(int plot)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            if (plot < 0 || plot >= rules.PlotCount) return MinistryResult.Fail("种植地块不存在");
            if (Pending(MinistryJobKind.Plant, slot: plot)) return MinistryResult.Fail("此地块正在种植，请先领取");
            return Start(MinistryJobKind.Plant, now, rules.PlantCopper, 0, slot: plot);
        }
        public MinistryResult StartRelief(MinistryOfficer officer) { return StartCivil(officer, MinistryJobKind.Relief); }
        public MinistryResult StartStudy(MinistryOfficer officer) { return StartCivil(officer, MinistryJobKind.Study); }
        private MinistryResult StartCivil(MinistryOfficer officer, MinistryJobKind kind)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            if (!Owned(officer)) return MinistryResult.Fail("文官引用或归属已变更");
            if (IsBusy(officer)) return MinistryResult.Fail("文官正在执行其他政务");
            double exp = kind == MinistryJobKind.Relief ? rules.ReliefExperience : rules.StudyExperience;
            if (officer.Experience + exp > rules.OfficerExperienceLimit) return MinistryResult.Fail("文官经验已达到上限");
            return Start(kind, now, kind == MinistryJobKind.Relief ? rules.ReliefCopper : rules.StudyCopper,
                kind == MinistryJobKind.Relief ? rules.ReliefGrain : 0, officer: officer);
        }
        public MinistryResult StartMilitary(bool attack)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            var kind = attack ? MinistryJobKind.MilitaryAttack : MinistryJobKind.MilitaryDefense;
            if (Pending(kind) || state.Buffs.Any(b => b.Owner == ActorKey && b.Kind == kind && b.ExpiresAt > now))
                return MinistryResult.Fail("同类军务已在进行或生效，不能叠加");
            if (state.Buffs.Count >= rules.MaxBuffs && !state.Buffs.Any(b => b.Owner == ActorKey && b.Kind == kind)) return MinistryResult.Fail("军务记录已满");
            return Start(kind, now, rules.MilitaryCopper, rules.MilitaryGrain);
        }
        public MinistryResult TrainGeneral(将领信息 general)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            var result = world.Train(general);
            if (result.Success) state.LastTimestamp = now;
            return result;
        }
        public MinistryResult StartPersuasion(MinistryOfficer target)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            if (target == null || !state.Officers.Any(o => ReferenceEquals(o, target)) || !rules.NpcOwner(target.Owner) ||
                !rules.Npcs.Any(n => n.Key == target.Source) || target.Persuasion >= rules.PersuasionGoal)
                return MinistryResult.Fail("请选择当前世界中仍归属本地NPC官署的文官");
            if (IsBusy(target) || Pending(MinistryJobKind.Persuasion)) return MinistryResult.Fail("策反待办或目标已被占用");
            if (Roster.Count + ReservedSlots() >= rules.RosterLimit)
                return MinistryResult.Fail("文官名册已满，不能开始策反");
            return Start(MinistryJobKind.Persuasion, now, rules.PersuasionCopper, 0, target: target);
        }
        public MinistryResult Claim(long jobId)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            var job = state.Jobs.FirstOrDefault(j => j.Id == jobId && j.Owner == ActorKey);
            if (job == null) return MinistryResult.Fail("待办不存在或已经领取");
            if (now < job.ReadyAt) return MinistryResult.Fail("尚未到期，剩余" + (job.ReadyAt - now) + "秒");
            var officer = state.Officers.FirstOrDefault(o => o.Id == job.OfficerId);
            var target = state.Officers.FirstOrDefault(o => o.Id == job.TargetId);
            double copper = 0, grain = 0, experience = 0;
            if (job.Kind == MinistryJobKind.Recruit)
            {
                if (Roster.Count + ReservedSlots(jobId) >= rules.RosterLimit || state.Officers.Count >= rules.MaxOfficers || !HasIdentity()) return MinistryResult.Fail("文官名册或标识已满，请先整理");
            }
            else if (job.Kind == MinistryJobKind.Plant) grain = rules.PlantGrain;
            else if (job.Kind == MinistryJobKind.Relief || job.Kind == MinistryJobKind.Study)
            {
                if (!Owned(officer)) return MinistryResult.Fail("执行文官的归属或登记已变更，未发放奖励");
                experience = job.Kind == MinistryJobKind.Relief ? rules.ReliefExperience : rules.StudyExperience;
                if (officer.Experience + experience > rules.OfficerExperienceLimit) return MinistryResult.Fail("文官经验上限不足，本次未发放");
                if (job.Kind == MinistryJobKind.Relief) copper = rules.ReliefRewardCopper;
            }
            else if (job.Kind == MinistryJobKind.Persuasion)
            {
                if (target == null || target.Owner != job.TargetOwner || target.Persuasion != job.ExpectedProgress || !rules.NpcOwner(target.Owner))
                    return MinistryResult.Fail("策反目标归属或进度已变更，本次未转移");
                if (target.Persuasion + rules.PersuasionStep >= rules.PersuasionGoal &&
                    Roster.Count + ReservedSlots(jobId) >= rules.RosterLimit)
                    return MinistryResult.Fail("文官名册已满，尚未转移目标");
            }
            else
            {
                if (now > rules.MaxTimestamp - rules.BuffSeconds || state.Buffs.Any(b => b.Owner == ActorKey && b.Kind == job.Kind && b.ExpiresAt > now))
                    return MinistryResult.Fail("同类军务已生效或时间无效，本次未延长");
                if (state.Buffs.Count >= rules.MaxBuffs && !state.Buffs.Any(b => b.Owner == ActorKey && b.Kind == job.Kind)) return MinistryResult.Fail("军务记录已满");
            }
            if (!world.CanReceive(copper, grain)) return MinistryResult.Fail("资源无效或发奖后超过20亿上限，本次未发放");
            // 上述校验均完成。以下仅同步写入已验证对象，无回调/第二次随机生成。
            world.Receive(copper, grain);
            string message = "政务领取完成";
            if (job.Kind == MinistryJobKind.Recruit)
            {
                var recruited = new MinistryOfficer { Id = state.NextId++, Name = rules.Candidates[job.Candidate], Owner = ActorKey, Source = "candidate:" + job.Candidate };
                state.Officers.Add(recruited); message = "已招募" + recruited.Name + "，文官ID " + recruited.Id;
            }
            else if (job.Kind == MinistryJobKind.Plant) message = "采摘完成，粮食+" + grain;
            else if (experience > 0) { officer.Experience += experience; message = "政务完成，文官经验+" + experience + (copper > 0 ? "、铜钱+" + copper : ""); }
            else if (job.Kind == MinistryJobKind.Persuasion)
            {
                target.Persuasion += rules.PersuasionStep;
                if (target.Persuasion >= rules.PersuasionGoal) { target.Owner = ActorKey; message = "策反完成，原文官ID " + target.Id + " 已归属本君主"; }
                else message = "策反进度 " + target.Persuasion + "/" + rules.PersuasionGoal;
            }
            else
            {
                state.Buffs.RemoveAll(b => b.Owner == ActorKey && b.Kind == job.Kind);
                state.Buffs.Add(new MinistryBuff { Owner = ActorKey, Kind = job.Kind, StartedAt = now, ExpiresAt = now + rules.BuffSeconds, Bonus = rules.MilitaryBonus });
                message = (job.Kind == MinistryJobKind.MilitaryAttack ? "攻击" : "防御") + "军务生效，+" + (rules.MilitaryBonus * 100) + "%，持续" + (rules.BuffSeconds / 60) + "分钟";
            }
            state.Jobs.Remove(job); state.LastTimestamp = now;
            return MinistryResult.Ok(message, jobId);
        }
        public MinistryResult Cancel(long jobId)
        {
            long now; string error;
            if (!Guard(out now, out error)) return MinistryResult.Fail(error);
            var job = state.Jobs.FirstOrDefault(j => j.Id == jobId && j.Owner == ActorKey);
            if (job == null) return MinistryResult.Fail("待办不存在或已结束");
            // 本地规则：撤销仅释放待办/文官，不退款，也不发放进度或奖励。
            state.Jobs.Remove(job); state.LastTimestamp = now;
            return MinistryResult.Ok("已撤销待办，费用不退还");
        }
        public double CombatCoefficient(int playerId, bool attack, long now)
        { return 1 + BattleBonus(playerId, attack, now); }
        public double BuffPercent(int playerId, string effect, long now)
        { return effect == "攻击" || effect == "防御" ? BattleBonus(playerId, effect == "攻击", now) * 100 : 0; }
        private double BattleBonus(int playerId, bool attack, long now)
        {
            string error;
            string owner = SixMinistriesAdapter.Key(playerId);
            if (!world.IsBoundWorld || !world.PlayerKeys.Contains(owner) || !state.Validate(state.WorldId, rules, now, world.PlayerKeys, out error)) return 0;
            var kind = attack ? MinistryJobKind.MilitaryAttack : MinistryJobKind.MilitaryDefense;
            var buff = state.Buffs.FirstOrDefault(b => b.Owner == owner && b.Kind == kind && b.StartedAt <= now && now < b.ExpiresAt);
            return buff == null ? 0 : buff.Bonus;
        }
    }
}
