using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dwsg.Window1
{
    // 本模块的全部数值是本地规则。进度来自当前世界，网络邮件由未来适配器接入。
    public enum GoalKind { Growth, Daily, Achievement }
    public enum WorldMetric { LordLevel, Buildings, BuildingLevels, HallLevel, Generals, GeneralLevels, GeneralLevel, Merit, Troops, Technology }

    [Serializable]
    public sealed class WorldProgress
    {
        public double LordLevel, Buildings, BuildingLevels, HallLevel, Generals, GeneralLevels, GeneralLevel, Merit, Troops, Technology;
        public double Value(WorldMetric metric)
        {
            switch (metric)
            {
                case WorldMetric.LordLevel: return LordLevel;
                case WorldMetric.Buildings: return Buildings;
                case WorldMetric.BuildingLevels: return BuildingLevels;
                case WorldMetric.HallLevel: return HallLevel;
                case WorldMetric.Generals: return Generals;
                case WorldMetric.GeneralLevels: return GeneralLevels;
                case WorldMetric.GeneralLevel: return GeneralLevel;
                case WorldMetric.Merit: return Merit;
                case WorldMetric.Troops: return Troops;
                default: return Technology;
            }
        }
        public bool IsValid()
        {
            return Enum.GetValues(typeof(WorldMetric)).Cast<WorldMetric>().All(m => Number(Value(m)));
        }
        public WorldProgress Copy() { return (WorldProgress)MemberwiseClone(); }
        public static bool Number(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= Reward.ResourceLimit; }
    }

    [Serializable]
    public sealed class RewardItem
    {
        public string Name;
        public int Count;
        public RewardItem() { }
        public RewardItem(string name, int count) { Name = name; Count = count; }
    }

    [Serializable]
    public sealed class Reward
    {
        // 旧世界没有统一仓储上限；此上限防止非有限值和过大金额入账，不截断奖励。
        public const double ResourceLimit = 2000000000;
        public double Copper, Grain, Silver, Gold;
        public List<RewardItem> Items = new List<RewardItem>();
        public bool HasAnything { get { return Copper > 0 || Grain > 0 || Silver > 0 || Gold > 0 || (Items != null && Items.Count > 0); } }
        public bool IsValid()
        {
            double[] values = { Copper, Grain, Silver, Gold };
            return values.All(v => WorldProgress.Number(v) && v == Math.Floor(v)) && Items != null && Items.Count <= 32 &&
                Items.All(i => i != null && !string.IsNullOrWhiteSpace(i.Name) && i.Name.Length <= 80 && i.Count > 0 && i.Count <= 9999);
        }
        public Reward Copy()
        {
            return new Reward { Copper = Copper, Grain = Grain, Silver = Silver, Gold = Gold,
                Items = Items.Select(i => new RewardItem(i.Name, i.Count)).ToList() };
        }
        public string Preview()
        {
            var lines = new List<string>();
            if (Copper > 0) lines.Add("铜钱 " + Copper.ToString("N0", CultureInfo.InvariantCulture));
            if (Grain > 0) lines.Add("粮食 " + Grain.ToString("N0", CultureInfo.InvariantCulture));
            if (Silver > 0) lines.Add("白银 " + Silver.ToString("N0", CultureInfo.InvariantCulture));
            if (Gold > 0) lines.Add("黄金 " + Gold.ToString("N0", CultureInfo.InvariantCulture));
            if (Items != null) lines.AddRange(Items.Select(i => i.Name + " ×" + i.Count));
            return lines.Count == 0 ? "无附件" : string.Join("   ·   ", lines.ToArray());
        }
    }

    public sealed class GoalDefinition
    {
        public readonly string Id, Title, Description;
        public readonly GoalKind Kind;
        public readonly WorldMetric Metric;
        public readonly double Target;
        private readonly Reward reward;
        public Reward Reward { get { return reward.Copy(); } }
        public GoalDefinition(string id, string title, string description, GoalKind kind, WorldMetric metric, double target, Reward reward)
        { Id = id; Title = title; Description = description; Kind = kind; Metric = metric; Target = target; this.reward = reward; }
    }

    public static class GoalCatalog
    {
        private static Reward R(double copper, double grain, string item = null)
        { var r = new Reward { Copper = copper, Grain = grain }; if (item != null) r.Items.Add(new RewardItem(item, 1)); return r; }
        private static readonly GoalDefinition[] goals =
        {
            new GoalDefinition("growth.land", "立足封地", "拥有 1 座已建成的建筑。大厅计入建筑总数。", GoalKind.Growth, WorldMetric.Buildings, 1, R(1000, 1000)),
            new GoalDefinition("growth.build", "百业初兴", "拥有 4 座已建成的建筑。空地不计入。", GoalKind.Growth, WorldMetric.Buildings, 4, R(2000, 1000)),
            new GoalDefinition("growth.general", "招贤纳士", "麾下拥有 3 名将领；俘虏与驻防他人的将领不计入。", GoalKind.Growth, WorldMetric.Generals, 3, R(2000, 2000, "活血丹")),
            new GoalDefinition("growth.level", "君主成长", "君主达到 10 级。", GoalKind.Growth, WorldMetric.LordLevel, 10, R(3000, 3000)),
            new GoalDefinition("growth.study", "经世之学", "个人科技各项等级合计达到 10。", GoalKind.Growth, WorldMetric.Technology, 10, R(3000, 2000)),
            new GoalDefinition("growth.army", "整军备战", "闲兵与将领配兵合计达到 1000；伤兵不计入。", GoalKind.Growth, WorldMetric.Troops, 1000, R(4000, 4000)),
            new GoalDefinition("growth.hero", "将才初成", "任意麾下将领达到 20 级。", GoalKind.Growth, WorldMetric.GeneralLevel, 20, R(4000, 3000, "活血丹")),
            new GoalDefinition("growth.merit", "沙场扬名", "累计战功达到 100。", GoalKind.Growth, WorldMetric.Merit, 100, R(5000, 5000)),
            new GoalDefinition("daily.build", "今日营建", "今日基线后，已建成建筑等级合计净增加 1。", GoalKind.Daily, WorldMetric.BuildingLevels, 1, R(1000, 1000)),
            new GoalDefinition("daily.study", "今日治学", "今日基线后，个人科技等级合计净增加 1。", GoalKind.Daily, WorldMetric.Technology, 1, R(1000, 500)),
            new GoalDefinition("daily.train", "今日练将", "今日基线后，麾下将领等级合计净增加 1。招募也可能增加此值。", GoalKind.Daily, WorldMetric.GeneralLevels, 1, R(500, 1000)),
            new GoalDefinition("daily.merit", "今日战功", "今日基线后，战功净增加 10。", GoalKind.Daily, WorldMetric.Merit, 10, R(1500, 1500)),
            new GoalDefinition("achievement.lord10", "初露锋芒", "君主达到 10 级。", GoalKind.Achievement, WorldMetric.LordLevel, 10, R(2000, 2000)),
            new GoalDefinition("achievement.lord30", "一方雄主", "君主达到 30 级。", GoalKind.Achievement, WorldMetric.LordLevel, 30, R(6000, 6000)),
            new GoalDefinition("achievement.lord60", "王者之路", "君主达到 60 级。", GoalKind.Achievement, WorldMetric.LordLevel, 60, R(12000, 12000)),
            new GoalDefinition("achievement.build8", "百业俱兴", "拥有 8 座已建成建筑。", GoalKind.Achievement, WorldMetric.Buildings, 8, R(4000, 4000)),
            new GoalDefinition("achievement.build20", "营建大家", "所有已建成建筑等级合计达到 50。", GoalKind.Achievement, WorldMetric.BuildingLevels, 50, R(6000, 6000)),
            new GoalDefinition("achievement.general5", "群英聚首", "拥有 5 名将领。", GoalKind.Achievement, WorldMetric.Generals, 5, R(4000, 4000, "活血丹")),
            new GoalDefinition("achievement.general20", "将星云集", "拥有 20 名将领。", GoalKind.Achievement, WorldMetric.Generals, 20, R(10000, 10000)),
            new GoalDefinition("achievement.hero50", "百战名将", "任意将领达到 50 级。", GoalKind.Achievement, WorldMetric.GeneralLevel, 50, R(8000, 8000, "活血丹")),
            new GoalDefinition("achievement.merit500", "骁勇善战", "累计战功达到 500。名称参考公开玩法；此条件为本地规则。", GoalKind.Achievement, WorldMetric.Merit, 500, R(8000, 8000)),
            new GoalDefinition("achievement.army10000", "千军万马", "闲兵与将领配兵合计达到 10000。名称参考公开玩法；此条件为本地规则。", GoalKind.Achievement, WorldMetric.Troops, 10000, R(10000, 10000)),
            new GoalDefinition("achievement.tech50", "博学经世", "个人科技等级合计达到 50。", GoalKind.Achievement, WorldMetric.Technology, 50, R(6000, 6000)),
            new GoalDefinition("achievement.hall15", "封地之主", "任意封地大厅达到 15 级。", GoalKind.Achievement, WorldMetric.HallLevel, 15, R(8000, 8000))
        };
        public static IEnumerable<GoalDefinition> All { get { return goals; } }
        public static IEnumerable<GoalDefinition> OfKind(GoalKind kind) { return goals.Where(g => g.Kind == kind); }
        public static GoalDefinition Find(string id) { return goals.FirstOrDefault(g => g.Id == id); }
    }

    [Serializable]
    public sealed class GoalRecord
    {
        public string Id;
        public double Best;
        public bool Claimed;
    }

    [Serializable]
    public sealed class LocalMail
    {
        public string Id, Sender, Title, Body;
        public long SentUtcTicks;
        public bool Read, Claimed;
        public Reward Attachment = new Reward();
        public LocalMail Copy()
        { return new LocalMail { Id = Id, Sender = Sender, Title = Title, Body = Body, SentUtcTicks = SentUtcTicks, Read = Read, Claimed = Claimed, Attachment = Attachment.Copy() }; }
        public bool IsValid()
        {
            return ValidText(Id, 120) && ValidText(Sender, 80) && ValidText(Title, 120) && ValidText(Body, 12000) &&
                SentUtcTicks >= DateTime.MinValue.Ticks && SentUtcTicks <= DateTime.MaxValue.Ticks && Attachment != null && Attachment.IsValid();
        }
        internal static bool ValidText(string text, int max) { return !string.IsNullOrWhiteSpace(text) && text.Length <= max; }
    }

    [Serializable]
    public sealed class PlayerJournal
    {
        public string PlayerKey;
        public string Day;
        public WorldProgress DailyBaseline = new WorldProgress();
        public List<GoalRecord> Goals = new List<GoalRecord>();
        public List<LocalMail> Mails = new List<LocalMail>();
        // 删除后的 ID 仍保存，避免本地事件重新投递时重复发奖。
        public List<string> DeliveredMailIds = new List<string>();
        public List<string> ReadNoticeIds = new List<string>();
    }

    [Serializable]
    public sealed class LocalNotice
    {
        public string Id, Title, Body;
        public long PublishedUtcTicks;
        public bool Pinned;
        public bool IsValid()
        { return LocalMail.ValidText(Id, 160) && LocalMail.ValidText(Title, 120) && LocalMail.ValidText(Body, 12000) && PublishedUtcTicks >= 0 && PublishedUtcTicks <= DateTime.MaxValue.Ticks; }
        public LocalNotice Copy()
        { return new LocalNotice { Id = Id, Title = Title, Body = Body, PublishedUtcTicks = PublishedUtcTicks, Pinned = Pinned }; }
    }

    [Serializable]
    public sealed class Window1WorldState
    {
        public int Version = 1;
        public string WorldId;
        public List<PlayerJournal> Players = new List<PlayerJournal>();
        public List<LocalNotice> Notices = new List<LocalNotice>();
        public bool Validate(string worldId, out string error)
        {
            error = "任务邮件存档无效";
            if (Version != 1 || !LocalMail.ValidText(WorldId, 160) || WorldId != worldId || Players == null || Players.Count > 512 || Notices == null || Notices.Count > 100) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (Notices.Any(n => n == null || !n.IsValid() || !ids.Add(n.Id))) return false;
            var playerKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in Players)
            {
                DateTime parsed;
                if (p == null || !LocalMail.ValidText(p.PlayerKey, 160) || !playerKeys.Add(p.PlayerKey) ||
                    (!string.IsNullOrEmpty(p.Day) && !DateTime.TryParseExact(p.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)) ||
                    p.DailyBaseline == null || !p.DailyBaseline.IsValid() || p.Goals == null || p.Goals.Count > 100 ||
                    p.Mails == null || p.Mails.Count > 100 || p.DeliveredMailIds == null || p.DeliveredMailIds.Count > 10000 ||
                    p.ReadNoticeIds == null || p.ReadNoticeIds.Count > 1000) return false;
                var goalIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var r in p.Goals)
                {
                    var def = r == null ? null : GoalCatalog.Find(r.Id);
                    if (def == null || !goalIds.Add(r.Id) || !WorldProgress.Number(r.Best) || r.Best > def.Target || (r.Claimed && r.Best < def.Target)) return false;
                }
                var deliveryIds = new HashSet<string>(StringComparer.Ordinal);
                if (p.DeliveredMailIds.Any(id => !LocalMail.ValidText(id, 120) || !deliveryIds.Add(id))) return false;
                var mailIds = new HashSet<string>(StringComparer.Ordinal);
                if (p.Mails.Any(m => m == null || !m.IsValid() || !mailIds.Add(m.Id) || !deliveryIds.Contains(m.Id) || (m.Claimed && !m.Read))) return false;
                var noticeIds = new HashSet<string>(StringComparer.Ordinal);
                if (p.ReadNoticeIds.Any(id => !LocalMail.ValidText(id, 160) || !noticeIds.Add(id))) return false;
            }
            error = null;
            return true;
        }
    }

    // 适配器只负责现有世界数据与一次性入账，规则层可在无 Unity 的测试中运行。
    public interface ILocalWorld
    {
        string PlayerKey { get; }
        bool TryRead(out WorldProgress progress, out string error);
        bool TryGrant(Reward reward, out string error);
    }

    public sealed class GoalView
    {
        public GoalDefinition Definition;
        public double Current;
        public bool Claimed;
        public bool Complete { get { return Current >= Definition.Target; } }
        public string Status { get { return Claimed ? "已领取" : Complete ? "可领取" : "进行中"; } }
    }

    public sealed class JournalService
    {
        private readonly Window1WorldState state;
        private readonly ILocalWorld world;
        private readonly Func<DateTime> clock;
        private bool mutating;
        public JournalService(Window1WorldState state, ILocalWorld world, Func<DateTime> clock)
        { this.state = state; this.world = world; this.clock = clock; }
        private PlayerJournal Journal()
        {
            if (string.IsNullOrEmpty(world.PlayerKey)) throw new InvalidOperationException("没有当前君主");
            var p = state.Players.Find(x => x.PlayerKey == world.PlayerKey);
            if (p == null) { p = new PlayerJournal { PlayerKey = world.PlayerKey }; state.Players.Add(p); }
            return p;
        }
        public bool Refresh(out string error)
        {
            WorldProgress progress;
            if (!world.TryRead(out progress, out error)) return false;
            if (progress == null || !progress.IsValid()) { error = "世界进度包含无效数值"; return false; }
            var p = Journal();
            string day = clock().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            // 回拨本机日期不反复重置；日常采用首次观察后的净增加，基线随世界保存。
            if (string.IsNullOrEmpty(p.Day) || string.CompareOrdinal(day, p.Day) > 0)
            {
                p.Day = day; p.DailyBaseline = progress.Copy();
                p.Goals.RemoveAll(r => GoalCatalog.Find(r.Id).Kind == GoalKind.Daily);
            }
            foreach (var def in GoalCatalog.All)
            {
                var r = p.Goals.Find(x => x.Id == def.Id);
                if (r == null) { r = new GoalRecord { Id = def.Id }; p.Goals.Add(r); }
                double current = progress.Value(def.Metric);
                if (def.Kind == GoalKind.Daily) current = Math.Max(0, current - p.DailyBaseline.Value(def.Metric));
                r.Best = Math.Max(r.Best, Math.Min(def.Target, current));
            }
            error = null; return true;
        }
        public List<GoalView> Goals(GoalKind kind)
        {
            string ignored;
            if (!Refresh(out ignored)) return new List<GoalView>();
            var p = Journal();
            return GoalCatalog.OfKind(kind).Select(d => { var r = p.Goals.Find(x => x.Id == d.Id); return new GoalView { Definition = d, Current = r.Best, Claimed = r.Claimed }; }).ToList();
        }
        public string DailyDate { get { return Journal().Day; } }
        public bool ClaimGoal(string id, out string error)
        {
            if (mutating) { error = "奖励正在处理"; return false; }
            if (!Refresh(out error)) return false;
            var def = GoalCatalog.Find(id);
            var r = Journal().Goals.Find(x => x.Id == id);
            if (def == null || r == null || r.Best < def.Target) { error = "尚未达成任务条件"; return false; }
            if (r.Claimed) { error = "该奖励已经领取"; return false; }
            mutating = true;
            try { if (!world.TryGrant(def.Reward, out error)) return false; r.Claimed = true; return true; }
            finally { mutating = false; }
        }
        public List<LocalMail> Mails()
        { return Journal().Mails.OrderByDescending(m => m.SentUtcTicks).ThenBy(m => m.Id, StringComparer.Ordinal).Select(m => m.Copy()).ToList(); }
        public bool ReceiveLocalMail(LocalMail mail, out string error)
        {
            error = "本地信件内容无效";
            if (mail == null || !mail.IsValid()) return false;
            var p = Journal();
            if (p.DeliveredMailIds.Contains(mail.Id)) { error = "信件已接收过"; return false; }
            if (p.Mails.Count >= 100 || p.DeliveredMailIds.Count >= 10000) { error = "本地收件记录已满，请先整理邮件"; return false; }
            var copy = mail.Copy(); copy.Read = false; copy.Claimed = false;
            p.Mails.Add(copy); p.DeliveredMailIds.Add(copy.Id); error = null; return true;
        }
        public bool ReadMail(string id)
        { var m = Journal().Mails.Find(x => x.Id == id); if (m == null) return false; m.Read = true; return true; }
        public bool ClaimMail(string id, out string error)
        {
            if (mutating) { error = "附件正在处理"; return false; }
            var m = Journal().Mails.Find(x => x.Id == id);
            if (m == null) { error = "信件不存在"; return false; }
            if (m.Claimed) { error = "附件已领取"; return false; }
            if (!m.Attachment.HasAnything) { error = "此信没有附件"; return false; }
            mutating = true;
            try { if (!world.TryGrant(m.Attachment.Copy(), out error)) return false; m.Read = true; m.Claimed = true; return true; }
            finally { mutating = false; }
        }
        public bool DeleteMail(string id, out string error)
        {
            var p = Journal(); var m = p.Mails.Find(x => x.Id == id);
            if (m == null) { error = "信件不存在"; return false; }
            if (m.Attachment.HasAnything && !m.Claimed) { error = "请先领取附件，再删除信件"; return false; }
            p.Mails.Remove(m); error = null; return true;
        }
        public void ReadNotice(string id)
        { var p = Journal(); if (!p.ReadNoticeIds.Contains(id) && p.ReadNoticeIds.Count < 1000) p.ReadNoticeIds.Add(id); }
        public bool NoticeRead(string id) { return Journal().ReadNoticeIds.Contains(id); }
        public static int PageCount(int count, int size) { return Math.Max(1, (Math.Max(0, count) + Math.Max(1, size) - 1) / Math.Max(1, size)); }
        public static int ClampPage(int page, int count, int size) { return Math.Max(0, Math.Min(PageCount(count, size) - 1, page)); }
    }
}
