using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dwsg.Window1
{
    // 将真实成就绑定到君主页原列表；原切换、网格、文字、头像和翻页组件继续使用。
    public sealed class Window1Achievements : MonoBehaviour
    {
        private readonly List<Row> rows = new List<Row>();
        private readonly List<Action> unbind = new List<Action>();
        private Window1Pages details;
        private Transform lord;
        private Toggle entry;
        private Text pageLabel, completedLabel;
        private Button previous, next;
        private int category, pageIndex;
        private bool ready;
        private Coroutine refreshLoop;
        private string signature;
        private sealed class Row
        {
            public GameObject Root;
            public Text Title, Progress;
            public Image Icon;
            public Color IconColor;
            public string Id;
        }

        public bool Initialize(Window1Pages detailPage, Transform lordRoot)
        {
            if (ready) return true;
            var list = transform.Find("成就列表布局");
            var template = list == null ? null : list.Find("成就信息布局");
            var grid = list == null ? null : list.GetComponent<GridLayoutGroup>();
            if (template == null || grid == null || template.Find("成就名字") == null || template.Find("成就进度") == null)
            { Debug.LogWarning("窗口1：君主页原成就行组件缺失，未装配成就绑定"); return false; }
            details = detailPage; lord = lordRoot;
            var entryTransform = lord.Find("选项列表/建筑信息切换/成就");
            entry = entryTransform == null ? null : entryTransform.GetComponent<Toggle>();
            pageLabel = Find<Text>("页数显示布局/页数数字显示");
            completedLabel = Find<Text>("称号达成显示");
            previous = Find<Button>("左翻页"); next = Find<Button>("右翻页");

            // 从原网格可视范围确定页容量，不改变网格参数或原行布局。
            var area = (RectTransform)list;
            int columns = Mathf.Max(1, Mathf.FloorToInt((area.rect.width - grid.padding.horizontal + grid.spacing.x) / Mathf.Max(1, grid.cellSize.x + grid.spacing.x)));
            int lines = Mathf.Max(1, Mathf.FloorToInt((area.rect.height - grid.padding.vertical + grid.spacing.y) / Mathf.Max(1, grid.cellSize.y + grid.spacing.y)));
            int capacity = Mathf.Clamp(columns * lines, 1, GoalCatalog.OfKind(GoalKind.Achievement).Count());
            var originals = new List<Transform> { template };
            // 先复制原纯 UI 行，再补点击动作，避免复制本模块的运行时监听。
            for (int i = 1; i < capacity; i++)
            {
                var copy = Instantiate(template.gameObject, list, false);
                copy.name = template.name;
                originals.Add(copy.transform);
            }
            foreach (var original in originals)
            {
                var row = new Row
                {
                    Root = original.gameObject,
                    Title = original.Find("成就名字").GetComponent<Text>(),
                    Progress = original.Find("成就进度").GetComponent<Text>(),
                    Icon = original.Find("成就头像").GetComponent<Image>()
                };
                row.IconColor = row.Icon.color;
                // 原 64×20.8 名称框保留，缩小字体给 Noto CJK 行高留出余量。
                row.Title.fontSize = 13; row.Title.supportRichText = false; row.Title.raycastTarget = false;
                row.Progress.supportRichText = false; row.Progress.raycastTarget = false;
                var button = original.gameObject.AddComponent<Button>();
                button.targetGraphic = original.Find("成就背景").GetComponent<Image>();
                button.targetGraphic.raycastTarget = true;
                Bind(button, () => { if (!string.IsNullOrEmpty(row.Id)) details.OpenAchievementDetail(row.Id); });
                rows.Add(row);
            }
            if (completedLabel != null) completedLabel.fontSize = 13;
            Bind(previous, () => ChangePage(-1)); Bind(next, () => ChangePage(1));
            string[] names = { "修身", "齐家", "治国", "平天下" };
            for (int i = 0; i < names.Length; i++)
            {
                var toggle = Find<Toggle>("切换布局/" + names[i]);
                if (toggle == null) continue;
                int selectedCategory = i;
                if (toggle.isOn) category = i;
                UnityAction<bool> action = on =>
                { if (on) { category = selectedCategory; pageIndex = 0; Refresh(true); } };
                toggle.onValueChanged.AddListener(action);
                unbind.Add(() => { if (toggle != null) toggle.onValueChanged.RemoveListener(action); });
            }
            ready = true;
            if (isActiveAndEnabled) BeginRefresh();
            return true;
        }

        private T Find<T>(string path) where T : Component
        { var child = transform.Find(path); return child == null ? null : child.GetComponent<T>(); }
        private void Bind(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
            unbind.Add(() => { if (button != null) button.onClick.RemoveListener(action); });
            界面窗口管理器.注册运行时按钮(button);
        }
        public void Open()
        {
            if (!ready) return;
            lord.gameObject.SetActive(true);
            if (entry != null) entry.isOn = true;
            // Toggle 已处于选中状态时无需重放其原有回调。
            gameObject.SetActive(true);
            Refresh(true);
        }
        public void ChangePage(int step)
        { pageIndex += step; Refresh(true); }
        private void OnEnable() { if (ready) BeginRefresh(); }
        private void BeginRefresh()
        {
            Window1Module.Changed -= OnChanged; Window1Module.Changed += OnChanged;
            Refresh(true);
            if (refreshLoop == null) refreshLoop = StartCoroutine(VisibleRefresh());
        }
        private void OnDisable()
        { Window1Module.Changed -= OnChanged; if (refreshLoop != null) StopCoroutine(refreshLoop); refreshLoop = null; }
        private void OnDestroy()
        { Window1Module.Changed -= OnChanged; foreach (var remove in unbind) remove(); }
        private void OnChanged() { Refresh(false); }
        private IEnumerator VisibleRefresh()
        { while (isActiveAndEnabled) { yield return new WaitForSecondsRealtime(2); Refresh(false); } }
        private static int Category(WorldMetric metric)
        {
            switch (metric)
            {
                case WorldMetric.LordLevel:
                case WorldMetric.Generals:
                case WorldMetric.GeneralLevel: return 0;
                case WorldMetric.Buildings:
                case WorldMetric.BuildingLevels:
                case WorldMetric.HallLevel: return 1;
                case WorldMetric.Technology: return 2;
                default: return 3;
            }
        }
        private void Refresh(bool force)
        {
            if (!ready || !isActiveAndEnabled) return;
            var service = Window1Module.Service;
            string error;
            if (service == null || !service.Refresh(out error)) return;
            var all = service.Goals(GoalKind.Achievement);
            var goals = all.Where(g => Category(g.Definition.Metric) == category).ToList();
            pageIndex = JournalService.ClampPage(pageIndex, goals.Count, rows.Count);
            string nextSignature = category + ":" + pageIndex + ":" + string.Join("|", all.Select(g => g.Definition.Id + g.Current + g.Claimed).ToArray());
            if (!force && signature == nextSignature) return;
            signature = nextSignature;
            if (completedLabel != null) completedLabel.text = "成就达成: " + all.Count(g => g.Complete) + "/" + all.Count;
            if (pageLabel != null) pageLabel.text = (pageIndex + 1) + "/" + JournalService.PageCount(goals.Count, rows.Count);
            if (previous != null) previous.interactable = pageIndex > 0;
            if (next != null) next.interactable = pageIndex + 1 < JournalService.PageCount(goals.Count, rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; int index = pageIndex * rows.Count + i;
                row.Root.SetActive(index < goals.Count);
                if (index >= goals.Count) { row.Id = null; continue; }
                var goal = goals[index]; row.Id = goal.Definition.Id;
                row.Title.text = goal.Definition.Title;
                row.Progress.text = goal.Current.ToString("0") + "/" + goal.Definition.Target.ToString("0") + " " + goal.Status;
                row.Icon.color = goal.Claimed ? row.IconColor * new Color(.6f, .6f, .6f, 1) : row.IconColor;
            }
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)rows[0].Root.transform.parent);
        }
    }
}
