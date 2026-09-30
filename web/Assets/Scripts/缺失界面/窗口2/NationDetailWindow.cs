using Dwsg.Administration;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace 缺失界面.窗口2
{
    public enum NationPage { 概况, 国王, 国都, 城池, 成员, 民生, 公告, 宣言, 战功, 贡献, 轮选, 官职, 国家排行, 管理, 编辑公告, 编辑宣言, 玩家资料, 城池资料, 任免, 确认任免, 确认俸禄 }

    public sealed class NationDetailWindow : MonoBehaviour
    {
        private struct Context
        {
            internal NationPage Page;
            internal string Code;
            internal int PlayerId, X, Y, ActorId;
            internal NationOffice Office;
            internal float Scroll;
        }

        private readonly Stack<Context> history = new Stack<Context>();
        private Context context;
        private NationUiFactory ui;
        private RectTransform content, body;
        private ScrollRect scroll;
        private Text title, heading, feedback, timerText;
        private NationSnapshot snapshot;
        private float nextTimer;
        private bool settleFirstLayout, submitting;
        private int viewGeneration;
        private string laidOutFeedback;
        private 所有城池界面脚本 existingCityMap;
        private Transform originalCountry;
        // The coordinating window may supply its city navigation adapter. The default reuses the scene's old city panel.
        public Func<int, int, bool> ShowCity;
        internal Func<string, bool> ShowExistingNation;
        public INationDataSource Data { get { return NationDataSource.Current; } }

        internal void Build(Transform countryRoot)
        {
            ui = new NationUiFactory(countryRoot);
            originalCountry = countryRoot;
            foreach (GameObject root in countryRoot.gameObject.scene.GetRootGameObjects())
            {
                existingCityMap = root.GetComponentInChildren<所有城池界面脚本>(true);
                if (existingCityMap != null) break;
            }
            foreach (string name in new[] { "黄色背景图", "信息背景", "信息边框", "标题栏背景" })
            {
                var rect = NationUiFactory.Decoration(countryRoot.Find(name), transform);
                if (rect != null && (name == "信息背景" || name == "信息边框"))
                {
                    rect.sizeDelta = new Vector2(648, 364);
                    rect.anchoredPosition = new Vector2(0, 6);
                    if (name == "信息背景")
                    {
                        // The scene's child image is larger than its container. Fit the actual painted
                        // background as well, otherwise it covers the yellow frame and ornate title.
                        foreach (Image image in rect.GetComponentsInChildren<Image>(true))
                        {
                            var painted = image.rectTransform; if (painted == rect) continue;
                            painted.SetParent(rect, false); painted.pivot = new Vector2(.5f, .5f);
                            painted.localScale = Vector3.one; painted.localRotation = Quaternion.identity;
                            NationUiFactory.Place(painted, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                        }
                    }
                }
            }
            // The invisible backdrop prevents clicks leaking to the map through the window's blank areas.
            var blocker = NationUiFactory.Rect("窗口点击范围", transform);
            blocker.anchorMin = blocker.anchorMax = new Vector2(.5f, .5f); blocker.sizeDelta = new Vector2(696, 490); blocker.anchoredPosition = new Vector2(0, -2);
            blocker.gameObject.AddComponent<Image>().color = Color.clear; blocker.SetAsFirstSibling();
            var titleBar = transform.Find("标题栏背景");
            title = ui.Text("详情标题", transform, "国家详情", 22, NationUiFactory.Gold); title.alignment = TextAnchor.MiddleCenter;
            原界面文字样式.标题(title);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(.5f, .5f);
            title.rectTransform.sizeDelta = new Vector2(510, 44);
            title.rectTransform.anchoredPosition = ((RectTransform)titleBar).anchoredPosition + ((RectTransform)countryRoot.Find("标题栏背景/8 (21)")).anchoredPosition;
            body = NationUiFactory.Rect("详情内容", transform); body.anchorMin = body.anchorMax = new Vector2(.5f, .5f);
            body.sizeDelta = new Vector2(624, 344); body.anchoredPosition = new Vector2(0, 6);
            NationUiFactory.Vertical(body);
            heading = ui.Text("国家上下文", body, "", 18, NationUiFactory.Gold);
            NationUiFactory.Height(heading, 30);
            content = ui.Scroll(body, out scroll);
            feedback = ui.Text("操作反馈", body, "", 18, NationUiFactory.Muted);
            NationUiFactory.Height(feedback, 0);
            feedback.gameObject.SetActive(false);
            var footer = NationUiFactory.Rect("详情操作", transform); footer.anchorMin = footer.anchorMax = new Vector2(.5f, .5f);
            footer.sizeDelta = new Vector2(632, 40); footer.anchoredPosition = new Vector2(0, -209);
            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 24; layout.childControlHeight = true;
            layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = false; layout.childAlignment = TextAnchor.MiddleRight;
            ui.Button("刷新", footer, "刷新", () => { context.Scroll = 1; Render(); });
            ui.Button("返回国家", footer, "国家页", () => 界面窗口动画.关闭(gameObject));
            ui.Button("返回上级", footer, "返回", Back);
            ui.Close(countryRoot.Find("标题栏背景/关闭"), transform, () => 界面窗口动画.关闭(gameObject));
        }

        public void Open(NationPage page, string code)
        {
            if (page == NationPage.概况 && OpenExistingNation(code)) return;
            if (page == NationPage.官职 && code == Data.OwnNationCode && OpenOriginalOffice()) return;
            if (page == NationPage.国都)
            {
                var nation = Data.ReadNation(code);
                if (nation != null && nation.Capital != null && OpenCity(nation.Capital)) return;
            }
            history.Clear(); context = new Context { Page = page, Code = code, PlayerId = -1, ActorId = Data.ActorId, Scroll = 1 };
            bool wasActive = gameObject.activeSelf;
            界面窗口动画.设置显示(gameObject, true);
            if (wasActive) Render();
        }

        public bool OpenExistingNation(string code)
        {
            return ShowExistingNation != null && ShowExistingNation(code);
        }

        // Integration point for a city lord/player link. Accepts a player ID, never changes the local actor.
        // The caller can fall back to its own notice when false is returned.
        public bool OpenPlayer(int playerId)
        {
            var player = Data.ReadPlayer(playerId); if (player == null) return false;
            history.Clear(); context = new Context { Page = NationPage.玩家资料, Code = player.NationCode, PlayerId = playerId, ActorId = Data.ActorId, Scroll = 1 };
            bool wasActive = gameObject.activeSelf;
            界面窗口动画.设置显示(gameObject, true);
            if (wasActive) Render();
            return true;
        }

        private void OnEnable() { if (ui != null) { Render(); settleFirstLayout = true; } }
        private void OnDisable() { timerText = null; settleFirstLayout = false; }
        private void LateUpdate()
        {
            if (feedback != null && laidOutFeedback != feedback.text) ResizeChrome();
            // Canvas registration completes after OnEnable. Settle that first layout before it is painted.
            if (!settleFirstLayout) return;
            settleFirstLayout = false; FinishLayout();
        }

        private void Navigate(NationPage page, int playerId = -1, int x = 0, int y = 0, string code = null)
        {
            // Short pages may report 0 at their visible top; restoring that value to
            // newly lengthened content would scroll it to the bottom.
            context.Scroll = content.rect.height > scroll.viewport.rect.height ? scroll.verticalNormalizedPosition : 1;
            history.Push(context); context = new Context { Page = page, Code = code ?? context.Code, PlayerId = playerId, X = x, Y = y, ActorId = Data.ActorId, Scroll = 1 }; Render();
        }

        private void Back()
        {
            if (history.Count == 0) 界面窗口动画.关闭(gameObject);
            else { context = history.Pop(); Render(); }
        }

        private void ClearContent()
        {
            timerText = null;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject);
            }
            scroll.StopMovement(); content.anchoredPosition = Vector2.zero;
        }

        private void Render()
        {
            viewGeneration++;
            ClearContent(); snapshot = Data.ReadNation(context.Code); title.text = context.Page == NationPage.宣言 ? "国家宣告" : context.Page.ToString();
            heading.text = snapshot == null ? "未加入国家" : Short(snapshot.Name, 18) + "（" + Short(snapshot.Code, 6) + "）";
            feedback.text = "";
            if (context.Page == NationPage.国家排行) { ShowNationRanking(); FinishLayout(); return; }
            if (snapshot == null)
            {
                if (context.Page == NationPage.玩家资料)
                {
                    var player = Data.ReadPlayer(context.PlayerId);
                    heading.text = player == null ? "角色记录已不存在" : Short(player.Name, 16) + " · 未加入国家";
                    Player(player, false); FinishLayout(); return;
                }
                ui.Paragraph(content, "尚未加入国家。返回国家页，选择换国入口加入或建立国家。");
                FinishLayout(); return;
            }
            switch (context.Page)
            {
                case NationPage.概况: ui.Paragraph(content, "无法打开国家页，请返回重试。"); break;
                case NationPage.国王: Player(snapshot.King, true); break;
                case NationPage.国都: ui.Paragraph(content, "本国暂无可查看的国都，请刷新后重试。"); break;
                case NationPage.城池: Cities(); break;
                case NationPage.成员: Members(); break;
                case NationPage.民生: Welfare(); break;
                case NationPage.公告: Notice(NationNoticeKind.公告); break;
                case NationPage.宣言: Notice(NationNoticeKind.宣言); break;
                case NationPage.战功: PlayerRanking(NationRanking.战功); break;
                case NationPage.贡献: PlayerRanking(NationRanking.贡献); break;
                case NationPage.轮选: Election(); break;
                case NationPage.官职: ui.Paragraph(content, "请返回自己所属的国家查看官职与俸禄。"); break;
                case NationPage.管理: Management(); break;
                case NationPage.编辑公告: Editor(NationNoticeKind.公告); break;
                case NationPage.编辑宣言: Editor(NationNoticeKind.宣言); break;
                case NationPage.玩家资料:
                    var player = Data.ReadPlayer(context.PlayerId);
                    Player(player != null && player.NationCode == context.Code ? player : null, false); break;
                case NationPage.城池资料: ui.Paragraph(content, "请从国土列表选择要查看的城池。"); break;
                case NationPage.任免: Appointment(); break;
                case NationPage.确认任免: AppointmentConfirmation(); break;
                case NationPage.确认俸禄: ui.Paragraph(content, "请从个人页领取俸禄，每5分钟可领取一次。"); break;
            }
            FinishLayout(); nextTimer = 0;
        }

        private void FinishLayout()
        {
            ResizeChrome();
            // Resolve the stretched viewport width before measuring text on the first activation.
            Canvas.ForceUpdateCanvases();
            // Width must settle before measuring wrapped rows; later passes may reduce an earlier height.
            NationUiFactory.MeasureContent(content);
            foreach (InputField field in content.GetComponentsInChildren<InputField>()) field.ForceLabelUpdate();
            scroll.StopMovement(); scroll.verticalNormalizedPosition = Mathf.Clamp01(context.Scroll);
            // 强制布局后文字高度和滚动位置都可能变化，再更新裁切缓存，避免新按钮继承上一页的剔除状态。
            Canvas.ForceUpdateCanvases();
        }

        private void ResizeChrome()
        {
            // The list is anchored at the viewport top. Preserve its visible offset before
            // activating feedback triggers a layout/ScrollRect pass against the new height.
            float topOffset = content.anchoredPosition.y;
            // Empty feedback must not reserve a fixed strip inside a scrollable list.
            feedback.gameObject.SetActive(!string.IsNullOrEmpty(feedback.text));
            Canvas.ForceUpdateCanvases();
            NationUiFactory.Height(heading, Mathf.Ceil(heading.preferredHeight + 4));
            NationUiFactory.Height(feedback, feedback.gameObject.activeSelf ? Mathf.Ceil(feedback.preferredHeight + 4) : 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            Canvas.ForceUpdateCanvases();
            float range = Mathf.Max(0, content.rect.height - scroll.viewport.rect.height);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = range > 0 ? 1 - Mathf.Clamp(topOffset, 0, range) / range : 1;
            Canvas.ForceUpdateCanvases();
            laidOutFeedback = feedback.text;
        }

        private void Link(string text, string button, UnityEngine.Events.UnityAction action, bool enabled = true)
        {
            var row = ui.Row(content); ui.ListBackground(row); ui.RowText(row, text); ui.Button(button, row, button, action).interactable = enabled;
        }

        private void Value(string label, string value)
        {
            var row = ui.Row(content, 30); ValueLabel(row, label);
            ui.RowText(row, value);
        }

        private void ValueLabel(Transform row, string label)
        {
            var caption = ui.Text("项目", row, label, 18, NationUiFactory.Gold);
            var layout = caption.gameObject.AddComponent<LayoutElement>(); layout.minWidth = layout.preferredWidth = 116;
        }

        private void Player(NationPlayerSnapshot player, bool king)
        {
            if (player == null) { ui.Paragraph(content, king ? "本国暂无国王。" : "此成员已换国或记录已不存在，请返回列表刷新。"); return; }
            var row = ui.Row(content, 66);
            if (player.Avatar >= 0 && player.Avatar < 全局变量.所有头像资源表.Count)
            {
                var portrait = NationUiFactory.Rect("君主头像", row); var image = portrait.gameObject.AddComponent<Image>(); image.sprite = 全局变量.所有头像资源表[player.Avatar]; image.preserveAspect = true; image.raycastTarget = false;
                var layout = portrait.gameObject.AddComponent<LayoutElement>(); layout.preferredWidth = layout.minWidth = 66; layout.preferredHeight = layout.minHeight = 66;
            }
            ui.RowText(row, player.Name + "\n等级 " + N(player.Level) + "  ·  " + player.Title, NationUiFactory.Gold);
            Value("官职 / 任命", player.Office + " / " + player.Appointment);
            Value("战功 / 贡献", N(player.Merit) + " / " + N(player.Contribution));
            Value("声望", N(player.Prestige)); Value("封地 / 将领", player.Fiefs + " / " + N(player.Generals));
            if (snapshot != null && snapshot.CanManage && player.Id != snapshot.KingId) Link("选择此成员进行国家职务任免", "任免职务", () => Navigate(NationPage.任免, player.Id));
        }

        private bool OpenCity(NationCitySnapshot city)
        {
            if (city == null || Data.ReadCity(city.NationCode, city.X, city.Y) == null)
            { if (feedback != null) feedback.text = "城池归属已变化，请刷新国土列表。"; return false; }
            if (ShowCity != null)
            {
                bool opened = ShowCity(city.X, city.Y);
                if (!opened && feedback != null) feedback.text = "无法打开城池，请刷新后重试。";
                return opened;
            }
            if (existingCityMap != null && existingCityMap.城池信息界面UI != null)
            {
                for (int i = 0; i < 全局变量.所有城池列表.Count; i++)
                {
                    var item = 全局变量.所有城池列表[i];
                    if (item != null && item.坐标x == city.X && item.坐标y == city.Y)
                    { existingCityMap.打开城池信息(i); return true; }
                }
            }
            if (feedback != null) feedback.text = "无法打开城池，请返回地图查看。";
            return false;
        }

        private void Cities()
        {
            ui.Paragraph(content, "国土 " + snapshot.Cities.Count + " 座 · 国都优先");
            if (snapshot.Cities.Count == 0) ui.Paragraph(content, "本国暂无城池。");
            foreach (var city in snapshot.Cities)
            {
                var item = city;
                Link((item.Capital ? "【国都】" : "") + item.Name + "（" + item.X + "," + item.Y + "）\n" + item.Scale + " · 城主 " + item.Owner + (item.InBattle ? " · 交战中" : ""), "城池资料", () => OpenCity(item));
            }
        }

        private void Members()
        {
            ui.Paragraph(content, "国民 " + snapshot.Members.Count + " 人");
            if (snapshot.Members.Count == 0) ui.Paragraph(content, "本国暂无成员。");
            foreach (var member in snapshot.Members)
            {
                var item = member;
                Link(item.Name + (item.Id == snapshot.KingId ? "【国王】" : "") + " · " + item.Office + "\n战功 " + N(item.Merit) + " · 贡献 " + N(item.Contribution), "角色资料", () => Navigate(NationPage.玩家资料, item.Id));
            }
        }

        private void Welfare()
        {
            Value("民生值", N(snapshot.Welfare)); Value("国家效率", N(snapshot.Efficiency) + "%");
            Value("科技等级", N(snapshot.Technology)); Value("国库铜钱", NationUiFactory.Amount(snapshot.Copper)); Value("国库粮食", NationUiFactory.Amount(snapshot.Grain));
        }

        private void Notice(NationNoticeKind kind)
        {
            string value = kind == NationNoticeKind.公告 ? snapshot.Notice : snapshot.Declaration;
            bool canEdit = kind == NationNoticeKind.公告 ? snapshot.CanPublishNotice : snapshot.CanPublishDeclaration;
            ui.Paragraph(content, value.Length == 0 ? "本国尚未发布" + kind + "。" : value);
            Link(kind == NationNoticeKind.公告 ? "国王或丞相可编辑" : "国王可编辑", "编辑", () => Navigate(kind == NationNoticeKind.公告 ? NationPage.编辑公告 : NationPage.编辑宣言), canEdit);
        }

        private void Editor(NationNoticeKind kind)
        {
            bool allowed = kind == NationNoticeKind.公告 ? snapshot.CanPublishNotice : snapshot.CanPublishDeclaration;
            if (!allowed) { ui.Paragraph(content, "当前身份不能编辑此内容。请返回查看页；保存操作也会再次检查身份。"); return; }
            int limit = kind == NationNoticeKind.公告 ? 400 : 100;
            InputField field = null;
            field = ui.Input(content, kind == NationNoticeKind.公告 ? snapshot.Notice : snapshot.Declaration, limit,
                text => feedback.text = "已输入" + text.Length + "/" + limit + "字；尚未保存。");
            var row = ui.Row(content, 40);
            ui.Button("保存正文", row, "保存", () =>
            {
                if (context.ActorId != Data.ActorId) { feedback.text = "当前角色已变化，请返回重新编辑。"; return; }
                if (submitting) return;
                submitting = true; var opened = context; int generation = viewGeneration; field.interactable = false;
                feedback.text = "正在保存，请稍候。";
                AdministrationClient.Publish(Data, context.Code, kind, field.text, kind == NationNoticeKind.公告 ? snapshot.Notice : snapshot.Declaration, result =>
                {
                    if (this == null) return; submitting = false; if (field != null) field.interactable = true;
                    if (!gameObject.activeInHierarchy || generation != viewGeneration || context.Page != opened.Page || context.Code != opened.Code || context.ActorId != opened.ActorId) return;
                    if (result.Success) Back(); feedback.text = result.Message;
                });
            });
            ui.Button("清空正文", row, "清空输入", () => { if (!submitting) field.text = ""; });
            ui.Paragraph(content, "最多" + limit + "字。清空后保存会撤下原内容。\n返回或刷新会放弃未保存的输入。", NationUiFactory.Muted);
            feedback.text = "正在编辑" + kind + "；尚未保存。";
        }

        private void PlayerRanking(NationRanking ranking)
        {
            var rows = Data.ReadPlayerRanking(context.Code, ranking);
            ui.Paragraph(content, "本国" + ranking + "榜 · 共 " + rows.Count + " 人");
            if (rows.Count == 0) ui.Paragraph(content, "本国暂无成员。");
            for (int i = 0; i < rows.Count; i++)
            {
                var item = rows[i]; string mark = item.Id == Data.ActorId ? "【我】" : "";
                Link((i + 1) + "  " + item.Name + mark + "\n" + ranking + " " + N(ranking == NationRanking.战功 ? item.Merit : item.Contribution) + " · " + item.Office,
                    "角色资料", () => Navigate(NationPage.玩家资料, item.Id));
            }
        }

        private void ShowNationRanking()
        {
            var rows = Data.ReadNationRanking();
            ui.Paragraph(content, "国土榜 · 按城池数量排名");
            if (rows.Count == 0) ui.Paragraph(content, "暂无国家。");
            foreach (var nation in rows)
            {
                var item = nation;
                Link(item.Rank + "  " + item.Name + "（" + item.Code + "）\n城池 " + item.Cities.Count + " · " + item.Scale, "国家资料", () => OpenExistingNation(item.Code));
            }
        }

        private void Election()
        {
            Value("轮选周期", snapshot.ElectionInterval + "秒"); Value("上次轮选", SafeDate(snapshot.LastElection));
            var timerRow = ui.Row(content, 30); ValueLabel(timerRow, "下次轮选");
            timerText = ui.Text("轮选计时", timerRow, "", 18); timerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Value("现任国王", snapshot.King == null ? "无" : snapshot.King.Name);
            ui.Paragraph(content, "到期后重新计时；国家职务由国王任命。", NationUiFactory.Muted);
            Link("查看已有官职说明与俸禄规则", "官职俸禄", () => OpenOriginalOffice(), context.Code == Data.OwnNationCode);
            UpdateTimer();
        }

        private bool OpenOriginalOffice()
        {
            var panel = originalCountry == null ? null : originalCountry.Find("官职说明");
            if (panel == null || !OpenExistingNation(Data.OwnNationCode)) return false;
            panel.gameObject.SetActive(true); return true;
        }

        private void Management()
        {
            ui.Paragraph(content, "国王任免职务与编辑宣告；国王或丞相可编辑公告。");
            Link("公告发布与撤下", "查看公告", () => Navigate(NationPage.公告));
            Link("国家宣告", "查看宣告", () => Navigate(NationPage.宣言));
            foreach (var appointment in snapshot.Appointments)
                Value(appointment.Key.ToString(), appointment.Value == null ? "空缺" : appointment.Value.Name);
            Link("选择成员，查看资料并任免", "国民列表", () => Navigate(NationPage.成员));
            if (!snapshot.CanManage) ui.Paragraph(content, "仅本国国王可任免职务。", NationUiFactory.Muted);
        }

        private void Appointment()
        {
            var member = Data.ReadPlayer(context.PlayerId);
            if (!snapshot.CanManage || member == null || member.NationCode != context.Code || member.Id == snapshot.KingId)
            { ui.Paragraph(content, "当前身份或成员状态不能执行任免。请返回国民列表刷新。"); return; }
            ui.Paragraph(content, "任命对象：" + member.Name, NationUiFactory.Gold);
            ui.Paragraph(content, "每人可任一职；改任会免去原职。", NationUiFactory.Muted);
            foreach (var pair in snapshot.Appointments)
            {
                var office = pair.Key; var holder = pair.Value; var row = ui.Row(content, 42);
                ui.RowText(row, office + "\n现任：" + (holder == null ? "无有效任命" : holder.Name));
                ui.Button("任命" + office, row, "任命", () => ConfirmAppointment(office, context.PlayerId));
                ui.Button("免去" + office, row, "免职", () => ConfirmAppointment(office, -1)).interactable = holder != null;
            }
        }

        private void ConfirmAppointment(NationOffice office, int id)
        {
            context.Scroll = scroll.verticalNormalizedPosition;
            history.Push(context); context.Page = NationPage.确认任免; context.PlayerId = id; context.ActorId = Data.ActorId; context.Office = office; context.Scroll = 1; Render();
        }

        private void AppointmentConfirmation()
        {
            if (!snapshot.CanManage || context.ActorId != Data.ActorId) { ui.Paragraph(content, "国王身份已变化，不能执行任免。请返回国家管理页刷新。"); return; }
            var member = context.PlayerId < 0 ? null : Data.ReadPlayer(context.PlayerId);
            if (context.PlayerId >= 0 && (member == null || member.NationCode != context.Code))
            { ui.Paragraph(content, "目标成员已不在本国，请返回国民列表刷新。"); return; }
            var old = snapshot.Appointments[context.Office];
            ui.Paragraph(content, "国家职务：" + context.Office + "\n原任：" + (old == null ? "无有效任命" : old.Name) +
                "\n新任：" + (member == null ? "免去职务" : member.Name), NationUiFactory.Gold);
            ui.Paragraph(content, "改任会免去成员原职；返回取消。", NationUiFactory.Muted);
            var row = ui.Row(content, 40);
            ui.Button("确认任免", row, "确认任免", () =>
            {
                if (context.ActorId != Data.ActorId) { feedback.text = "当前角色已变化，请返回重新查看。"; return; }
                if (submitting) return;
                submitting = true; var opened = context; int generation = viewGeneration; feedback.text = "正在任免，请稍候。";
                AdministrationClient.Appoint(Data, context.Code, context.Office, context.PlayerId, old == null ? -1 : old.Id, result =>
                {
                    if (this == null) return; submitting = false;
                    if (!gameObject.activeInHierarchy || generation != viewGeneration || context.Page != opened.Page || context.Code != opened.Code || context.ActorId != opened.ActorId || context.Office != opened.Office || context.PlayerId != opened.PlayerId) return;
                    if (result.Success) Back(); feedback.text = result.Message;
                });
            });
        }

        private void Update()
        {
            if (timerText == null || Time.unscaledTime < nextTimer) return;
            nextTimer = Time.unscaledTime + 1; UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (timerText == null) return;
            if (context.Page == NationPage.轮选)
            {
                var fresh = Data.ReadNation(context.Code);
                timerText.text = fresh == null ? "国家已不存在" : fresh.ElectionRemaining > 0 ? TIME.ToTimeFormat(fresh.ElectionRemaining) : "轮选到期，即将重新计时";
            }
        }

        private static string N(double value) { return NationDataSource.Number(value); }
        private static string Short(string value, int max) { value = value ?? ""; return value.Length <= max ? value : value.Substring(0, max) + "…"; }
        private static string SafeDate(long timestamp) { try { return TIME.TimeStampToDateTime(timestamp).ToString("yyyy-MM-dd HH:mm:ss"); } catch (ArgumentOutOfRangeException) { return "时间记录异常"; } }
    }
}
