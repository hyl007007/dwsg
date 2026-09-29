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
        }

        private readonly Stack<Context> history = new Stack<Context>();
        private Context context;
        private NationUiFactory ui;
        private RectTransform content;
        private Text title, heading, feedback, timerText;
        private NationSnapshot snapshot;
        private float nextTimer;
        private bool settleFirstLayout;
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
            var body = NationUiFactory.Rect("详情内容", transform); body.anchorMin = body.anchorMax = new Vector2(.5f, .5f);
            body.sizeDelta = new Vector2(624, 344); body.anchoredPosition = new Vector2(0, 6);
            heading = ui.Text("国家上下文", body, "", 16, NationUiFactory.Gold);
            NationUiFactory.Place(heading.rectTransform, new Vector2(0, .82f), Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));
            content = ui.Scroll(body);
            feedback = ui.Text("操作反馈", body, "", 14, NationUiFactory.Muted);
            NationUiFactory.Place(feedback.rectTransform, Vector2.zero, new Vector2(1, .14f), new Vector2(4, 0), new Vector2(-4, 0));
            var footer = NationUiFactory.Rect("详情操作", transform); footer.anchorMin = footer.anchorMax = new Vector2(.5f, .5f);
            footer.sizeDelta = new Vector2(632, 40); footer.anchoredPosition = new Vector2(0, -209);
            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 24; layout.childControlHeight = true;
            layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = false; layout.childAlignment = TextAnchor.MiddleRight;
            ui.Button("刷新", footer, "刷新", () => Render());
            ui.Button("返回国家", footer, "国家页", () => gameObject.SetActive(false));
            ui.Button("返回上级", footer, "返回", Back);
            ui.Close(countryRoot.Find("标题栏背景/关闭"), transform, () => gameObject.SetActive(false));
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
            history.Clear(); context = new Context { Page = page, Code = code, PlayerId = -1 };
            if (!gameObject.activeSelf) gameObject.SetActive(true); else Render();
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
            history.Clear(); context = new Context { Page = NationPage.玩家资料, Code = player.NationCode, PlayerId = playerId };
            if (!gameObject.activeSelf) gameObject.SetActive(true); else Render();
            return true;
        }

        private void OnEnable() { if (ui != null) { Render(); settleFirstLayout = true; } }
        private void OnDisable() { timerText = null; settleFirstLayout = false; }
        private void LateUpdate()
        {
            // Canvas registration completes after OnEnable. Settle that first layout before it is painted.
            if (!settleFirstLayout) return;
            settleFirstLayout = false; FinishLayout();
        }

        private void Navigate(NationPage page, int playerId = -1, int x = 0, int y = 0, string code = null)
        {
            history.Push(context); context = new Context { Page = page, Code = code ?? context.Code, PlayerId = playerId, X = x, Y = y }; Render();
        }

        private void Back()
        {
            if (history.Count == 0) gameObject.SetActive(false);
            else { context = history.Pop(); Render(); }
        }

        private void ClearContent()
        {
            timerText = null;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject);
            }
            var scroll = content.GetComponentInParent<ScrollRect>(); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
        }

        private void Render()
        {
            ClearContent(); snapshot = Data.ReadNation(context.Code); title.text = context.Page == NationPage.宣言 ? "国家宣告" : context.Page.ToString();
            heading.text = (snapshot == null ? "未加入有效国家" : Short(snapshot.Name, 18) + "（" + Short(snapshot.Code, 6) + "）") + "\n" + Data.ConnectionLabel;
            feedback.text = "列表为当前世界快照；刷新后重新读取。";
            if (context.Page == NationPage.国家排行) { ShowNationRanking(); FinishLayout(); return; }
            if (snapshot == null)
            {
                if (context.Page == NationPage.玩家资料)
                {
                    var player = Data.ReadPlayer(context.PlayerId);
                    heading.text = (player == null ? "角色记录已不存在" : Short(player.Name, 16) + " · 未加入有效国家") + "\n" + Data.ConnectionLabel;
                    Player(player, false); FinishLayout(); return;
                }
                ui.Paragraph(content, "当前角色未加入国家，或该国家已不存在。\n可返回国家页，使用已有的换国入口加入有效国家，再查看成员、国都和俸禄。");
                FinishLayout(); return;
            }
            switch (context.Page)
            {
                case NationPage.概况: ui.Paragraph(content, "国家概况继续使用原有国家信息面板。"); break;
                case NationPage.国王: Player(snapshot.King, true); break;
                case NationPage.国都: ui.Paragraph(content, "当前国都记录无效，或原城池信息面板尚不可用。请返回地图刷新后查看。"); break;
                case NationPage.城池: Cities(); break;
                case NationPage.成员: Members(); break;
                case NationPage.民生: Welfare(); break;
                case NationPage.公告: Notice(NationNoticeKind.公告); break;
                case NationPage.宣言: Notice(NationNoticeKind.宣言); break;
                case NationPage.战功: PlayerRanking(NationRanking.战功); break;
                case NationPage.贡献: PlayerRanking(NationRanking.贡献); break;
                case NationPage.轮选: Election(); break;
                case NationPage.官职: ui.Paragraph(content, "本人官职说明和俸禄继续使用原个人页，查看其他国家不能执行本人操作。"); break;
                case NationPage.管理: Management(); break;
                case NationPage.编辑公告: Editor(NationNoticeKind.公告); break;
                case NationPage.编辑宣言: Editor(NationNoticeKind.宣言); break;
                case NationPage.玩家资料:
                    var player = Data.ReadPlayer(context.PlayerId);
                    Player(player != null && player.NationCode == context.Code ? player : null, false); break;
                case NationPage.城池资料: ui.Paragraph(content, "城池资料继续使用原有城池信息面板，请从国土列表选择城池。"); break;
                case NationPage.任免: Appointment(); break;
                case NationPage.确认任免: AppointmentConfirmation(); break;
                case NationPage.确认俸禄: ui.Paragraph(content, "俸禄继续从原个人页领取，使用原有300秒冷却和真实国库余额。"); break;
            }
            FinishLayout(); nextTimer = 0;
        }

        private void FinishLayout()
        {
            // Resolve the stretched viewport width before measuring text on the first activation.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            // Measure the real Text width after horizontal layout, so long names grow a row instead of covering its buttons.
            foreach (Transform child in content)
            {
                var paragraph = child.GetComponent<Text>();
                var paragraphLayout = child.GetComponent<LayoutElement>();
                if (paragraph != null && paragraphLayout != null) paragraphLayout.preferredHeight = Mathf.Max(30, paragraph.preferredHeight + 6);
                if (child.GetComponent<HorizontalLayoutGroup>() == null) continue;
                var element = child.GetComponent<LayoutElement>();
                foreach (Transform item in child)
                {
                    var text = item.GetComponent<Text>();
                    if (text != null && element != null) element.preferredHeight = Mathf.Max(element.preferredHeight, text.preferredHeight + 10);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            foreach (InputField field in content.GetComponentsInChildren<InputField>()) field.ForceLabelUpdate();
            content.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1;
        }

        private void Link(string text, string button, UnityEngine.Events.UnityAction action, bool enabled = true)
        {
            var row = ui.Row(content); ui.RowText(row, text); ui.Button(button, row, button, action).interactable = enabled;
        }

        private void Player(NationPlayerSnapshot player, bool king)
        {
            if (player == null) { ui.Paragraph(content, king ? "当前国家没有有效国王记录。不会访问无效玩家索引；任免与宣言编辑需本国国王身份。" : "此角色已不在该国家或记录已不存在，请返回成员列表刷新。"); return; }
            var row = ui.Row(content, 78);
            if (player.Avatar >= 0 && player.Avatar < 全局变量.所有头像资源表.Count)
            {
                var portrait = NationUiFactory.Rect("君主头像", row); var image = portrait.gameObject.AddComponent<Image>(); image.sprite = 全局变量.所有头像资源表[player.Avatar]; image.preserveAspect = true; image.raycastTarget = false;
                var layout = portrait.gameObject.AddComponent<LayoutElement>(); layout.preferredWidth = 66; layout.minWidth = 66;
            }
            ui.RowText(row, Short(player.Name, 16) + "  ·  角色ID " + player.Id + "\n等级 " + N(player.Level) + "  ·  称号 " + Short(player.Title, 12), NationUiFactory.Gold);
            ui.Paragraph(content, "名字：" + player.Name + "\n称号：" + player.Title);
            ui.Paragraph(content, "俸禄官职：" + player.Office + "\n国家任命：" + player.Appointment + "\n战功：" + N(player.Merit) + "  ·  贡献：" + N(player.Contribution) +
                "\n声望：" + N(player.Prestige) + "\n封地：" + player.Fiefs + "  ·  将领：" + N(player.Generals));
            ui.Paragraph(content, "资料来自本地世界角色记录，包含初始化角色；不代表玩家在线。", NationUiFactory.Muted);
            if (snapshot != null && snapshot.CanManage && player.Id != snapshot.KingId) Link("选择此成员进行国家职务任免", "任免职务", () => Navigate(NationPage.任免, player.Id));
        }

        private bool OpenCity(NationCitySnapshot city)
        {
            if (city == null || Data.ReadCity(city.NationCode, city.X, city.Y) == null)
            { if (feedback != null) feedback.text = "城池归属已变化，请刷新国土列表。"; return false; }
            if (ShowCity != null) return ShowCity(city.X, city.Y);
            if (existingCityMap != null && existingCityMap.城池信息界面UI != null)
            {
                for (int i = 0; i < 全局变量.所有城池列表.Count; i++)
                {
                    var item = 全局变量.所有城池列表[i];
                    if (item != null && item.坐标x == city.X && item.坐标y == city.Y)
                    { existingCityMap.打开城池信息(i); return true; }
                }
            }
            if (feedback != null) feedback.text = "原城池信息面板尚不可用，请返回地图查看。";
            return false;
        }

        private void Cities()
        {
            ui.Paragraph(content, "按国都优先、坐标顺序显示当前真实归属，共" + snapshot.Cities.Count + "座。");
            if (snapshot.Cities.Count == 0) ui.Paragraph(content, "本国当前没有城池。可能已失去国土；列表会在刷新后反映世界归属变化。");
            foreach (var city in snapshot.Cities)
            {
                var item = city;
                Link((item.Capital ? "【国都】" : "") + item.Name + "（" + item.X + "," + item.Y + "）\n" + item.Scale + " · 城主 " + item.Owner + (item.InBattle ? " · 交战中" : ""), "城池资料", () => OpenCity(item));
            }
        }

        private void Members()
        {
            ui.Paragraph(content, "有效国民" + snapshot.Members.Count + "人；按角色ID显示。角色记录不表示在线状态。");
            if (snapshot.Members.Count == 0) ui.Paragraph(content, "国家成员记录为空。加入国家后可在此查看；不会用虚构成员填充列表。");
            foreach (var member in snapshot.Members)
            {
                var item = member;
                Link(item.Name + (item.Id == snapshot.KingId ? "【国王】" : "") + " · " + item.Office + "\n战功 " + N(item.Merit) + " · 贡献 " + N(item.Contribution), "角色资料", () => Navigate(NationPage.玩家资料, item.Id));
            }
        }

        private void Welfare()
        {
            ui.Paragraph(content, "当前民生值：" + N(snapshot.Welfare) + "\n国家效率：" + N(snapshot.Efficiency) + "%\n科技等级：" + N(snapshot.Technology), NationUiFactory.Gold);
            ui.Paragraph(content, "本工程将民生值保存在国家记录中。现有代码没有民生调整、民生投票或以此征税的独立规则，因此本页只展示真实记录。\n国家科技和国库仍使用国家页已有入口；不增加未实现的购买或服务器操作。", NationUiFactory.Muted);
        }

        private void Notice(NationNoticeKind kind)
        {
            string value = kind == NationNoticeKind.公告 ? snapshot.Notice : snapshot.Declaration;
            bool canEdit = kind == NationNoticeKind.公告 ? snapshot.CanPublishNotice : snapshot.CanPublishDeclaration;
            ui.Paragraph(content, value.Length == 0 ? "本国尚未发布" + kind + "。有权限的角色可以在下方编辑，发布后本地国民可查看。" : value);
            Link(kind == NationNoticeKind.公告 ? "本地权限：国王或本国丞相可编辑。" : "本地权限：仅本国国王可编辑。", "编辑", () => Navigate(kind == NationNoticeKind.公告 ? NationPage.编辑公告 : NationPage.编辑宣言), canEdit);
            ui.Paragraph(content, "修改写入现有国家字段，使用世界存档保存；当前没有网络广播。", NationUiFactory.Muted);
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
                var result = Data.Publish(context.Code, kind, field.text);
                if (result.Success) Back(); feedback.text = result.Message;
            });
            ui.Button("清空正文", row, "清空输入", () => field.text = "");
            ui.Paragraph(content, "最多" + limit + "字。清空后保存会撤下原内容；返回或刷新会放弃尚未保存的输入。", NationUiFactory.Muted);
            feedback.text = "正在编辑" + kind + "；尚未保存。";
        }

        private void PlayerRanking(NationRanking ranking)
        {
            var rows = Data.ReadPlayerRanking(context.Code, ranking);
            ui.Paragraph(content, "本国" + ranking + "榜 · 降序排列，同分按角色ID排序。\n当前数值快照，不改变任何玩家列表或国家成员顺序。");
            if (rows.Count == 0) ui.Paragraph(content, "本国还没有有效成员记录。加入国家并获得战功后，再刷新此榜单。");
            for (int i = 0; i < rows.Count; i++)
            {
                var item = rows[i]; string mark = item.Id == Data.ActorId ? "【我】" : "";
                Link((i + 1) + "  " + item.Name + mark + "\n" + ranking + " " + N(ranking == NationRanking.战功 ? item.Merit : item.Contribution) + " · " + item.Office,
                    "角色资料", () => Navigate(NationPage.玩家资料, item.Id));
            }
            if (ranking == NationRanking.贡献) ui.Paragraph(content, "贡献按已有角色字段显示。当前工程未建立独立国库捐献规则，不生成捐献成功或虚构贡献。", NationUiFactory.Muted);
        }

        private void ShowNationRanking()
        {
            var rows = Data.ReadNationRanking();
            ui.Paragraph(content, "国土榜 · 以当前真实城池归属统计，同城池数按国家ID、国号排序。\n列表为独立快照，可查看任意国家的正确资料。");
            if (rows.Count == 0) ui.Paragraph(content, "本地世界尚无国家记录。请创建或载入有效世界后刷新。");
            foreach (var nation in rows)
            {
                var item = nation;
                Link(item.Rank + "  " + item.Name + "（" + item.Code + "）\n城池 " + item.Cities.Count + " · " + item.Scale, "国家资料", () => OpenExistingNation(item.Code));
            }
        }

        private void Election()
        {
            ui.Paragraph(content, "轮选周期：" + snapshot.ElectionInterval + "秒（本工程本地记录）\n上次记录：" + SafeDate(snapshot.LastElection));
            timerText = ui.Text("轮选计时", content, "", 17, NationUiFactory.Gold); timerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
            ui.Paragraph(content, "现有世界任务到期只更新时间戳，没有国王投票、候选票数或自动任免操作。这里显示真实周期，不把俸禄冷却当作轮选时间。\n国家职务由本地国王在国家管理页任免；战功俸禄官职由已有战功档位决定。");
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
            ui.Paragraph(content, "本地国家管理规则：国王任免国家职务；国王或丞相编辑公告；国王编辑宣言。\n任命只保存国家职务，不更改战功俸禄官职。每名非国王成员可任一职，改任会免去原职。");
            Link("公告发布与撤下", "查看公告", () => Navigate(NationPage.公告));
            Link("国家宣告", "查看宣告", () => Navigate(NationPage.宣言));
            foreach (var appointment in snapshot.Appointments)
                ui.Paragraph(content, appointment.Key + "：" + (appointment.Value == null ? "未任命有效成员" : appointment.Value.Name), NationUiFactory.Gold);
            Link("选择成员，查看资料并任免", "国民列表", () => Navigate(NationPage.成员));
            if (!snapshot.CanManage) ui.Paragraph(content, "当前身份为管理查看权限，任免按钮仅向本国国王开放。", NationUiFactory.Muted);
        }

        private void Appointment()
        {
            var member = Data.ReadPlayer(context.PlayerId);
            if (!snapshot.CanManage || member == null || member.NationCode != context.Code || member.Id == snapshot.KingId)
            { ui.Paragraph(content, "当前身份或成员状态不能执行任免。请返回国民列表刷新。"); return; }
            ui.Paragraph(content, "任命对象：" + member.Name + "（ID " + member.Id + "）\n本地职务记录；保存至已有国家字段。替换任命会免去该职的原成员。", NationUiFactory.Gold);
            foreach (var pair in snapshot.Appointments)
            {
                var office = pair.Key; var holder = pair.Value; var row = ui.Row(content, 56);
                ui.RowText(row, office + "\n现任：" + (holder == null ? "无有效任命" : holder.Name));
                ui.Button("任命" + office, row, "任命", () => ConfirmAppointment(office, context.PlayerId));
                ui.Button("免去" + office, row, "免职", () => ConfirmAppointment(office, -1)).interactable = holder != null;
            }
        }

        private void ConfirmAppointment(NationOffice office, int id)
        {
            history.Push(context); context.Page = NationPage.确认任免; context.PlayerId = id; context.ActorId = Data.ActorId; context.Office = office; Render();
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
            ui.Paragraph(content, "本地国家职务任免，不改变战功俸禄官职。改任时会免去目标成员原职；返回会取消此次操作。", NationUiFactory.Muted);
            var row = ui.Row(content, 40);
            ui.Button("确认任免", row, "确认任免", () =>
            {
                if (context.ActorId != Data.ActorId) { feedback.text = "当前角色已变化，请返回重新查看。"; return; }
                var result = Data.Appoint(context.Code, context.Office, context.PlayerId); if (result.Success) Back(); feedback.text = result.Message;
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
                timerText.text = fresh == null ? "国家记录已不存在，请返回刷新。" : fresh.ElectionRemaining > 0 ? "轮选周期剩余 " + TIME.ToTimeFormat(fresh.ElectionRemaining) : "轮选周期已到，等待本地世界任务更新时间戳。";
            }
        }

        private static string N(double value) { return NationDataSource.Number(value); }
        private static string Short(string value, int max) { value = value ?? ""; return value.Length <= max ? value : value.Substring(0, max) + "…"; }
        private static string SafeDate(long timestamp) { try { return TIME.TimeStampToDateTime(timestamp).ToString("yyyy-MM-dd HH:mm:ss"); } catch (ArgumentOutOfRangeException) { return "时间记录异常"; } }
    }
}
