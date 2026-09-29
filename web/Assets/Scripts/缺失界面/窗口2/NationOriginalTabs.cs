using System;
using System.Collections.Generic;
using Dwsg.Social;
using UnityEngine;
using UnityEngine.UI;

namespace 缺失界面.窗口2
{
    public sealed class NationOriginalTabs : MonoBehaviour
    {
        private NationUiFactory ui;
        private NationDetailWindow window;
        private Func<string> viewedCode;
        private string tab;
        private Text heading, status;
        private RectTransform content, footer;
        private ScrollRect scroll;
        private bool settleLayout;

        internal static void Install(Transform country, NationDetailWindow window, Func<string> viewedCode)
        {
            foreach (string name in new[] { "动态", "军团", "盟国" })
            {
                var toggleObject = country.Find("选项列表/切换/" + name);
                var toggle = toggleObject == null ? null : toggleObject.GetComponent<Toggle>();
                if (toggle == null) continue;
                var pane = country.Find(name + "布局");
                if (pane == null)
                {
                    // 原场景有盟国页签但没有内容布局，沿用概况页的中央区域。
                    var created = NationUiFactory.Rect(name + "布局", country);
                    var template = country.Find("概况布局") as RectTransform;
                    if (template == null) { Destroy(created.gameObject); continue; }
                    NationUiFactory.CopyRect(template, created); pane = created;
                }
                foreach (Transform child in pane) child.gameObject.SetActive(false);
                var root = NationUiFactory.Rect("窗口2" + name + "内容", pane);
                root.gameObject.SetActive(false);
                NationUiFactory.Place(root, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
                var panel = root.gameObject.AddComponent<NationOriginalTabs>();
                panel.Build(country, window, viewedCode, name);
                var target = pane;
                toggle.onValueChanged.AddListener(selected => target.gameObject.SetActive(selected));
                root.gameObject.SetActive(true); pane.gameObject.SetActive(toggle.isOn);
            }
        }

        private void Build(Transform country, NationDetailWindow detail, Func<string> context, string name)
        {
            ui = new NationUiFactory(country); window = detail; viewedCode = context; tab = name;
            NationUiFactory.Vertical((RectTransform)transform, 0);
            heading = ui.Text("页签标题", transform, "", 18, NationUiFactory.Gold);
            NationUiFactory.Height(heading, 30);
            status = ui.Text("页签说明", transform, "", 16, NationUiFactory.Muted);
            NationUiFactory.Height(status, 28);
            content = ui.Scroll(transform, out scroll);
            footer = NationUiFactory.Rect("页签操作", transform);
            NationUiFactory.Height(footer, ui.ButtonSize.y);
            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleRight; layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            Refresh();
        }

        private void OnEnable() { if (ui != null) Refresh(); }
        private void LateUpdate()
        {
            if (!settleLayout) return;
            settleLayout = false; FinishLayout();
        }
        private static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            { var child = parent.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        private void Refresh()
        {
            Clear(content); Clear(footer);
            var nation = NationDataSource.Current.ReadNation(viewedCode());
            heading.text = tab == "军团" ? "本机军团" : (nation == null ? "未加入国家" : nation.Name) + " · " + tab;
            if (tab == "动态") Dynamics(nation);
            else if (tab == "军团") Guilds();
            else Allies(nation);
            ui.Button("刷新页签", footer, "刷新", Refresh);
            FinishLayout(); settleLayout = true;
            scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
        }
        private void FinishLayout()
        {
            Canvas.ForceUpdateCanvases();
            NationUiFactory.Height(heading, Mathf.Ceil(heading.preferredHeight + 4));
            NationUiFactory.Height(status, Mathf.Ceil(status.preferredHeight + 4));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            NationUiFactory.MeasureContent(content);
        }
        private void Link(string text, string caption, UnityEngine.Events.UnityAction action, bool enabled = true)
        {
            var row = ui.Row(content); ui.ListBackground(row); ui.RowText(row, text);
            ui.Button(caption, row, caption, action).interactable = enabled;
        }
        private void Dynamics(NationSnapshot nation)
        {
            status.text = "查看当前公告、国家宣告与轮选信息。";
            if (nation == null)
            {
                ui.Paragraph(content, "尚未加入国家，加入后可查看本国公告和轮选。");
                return;
            }
            Link("公告：" + Preview(nation.Notice, "尚未发布"), "查看公告", () => window.Open(NationPage.公告, viewedCode()));
            Link("宣告：" + Preview(nation.Declaration, "尚未发布"), "国家宣告", () => window.Open(NationPage.宣言, viewedCode()));
            Link("本国成员 " + nation.Members.Count + " 人 · 距下次轮选 " + TIME.ToTimeFormat(nation.ElectionRemaining),
                "轮选详情", () => window.Open(NationPage.轮选, viewedCode()));
        }
        private void Guilds()
        {
            status.text = "查看本世界军团，创建、申请与公告管理请进入军团页。";
            var adapter = 社交界面入口.Adapter;
            var state = adapter == null ? null : adapter.Snapshot();
            if (state == null) ui.Paragraph(content, "军团资料正在准备，可稍后刷新或打开军团页。");
            else if (state.Guilds.Count == 0) ui.Paragraph(content, "本世界还没有军团记录。可在军团页创建本地军团。");
            else
            {
                foreach (var guild in state.Guilds)
                {
                    string leader = guild.Leader;
                    foreach (var player in state.Players) if (player.Id == guild.Leader) { leader = player.Name; break; }
                    Link(guild.Name + " · " + guild.Members.Count + "/5\n团长：" + leader, "军团列表", () => OpenSocial("军团"));
                }
            }
            ui.Button("打开军团", footer, "进入军团", () => OpenSocial("军团"));
        }
        private void Allies(NationSnapshot nation)
        {
            var data = NationDataSource.Current;
            status.text = "盟友名单与所属国家 · 以当前世界已记录的关系为准。";
            int count = 0;
            if (viewedCode() == data.OwnNationCode && 全局变量.盟友列表 != null)
            {
                var seen = new HashSet<int>();
                foreach (double id in 全局变量.盟友列表)
                {
                    if (double.IsNaN(id) || double.IsInfinity(id) || id < 0 || id > int.MaxValue || id != Math.Truncate(id) || !seen.Add((int)id)) continue;
                    var player = data.ReadPlayer((int)id); if (player == null || player.Id == data.ActorId) continue;
                    var alliedNation = data.ReadNation(player.NationCode);
                    string code = player.NationCode;
                    Link("盟友：" + player.Name + "\n所属国家：" + (alliedNation == null ? "无国家" : alliedNation.Name + "（" + code + "）"),
                        "查看国家", () => window.OpenExistingNation(code), alliedNation != null);
                    count++;
                }
            }
            if (count == 0)
                ui.Paragraph(content, nation == null ? "尚未加入国家，也没有可查看的盟国资料。" : viewedCode() == data.OwnNationCode ?
                    "本世界尚未记录本机盟友，暂无盟国资料。可查看其他国家，或在社交页管理好友。" : "本世界尚未记录该国的盟国资料，可查看其他国家。");
            ui.Button("社交关系", footer, "社交关系", () => OpenSocial("好友"));
        }
        private void OpenSocial(string page)
        {
            if (!社交界面入口.打开(page)) { status.text = "社交页尚未就绪，请稍后重试。"; settleLayout = true; }
        }
        private static string Preview(string text, string empty)
        {
            text = (text ?? "").Replace('\n', ' ').Replace('\r', ' ');
            return text.Length == 0 ? empty : text.Length <= 28 ? text : text.Substring(0, 28) + "…";
        }
    }
}
