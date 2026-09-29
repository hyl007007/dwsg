using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dwsg.Social
{
    public sealed class SocialPanel : MonoBehaviour
    {
        public bool Ready { get; private set; }
        private ISocialAdapter adapter;
        private SocialUi ui;
        private SocialScreen home, profile, conversation, guild, form, confirmation, noticeScreen;
        private readonly List<SocialScreen> screens = new List<SocialScreen>();
        private string page = "好友", friendFilter = "联系人", relationFilter = "关系";
        private string search = "", targetId, guildId;
        private string contactId = "", contactName = "", requestNote = "", brotherName = "桃园同心";
        private string editName = "", editNotice = "";
        private string conversationText = "", conversationTarget;
        private bool onlyFriends;
        private string confirmText;
        private SocialCommand confirmCommand;
        private Action<SocialResult> confirmed;
        private const float W = 660;

        internal void Initialize(ISocialAdapter newAdapter)
        {
            Ready = false;
            foreach (var screen in screens)
                if (screen != null) { screen.gameObject.SetActive(false); Destroy(screen.gameObject); }
            screens.Clear(); home = profile = conversation = guild = form = confirmation = noticeScreen = null;
            adapter = newAdapter; ui = new SocialUi(gameObject.scene); Ready = adapter != null;
            page = "好友"; friendFilter = "联系人"; relationFilter = "关系"; search = "";
            contactId = contactName = requestNote = editName = editNotice = "";
            brotherName = "桃园同心"; onlyFriends = false;
            targetId = guildId = conversationTarget = null; conversationText = "";
        }
        private void OnDestroy()
        {
            foreach (var screen in screens) if (screen != null) Destroy(screen.gameObject);
            社交界面入口.Detached(this);
        }
        private SocialStateDto State { get { return adapter.Snapshot(); } }
        private static void Show(SocialScreen screen)
        {
            if (screen == null) return;
            if (screen.gameObject.activeSelf) screen.Refresh();
            else screen.gameObject.SetActive(true); // OnEnable 完成唯一一次重建。
        }
        private string Me { get { return adapter.CurrentPlayerId; } }
        private string Name(SocialStateDto s, string id)
        { var p = s.Players.FirstOrDefault(x => x.Id == id); return p == null ? "未知身份" : p.Name; }
        private bool IsFriend(SocialStateDto s, string id)
        { return s.Friends.Any(x => (x.A == Me && x.B == id) || (x.B == Me && x.A == id)); }
        private bool IsBlocked(SocialStateDto s, string id)
        { return s.Blocks.Any(x => x.Owner == Me && x.Target == id); }
        private static string RequestLabel(RequestState state)
        { return state == RequestState.Pending ? "待确认 · 本地记录" : state == RequestState.Accepted ? "已确认 · 本地记录" : state == RequestState.Declined ? "已拒绝" : "已撤销"; }
        private SocialScreen Screen(string title, Action<SocialScreen> render)
        {
            var root = new GameObject(title, typeof(RectTransform)); root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 6;
            var scale = root.AddComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(960, 540); scale.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();
            var screen = root.AddComponent<SocialScreen>(); screen.Adapter = adapter; screen.Render = render;
            // 真实组件构造在 inactive 根下，注册后才激活。
            var panel = ui.Node(root.transform, "社交面板", 0, 0, 700, 476);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f); panel.anchoredPosition = Vector2.zero;
            screen.Frame = panel;
            ui.Shell(panel, title, () => screen.gameObject.SetActive(false), out screen.Title);
            ui.Text(panel, adapter.ConnectionStatus, 24, 46, 652, 30, 15, SocialUi.PaperInk);
            screen.Body = ui.Node(panel, "内容", 36.5f, 96.3f, W, 332);
            screen.Body.localScale = new Vector3(.95f, .95f, 1);
            screen.Status = ui.Text(panel, "本地规则 · 保存游戏时写入当前槽位", 24, 432, 652, 34, 15, SocialUi.PaperInk);
            if (!界面窗口管理器.注册运行时窗口(root))
            { Destroy(root); Debug.LogWarning("社交窗口无法接入导航管理器"); return null; }
            screens.Add(screen); return screen;
        }
        public void Open(string requestedPage)
        {
            if (!Ready) return;
            page = new[] { "好友", "私聊", "军团", "师徒", "结拜" }.Contains(requestedPage) ? requestedPage : "好友";
            if (home == null) home = Screen("社交", RenderHome);
            Show(home);
        }
        private void Tabs(Transform parent, string[] names, string current, Action<string> choose, float y = 0)
        {
            float width = (W - (names.Length - 1) * 8) / names.Length;
            var container = ui.Node(parent, "页签", 0, y, W, 34);
            var layout = container.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(width, 34); layout.spacing = new Vector2(8, 0);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft; layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                string value = names[i];
                var button = ui.Button(container, value, 0, 0, width, 34, () => choose(value));
                if (value == current) button.GetComponent<Image>().color = new Color(.70f, 1, .85f);
            }
        }
        private void RenderHome(SocialScreen screen)
        {
            screen.Title.text = "社交 · " + page;
            Tabs(screen.Body, new[] { "好友", "私聊", "军团", "师徒", "结拜" }, page, selected => { page = selected; screen.Refresh(); });
            var body = ui.Node(screen.Body, page, 0, 43, W, 289);
            if (page == "好友") RenderFriends(screen, body);
            else if (page == "私聊") RenderConversations(screen, body);
            else if (page == "军团") RenderGuilds(screen, body);
            else RenderRelations(screen, body, page == "师徒" ? RelationKind.Mentor : RelationKind.Brotherhood);
        }
        private void Apply(SocialScreen screen, SocialCommand command)
        { screen.Feedback(adapter.Execute(command)); }
        private void ShowProfile(string id)
        {
            targetId = id;
            if (profile == null) profile = Screen("玩家名片", RenderProfile);
            Show(profile);
        }
        private void RenderFriends(SocialScreen screen, Transform body)
        {
            var s = State;
            Tabs(body, new[] { "联系人", "好友", "申请", "黑名单" }, friendFilter, selected => { friendFilter = selected; screen.Refresh(); });
            if (friendFilter == "申请")
            {
                var list = ui.List(body, 0, 42, W, 247);
                var requests = s.FriendRequests.Where(x => x.From == Me || x.To == Me).Reverse().ToList();
                foreach (var request in requests)
                {
                    var f = request;
                    var peer = f.From == Me ? f.To : f.From;
                    var row = list.Row((f.To == Me ? "收到：" : "申请：") + Name(s, peer), RequestLabel(f.State) + "  " + f.Note, 72, 390);
                    ui.Button(row, "名片", 410, 4, 70, 32, () => ShowProfile(peer));
                    if (f.State == RequestState.Pending)
                    {
                        if (f.To == Me)
                        {
                            ui.Button(row, "接受", 490, 4, 74, 32, () => Confirm("接受 " + Name(s, peer) + " 的好友申请？", new SocialCommand { Kind = SocialCommandKind.AnswerFriend, Entity = f.Id, Accept = true }));
                            ui.Button(row, "拒绝", 574, 4, 74, 32, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.AnswerFriend, Entity = f.Id }));
                        }
                        else ui.Button(row, "撤销", 490, 4, 158, 32, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.CancelFriend, Entity = f.Id }));
                    }
                }
                if (requests.Count == 0) list.Empty("没有待处理申请", "在联系人名片中申请好友。离线申请不会送达对方，也不会自动通过。");
                return;
            }
            var input = ui.Input(body, "按称呼或 ID 查找", 0, 43, 435, 34, 48, search);
            input.onValueChanged.AddListener(v => search = v);
            ui.Button(body, "查找", 443, 43, 80, 34, () => screen.Refresh());
            ui.Button(body, "登记联系人", 531, 43, 129, 34, ShowContactForm);
            var people = s.Players.Where(p => p.Id != Me && (p.Name.Contains(search) || p.Id.Contains(search)) &&
                (friendFilter != "好友" || IsFriend(s, p.Id)) && (friendFilter != "黑名单" || IsBlocked(s, p.Id))).ToList();
            var rows = ui.List(body, 0, 84, W, 205);
            foreach (var p in people)
            {
                string id = p.Id;
                var row = rows.Row(p.Name + (IsFriend(s, id) ? " · 好友" : ""), "ID " + id + "  · " + (IsBlocked(s, id) ? "已拉黑" : "离线联系人 · 身份未核验"), 62, 395);
                ui.Button(row, "名片", 408, 13, 112, 34, () => ShowProfile(id));
                ui.Button(row, friendFilter == "黑名单" ? "移出" : "私聊", 530, 13, 118, 34,
                    () => { if (friendFilter == "黑名单") Apply(screen, new SocialCommand { Kind = SocialCommandKind.Unblock, Target = id }); else ShowConversation(id); });
            }
            if (people.Count == 0) rows.Empty(search.Length > 0 ? "没有匹配的联系人" : "尚未登记此类联系人", "点击“登记联系人”输入对方 ID 与称呼；世界 NPC 不会出现在这里。后续联网由服务端提供真人名片。");
        }
        private void ShowContactForm()
        {
            editName = editNotice = "";
            if (form == null) form = Screen("登记联系人", RenderContactForm);
            if (form == null) return;
            form.Render = RenderContactForm; Show(form);
        }
        private void RenderContactForm(SocialScreen screen)
        {
            screen.Title.text = "登记离线联系人";
            ui.Text(screen.Body, "登记用于记录申请与私聊草稿。\n不会确认对方身份、添加在线玩家或冒充对方回复。", 12, 8, 630, 64, 18, SocialUi.Muted);
            ui.Text(screen.Body, "联系人 ID", 12, 88, 130, 34, 18, SocialUi.Cyan);
            var id = ui.Input(screen.Body, "字母/数字/_/-，最多 48 位", 146, 88, 480, 36, 48, contactId);
            id.onValueChanged.AddListener(v => contactId = v);
            ui.Text(screen.Body, "本地称呼", 12, 137, 130, 34, 18, SocialUi.Cyan);
            var name = ui.Input(screen.Body, "最多 20 字", 146, 137, 480, 36, 20, contactName);
            name.onValueChanged.AddListener(v => contactName = v);
            ui.Text(screen.Body, "当前本机 ID：" + Me, 12, 190, 620, 30, 16, SocialUi.Muted);
            ui.Button(screen.Body, "登记", 430, 255, 196, 40, () =>
            {
                var result = adapter.Execute(new SocialCommand { Kind = SocialCommandKind.RegisterContact, Target = id.text.Trim(), Name = name.text.Trim() });
                screen.Feedback(result);
                if (result.Succeeded) { contactId = contactName = ""; screen.gameObject.SetActive(false); ShowProfile(result.EntityId); }
            });
        }
        private void RenderProfile(SocialScreen screen)
        {
            var s = State; var p = s.Players.FirstOrDefault(x => x.Id == targetId);
            if (p == null) { ui.Text(screen.Body, "联系人已失效，请返回刷新", 12, 12, 630, 60); return; }
            screen.Title.text = "名片 · " + p.Name;
            var sprite = ui.Avatar;
            ui.Image(screen.Body, "头像", 12, 8, 64, 70, sprite, sprite == null ? SocialUi.Green : Color.white);
            ui.Text(screen.Body, p.Name, 92, 3, 550, 32, 21);
            ui.Text(screen.Body, "ID " + p.Id, 92, 35, 550, 27, 15, SocialUi.Muted);
            ui.Text(screen.Body, p.Id == Me ? "本机角色 · 等级 " + p.Level + " · " + p.Country :
                (p.Verified && adapter.IsConnected ? "服务器核验角色 · 等级 " + p.Level + " · " + p.Country : "离线联系人 · 等级/国家待核验"), 92, 63, 550, 48, 16, SocialUi.Muted);
            ui.Text(screen.Body, "关系：" + (IsBlocked(s, p.Id) ? "黑名单" : IsFriend(s, p.Id) ? "好友" : "未建立好友关系"), 12, 115, 620, 30, 18, SocialUi.Cyan);
            var note = ui.Input(screen.Body, "好友申请附言，最多 60 字", 12, 151, 632, 36, 60, requestNote);
            note.onValueChanged.AddListener(v => requestNote = v);
            ui.Button(screen.Body, IsFriend(s, p.Id) ? "解除好友" : "申请好友", 12, 204, 194, 38, () =>
            {
                if (IsFriend(State, targetId)) Confirm("解除与 " + p.Name + " 的好友关系？", new SocialCommand { Kind = SocialCommandKind.RemoveFriend, Target = p.Id });
                else Apply(screen, new SocialCommand { Kind = SocialCommandKind.RequestFriend, Target = p.Id, Text = note.text.Trim() });
            }, p.Id != Me);
            ui.Button(screen.Body, "私聊会话", 230, 204, 194, 38, () => ShowConversation(p.Id), p.Id != Me && !IsBlocked(s, p.Id));
            ui.Button(screen.Body, IsBlocked(s, p.Id) ? "移出黑名单" : "拉入黑名单", 450, 204, 194, 38, () => Confirm(
                IsBlocked(State, p.Id) ? "移出黑名单？好友关系需重新申请。" : "拉黑 " + p.Name + "？好友关系及待确认邀请将取消。",
                new SocialCommand { Kind = IsBlocked(State, p.Id) ? SocialCommandKind.Unblock : SocialCommandKind.Block, Target = p.Id }), p.Id != Me);
            ui.Text(screen.Body, "本地申请需要接收方身份确认。当前无网络，\n未送达的申请可在“好友 → 申请”撤销。", 12, 251, 632, 70, 17, SocialUi.Muted);
        }
        private void RenderConversations(SocialScreen screen, Transform body)
        {
            var s = State;
            ui.Text(body, "选择私聊对象", 0, 0, 260, 32, 19, SocialUi.Cyan);
            ui.Toggle(body, "仅看好友", 430, 0, 218, onlyFriends, value => { onlyFriends = value; screen.Refresh(); });
            var list = ui.List(body, 0, 40, W, 249);
            var players = s.Players.Where(p => p.Id != Me && !IsBlocked(s, p.Id) && (!onlyFriends || IsFriend(s, p.Id))).ToList();
            foreach (var p in players)
            {
                string id = p.Id;
                var draft = s.Drafts.FirstOrDefault(x => x.Owner == Me && x.Target == id);
                var row = list.Row(p.Name, draft != null && !string.IsNullOrEmpty(draft.Text) ? "有未发送草稿" : "离线 · 尚无已发送消息", 64, 410);
                ui.Button(row, "名片", 432, 14, 92, 34, () => ShowProfile(id));
                ui.Button(row, "会话", 536, 14, 112, 34, () => ShowConversation(id));
            }
            if (players.Count == 0)
            {
                list.Empty("没有可选的私聊对象", "先在好友页登记联系人。黑名单联系人不可发送，离线时可编辑并保存草稿。");
                ui.Button(body, "登记联系人", 416, 177, 232, 38, ShowContactForm);
            }
        }
        private void ShowConversation(string id)
        {
            targetId = id;
            if (conversationTarget != id)
            {
                conversationTarget = id;
                var draft = State.Drafts.FirstOrDefault(x => x.Owner == Me && x.Target == id);
                conversationText = draft == null ? "" : draft.Text;
            }
            if (conversation == null) conversation = Screen("私聊会话", RenderConversation);
            Show(conversation);
        }
        private void RenderConversation(SocialScreen screen)
        {
            var s = State; string id = conversationTarget; screen.Title.text = "私聊 · " + Name(s, id);
            ui.Text(screen.Body, "联系人：" + Name(s, id), 0, 0, W, 28, 18, SocialUi.Cyan);
            ui.Text(screen.Body, "离线发送不会成功。草稿不会自动发送，联网后需手动确认。", 0, 28, W, 40, 16, SocialUi.Muted);
            var list = ui.List(screen.Body, 0, 73, W, 163);
            var messages = s.Messages.Where(x => (x.From == Me && x.To == id) || (x.To == Me && x.From == id)).Reverse().Take(60).Reverse().ToList();
            foreach (var message in messages)
            {
                string state = adapter.IsConnected ? (message.Delivery == MessageDelivery.Failed ? "发送失败" : message.Delivery == MessageDelivery.Received ? "收到" : "已发送") : "导入历史 · 非当前送达";
                list.Row(Name(s, message.From) + " · " + state, message.Text, 96, W - 24);
            }
            if (messages.Count == 0) list.Empty("没有已发送消息", "此会话不会生成对方回复。可以在下方编辑内容并保存未发送草稿。");
            var input = ui.Input(screen.Body, "私聊内容，最多 200 字", 0, 246, 490, 36, 200, conversationText);
            input.onValueChanged.AddListener(v => conversationText = v);
            ui.Button(screen.Body, "保存草稿", 500, 246, 160, 36, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.SaveDraft, Target = id, Text = input.text }));
            ui.Button(screen.Body, adapter.IsConnected ? "发送" : "尝试发送", 500, 291, 160, 36, () =>
            {
                var result = adapter.Execute(new SocialCommand { Kind = SocialCommandKind.SendPrivate, Target = id, Text = input.text.Trim() });
                screen.Feedback(result);
                if (result.Succeeded) { conversationText = ""; screen.Refresh(); }
            });
            ui.Text(screen.Body, "ID " + id, 0, 293, 482, 30, 15, SocialUi.Muted);
        }
        private void RenderGuilds(SocialScreen screen, Transform body)
        {
            var s = State; var mine = s.Guilds.FirstOrDefault(x => x.Members.Contains(Me));
            ui.Text(body, mine == null ? "尚未加入军团" : "我的军团：" + mine.Name, 0, 0, 420, 34, 19, SocialUi.Cyan);
            ui.Button(body, mine == null ? "创建军团" : "军团详情", 440, 0, 220, 34, () => { if (mine == null) ShowGuildForm(); else ShowGuild(mine.Id); });
            var list = ui.List(body, 0, 44, W, 245);
            foreach (var g in s.Guilds)
            {
                string id = g.Id;
                var row = list.Row(g.Name + "  " + g.Members.Count + "/5", "团长：" + Name(s, g.Leader) + " · 本地军团", 64, 415);
                ui.Button(row, "详情", 432, 14, 92, 34, () => ShowGuild(id));
                ui.Button(row, "申请", 536, 14, 112, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.ApplyGuild, Entity = id }), mine == null && g.Members.Count < LocalSocialAdapter.MemberLimit);
            }
            foreach (var a in s.GuildApplications.Where(x => x.Applicant == Me && x.State == RequestState.Pending))
            {
                var app = a; var group = s.Guilds.FirstOrDefault(x => x.Id == app.Guild);
                var row = list.Row("待入团：" + (group == null ? "失效军团" : group.Name), "申请尚未送达团长", 64, 480);
                ui.Button(row, "撤销申请", 530, 14, 118, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.CancelGuild, Entity = app.Id }));
            }
            if (s.Guilds.Count == 0) list.Empty("本世界还没有军团记录", "可创建本地军团、编辑公告并管理真实的本地成员记录。人数上限为 5；没有入团奖励或网络创建成功提示。");
        }
        private void ShowGuildForm()
        {
            editName = editNotice = "";
            if (form == null) form = Screen("创建军团", RenderGuildForm);
            if (form == null) return;
            form.Render = RenderGuildForm; Show(form);
        }
        private void RenderGuildForm(SocialScreen screen)
        {
            screen.Title.text = "创建本地军团";
            ui.Text(screen.Body, "本地规则：每人加入一个军团，最多 5 人。\n创建不扣游戏货币，也不授予原版奖励；当前未建立服务器军团。", 12, 4, 632, 75, 17, SocialUi.Muted);
            ui.Text(screen.Body, "军团名称", 12, 93, 126, 36, 18, SocialUi.Cyan);
            var name = ui.Input(screen.Body, "1–12 字", 144, 93, 488, 36, 12, editName); name.onValueChanged.AddListener(v => editName = v);
            ui.Text(screen.Body, "军团公告", 12, 144, 126, 36, 18, SocialUi.Cyan);
            var notice = ui.Input(screen.Body, "最多 120 字", 144, 144, 488, 36, 120, editNotice); notice.onValueChanged.AddListener(v => editNotice = v);
            ui.Button(screen.Body, "确认创建", 430, 250, 202, 40, () =>
            {
                var result = adapter.Execute(new SocialCommand { Kind = SocialCommandKind.CreateGuild, Name = name.text.Trim(), Text = notice.text.Trim() });
                screen.Feedback(result);
                if (result.Succeeded) { screen.gameObject.SetActive(false); ShowGuild(result.EntityId); }
            });
        }
        private void ShowGuild(string id)
        {
            guildId = id;
            if (guild == null) guild = Screen("军团详情", RenderGuild);
            Show(guild);
        }
        private void RenderGuild(SocialScreen screen)
        {
            var s = State; var g = s.Guilds.FirstOrDefault(x => x.Id == guildId);
            if (g == null) { ui.Text(screen.Body, "军团已解散，请返回军团列表", 12, 15, 630, 60); return; }
            screen.Title.text = g.Name + " · " + g.Members.Count + "/5";
            bool leader = g.Leader == Me; bool member = g.Members.Contains(Me);
            ui.Text(screen.Body, "军团：" + g.Name, 0, 0, W, 28, 18, SocialUi.Cyan);
            ui.Text(screen.Body, "团长：" + Name(s, g.Leader) + "  · " + (leader ? "你可审批、踢人、转让" : member ? "你可退出本团" : "你尚未加入本团"), 0, 28, W, 32, 17, SocialUi.Cyan);
            var notice = ui.Input(screen.Body, "军团公告", 0, 64, 430, 34, 120, g.Notice); notice.interactable = leader;
            ui.Button(screen.Body, "查看公告", 440, 64, 100, 34, () => ShowNotice(g.Id));
            ui.Button(screen.Body, "保存公告", 550, 64, 110, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.UpdateGuild, Entity = g.Id, Text = notice.text.Trim() }), leader);
            var list = ui.List(screen.Body, 0, 108, W, 172);
            foreach (var id in g.Members)
            {
                string peer = id;
                var row = list.Row(Name(s, id) + (id == g.Leader ? " · 团长" : " · 成员"), "ID " + id + " · 离线记录", 62, 390);
                ui.Button(row, "名片", 400, 14, 70, 34, () => ShowProfile(peer));
                if (leader && id != Me)
                {
                    ui.Button(row, "转让", 480, 14, 78, 34, () => Confirm("将团长转让给 " + Name(s, peer) + "？你将失去管理权限。", new SocialCommand { Kind = SocialCommandKind.TransferGuild, Entity = g.Id, Target = peer }));
                    ui.Button(row, "移出", 570, 14, 78, 34, () => Confirm("将 " + Name(s, peer) + " 移出军团？", new SocialCommand { Kind = SocialCommandKind.KickGuild, Entity = g.Id, Target = peer }));
                }
            }
            if (leader)
                foreach (var app in s.GuildApplications.Where(x => x.Guild == g.Id && x.State == RequestState.Pending))
                {
                    var a = app; var row = list.Row("入团申请：" + Name(s, a.Applicant), "本地待确认 · " + a.Note, 72, 420);
                    ui.Button(row, "批准", 480, 14, 78, 34, () => Confirm("批准 " + Name(s, a.Applicant) + " 入团？", new SocialCommand { Kind = SocialCommandKind.AnswerGuild, Entity = a.Id, Accept = true }));
                    ui.Button(row, "拒绝", 570, 14, 78, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.AnswerGuild, Entity = a.Id }));
                }
            ui.Button(screen.Body, member ? leader ? "解散军团" : "退出军团" : "申请入团", 440, 291, 220, 36, () =>
            {
                if (member) Confirm(leader ? "解散军团？全部成员及待入团申请将解除。" : "退出当前军团？", new SocialCommand { Kind = leader ? SocialCommandKind.DissolveGuild : SocialCommandKind.LeaveGuild, Entity = g.Id });
                else Apply(screen, new SocialCommand { Kind = SocialCommandKind.ApplyGuild, Entity = g.Id });
            });
        }
        private void ShowNotice(string id)
        {
            guildId = id;
            if (noticeScreen == null) noticeScreen = Screen("军团公告", screen =>
            {
                var s = State; var g = s.Guilds.FirstOrDefault(x => x.Id == guildId);
                if (g == null) { ui.Text(screen.Body, "军团已解散，请返回", 12, 12, 630, 60); return; }
                screen.Title.text = "公告 · " + g.Name;
                ui.Text(screen.Body, "军团：" + g.Name + "\n团长：" + Name(s, g.Leader), 12, 8, 632, 60, 18, SocialUi.Cyan);
                ui.Text(screen.Body, string.IsNullOrEmpty(g.Notice) ? "尚未发布公告。团长可在详情中编辑并保存。" : g.Notice,
                    12, 80, 632, 224, 18, SocialUi.Ink, TextAnchor.UpperLeft);
            });
            Show(noticeScreen);
        }
        private void RenderRelations(SocialScreen screen, Transform body, RelationKind kind)
        {
            var s = State;
            Tabs(body, new[] { "关系", "邀请", "发起" }, relationFilter, selected => { relationFilter = selected; screen.Refresh(); });
            if (relationFilter == "发起") { RenderInvite(screen, body, kind, s); return; }
            var list = ui.List(body, 0, 44, W, 245);
            if (relationFilter == "邀请")
            {
                var invitations = s.Invitations.Where(x => x.Kind == kind && (x.From == Me || x.To == Me)).Reverse().ToList();
                foreach (var invitation in invitations)
                {
                    var inv = invitation; var peer = inv.From == Me ? inv.To : inv.From;
                    string label = kind == RelationKind.Mentor ? "师父：" + Name(s, inv.Mentor) + " / 徒弟：" + Name(s, inv.Apprentice) : "结拜：" + inv.Name;
                    var row = list.Row((inv.To == Me ? "收到 " : "发给 ") + Name(s, peer), label + "\n" + RequestLabel(inv.State), 88, 400);
                    ui.Button(row, "名片", 410, 10, 70, 34, () => ShowProfile(peer));
                    if (inv.State == RequestState.Pending)
                    {
                        if (inv.To == Me)
                        {
                            ui.Button(row, "接受", 490, 10, 74, 34, () => Confirm("确认与 " + Name(s, peer) + " 建立" + page + "关系？", new SocialCommand { Kind = SocialCommandKind.AnswerRelation, Entity = inv.Id, Accept = true }));
                            ui.Button(row, "拒绝", 574, 10, 74, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.AnswerRelation, Entity = inv.Id }));
                        }
                        else ui.Button(row, "撤销", 490, 10, 158, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.CancelRelation, Entity = inv.Id }));
                    }
                }
                if (invitations.Count == 0) list.Empty("没有关系邀请", "在“发起”页从好友选择对象。邀请需接收方确认，离线不会自动建立关系。");
                return;
            }
            if (kind == RelationKind.Mentor)
            {
                var bonds = s.Mentors.Where(x => x.Mentor == Me || x.Apprentice == Me).ToList();
                foreach (var bond in bonds)
                {
                    var m = bond; var peer = m.Mentor == Me ? m.Apprentice : m.Mentor;
                    var row = list.Row((m.Mentor == Me ? "徒弟：" : "师父：") + Name(s, peer), "本地已确认关系 · ID " + peer, 70, 418);
                    ui.Button(row, "名片", 430, 14, 92, 34, () => ShowProfile(peer));
                    ui.Button(row, "解除", 534, 14, 114, 34, () => Confirm("解除与 " + Name(s, peer) + " 的师徒关系？", new SocialCommand { Kind = SocialCommandKind.LeaveMentor, Entity = m.Id }));
                }
                if (bonds.Count == 0) list.Empty("尚未建立师徒关系", "本地规则：一位师父、最多三位徒弟；师父等级高于徒弟，双方须为好友。没有出师奖励或经验加成。");
            }
            else
            {
                var g = s.Brotherhoods.FirstOrDefault(x => x.Members.Contains(Me));
                if (g == null) { list.Empty("尚未结拜", "本地规则：先成为好友，发起人邀请、对方确认后建立；最多五人。少于两人自动解除，不发放结拜奖励。"); return; }
                list.Row(g.Name + "  " + g.Members.Count + "/5", "发起人：" + Name(s, g.Leader) + " · 本地记录", 62, W - 24);
                foreach (var id in g.Members)
                {
                    string peer = id;
                    var row = list.Row(Name(s, id), "ID " + id, 62, 390);
                    ui.Button(row, "名片", 400, 13, 70, 34, () => ShowProfile(peer));
                    if (g.Leader == Me && peer != Me)
                    {
                        ui.Button(row, "转让", 480, 13, 78, 34, () => Confirm("转让结拜发起人给 " + Name(s, peer) + "？", new SocialCommand { Kind = SocialCommandKind.TransferBrother, Entity = g.Id, Target = peer }));
                        ui.Button(row, "移出", 570, 13, 78, 34, () => Confirm("将 " + Name(s, peer) + " 移出结拜？", new SocialCommand { Kind = SocialCommandKind.KickBrother, Entity = g.Id, Target = peer }));
                    }
                }
                var exit = list.Row("退出结拜", "发起人多于两人时需先转让，少于两人时解除关系", 74, 474);
                ui.Button(exit, "退出", 530, 15, 118, 34, () => Confirm("退出结拜？成员少于两人时结拜自动解除。", new SocialCommand { Kind = SocialCommandKind.LeaveBrother, Entity = g.Id }));
            }
        }
        private void RenderInvite(SocialScreen screen, Transform body, RelationKind kind, SocialStateDto s)
        {
            string rule = kind == RelationKind.Mentor ? "本地规则：师父等级高于徒弟，最多三位徒弟；需要好友确认。" : "本地规则：结拜最多五人，已有结拜仅发起人可邀请。";
            ui.Text(body, rule, 0, 43, W, 38, 16, SocialUi.Muted);
            float top = 85;
            if (kind == RelationKind.Brotherhood)
            {
                var input = ui.Input(body, "新结拜名称，1–12 字", 0, 84, W, 34, 12, brotherName);
                input.onValueChanged.AddListener(v => brotherName = v); top = 126;
            }
            var list = ui.List(body, 0, top, W, 289 - top);
            var friends = s.Players.Where(p => p.Id != Me && IsFriend(s, p.Id) && !IsBlocked(s, p.Id)).ToList();
            foreach (var p in friends)
            {
                string peer = p.Id;
                var row = list.Row(p.Name, p.Verified ? "等级 " + p.Level : "身份及等级待核验 · 离线联系人", 66, 370);
                if (kind == RelationKind.Mentor)
                {
                    ui.Button(row, "拜师", 404, 14, 116, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.InviteMentor, Target = peer }));
                    ui.Button(row, "收徒", 532, 14, 116, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.InviteApprentice, Target = peer }));
                }
                else ui.Button(row, "邀请结拜", 482, 14, 166, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.InviteBrother, Target = peer, Name = brotherName.Trim() }));
            }
            if (friends.Count == 0) list.Empty("没有可邀请的好友", "先在好友页申请并等待接收方确认；登记联系人不等于已经成为好友。");
        }
        private void Confirm(string text, SocialCommand command, Action<SocialResult> after = null)
        {
            confirmText = text; confirmCommand = command; confirmed = after;
            if (confirmation == null) confirmation = Screen("确认操作", RenderConfirmation);
            Show(confirmation);
        }
        private void RenderConfirmation(SocialScreen screen)
        {
            ui.Text(screen.Body, confirmText, 22, 40, 616, 110, 21, SocialUi.Ink, TextAnchor.MiddleCenter);
            ui.Text(screen.Body, adapter.IsConnected ? "操作结果以服务器返回为准。" : "当前离线，确认只改变本机关系记录。", 22, 163, 616, 54, 17, SocialUi.Muted, TextAnchor.MiddleCenter);
            ui.Button(screen.Body, "取消", 84, 261, 218, 42, () => screen.gameObject.SetActive(false));
            ui.Button(screen.Body, "确认", 360, 261, 218, 42, () =>
            {
                var result = adapter.Execute(confirmCommand); screen.Feedback(result);
                if (!result.Succeeded) return;
                screen.gameObject.SetActive(false);
                if (confirmed != null) confirmed(result);
            });
        }
    }
}
