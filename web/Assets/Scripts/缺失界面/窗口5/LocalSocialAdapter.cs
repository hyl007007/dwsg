using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Dwsg.Social
{
    // 本地共享容器使回归测试能以多个明确身份走确认流程；游戏 UI 不提供冒充对方的入口。
    public sealed class SocialLocalStore
    {
        internal SocialStateDto State;
        internal event Action Changed;
        public SocialLocalStore(SocialPlayerDto self, string worldKey)
        {
            if (self == null || !LocalSocialAdapter.ValidId(self.Id) || !LocalSocialAdapter.ValidName(self.Name, 20))
                throw new ArgumentException("无效的本机社交身份");
            State = new SocialStateDto { WorldKey = worldKey, OwnerId = self.Id };
            State.Players.Add(LocalSocialAdapter.Copy(self));
        }
        internal void Notify() { if (Changed != null) Changed(); }
    }

    public sealed class LocalSocialAdapter : ISocialAdapter
    {
        public const int MemberLimit = 5;
        public const int ApprenticeLimit = 3;
        public const int ContactLimit = 128;
        public const int HistoryLimit = 512;
        private readonly SocialLocalStore store;
        public string CurrentPlayerId { get; private set; }
        public bool IsConnected { get { return false; } }
        public string ConnectionStatus { get { return "离线 · 申请仅在本机记录，私聊未发送"; } }
        public event Action Changed { add { store.Changed += value; } remove { store.Changed -= value; } }
        private SocialStateDto S { get { return store.State; } }

        public LocalSocialAdapter(SocialPlayerDto self, string worldKey)
            : this(new SocialLocalStore(self, worldKey), self.Id) { }
        public LocalSocialAdapter(SocialLocalStore localStore, string sessionPlayerId)
        {
            if (localStore == null || !localStore.State.Players.Any(p => p.Id == sessionPlayerId))
                throw new ArgumentException("会话必须绑定已登记的本地身份");
            store = localStore;
            CurrentPlayerId = sessionPlayerId;
        }
        internal static T Copy<T>(T value)
        { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value)); }
        public SocialStateDto Snapshot() { return Copy(S); }
        public string ExportJson() { return JsonConvert.SerializeObject(S); }
        // 本机名片读取现有世界角色资料；不据此核验任何联系人身份。
        public SocialResult UpdateLocalPlayer(SocialPlayerDto profile)
        {
            if (profile == null || profile.Id != CurrentPlayerId || !ValidName(profile.Name, 20) ||
                profile.Level < 1 || profile.Level > 999 || !ValidText(profile.Country, 24))
                return SocialResult.Fail("identity", "本机名片资料与当前角色不匹配");
            var player = Player(CurrentPlayerId);
            if (player == null) return Denied();
            if (player.Name == profile.Name && player.Level == profile.Level && player.Country == profile.Country)
                return SocialResult.Local("本机名片资料已是最新");
            player.Name = profile.Name; player.Level = profile.Level; player.Country = profile.Country;
            player.Verified = false;
            return Done("本机名片资料已同步");
        }
        public void Reset()
        {
            var owner = Copy(S.Players.First(p => p.Id == S.OwnerId));
            var session = Copy(S.Players.First(p => p.Id == CurrentPlayerId));
            store.State = new SocialStateDto { WorldKey = S.WorldKey, OwnerId = owner.Id };
            S.Players.Add(owner);
            if (session.Id != owner.Id) S.Players.Add(session);
            store.Notify();
        }
        public SocialResult ImportJson(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 1024 * 1024)
                return SocialResult.Fail("invalid", "社交存档为空或超过 1MB");
            SocialStateDto candidate;
            try
            {
                candidate = JsonConvert.DeserializeObject<SocialStateDto>(json,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 24 });
            }
            catch (JsonException) { return SocialResult.Fail("invalid", "社交存档 JSON 无效，原记录保留"); }
            string problem = Validate(candidate);
            if (problem != null) return SocialResult.Fail("invalid", problem + "，原记录保留");
            if (candidate.WorldKey != S.WorldKey || candidate.OwnerId != S.OwnerId ||
                !candidate.Players.Any(p => p.Id == CurrentPlayerId))
                return SocialResult.Fail("identity", "社交存档与当前世界或角色不匹配");
            // 本地导入不能恢复虚假在线/服务器已核验身份。
            foreach (var player in candidate.Players) player.Verified = false;
            store.State = candidate;
            store.Notify();
            return SocialResult.Local("已恢复社交记录");
        }
        public static bool ValidId(string id)
        { return id != null && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{1,48}\z"); }
        public static bool ValidName(string text, int max)
        { return !string.IsNullOrWhiteSpace(text) && text == text.Trim() && text.Length <= max && Clean(text); }
        private static bool Clean(string text)
        { return text != null && !text.Any(c => char.IsControl(c) || c == '<' || c == '>'); }
        private static bool ValidText(string text, int max)
        { return text != null && text.Length <= max && Clean(text); }
        private static string Id() { return Guid.NewGuid().ToString("N"); }
        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
        private SocialPlayerDto Player(string id) { return S.Players.FirstOrDefault(p => p.Id == id); }
        private GuildDto GuildFor(string id) { return S.Guilds.FirstOrDefault(g => g.Members.Contains(id)); }
        private BrotherhoodDto BrothersFor(string id) { return S.Brotherhoods.FirstOrDefault(g => g.Members.Contains(id)); }
        private bool Blocked(string a, string b)
        { return S.Blocks.Any(x => (x.Owner == a && x.Target == b) || (x.Owner == b && x.Target == a)); }
        private bool Friends(string a, string b)
        { return S.Friends.Any(x => (x.A == a && x.B == b) || (x.A == b && x.B == a)); }
        private SocialResult TargetProblem(string target, bool respectBlock = true)
        {
            if (!ValidId(target) || Player(target) == null) return SocialResult.Fail("missing", "请先登记正确的联系人 ID");
            if (target == CurrentPlayerId) return SocialResult.Fail("self", "不能对自己发起此操作");
            if (respectBlock && Blocked(CurrentPlayerId, target)) return SocialResult.Fail("blocked", "双方存在黑名单关系，操作被拒绝");
            return null;
        }
        private SocialResult Capacity(bool full) { return full ? SocialResult.Fail("capacity", "本地记录已达上限") : null; }
        private SocialResult Denied() { return SocialResult.Fail("permission", "当前角色无权执行此操作"); }
        private SocialResult Missing() { return SocialResult.Fail("missing", "记录不存在或已失效，请返回刷新"); }
        private SocialResult Done(string message, string id = null)
        { store.Notify(); return SocialResult.Local(message, id); }

        public SocialResult Execute(SocialCommand c)
        {
            if (c == null || !Enum.IsDefined(typeof(SocialCommandKind), c.Kind)) return SocialResult.Fail("invalid", "无效的社交命令");
            if (Player(CurrentPlayerId) == null) return Denied();
            c.Text = c.Text ?? "";
            switch (c.Kind)
            {
                case SocialCommandKind.RegisterContact:
                    if (!ValidId(c.Target) || !ValidName(c.Name, 20)) return SocialResult.Fail("invalid", "ID 限 1–48 位字母/数字/_/-；称呼限 1–20 字且不能含尖括号");
                    if (c.Target == CurrentPlayerId) return SocialResult.Fail("self", "不能登记自己为联系人");
                    if (Player(c.Target) != null) return SocialResult.Fail("duplicate", "此 ID 已登记，请查看名片");
                    if (S.Players.Count >= ContactLimit) return Capacity(true);
                    S.Players.Add(new SocialPlayerDto { Id = c.Target, Name = c.Name });
                    return Done("已登记离线联系人，身份尚未核验", c.Target);
                case SocialCommandKind.RequestFriend: return RequestFriend(c);
                case SocialCommandKind.AnswerFriend: return AnswerFriend(c);
                case SocialCommandKind.CancelFriend:
                    var fr = S.FriendRequests.FirstOrDefault(x => x.Id == c.Entity && x.State == RequestState.Pending);
                    if (fr == null) return Missing();
                    if (fr.From != CurrentPlayerId) return Denied();
                    fr.State = RequestState.Cancelled; return Done("已撤销好友申请");
                case SocialCommandKind.RemoveFriend:
                    var tp = TargetProblem(c.Target, false); if (tp != null) return tp;
                    if (!Friends(CurrentPlayerId, c.Target)) return Missing();
                    S.Friends.RemoveAll(x => (x.A == CurrentPlayerId && x.B == c.Target) || (x.B == CurrentPlayerId && x.A == c.Target));
                    return Done("已解除好友关系");
                case SocialCommandKind.Block:
                    var bp = TargetProblem(c.Target, false); if (bp != null) return bp;
                    if (S.Blocks.Any(x => x.Owner == CurrentPlayerId && x.Target == c.Target)) return SocialResult.Fail("duplicate", "已在黑名单中");
                    if (S.Blocks.Count >= HistoryLimit) return Capacity(true);
                    S.Blocks.Add(new BlockDto { Owner = CurrentPlayerId, Target = c.Target });
                    S.Friends.RemoveAll(x => (x.A == CurrentPlayerId && x.B == c.Target) || (x.B == CurrentPlayerId && x.A == c.Target));
                    foreach (var f in S.FriendRequests.Where(x => x.State == RequestState.Pending && Pair(x.From, x.To, CurrentPlayerId, c.Target))) f.State = RequestState.Cancelled;
                    foreach (var i in S.Invitations.Where(x => x.State == RequestState.Pending && Pair(x.From, x.To, CurrentPlayerId, c.Target))) i.State = RequestState.Cancelled;
                    return Done("已拉黑，好友关系与双方待确认邀请已取消");
                case SocialCommandKind.Unblock:
                    if (S.Blocks.RemoveAll(x => x.Owner == CurrentPlayerId && x.Target == c.Target) == 0) return Missing();
                    return Done("已移出黑名单，需重新申请好友");
                case SocialCommandKind.SendPrivate:
                    var sp = TargetProblem(c.Target); if (sp != null) return sp;
                    if (!ValidText(c.Text, 200) || string.IsNullOrWhiteSpace(c.Text)) return SocialResult.Fail("invalid", "私聊限 1–200 字且不能含换行/尖括号");
                    return SocialResult.Fail("offline", "离线，消息未发送；请保存草稿，联网后再手动发送");
                case SocialCommandKind.SaveDraft:
                    var dp = TargetProblem(c.Target, false); if (dp != null) return dp;
                    if (!ValidText(c.Text, 200)) return SocialResult.Fail("invalid", "草稿最多 200 字且不能含换行/尖括号");
                    var draft = S.Drafts.FirstOrDefault(x => x.Owner == CurrentPlayerId && x.Target == c.Target);
                    if (draft == null)
                    {
                        if (S.Drafts.Count >= HistoryLimit) return Capacity(true);
                        draft = new PrivateDraftDto { Owner = CurrentPlayerId, Target = c.Target }; S.Drafts.Add(draft);
                    }
                    draft.Text = c.Text; return Done("草稿已保留，未发送");
                case SocialCommandKind.CreateGuild: return CreateGuild(c);
                case SocialCommandKind.ApplyGuild: return ApplyGuild(c);
                case SocialCommandKind.AnswerGuild: return AnswerGuild(c);
                case SocialCommandKind.CancelGuild:
                    var ga = S.GuildApplications.FirstOrDefault(x => x.Id == c.Entity && x.State == RequestState.Pending);
                    if (ga == null) return Missing();
                    if (ga.Applicant != CurrentPlayerId) return Denied();
                    ga.State = RequestState.Cancelled; return Done("已撤销入团申请");
                case SocialCommandKind.LeaveGuild: return LeaveGuild(c);
                case SocialCommandKind.KickGuild: return ManageGuild(c, false);
                case SocialCommandKind.TransferGuild: return ManageGuild(c, true);
                case SocialCommandKind.UpdateGuild:
                    var ug = S.Guilds.FirstOrDefault(x => x.Id == c.Entity); if (ug == null) return Missing();
                    if (ug.Leader != CurrentPlayerId) return Denied();
                    if (!ValidText(c.Text, 120)) return SocialResult.Fail("invalid", "公告最多 120 字且不能含换行/尖括号");
                    ug.Notice = c.Text; return Done("军团公告已更新");
                case SocialCommandKind.DissolveGuild:
                    var dg = S.Guilds.FirstOrDefault(x => x.Id == c.Entity); if (dg == null) return Missing();
                    if (dg.Leader != CurrentPlayerId) return Denied();
                    S.Guilds.Remove(dg);
                    foreach (var a in S.GuildApplications.Where(x => x.Guild == dg.Id && x.State == RequestState.Pending)) a.State = RequestState.Cancelled;
                    return Done("军团已解散");
                case SocialCommandKind.InviteMentor:
                case SocialCommandKind.InviteApprentice:
                case SocialCommandKind.InviteBrother: return Invite(c);
                case SocialCommandKind.AnswerRelation: return AnswerRelation(c);
                case SocialCommandKind.CancelRelation:
                    var ri = S.Invitations.FirstOrDefault(x => x.Id == c.Entity && x.State == RequestState.Pending);
                    if (ri == null) return Missing();
                    if (ri.From != CurrentPlayerId) return Denied();
                    ri.State = RequestState.Cancelled; return Done("已撤销关系邀请");
                case SocialCommandKind.LeaveMentor:
                    var mb = S.Mentors.FirstOrDefault(x => x.Id == c.Entity); if (mb == null) return Missing();
                    if (mb.Mentor != CurrentPlayerId && mb.Apprentice != CurrentPlayerId) return Denied();
                    S.Mentors.Remove(mb); return Done("已解除师徒关系");
                case SocialCommandKind.LeaveBrother: return LeaveBrother(c);
                case SocialCommandKind.KickBrother: return ManageBrother(c, false);
                case SocialCommandKind.TransferBrother: return ManageBrother(c, true);
                default: return SocialResult.Fail("unsupported", "此命令尚未支持");
            }
        }
        private static bool Pair(string a, string b, string c, string d) { return (a == c && b == d) || (a == d && b == c); }
        private SocialResult RequestFriend(SocialCommand c)
        {
            var p = TargetProblem(c.Target); if (p != null) return p;
            if (!ValidText(c.Text, 60)) return SocialResult.Fail("invalid", "申请附言最多 60 字");
            if (Friends(CurrentPlayerId, c.Target)) return SocialResult.Fail("duplicate", "已经是好友");
            if (S.FriendRequests.Any(x => x.State == RequestState.Pending && Pair(x.From, x.To, CurrentPlayerId, c.Target)))
                return SocialResult.Fail("duplicate", "双方已有待确认申请，请处理原申请");
            if (S.FriendRequests.Count >= HistoryLimit) return Capacity(true);
            var f = new FriendRequestDto { Id = Id(), From = CurrentPlayerId, To = c.Target, Note = c.Text, CreatedUtc = Now() };
            S.FriendRequests.Add(f); return Done("好友申请已记录，尚未送达对方", f.Id);
        }
        private SocialResult AnswerFriend(SocialCommand c)
        {
            var f = S.FriendRequests.FirstOrDefault(x => x.Id == c.Entity && x.State == RequestState.Pending);
            if (f == null) return Missing();
            if (f.To != CurrentPlayerId) return Denied();
            if (c.Accept)
            {
                var p = TargetProblem(f.From); if (p != null) return p;
                if (Friends(f.From, f.To)) return SocialResult.Fail("duplicate", "已经是好友");
                if (S.Friends.Count >= HistoryLimit) return Capacity(true);
                S.Friends.Add(new FriendDto { A = f.From, B = f.To });
            }
            f.State = c.Accept ? RequestState.Accepted : RequestState.Declined;
            return Done(c.Accept ? "已确认好友关系" : "已拒绝好友申请");
        }
        private SocialResult CreateGuild(SocialCommand c)
        {
            if (!ValidName(c.Name, 12) || !ValidText(c.Text, 120)) return SocialResult.Fail("invalid", "军团名限 1–12 字，公告最多 120 字");
            if (GuildFor(CurrentPlayerId) != null) return SocialResult.Fail("membership", "请先退出当前军团");
            if (S.Guilds.Any(x => string.Equals(x.Name, c.Name, StringComparison.OrdinalIgnoreCase))) return SocialResult.Fail("duplicate", "已有同名军团");
            if (S.Guilds.Count >= ContactLimit) return Capacity(true);
            var g = new GuildDto { Id = Id(), Name = c.Name, Notice = c.Text, Leader = CurrentPlayerId };
            g.Members.Add(CurrentPlayerId); S.Guilds.Add(g);
            CancelGuildApplications(CurrentPlayerId);
            return Done("本地军团已建立；未创建服务器军团", g.Id);
        }
        private void CancelGuildApplications(string player)
        { foreach (var a in S.GuildApplications.Where(x => x.Applicant == player && x.State == RequestState.Pending)) a.State = RequestState.Cancelled; }
        private SocialResult ApplyGuild(SocialCommand c)
        {
            var g = S.Guilds.FirstOrDefault(x => x.Id == c.Entity); if (g == null) return Missing();
            if (GuildFor(CurrentPlayerId) != null) return SocialResult.Fail("membership", "已有军团，不能重复入团");
            if (Blocked(CurrentPlayerId, g.Leader)) return SocialResult.Fail("blocked", "与团长存在黑名单关系");
            if (g.Members.Count >= MemberLimit) return SocialResult.Fail("capacity", "军团已满（本地规则：5 人）");
            if (!ValidText(c.Text, 60)) return SocialResult.Fail("invalid", "申请附言最多 60 字");
            if (S.GuildApplications.Any(x => x.Guild == g.Id && x.Applicant == CurrentPlayerId && x.State == RequestState.Pending)) return SocialResult.Fail("duplicate", "已提交入团申请");
            if (S.GuildApplications.Count >= HistoryLimit) return Capacity(true);
            var a = new GuildApplicationDto { Id = Id(), Guild = g.Id, Applicant = CurrentPlayerId, Note = c.Text, CreatedUtc = Now() };
            S.GuildApplications.Add(a); return Done("入团申请已记录，尚未送达团长", a.Id);
        }
        private SocialResult AnswerGuild(SocialCommand c)
        {
            var a = S.GuildApplications.FirstOrDefault(x => x.Id == c.Entity && x.State == RequestState.Pending); if (a == null) return Missing();
            var g = S.Guilds.FirstOrDefault(x => x.Id == a.Guild); if (g == null) return Missing();
            if (g.Leader != CurrentPlayerId) return Denied();
            if (c.Accept)
            {
                if (Blocked(CurrentPlayerId, a.Applicant)) return SocialResult.Fail("blocked", "与申请人存在黑名单关系");
                if (GuildFor(a.Applicant) != null) return SocialResult.Fail("membership", "申请人已有军团");
                if (g.Members.Count >= MemberLimit) return SocialResult.Fail("capacity", "军团已满");
                g.Members.Add(a.Applicant);
                CancelGuildApplications(a.Applicant);
            }
            a.State = c.Accept ? RequestState.Accepted : RequestState.Declined;
            return Done(c.Accept ? "已批准入团" : "已拒绝入团申请");
        }
        private SocialResult LeaveGuild(SocialCommand c)
        {
            var g = S.Guilds.FirstOrDefault(x => x.Id == c.Entity); if (g == null) return Missing();
            if (!g.Members.Contains(CurrentPlayerId)) return Denied();
            if (g.Leader == CurrentPlayerId) return SocialResult.Fail("leader", "团长请先转让团长或选择解散军团");
            g.Members.Remove(CurrentPlayerId); return Done("已退出军团");
        }
        private SocialResult ManageGuild(SocialCommand c, bool transfer)
        {
            var g = S.Guilds.FirstOrDefault(x => x.Id == c.Entity); if (g == null) return Missing();
            if (g.Leader != CurrentPlayerId) return Denied();
            if (c.Target == CurrentPlayerId) return SocialResult.Fail("self", "不能对自己执行此操作");
            if (!g.Members.Contains(c.Target)) return Missing();
            if (transfer) g.Leader = c.Target; else g.Members.Remove(c.Target);
            return Done(transfer ? "已转让团长" : "已移出军团成员");
        }
        private SocialResult MentorProblem(string mentor, string apprentice)
        {
            if (Player(mentor) == null || Player(apprentice) == null || mentor == apprentice) return SocialResult.Fail("invalid", "师徒身份无效");
            if (Blocked(mentor, apprentice)) return SocialResult.Fail("blocked", "师徒双方存在黑名单关系");
            if (Player(mentor).Level <= Player(apprentice).Level) return SocialResult.Fail("level", "本地规则：师父等级必须高于徒弟");
            if (S.Mentors.Any(x => x.Apprentice == apprentice)) return SocialResult.Fail("membership", "徒弟已有师父");
            if (S.Mentors.Count(x => x.Mentor == mentor) >= ApprenticeLimit) return SocialResult.Fail("capacity", "本地规则：每位师父最多 3 位徒弟");
            // 等级可能在存档恢复后改变，额外检查有向环。
            string cursor = mentor;
            for (int i = 0; i <= S.Mentors.Count; i++)
            {
                if (cursor == apprentice) return SocialResult.Fail("cycle", "不能形成循环师徒关系");
                var parent = S.Mentors.FirstOrDefault(x => x.Apprentice == cursor);
                if (parent == null) break; cursor = parent.Mentor;
            }
            return null;
        }
        private SocialResult BrotherProblem(string inviter, string target, string group)
        {
            if (Blocked(inviter, target)) return SocialResult.Fail("blocked", "双方存在黑名单关系");
            if (BrothersFor(target) != null) return SocialResult.Fail("membership", "对方已有结拜关系");
            var g = BrothersFor(inviter);
            if (string.IsNullOrEmpty(group))
            { if (g != null) return SocialResult.Fail("stale", "邀请人已加入结拜，请重新邀请"); }
            else
            {
                if (g == null || g.Id != group) return SocialResult.Fail("stale", "原结拜关系已变更");
                if (g.Leader != inviter) return Denied();
                if (g.Members.Count >= MemberLimit) return SocialResult.Fail("capacity", "本地规则：结拜最多 5 人");
                if (g.Members.Any(m => Blocked(m, target))) return SocialResult.Fail("blocked", "对方与结拜成员存在黑名单关系");
            }
            return null;
        }
        private SocialResult Invite(SocialCommand c)
        {
            var p = TargetProblem(c.Target); if (p != null) return p;
            if (!Friends(CurrentPlayerId, c.Target)) return SocialResult.Fail("friend", "本地规则：请先成为好友");
            var kind = c.Kind == SocialCommandKind.InviteBrother ? RelationKind.Brotherhood : RelationKind.Mentor;
            if (S.Invitations.Any(x => x.Kind == kind && x.State == RequestState.Pending && Pair(x.From, x.To, CurrentPlayerId, c.Target))) return SocialResult.Fail("duplicate", "双方已有待确认关系邀请");
            if (S.Invitations.Count >= HistoryLimit) return Capacity(true);
            var inv = new RelationInvitationDto { Id = Id(), From = CurrentPlayerId, To = c.Target, Kind = kind, CreatedUtc = Now() };
            if (kind == RelationKind.Mentor)
            {
                inv.Mentor = c.Kind == SocialCommandKind.InviteMentor ? c.Target : CurrentPlayerId;
                inv.Apprentice = inv.Mentor == c.Target ? CurrentPlayerId : c.Target;
                p = MentorProblem(inv.Mentor, inv.Apprentice);
            }
            else
            {
                var g = BrothersFor(CurrentPlayerId); inv.Group = g == null ? null : g.Id;
                inv.Name = g == null ? c.Name : g.Name;
                if (!ValidName(inv.Name, 12)) return SocialResult.Fail("invalid", "结拜名限 1–12 字");
                p = BrotherProblem(CurrentPlayerId, c.Target, inv.Group);
            }
            if (p != null) return p;
            S.Invitations.Add(inv); return Done("关系邀请已记录，尚未送达对方", inv.Id);
        }
        private SocialResult AnswerRelation(SocialCommand c)
        {
            var i = S.Invitations.FirstOrDefault(x => x.Id == c.Entity && x.State == RequestState.Pending); if (i == null) return Missing();
            if (i.To != CurrentPlayerId) return Denied();
            if (c.Accept)
            {
                var p = TargetProblem(i.From); if (p != null) return p;
                if (!Friends(i.From, i.To)) return SocialResult.Fail("friend", "已不是好友，不能确认关系");
                if (i.Kind == RelationKind.Mentor)
                {
                    p = MentorProblem(i.Mentor, i.Apprentice); if (p != null) return p;
                    if (S.Mentors.Count >= HistoryLimit) return Capacity(true);
                    S.Mentors.Add(new MentorDto { Id = Id(), Mentor = i.Mentor, Apprentice = i.Apprentice });
                }
                else
                {
                    p = BrotherProblem(i.From, i.To, i.Group); if (p != null) return p;
                    var g = BrothersFor(i.From);
                    if (g == null)
                    {
                        if (S.Brotherhoods.Count >= ContactLimit) return Capacity(true);
                        g = new BrotherhoodDto { Id = Id(), Name = i.Name, Leader = i.From };
                        g.Members.Add(i.From); S.Brotherhoods.Add(g);
                    }
                    g.Members.Add(i.To);
                }
            }
            i.State = c.Accept ? RequestState.Accepted : RequestState.Declined;
            return Done(c.Accept ? "已确认关系" : "已拒绝关系邀请");
        }
        private SocialResult LeaveBrother(SocialCommand c)
        {
            var g = S.Brotherhoods.FirstOrDefault(x => x.Id == c.Entity); if (g == null) return Missing();
            if (!g.Members.Contains(CurrentPlayerId)) return Denied();
            if (g.Leader == CurrentPlayerId && g.Members.Count > 2) return SocialResult.Fail("leader", "请先转让结拜发起人再退出");
            g.Members.Remove(CurrentPlayerId);
            if (g.Members.Count < 2)
            {
                S.Brotherhoods.Remove(g);
                foreach (var i in S.Invitations.Where(x => x.Group == g.Id && x.State == RequestState.Pending)) i.State = RequestState.Cancelled;
            }
            return Done("已退出结拜；少于 2 人的结拜自动解除");
        }
        private SocialResult ManageBrother(SocialCommand c, bool transfer)
        {
            var g = S.Brotherhoods.FirstOrDefault(x => x.Id == c.Entity); if (g == null) return Missing();
            if (g.Leader != CurrentPlayerId) return Denied();
            if (c.Target == CurrentPlayerId) return SocialResult.Fail("self", "不能对自己执行此操作");
            if (!g.Members.Contains(c.Target)) return Missing();
            if (transfer) g.Leader = c.Target;
            else
            {
                g.Members.Remove(c.Target);
                if (g.Members.Count < 2)
                {
                    S.Brotherhoods.Remove(g);
                    foreach (var i in S.Invitations.Where(x => x.Group == g.Id && x.State == RequestState.Pending)) i.State = RequestState.Cancelled;
                }
            }
            return Done(transfer ? "已转让结拜发起人" : "已移出结拜成员");
        }

        // 拒绝坏档、重复身份/成员、越界状态、悬空引用和循环关系；校验后才替换。
        public static string Validate(SocialStateDto s)
        {
            if (s == null || s.Version != 1 || string.IsNullOrEmpty(s.WorldKey) || s.WorldKey.Length > 128 || !ValidId(s.OwnerId)) return "社交存档版本/身份无效";
            if (s.Players == null || s.Friends == null || s.Blocks == null || s.FriendRequests == null || s.Guilds == null || s.GuildApplications == null || s.Invitations == null || s.Mentors == null || s.Brotherhoods == null || s.Drafts == null || s.Messages == null) return "社交存档缺少列表";
            if (s.Players.Count > ContactLimit || s.Guilds.Count > ContactLimit || s.Brotherhoods.Count > ContactLimit || new[] { s.Friends.Count, s.Blocks.Count, s.FriendRequests.Count, s.GuildApplications.Count, s.Invitations.Count, s.Mentors.Count, s.Drafts.Count, s.Messages.Count }.Any(n => n > HistoryLimit)) return "社交存档超过容量上限";
            if (s.Players.Any(p => p == null || !ValidId(p.Id) || !ValidName(p.Name, 20) || p.Level < 1 || p.Level > 999 || !ValidText(p.Country, 24))) return "联系人资料无效";
            var players = new HashSet<string>(s.Players.Select(p => p.Id));
            if (players.Count != s.Players.Count || !players.Contains(s.OwnerId)) return "联系人身份重复或缺少本机身份";
            Func<string, string, bool> pair = (a, b) => a != b && players.Contains(a) && players.Contains(b);
            Func<RequestState, bool> state = x => Enum.IsDefined(typeof(RequestState), x);
            Func<string, bool> exists = x => players.Contains(x);
            if (s.Friends.Any(x => x == null || !pair(x.A, x.B)) || s.Blocks.Any(x => x == null || !pair(x.Owner, x.Target))) return "好友或黑名单引用无效";
            if (s.Friends.Select(x => string.CompareOrdinal(x.A, x.B) < 0 ? x.A + ":" + x.B : x.B + ":" + x.A).Distinct().Count() != s.Friends.Count || s.Blocks.Select(x => x.Owner + ":" + x.Target).Distinct().Count() != s.Blocks.Count) return "好友或黑名单重复";
            if (s.Friends.Any(f => s.Blocks.Any(b => Pair(f.A, f.B, b.Owner, b.Target)))) return "好友与黑名单冲突";
            if (s.FriendRequests.Any(x => x == null || !ValidId(x.Id) || !pair(x.From, x.To) || !state(x.State) || !ValidText(x.Note, 60))) return "好友申请无效";
            var membership = new HashSet<string>();
            foreach (var g in s.Guilds)
            {
                if (g == null || !ValidId(g.Id) || !ValidName(g.Name, 12) || !ValidText(g.Notice, 120) || g.Members == null || g.Members.Count < 1 || g.Members.Count > MemberLimit || !g.Members.Contains(g.Leader)) return "军团结构无效";
                foreach (var m in g.Members) if (!exists(m) || !membership.Add(m)) return "军团成员重复或身份缺失";
            }
            if (s.Guilds.Select(g => g.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != s.Guilds.Count) return "军团名重复";
            var guildIds = new HashSet<string>(s.Guilds.Select(g => g.Id));
            if (s.GuildApplications.Any(x => x == null || !ValidId(x.Id) || !exists(x.Applicant) || !state(x.State) || !ValidText(x.Note, 60) || (x.State == RequestState.Pending && (!guildIds.Contains(x.Guild) || membership.Contains(x.Applicant))))) return "军团申请无效";
            membership.Clear();
            foreach (var g in s.Brotherhoods)
            {
                if (g == null || !ValidId(g.Id) || !ValidName(g.Name, 12) || g.Members == null || g.Members.Count < 2 || g.Members.Count > MemberLimit || !g.Members.Contains(g.Leader)) return "结拜结构无效";
                foreach (var m in g.Members) if (!exists(m) || !membership.Add(m)) return "结拜成员重复或身份缺失";
            }
            if (s.Mentors.Any(x => x == null || !ValidId(x.Id) || !pair(x.Mentor, x.Apprentice)) || s.Mentors.Select(x => x.Apprentice).Distinct().Count() != s.Mentors.Count || s.Mentors.GroupBy(x => x.Mentor).Any(g => g.Count() > ApprenticeLimit)) return "师徒关系无效";
            foreach (var m in s.Mentors)
            {
                string cursor = m.Mentor; var seen = new HashSet<string> { m.Apprentice };
                while (cursor != null)
                {
                    if (!seen.Add(cursor)) return "师徒关系形成循环";
                    var next = s.Mentors.FirstOrDefault(x => x.Apprentice == cursor); cursor = next == null ? null : next.Mentor;
                }
            }
            if (s.Invitations.Any(x => x == null || !ValidId(x.Id) || !pair(x.From, x.To) || !state(x.State) || !Enum.IsDefined(typeof(RelationKind), x.Kind) ||
                (x.Kind == RelationKind.Mentor && (!pair(x.Mentor, x.Apprentice) || !Pair(x.From, x.To, x.Mentor, x.Apprentice))) ||
                (x.Kind == RelationKind.Brotherhood && (!ValidName(x.Name, 12) || (x.State == RequestState.Pending && !string.IsNullOrEmpty(x.Group) && !s.Brotherhoods.Any(g => g.Id == x.Group)))))) return "关系邀请无效";
            if (s.Drafts.Any(x => x == null || !pair(x.Owner, x.Target) || !ValidText(x.Text, 200)) || s.Drafts.Select(x => x.Owner + ":" + x.Target).Distinct().Count() != s.Drafts.Count) return "私聊草稿无效";
            if (s.Messages.Any(x => x == null || !ValidId(x.Id) || !pair(x.From, x.To) || !ValidText(x.Text, 200) || !Enum.IsDefined(typeof(MessageDelivery), x.Delivery))) return "私聊消息无效";
            var ids = s.FriendRequests.Select(x => x.Id).Concat(s.Guilds.Select(x => x.Id)).Concat(s.GuildApplications.Select(x => x.Id)).Concat(s.Invitations.Select(x => x.Id)).Concat(s.Mentors.Select(x => x.Id)).Concat(s.Brotherhoods.Select(x => x.Id)).Concat(s.Messages.Select(x => x.Id)).ToList();
            if (ids.Distinct().Count() != ids.Count) return "社交记录 ID 重复";
            if (s.FriendRequests.Where(x => x.State == RequestState.Pending).Select(x => string.CompareOrdinal(x.From, x.To) < 0 ? x.From + ":" + x.To : x.To + ":" + x.From).GroupBy(x => x).Any(g => g.Count() > 1)) return "待确认好友申请重复";
            if (s.GuildApplications.Where(x => x.State == RequestState.Pending).Select(x => x.Guild + ":" + x.Applicant).GroupBy(x => x).Any(g => g.Count() > 1)) return "待确认军团申请重复";
            if (s.Invitations.Where(x => x.State == RequestState.Pending).Select(x => x.Kind + ":" + (string.CompareOrdinal(x.From, x.To) < 0 ? x.From + ":" + x.To : x.To + ":" + x.From)).GroupBy(x => x).Any(g => g.Count() > 1)) return "待确认关系邀请重复";
            return null;
        }
    }
}
