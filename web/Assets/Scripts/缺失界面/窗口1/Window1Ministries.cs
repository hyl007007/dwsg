using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dwsg.Window1
{
    // 在原君主六部空布局内补组件；保留原皮肤、页签、关闭X及原君主页。
    // 仅六部页激活时每秒更新文字/到期按钮；模型不运行后台永久协程。
    public sealed class Window1Ministries : MonoBehaviour
    {
        private Window1Style style;
        private Text resources, feedback;
        private ScrollRect scroll;
        private readonly List<Toggle> tabs = new List<Toggle>();
        private readonly List<Row> rows = new List<Row>();
        private int department, usedRows;
        private bool ready;
        private Coroutine refreshLoop;
        private static readonly string[] Departments = { "吏部", "户部", "礼部", "兵部", "工部", "刑部" };
        private sealed class Row
        {
            public RectTransform Root;
            public Text Title, Detail;
            public Button Action, Cancel;
            public Func<MinistryResult> Command, CancelCommand;
        }
        public bool Initialize(Window1Style sceneStyle, Transform lord)
        {
            if (ready) return true;
            if (sceneStyle == null || sceneStyle.Font == null || lord == null || transform.parent != lord || transform.childCount != 0)
            { Debug.LogWarning("窗口1：原六部空布局或皮肤不可用，未装配六部"); return false; }
            style = sceneStyle;
            var tabStrip = Window1Style.Rect(transform, "六部页签布局");
            Window1Style.Place(tabStrip, 12, 0, 619, 32);
            Window1Style.ControlRow(tabStrip);
            for (int i = 0; i < Departments.Length; i++)
            {
                int selected = i;
                var tab = style.Tab(tabStrip, "六部_" + Departments[i], Departments[i], () => {
                    department = selected; scroll.verticalNormalizedPosition = 1; feedback.text = ""; Refresh();
                });
                tabs.Add(tab);
            }
            resources = Label(transform, "六部资源", "", 14, Window1Style.Gold, 14, 36, 610, 26);
            feedback = Label(transform, "六部反馈", "", 14, Window1Style.Ink, 14, 298, 610, 26);
            var area = Window1Style.Rect(transform, "六部条目列表"); Window1Style.Place(area, 12, 66, 619, 224);
            scroll = area.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Window1Style.Rect(area, "可视区域"); Window1Style.Anchors(viewport, Vector2.zero, Vector2.one);
            viewport.offsetMax = new Vector2(-20, 0); viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var content = Window1Style.Rect(viewport, "内容"); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = style.VerticalScrollbar(area);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            ready = true;
            if (isActiveAndEnabled) BeginRefresh();
            return true;
        }
        private Text Label(Transform parent, string name, string text, int size, Color color, float x, float y, float w, float h)
        {
            var label = Window1Style.Text(parent, name, text, style.Font, size, color);
            Window1Style.Place(label.rectTransform, x, y, w, h); return label;
        }
        private Row NewRow()
        {
            var row = new Row();
            row.Root = Window1Style.Rect(scroll.content, "条目" + rows.Count);
            var background = Window1Style.Image(row.Root, "底色", new Color(.04f, .20f, .18f));
            Window1Style.Anchors(background.rectTransform, Vector2.zero, Vector2.one);
            row.Title = Label(row.Root, "标题", "", 16, Window1Style.Gold, 8, 2, 422, 26);
            row.Detail = Label(row.Root, "说明", "", 14, Window1Style.Ink, 8, 31, 422, 46);
            row.Action = style.Button(row.Root, "操作", "", () => Execute(row.Command));
            Window1Style.Place(row.Action.GetComponent<RectTransform>(), 440, 22, 114, 34);
            row.Cancel = style.CloseButton(row.Root, () => Execute(row.CancelCommand)); row.Cancel.name = "撤销待办";
            Window1Style.Place(row.Cancel.GetComponent<RectTransform>(), 563, 25, 28, 28);
            界面窗口管理器.注册运行时按钮(row.Action); 界面窗口管理器.注册运行时按钮(row.Cancel);
            rows.Add(row); return row;
        }
        private void Add(string title, string detail, string action = "", Func<MinistryResult> command = null, Func<MinistryResult> cancel = null)
        {
            var row = usedRows < rows.Count ? rows[usedRows] : NewRow();
            Window1Style.Place(row.Root, 0, usedRows * 86, 599, 82); usedRows++;
            row.Root.gameObject.SetActive(true); row.Title.text = title; row.Detail.text = detail;
            row.Command = command; row.CancelCommand = cancel;
            row.Action.GetComponentInChildren<Text>(true).text = action;
            row.Action.gameObject.SetActive(!string.IsNullOrEmpty(action)); row.Action.interactable = command != null;
            row.Cancel.gameObject.SetActive(cancel != null);
        }
        private void Execute(Func<MinistryResult> action)
        {
            if (action == null) return;
            var result = action(); feedback.text = result.Message;
            feedback.color = result.Success ? Window1Style.Gold : Window1Style.Ink;
            Refresh();
        }
        private void OnEnable() { if (ready) BeginRefresh(); }
        private void BeginRefresh() { Refresh(); if (refreshLoop == null) refreshLoop = StartCoroutine(VisibleRefresh()); }
        private IEnumerator VisibleRefresh()
        { while (isActiveAndEnabled) { yield return new WaitForSecondsRealtime(1); if (isActiveAndEnabled) Refresh(); } }
        private void OnDisable() { if (refreshLoop != null) StopCoroutine(refreshLoop); refreshLoop = null; }
        private void JobRow(SixMinistriesService service, MinistryJob job, string title, string reward)
        {
            bool due = service.Now >= job.ReadyAt;
            Add(title, (due ? "已到期，可领取" : "剩余 " + (job.ReadyAt - service.Now) + " 秒") + " · " + reward + "\n右侧X撤销待办，费用不退还。",
                due ? "领取" : "进行中", due ? (Func<MinistryResult>)(() => ClaimForDisplay(service, job)) : null, () => service.Cancel(job.Id));
        }
        private static MinistryResult ClaimForDisplay(SixMinistriesService service, MinistryJob job)
        {
            var result = service.Claim(job.Id);
            if (result.Success && job.Kind == MinistryJobKind.Recruit)
                result.Message = "已招募" + service.Rules.Candidates[job.Candidate] + "，已加入文官名册";
            else if (result.Success && job.Kind == MinistryJobKind.Persuasion)
            {
                var recruited = service.Roster.FirstOrDefault(o => o.Id == job.TargetId);
                if (recruited != null) result.Message = "策反完成，" + recruited.Name + "已加入文官名册";
            }
            return result;
        }
        private static string OfficerTitle(MinistryOfficer officer, SixMinistriesConfig config)
        { return officer.Name + " · " + (1 + (int)(officer.Experience / config.ExperiencePerLevel)) + "级"; }
        private void Refresh()
        {
            if (!ready || !isActiveAndEnabled) return;
            var service = SixMinistriesModule.Service; usedRows = 0;
            for (int i = 0; i < tabs.Count; i++) tabs[i].SetIsOnWithoutNotify(i == department);
            if (service == null) { resources.text = "当前君主尚未就绪"; Add("六部政务", "载入世界后可办理政务。"); }
            else if (!service.World.ResourcesValid())
            { resources.text = "资源资料异常"; Add("政务暂不可办理", "请检查当前世界资源资料。此次不扣款或发奖。"); }
            else
            {
                try
                {
                    var config = service.Rules; var money = service.World.Actor.财产信息;
                    resources.text = "文官 " + service.Roster.Count + "/" + config.RosterLimit + "    铜钱 " + money.铜钱.ToString("0") + "    粮食 " + money.粮食.ToString("0");
                    if (department == 0) Officials(service, config);
                    else if (department == 1) Farming(service, config);
                    else if (department == 2) Relief(service, config);
                    else if (department == 3) Military(service, config);
                    else if (department == 4) Study(service, config);
                    else Persuasion(service, config);
                }
                catch (InvalidOperationException ex)
                { usedRows = 0; resources.text = "政务资料暂不可用"; Add("六部政务", ex.Message); }
            }
            for (int i = usedRows; i < rows.Count; i++) { rows[i].Command = rows[i].CancelCommand = null; rows[i].Root.gameObject.SetActive(false); }
            scroll.content.sizeDelta = new Vector2(0, Mathf.Max(224, usedRows * 86 - 4));
        }
        private void Officials(SixMinistriesService service, SixMinistriesConfig config)
        {
            for (int i = 0; i < config.Candidates.Length; i++)
            {
                int candidate = i;
                var job = service.PendingJobs.FirstOrDefault(j => j.Kind == MinistryJobKind.Recruit && j.Candidate == candidate);
                if (job != null) JobRow(service, job, "招募 · " + config.Candidates[i], "领取后加入文官名册");
                else Add("候选 · " + config.Candidates[i], "铜钱 " + config.RecruitCopper + " · " + config.RecruitSeconds + " 秒\n待领取招募也占用名额，名册上限 " + config.RosterLimit + "。", "招募", () => service.StartRecruit(candidate));
            }
            foreach (var officer in service.Roster)
                Add(OfficerTitle(officer, config), "文官经验 " + officer.Experience + " · " + (service.IsBusy(officer) ? "正在执行政务" : "空闲，可派遣或解雇"),
                    "解雇", service.IsBusy(officer) ? null : (Func<MinistryResult>)(() => service.Dismiss(officer)));
        }
        private void Farming(SixMinistriesService service, SixMinistriesConfig config)
        {
            for (int i = 0; i < config.PlotCount; i++)
            {
                int plot = i;
                var job = service.PendingJobs.FirstOrDefault(j => j.Kind == MinistryJobKind.Plant && j.Slot == plot);
                if (job != null) JobRow(service, job, "地块 " + (i + 1), "粮食+" + config.PlantGrain);
                else Add("地块 " + (i + 1), "铜钱 " + config.PlantCopper + " · " + config.PlantSeconds + " 秒\n采摘粮食+" + config.PlantGrain + "，每轮仅领取一次。", "种植", () => service.StartPlant(plot));
            }
        }
        private void Relief(SixMinistriesService service, SixMinistriesConfig config)
        {
            if (service.Roster.Count == 0) Add("赈济派遣", "先在吏部招募文官，再派空闲文官赈济。");
            foreach (var officer in service.Roster)
            {
                var job = service.PendingJobs.FirstOrDefault(j => j.Kind == MinistryJobKind.Relief && j.OfficerId == officer.Id);
                if (job != null) JobRow(service, job, "赈济 · " + OfficerTitle(officer, config), "铜钱+" + config.ReliefRewardCopper + "、经验+" + config.ReliefExperience);
                else Add(OfficerTitle(officer, config), "铜钱 " + config.ReliefCopper + "、粮食 " + config.ReliefGrain + " · " + config.ReliefSeconds + " 秒\n完成：铜钱+" + config.ReliefRewardCopper + "、文官经验+" + config.ReliefExperience, "赈济",
                    service.IsBusy(officer) ? null : (Func<MinistryResult>)(() => service.StartRelief(officer)));
            }
        }
        private void Military(SixMinistriesService service, SixMinistriesConfig config)
        {
            var snapshot = service.Snapshot();
            foreach (bool attack in new[] { true, false })
            {
                bool attacking = attack; var kind = attack ? MinistryJobKind.MilitaryAttack : MinistryJobKind.MilitaryDefense;
                string title = attack ? "攻击军务" : "防御军务";
                var job = service.PendingJobs.FirstOrDefault(j => j.Kind == kind);
                var buff = snapshot.Buffs.FirstOrDefault(b => b.Owner == service.ActorKey && b.Kind == kind && b.ExpiresAt > service.Now);
                if (job != null) JobRow(service, job, title, "+" + config.MilitaryBonus * 100 + "% · " + config.BuffSeconds / 60 + "分钟");
                else if (buff != null) Add(title + " · 已生效", "加成 " + buff.Bonus * 100 + "% · 剩余 " + (buff.ExpiresAt - service.Now) + " 秒\n同类军务不可叠加或续期。", "生效中");
                else Add(title, "铜钱 " + config.MilitaryCopper + "、粮食 " + config.MilitaryGrain + " · " + config.MilitarySeconds + " 秒\n领取后+" + config.MilitaryBonus * 100 + "%持续" + config.BuffSeconds / 60 + "分钟，同类不叠加。", "办理", () => service.StartMilitary(attacking));
            }
        }
        private void Study(SixMinistriesService service, SixMinistriesConfig config)
        {
            if (service.Roster.Count == 0) Add("文官研习", "可先到吏部招募文官，或选择下方空闲将领训练。");
            foreach (var officer in service.Roster)
            {
                var job = service.PendingJobs.FirstOrDefault(j => j.Kind == MinistryJobKind.Study && j.OfficerId == officer.Id);
                if (job != null) JobRow(service, job, "研习 · " + OfficerTitle(officer, config), "文官经验+" + config.StudyExperience);
                else Add(OfficerTitle(officer, config), "铜钱 " + config.StudyCopper + " · " + config.StudySeconds + " 秒\n研习经验+" + config.StudyExperience + "，忙碌文官不可派遣。", "研习",
                    service.IsBusy(officer) ? null : (Func<MinistryResult>)(() => service.StartStudy(officer)));
            }
            var generals = service.World.Generals();
            if (generals.Count == 0) Add("将领训练", "当前没有已登记将领，可从原将领页招募。");
            foreach (var general in generals)
            {
                bool valid = general.将领属性 != null && general.将领属性.初始属性 != null && general.将领属性.成长点数 != null && general.详细信息 != null;
                if (!valid) continue;
                string title = general.将领属性.初始属性.名字 + " · " + general.将领属性.成长点数.等级 + "级";
                double level = general.将领属性.成长点数.等级;
                bool idle = general.详细信息.状态 == 0 && SixMinistriesConfig.Number(level) && level >= 1 && level < 99;
                Add(title, idle ? "铜钱 " + SixMinistriesAdapter.TrainingCopper(general) + "、体力 " + SixMinistriesAdapter.TrainingStamina + "\n即时经验+" + SixMinistriesAdapter.TrainingExperience(general) + "（本级升级经验10%）。" : "将领忙碌或已满级，不能训练。",
                    "训练", idle ? (Func<MinistryResult>)(() => service.TrainGeneral(general)) : null);
            }
        }
        private void Persuasion(SixMinistriesService service, SixMinistriesConfig config)
        {
            var job = service.PendingJobs.FirstOrDefault(j => j.Kind == MinistryJobKind.Persuasion);
            if (job != null)
            {
                var pendingTarget = service.NpcTargets.FirstOrDefault(o => o.Id == job.TargetId);
                JobRow(service, job, "策反目标 · " + (pendingTarget == null ? "目标文官" : pendingTarget.Name), "策反进度+" + config.PersuasionStep);
            }
            if (service.NpcTargets.Count == 0) Add("策反名册", "当前本地官署已没有可策反的文官。");
            foreach (var target in service.NpcTargets)
                Add(OfficerTitle(target, config), "地方官署（本地NPC） · 进度 " + target.Persuasion + "/" + config.PersuasionGoal + "\n铜钱 " + config.PersuasionCopper + " · " + config.PersuasionSeconds + "秒 · 完成+" + config.PersuasionStep, "策反",
                    job != null || service.IsBusy(target) ? null : (Func<MinistryResult>)(() => service.StartPersuasion(target)));
        }
    }
}
