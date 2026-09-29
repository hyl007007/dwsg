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
        private bool manualContact;
        private string roleSearch = "", privateSearch = "", noticeDraftId;
        private int conversationLimit = 60;
        private SocialScreen returnScreen;
        private Action returnToChat;
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
            manualContact = false; roleSearch = privateSearch = ""; noticeDraftId = null;
            returnScreen = null; returnToChat = null; conversationLimit = 60;
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
        { return state == RequestState.Pending ? "待确认" : state == RequestState.Accepted ? "已确认" : state == RequestState.Declined ? "已拒绝" : "已撤销"; }
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
            ui.Shell(panel, title, () =>
            {
                if (screen == returnScreen)
                {
                    var returned = returnToChat; returnToChat = null; returnScreen = null;
                    if (returned != null) { 界面窗口管理器.关闭当前场景窗口(); returned(); return; }
                }
                screen.gameObject.SetActive(false);
            }, out screen.Title);
            ui.Text(panel, adapter.ConnectionStatus, 24, 46, 652, 30, 15, SocialUi.PaperInk);
            screen.Body = ui.Node(panel, "内容", 36.5f, 96.3f, W, 332);
            screen.Body.localScale = new Vector3(.95f, .95f, 1);
            screen.Status = ui.Text(panel, "", 24, 432, 652, 34, 15, SocialUi.PaperInk);
            if (!界面窗口管理器.注册运行时窗口(root))
            { Destroy(root); Debug.LogWarning("社交窗口无法接入导航管理器"); return null; }
            screens.Add(screen); return screen;
        }
        public void Open(string requestedPage)
        {
            if (!Ready) return;
            returnScreen = null; returnToChat = null;
            page = new[] { "好友", "私聊", "军团", "师徒", "结拜" }.Contains(requestedPage) ? requestedPage : "好友";
            if (home == null) home = Screen("社交", RenderHome);
            Show(home);
        }
        internal void ReturnToChatOnClose(Action returned) { returnScreen = home; returnToChat = returned; }
        public bool OpenContact(string id, Action returned = null)
        {
            if (!Ready || (State.Players.All(p => p.Id != id) && WorldRole(id) == null)) return false;
            ShowProfile(id); returnScreen = profile; returnToChat = returned; return true;
        }
        private void ReturnToContacts()
        {
            search = ""; ReturnToHome("好友", "联系人");
        }
        private void ReturnToHome(string requestedPage, string requestedFilter = null)
        {
            page = requestedPage;
            if (requestedFilter != null) friendFilter = requestedFilter;
            if (home == null) home = Screen("社交", RenderHome);
            if (returnToChat != null) returnScreen = home;
            Show(home);
        }
        private List<SocialPlayerDto> WorldRoles()
        {
            var result = new List<SocialPlayerDto>();
            if (!(adapter is LocalSocialAdapter) || 全局变量.所有玩家数据表 == null) return result;
            var seen = new HashSet<string>();
            foreach (var player in 全局变量.所有玩家数据表)
            {
                var info = player == null ? null : player.基础信息;
                if (info == null) continue;
                string id = "local-" + info.ID;
                if (id == Me || !seen.Add(id) || !LocalSocialAdapter.ValidName(info.名字, 20)) continue;
                result.Add(new SocialPlayerDto { Id = id, Name = info.名字, IsNpc = true, Portrait = Mathf.Max(0, info.头像),
                    Level = Mathf.Clamp(Mathf.FloorToInt(info.等级), 1, 999), Country = string.IsNullOrEmpty(info.国家) ? "无" : info.国家 });
            }
            return result;
        }
        private SocialPlayerDto WorldRole(string id) { return WorldRoles().FirstOrDefault(p => p.Id == id); }
        private static string RoleDescription(SocialPlayerDto p)
        { return p.IsNpc ? "NPC · " + p.Country + " · " + p.Level + "级" : p.Verified ? p.Country + " · " + p.Level + "级" : "手动联系人"; }
        private void Tabs(Transform parent, string[] names, string current, Action<string> choose, float y = 0)
        {
            ui.Tabs(parent, names, current, choose, W, y);
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
                if (requests.Count == 0) list.Empty("没有好友申请", "从联系人名片发起申请，或在这里处理收到的申请。");
                return;
            }
            var input = ui.Input(body, "按称呼或编号查找", 0, 43, 435, 34, 48, search);
            input.onValueChanged.AddListener(v => search = v);
            ui.Button(body, "查找", 443, 43, 80, 34, () => screen.Refresh());
            ui.Button(body, "添加联系人", 531, 43, 129, 34, ShowContactForm);
            var people = s.Players.Where(p => p.Id != Me && (p.Name.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 || p.Id.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) &&
                (friendFilter != "好友" || IsFriend(s, p.Id)) && (friendFilter != "黑名单" || IsBlocked(s, p.Id))).ToList();
            var rows = ui.List(body, 0, 84, W, 205);
            foreach (var p in people)
            {
                string id = p.Id;
                string relationship = IsBlocked(s, id) ? "已屏蔽 · " : IsFriend(s, id) ? "好友 · " : "";
                var row = rows.Row(p.Name, relationship + RoleDescription(p), 62, 395);
                ui.Button(row, "名片", 408, 13, 112, 34, () => ShowProfile(id));
                ui.Button(row, IsBlocked(s, id) ? "取消屏蔽" : p.IsNpc ? "相关消息" : "私聊", 530, 13, 118, 34,
                    () => { if (IsBlocked(s, id)) Apply(screen, new SocialCommand { Kind = SocialCommandKind.Unblock, Target = id });
                        else if (p.IsNpc) ShowRoleMessages(p); else ShowConversation(id); });
            }
            if (people.Count == 0) rows.Empty(search.Length > 0 ? "没有匹配的联系人" : "还没有这类联系人", "点击“添加联系人”，从本世界角色选择，或添加手动联系人。");
        }
        private void ShowContactForm()
        {
            manualContact = !(adapter is LocalSocialAdapter); roleSearch = "";
            if (form == null) form = Screen("登记联系人", RenderContactForm);
            if (form == null) return;
            form.Render = RenderContactForm; Show(form);
        }
        private void RenderContactForm(SocialScreen screen)
        {
            screen.Title.text = "添加联系人";
            Tabs(screen.Body, new[] { "本世界角色", "手动联系人" }, manualContact ? "手动联系人" : "本世界角色",
                selected => { manualContact = selected == "手动联系人"; screen.Refresh(); });
            if (!manualContact)
            {
                var searchInput = ui.Input(screen.Body, "按角色称呼查找", 12, 44, 460, 34, 20, roleSearch);
                searchInput.onValueChanged.AddListener(value => roleSearch = value);
                ui.Button(screen.Body, "查找", 482, 44, 150, 34, () => screen.Refresh());
                var list = ui.List(screen.Body, 12, 88, 636, 244);
                var roles = WorldRoles().Where(p => p.Name.IndexOf(roleSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                var saved = State;
                foreach (var role in roles)
                {
                    var selected = role;
                    var row = list.Row(role.Name, RoleDescription(role), 68, 420);
                    bool exists = saved.Players.Any(p => p.Id == role.Id);
                    ui.Button(row, exists ? "查看" : "添加联系人", 468, 14, 156, 34, () =>
                    {
                        var local = adapter as LocalSocialAdapter;
                        if (local == null) return;
                        var result = local.RegisterWorldRole(selected); screen.Feedback(result);
                        if (result.Succeeded) { screen.gameObject.SetActive(false); ShowProfile(selected.Id); }
                    });
                }
                if (roles.Count == 0) list.Empty("没有匹配的本世界角色", "清空查找条件重试；手动联系人可在另一页添加。");
                return;
            }
            ui.Text(screen.Body, "填写对方提供的编号与称呼。", 12, 42, 630, 34, 18, SocialUi.Muted);
            ui.Text(screen.Body, "联系人编号", 12, 88, 130, 34, 18, SocialUi.Cyan);
            var id = ui.Input(screen.Body, "字母/数字/_/-，最多 48 位", 146, 88, 480, 36, 48, contactId);
            id.onValueChanged.AddListener(v => contactId = v);
            ui.Text(screen.Body, "备注称呼", 12, 137, 130, 34, 18, SocialUi.Cyan);
            var name = ui.Input(screen.Body, "最多 20 字", 146, 137, 480, 36, 20, contactName);
            name.onValueChanged.AddListener(v => contactName = v);
            ui.Text(screen.Body, "自己的编号：" + Me, 12, 190, 620, 30, 16, SocialUi.Muted);
            ui.Button(screen.Body, "添加", 430, 255, 196, 40, () =>
            {
                var role = WorldRole(id.text.Trim());
                var local = adapter as LocalSocialAdapter;
                var result = role != null && local != null ? local.RegisterWorldRole(role) :
                    adapter.Execute(new SocialCommand { Kind = SocialCommandKind.RegisterContact, Target = id.text.Trim(), Name = name.text.Trim() });
                screen.Feedback(result);
                if (result.Succeeded) { contactId = contactName = ""; screen.gameObject.SetActive(false); ShowProfile(result.EntityId); }
            });
        }
        private void RenderProfile(SocialScreen screen)
        {
            var s = State; var p = s.Players.FirstOrDefault(x => x.Id == targetId);
            if (p != null && p.IsNpc) p = WorldRole(p.Id) ?? p;
            if (p == null) p = WorldRole(targetId);
            if (p == null) { ui.Text(screen.Body, "联系人已移除", 12, 12, 630, 60); ui.Button(screen.Body, "返回联系人", 12, 86, 200, 36, ReturnToContacts); return; }
            screen.Title.text = "名片 · " + p.Name;
            var sprite = p.Id == Me ? ui.Avatar : p.IsNpc && 全局变量.所有头像资源表 != null && p.Portrait < 全局变量.所有头像资源表.Count ? 全局变量.所有头像资源表[p.Portrait] : null;
            ui.Image(screen.Body, "头像", 12, 8, 64, 70, sprite, sprite == null ? SocialUi.Green : Color.white);
            if (sprite == null) ui.Text(screen.Body, "联系人", 12, 8, 64, 70, 16, SocialUi.Muted, TextAnchor.MiddleCenter);
            ui.Text(screen.Body, p.Name, 92, 3, 550, 32, 21);
            ui.Text(screen.Body, "编号 " + p.Id, 92, 35, 550, 27, 15, SocialUi.Muted);
            ui.Text(screen.Body, p.Id == Me ? "我的角色 · " + p.Level + "级 · " + p.Country : RoleDescription(p), 92, 63, 550, 48, 16, SocialUi.Muted);
            var person = p;
            bool registered = s.Players.Any(x => x.Id == p.Id);
            if (p.Id == Me)
            {
                ui.Button(screen.Body, "返回联系人", 12, 286, 194, 36, ReturnToContacts); return;
            }
            if (p.IsNpc)
            {
                ui.Text(screen.Body, "NPC 可加入联系人、屏蔽或查看相关世界播报。", 12, 115, 632, 50, 17, SocialUi.Muted);
                if (!registered) ui.Button(screen.Body, "添加联系人", 12, 190, 194, 38, () =>
                { var local = adapter as LocalSocialAdapter; if (local != null) screen.Feedback(local.RegisterWorldRole(person)); });
                else ui.Button(screen.Body, "移除联系人", 12, 190, 194, 38, () => RemoveContact(person));
                ui.Button(screen.Body, "相关消息", 230, 190, 194, 38, () => ShowRoleMessages(person));
                if (registered) ui.Button(screen.Body, IsBlocked(s, p.Id) ? "取消屏蔽" : "屏蔽", 450, 190, 194, 38, () =>
                    Apply(screen, new SocialCommand { Kind = IsBlocked(State, person.Id) ? SocialCommandKind.Unblock : SocialCommandKind.Block, Target = person.Id }));
                ui.Button(screen.Body, "返回联系人", 12, 286, 194, 36, ReturnToContacts);
                return;
            }
            bool blocked = IsBlocked(s, p.Id), friend = IsFriend(s, p.Id);
            var pending = s.FriendRequests.FirstOrDefault(x => x.State == RequestState.Pending && (x.From == Me && x.To == p.Id || x.To == Me && x.From == p.Id));
            ui.Text(screen.Body, "关系：" + (blocked ? "已屏蔽" : friend ? "好友" : pending != null ? "好友申请待确认" : "联系人"), 12, 115, 620, 30, 18, SocialUi.Cyan);
            if (!blocked && !friend && pending == null)
            {
                var note = ui.Input(screen.Body, "好友申请附言，最多 60 字", 12, 151, 632, 36, 60, requestNote);
                note.onValueChanged.AddListener(v => requestNote = v);
            }
            if (!blocked)
            {
                ui.Button(screen.Body, friend ? "解除好友" : pending != null ? pending.From == Me ? "撤销申请" : "查看申请" : "申请好友", 12, 204, 194, 38, () =>
                {
                    if (friend) Confirm("解除与 " + p.Name + " 的好友关系？", new SocialCommand { Kind = SocialCommandKind.RemoveFriend, Target = p.Id });
                    else if (pending != null && pending.From == Me) Apply(screen, new SocialCommand { Kind = SocialCommandKind.CancelFriend, Entity = pending.Id });
                    else if (pending != null) ReturnToHome("好友", "申请");
                    else Apply(screen, new SocialCommand { Kind = SocialCommandKind.RequestFriend, Target = p.Id, Text = requestNote.Trim() });
                });
                ui.Button(screen.Body, "私聊会话", 230, 204, 194, 38, () => ShowConversation(p.Id));
            }
            ui.Button(screen.Body, blocked ? "取消屏蔽" : "屏蔽", 450, 204, 194, 38, () => Confirm(
                IsBlocked(State, p.Id) ? "移出黑名单？好友关系需重新申请。" : "拉黑 " + p.Name + "？好友关系及待确认邀请将取消。",
                new SocialCommand { Kind = IsBlocked(State, p.Id) ? SocialCommandKind.Unblock : SocialCommandKind.Block, Target = p.Id }), p.Id != Me);
            if (p.Id != Me)
            {
                ui.Button(screen.Body, "移除联系人", 12, 286, 194, 36, () => RemoveContact(person));
                var name = ui.Input(screen.Body, "备注称呼", 230, 252, 280, 34, 20, p.Name);
                ui.Button(screen.Body, "保存称呼", 520, 252, 124, 34, () => Apply(screen,
                    new SocialCommand { Kind = SocialCommandKind.RenameContact, Target = person.Id, Name = name.text.Trim() }));
                ui.Button(screen.Body, "返回联系人", 230, 291, 194, 36, ReturnToContacts);
            }
        }
        private void RemoveContact(SocialPlayerDto person)
        {
            Confirm("移除“" + person.Name + "”？\n其草稿、消息历史和待处理申请也会清除。", new SocialCommand { Kind = SocialCommandKind.RemoveContact, Target = person.Id },
                result => { if (result.Succeeded) { if (conversationTarget == person.Id) { conversationTarget = null; conversationText = ""; } 界面窗口管理器.关闭当前场景窗口(); ReturnToContacts(); } });
        }
        private void ShowRoleMessages(SocialPlayerDto person)
        { 界面窗口管理器.关闭当前场景窗口(); 聊天系统.查看相关消息(person.Id, person.Name); }
        private void RenderConversations(SocialScreen screen, Transform body)
        {
            var s = State;
            ui.Text(body, "选择私聊对象", 0, 0, 260, 32, 19, SocialUi.Cyan);
            ui.Toggle(body, "仅看好友", 430, 0, 218, onlyFriends, value => { onlyFriends = value; screen.Refresh(); });
            var searchInput = ui.Input(body, "按称呼或编号查找", 0, 36, 530, 34, 48, privateSearch);
            searchInput.onValueChanged.AddListener(value => privateSearch = value);
            ui.Button(body, "查找", 540, 36, 120, 34, () => screen.Refresh());
            var list = ui.List(body, 0, 80, W, 209);
            var players = s.Players.Where(p => p.Id != Me && !p.IsNpc && !IsBlocked(s, p.Id) && (!onlyFriends || IsFriend(s, p.Id)) &&
                (p.Name.IndexOf(privateSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 || p.Id.IndexOf(privateSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            foreach (var p in players)
            {
                string id = p.Id;
                var draft = s.Drafts.FirstOrDefault(x => x.Owner == Me && x.Target == id);
                int history = s.Messages.Count(m => (m.From == Me && m.To == id) || (m.From == id && m.To == Me));
                var row = list.Row(p.Name, draft != null && !string.IsNullOrEmpty(draft.Text) ? "有草稿" : history > 0 ? "历史消息 " + history + " 条" : "还没有消息", 64, 410);
                ui.Button(row, "名片", 432, 14, 92, 34, () => ShowProfile(id));
                ui.Button(row, "会话", 536, 14, 112, 34, () => ShowConversation(id));
            }
            if (players.Count == 0)
            {
                list.Empty("没有匹配的私聊对象", "清空查找条件，或添加手动联系人。NPC 的相关播报在聊天页查看。");
                ui.Button(body, "添加联系人", 416, 228, 232, 38, ShowContactForm);
            }
        }
        private void ShowConversation(string id)
        {
            targetId = id;
            if (conversationTarget != id)
            {
                conversationTarget = id;
                conversationLimit = 60;
                var draft = State.Drafts.FirstOrDefault(x => x.Owner == Me && x.Target == id);
                conversationText = draft == null ? "" : draft.Text;
            }
            if (conversation == null) conversation = Screen("私聊会话", RenderConversation);
            Show(conversation);
        }
        private void RenderConversation(SocialScreen screen)
        {
            var s = State; string id = conversationTarget; screen.Title.text = "私聊 · " + Name(s, id);
            if (!s.Players.Any(p => p.Id == id && !p.IsNpc) || IsBlocked(s, id))
            {
                ui.Text(screen.Body, "该联系人已移除或屏蔽", 12, 12, 630, 60);
                ui.Button(screen.Body, "返回联系人", 12, 86, 200, 36, ReturnToContacts); return;
            }
            ui.Text(screen.Body, "联系人：" + Name(s, id), 0, 0, W, 28, 18, SocialUi.Cyan);
            ui.Text(screen.Body, adapter.IsConnected ? "已连接，消息状态以送达结果为准。" : "可编辑、保存或清空草稿。", 0, 28, 450, 40, 16, SocialUi.Muted);
            var list = ui.List(screen.Body, 0, 73, W, 163);
            var history = s.Messages.Where(x => (x.From == Me && x.To == id) || (x.To == Me && x.From == id)).ToList();
            if (history.Count > conversationLimit) ui.Button(screen.Body, "更早消息", 460, 32, 96, 32, () => { conversationLimit += 60; screen.Refresh(); });
            if (conversationLimit > 60) ui.Button(screen.Body, "最新", 564, 32, 96, 32, () => { conversationLimit = 60; screen.Refresh(); });
            var messages = history.Skip(Mathf.Max(0, history.Count - conversationLimit));
            foreach (var message in messages)
            {
                string state = adapter.IsConnected ? (message.Delivery == MessageDelivery.Failed ? "发送失败" : message.Delivery == MessageDelivery.Received ? "收到" : "已发送") : "历史记录";
                list.Row(Name(s, message.From) + " · " + state, message.Text, 96, W - 24);
            }
            if (history.Count == 0) list.Empty("还没有消息", "在下方编辑内容，可保存为草稿。");
            if (conversationLimit == 60) StartCoroutine(ConversationToBottom(list));
            var input = ui.Input(screen.Body, "私聊内容，最多 200 字", 0, 246, 490, 76, 200, conversationText, true);
            input.lineType = InputField.LineType.MultiLineSubmit;
            input.onValueChanged.AddListener(v => conversationText = v);
            ui.Button(screen.Body, "保存草稿", 500, 246, 160, 36, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.SaveDraft, Target = id, Text = input.text }));
            ui.Button(screen.Body, adapter.IsConnected ? "发送" : "尝试发送", 500, 291, 160, 36, () =>
            {
                var result = adapter.Execute(new SocialCommand { Kind = SocialCommandKind.SendPrivate, Target = id, Text = input.text.Trim() });
                screen.Feedback(result);
                if (result.Succeeded) { conversationText = ""; screen.Refresh(); }
            });
        }
        private static System.Collections.IEnumerator ConversationToBottom(SocialList list)
        {
            yield return null;
            if (list.Scroll == null || !list.Scroll.gameObject.activeInHierarchy) yield break;
            Canvas.ForceUpdateCanvases(); list.Scroll.verticalNormalizedPosition = 0;
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
                var row = list.Row(g.Name + "  " + g.Members.Count + "/5", "团长：" + Name(s, g.Leader), 64, 415);
                ui.Button(row, "详情", 432, 14, 92, 34, () => ShowGuild(id));
                ui.Button(row, "申请", 536, 14, 112, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.ApplyGuild, Entity = id }), mine == null && g.Members.Count < LocalSocialAdapter.MemberLimit);
            }
            foreach (var a in s.GuildApplications.Where(x => x.Applicant == Me && x.State == RequestState.Pending))
            {
                var app = a; var group = s.Guilds.FirstOrDefault(x => x.Id == app.Guild);
                var row = list.Row("待入团：" + (group == null ? "失效军团" : group.Name), "待团长确认", 64, 480);
                ui.Button(row, "撤销申请", 530, 14, 118, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.CancelGuild, Entity = app.Id }));
            }
            if (s.Guilds.Count == 0) list.Empty("尚无军团", "点击“创建军团”设置名称和公告，每团最多 5 人。");
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
            screen.Title.text = "创建军团";
            ui.Text(screen.Body, "每人加入一个军团，每团最多 5 人。创建不消耗货币。", 12, 4, 632, 75, 17, SocialUi.Muted);
            ui.Text(screen.Body, "军团名称", 12, 93, 126, 36, 18, SocialUi.Cyan);
            var name = ui.Input(screen.Body, "1–12 字", 144, 93, 488, 36, 12, editName); name.onValueChanged.AddListener(v => editName = v);
            ui.Text(screen.Body, "军团公告", 12, 144, 126, 36, 18, SocialUi.Cyan);
            var notice = ui.Input(screen.Body, "最多 120 字，可换行", 144, 144, 488, 90, 120, editNotice, true); notice.onValueChanged.AddListener(v => editNotice = v);
            ui.Button(screen.Body, "确认创建", 430, 250, 202, 40, () =>
            {
                var result = adapter.Execute(new SocialCommand { Kind = SocialCommandKind.CreateGuild, Name = name.text.Trim(), Text = notice.text });
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
            if (g == null)
            {
                ui.Text(screen.Body, "军团已解散", 12, 15, 630, 60);
                ui.Button(screen.Body, "军团列表", 440, 291, 220, 36, () => ReturnToHome("军团")); return;
            }
            screen.Title.text = g.Name + " · " + g.Members.Count + "/5";
            bool leader = g.Leader == Me; bool member = g.Members.Contains(Me);
            ui.Text(screen.Body, "军团：" + g.Name, 0, 0, W, 28, 18, SocialUi.Cyan);
            ui.Text(screen.Body, "团长：" + Name(s, g.Leader) + "  · " + (leader ? "你可审批、踢人、转让" : member ? "你可退出本团" : "你尚未加入本团"), 0, 28, W, 32, 17, SocialUi.Cyan);
            string summary = string.IsNullOrEmpty(g.Notice) ? "尚无公告" : g.Notice.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
            var preview = ui.Text(screen.Body, summary, 0, 64, 482, 34, 16, SocialUi.Ink);
            SocialScreen.TruncateSingleLine(preview);
            ui.Button(screen.Body, leader ? "编辑公告" : "查看公告", 500, 64, 160, 34, () => ShowNotice(g.Id));
            var list = ui.List(screen.Body, 0, 108, W, 172);
            foreach (var id in g.Members)
            {
                string peer = id;
                var row = list.Row(Name(s, id), (id == g.Leader ? "团长" : "成员") + " · " + (id == Me ? "我" : "联系人"), 62, 390);
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
                    var a = app; var row = list.Row("入团申请：" + Name(s, a.Applicant), "待确认 · " + a.Note, 72, 420);
                    ui.Button(row, "批准", 480, 14, 78, 34, () => Confirm("批准 " + Name(s, a.Applicant) + " 入团？", new SocialCommand { Kind = SocialCommandKind.AnswerGuild, Entity = a.Id, Accept = true }));
                    ui.Button(row, "拒绝", 570, 14, 78, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.AnswerGuild, Entity = a.Id }));
                }
            ui.Button(screen.Body, member ? leader ? "解散军团" : "退出军团" : "申请入团", 440, 291, 220, 36, () =>
            {
                if (member) Confirm(leader ? "解散军团？全部成员及待入团申请将解除。" : "退出当前军团？", new SocialCommand { Kind = leader ? SocialCommandKind.DissolveGuild : SocialCommandKind.LeaveGuild, Entity = g.Id },
                    result => { 界面窗口管理器.关闭当前场景窗口(); ReturnToHome("军团"); });
                else Apply(screen, new SocialCommand { Kind = SocialCommandKind.ApplyGuild, Entity = g.Id });
            });
        }
        private void ShowNotice(string id)
        {
            guildId = id;
            var existing = State.Guilds.FirstOrDefault(x => x.Id == id);
            noticeDraftId = id; editNotice = existing == null ? "" : existing.Notice;
            if (noticeScreen == null) noticeScreen = Screen("军团公告", screen =>
            {
                var s = State; var g = s.Guilds.FirstOrDefault(x => x.Id == guildId);
                if (g == null)
                {
                    ui.Text(screen.Body, "军团已解散", 12, 12, 630, 60);
                    ui.Button(screen.Body, "军团列表", 440, 286, 220, 36, () => ReturnToHome("军团")); return;
                }
                screen.Title.text = "公告 · " + g.Name;
                ui.Text(screen.Body, "军团：" + g.Name + "\n团长：" + Name(s, g.Leader), 12, 8, 632, 60, 18, SocialUi.Cyan);
                if (g.Leader == Me)
                {
                    if (noticeDraftId != g.Id) { noticeDraftId = g.Id; editNotice = g.Notice; }
                    var input = ui.Input(screen.Body, "输入公告，最多 120 字，可换行", 12, 80, 632, 162, 120, editNotice, true);
                    var count = ui.Text(screen.Body, editNotice.Length + "/120" + (editNotice == g.Notice ? "" : " · 未保存"), 344, 246, 300, 28, 15, SocialUi.Muted, TextAnchor.MiddleRight);
                    input.onValueChanged.AddListener(v => { editNotice = v; count.text = v.Length + "/120" + (v == g.Notice ? "" : " · 未保存"); screen.Status.text = ""; });
                    ui.Button(screen.Body, "恢复公告", 12, 286, 170, 36, () => { editNotice = g.Notice; screen.Refresh(); });
                    ui.Button(screen.Body, "保存公告", 474, 286, 170, 36, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.UpdateGuild, Entity = g.Id, Text = editNotice }));
                }
                else
                {
                    var list = ui.List(screen.Body, 12, 80, 632, 192);
                    list.Row("军团公告", string.IsNullOrEmpty(g.Notice) ? "团长尚未发布公告" : g.Notice, 110, 608);
                }
                ui.Button(screen.Body, "返回军团", 243, 286, 170, 36, () => ShowGuild(g.Id));
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
                if (invitations.Count == 0)
                {
                    list.Empty("没有关系邀请", "从好友选择对象，等待对方确认。");
                    ui.Button(body, "发起邀请", 440, 180, 220, 36, () => { relationFilter = "发起"; screen.Refresh(); });
                }
                return;
            }
            if (kind == RelationKind.Mentor)
            {
                var bonds = s.Mentors.Where(x => x.Mentor == Me || x.Apprentice == Me).ToList();
                foreach (var bond in bonds)
                {
                    var m = bond; var peer = m.Mentor == Me ? m.Apprentice : m.Mentor;
                    var row = list.Row((m.Mentor == Me ? "徒弟：" : "师父：") + Name(s, peer), "已确认", 70, 418);
                    ui.Button(row, "名片", 430, 14, 92, 34, () => ShowProfile(peer));
                    ui.Button(row, "解除", 534, 14, 114, 34, () => Confirm("解除与 " + Name(s, peer) + " 的师徒关系？", new SocialCommand { Kind = SocialCommandKind.LeaveMentor, Entity = m.Id }));
                }
                if (bonds.Count == 0)
                {
                    list.Empty("尚无师徒关系", "可拜一位师父、收三位徒弟。双方需先成为好友，师父等级高于徒弟。");
                    ui.Button(body, "选择好友", 440, 180, 220, 36, () => { relationFilter = "发起"; screen.Refresh(); });
                }
            }
            else
            {
                var g = s.Brotherhoods.FirstOrDefault(x => x.Members.Contains(Me));
                if (g == null)
                {
                    list.Empty("尚未结拜", "从好友中邀请，双方确认后建立结拜，最多五人。");
                    ui.Button(body, "选择好友", 440, 180, 220, 36, () => { relationFilter = "发起"; screen.Refresh(); }); return;
                }
                list.Row(g.Name + "  " + g.Members.Count + "/5", "发起人：" + Name(s, g.Leader), 62, W - 24);
                foreach (var id in g.Members)
                {
                    string peer = id;
                    var row = list.Row(Name(s, id), id == Me ? "我" : "成员", 62, 390);
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
            string rule = kind == RelationKind.Mentor ? "师父等级高于徒弟，最多三位徒弟；需对方确认。" : "结拜最多五人，已有结拜仅发起人可邀请。";
            ui.Text(body, rule, 0, 43, W, 38, 16, SocialUi.Muted);
            float top = 85;
            if (kind == RelationKind.Brotherhood)
            {
                var input = ui.Input(body, "新结拜名称，1–12 字", 0, 84, W, 34, 12, brotherName);
                input.onValueChanged.AddListener(v => brotherName = v); top = 126;
            }
            var list = ui.List(body, 0, top, W, 289 - top);
            var friends = s.Players.Where(p => p.Id != Me && !p.IsNpc && IsFriend(s, p.Id) && !IsBlocked(s, p.Id)).ToList();
            foreach (var p in friends)
            {
                string peer = p.Id;
                var row = list.Row(p.Name, p.Verified ? "等级 " + p.Level : "好友", 66, 370);
                if (kind == RelationKind.Mentor)
                {
                    ui.Button(row, "拜师", 404, 14, 116, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.InviteMentor, Target = peer }));
                    ui.Button(row, "收徒", 532, 14, 116, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.InviteApprentice, Target = peer }));
                }
                else ui.Button(row, "邀请结拜", 482, 14, 166, 34, () => Apply(screen, new SocialCommand { Kind = SocialCommandKind.InviteBrother, Target = peer, Name = brotherName.Trim() }));
            }
            if (friends.Count == 0)
            {
                list.Empty("没有可邀请的好友", "先添加联系人，好友申请需对方确认。NPC 不参与关系邀请。");
                ui.Button(body, "添加联系人", 440, 238, 220, 36, ShowContactForm);
            }
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
