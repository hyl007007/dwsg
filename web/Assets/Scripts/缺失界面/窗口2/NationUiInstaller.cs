using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace 缺失界面.窗口2
{
    public sealed class NationUiInstaller : MonoBehaviour
    {
        public NationDetailWindow Window { get; private set; }
        private Transform originalCountry;
        private 显示概况脚本 overview;
        private NationOverviewNotices notices;
        private Toggle overviewTab;
        private 显示国家列表 nationList;
        private readonly List<Selectable> ownControls = new List<Selectable>();
        private readonly Dictionary<Selectable, bool> savedControlStates = new Dictionary<Selectable, bool>();
        private string ViewedCode { get { return overview == null ? NationDataSource.Current.OwnNationCode : overview.当前查看国号; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded;
            SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GameObject country = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<NationUiInstaller>() != null) return;
                if (root.name == "国家信息界面UI") country = root;
            }
            if (country == null) return;
            var gameObject = new GameObject("窗口2国家界面安装器"); SceneManager.MoveGameObjectToScene(gameObject, scene);
            gameObject.AddComponent<NationUiInstaller>();
        }

        private IEnumerator Start()
        {
            // Run after the shared navigator's initial scene registration. No recurring scene scans.
            yield return null;
            Transform country = null;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects()) if (root.name == "国家信息界面UI") country = root.transform;
            if (country == null) yield break;
            InstallFor(country);
        }

        public bool InstallFor(Transform country)
        {
            if (Window != null) return true;
            originalCountry = country; overview = country.GetComponentInChildren<显示概况脚本>(true);
            var root = new GameObject("窗口2国家详情", typeof(RectTransform)); root.SetActive(false); root.layer = 5;
            SceneManager.MoveGameObjectToScene(root, country.gameObject.scene);
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 3;
            var scaler = root.AddComponent<CanvasScaler>(); var source = country.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = source != null ? source.uiScaleMode : CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = source != null ? source.referenceResolution : new Vector2(960, 540);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = source != null ? source.matchWidthOrHeight : 1;
            scaler.referencePixelsPerUnit = source != null ? source.referencePixelsPerUnit : 16;
            root.AddComponent<GraphicRaycaster>(); Window = root.AddComponent<NationDetailWindow>(); Window.Build(country);
            Window.ShowExistingNation = OpenOriginalNation;
            if (!界面窗口管理器.注册运行时窗口(root))
            { Debug.LogError("窗口2国家详情未能注册导航；未绑定国家入口。"); Destroy(root); Window = null; return false; }
            Bind(country, "概况布局/信息列表布局/国王/查看按钮", NationPage.国王);
            Bind(country, "概况布局/信息列表布局/国都/查看按钮", NationPage.国都);
            Bind(country, "概况布局/信息列表布局/城池数/查看按钮", NationPage.城池);
            Bind(country, "概况布局/信息列表布局/成员/查看按钮", NationPage.成员);
            Bind(country, "概况布局/信息列表布局/民生值/查看按钮", NationPage.民生);
            Bind(country, "概况布局/公告布局/查看按钮", NationPage.公告);
            Bind(country, "概况布局/宣告布局/查看按钮", NationPage.宣言);
            Bind(country, "个人布局/信息列表布局/战功/查看按钮", NationPage.战功);
            Bind(country, "个人布局/信息列表布局/贡献/查看按钮", NationPage.贡献);
            Bind(country, "个人布局/信息列表布局/轮选时间/查看按钮", NationPage.轮选);
            // The old rank callback opened change-country. Keep the existing office popup and correct its copy.
            Bind(country, "概况布局/信息列表布局/排名/查看按钮", NationPage.国家排行, true);
            var nationName = country.Find("概况布局/信息列表布局/国名/查看按钮");
            var nationNameButton = nationName == null ? null : nationName.GetComponent<Button>();
            if (nationNameButton != null)
            {
                nationNameButton.onClick.AddListener(() =>
                {
                    if (nationList != null) { 界面窗口动画.设置显示(nationList.gameObject, true); nationList.刷新显示(); }
                    else if (全局变量.提示类 != null) 全局变量.提示类.显示信息("国家列表未就绪，请返回重试。");
                });
                界面窗口管理器.注册运行时按钮(nationNameButton);
            }
            var ui = new NationUiFactory(country);
            var officeTextObject = country.Find("官职说明/官职说明文本"); var officeText = officeTextObject == null ? null : officeTextObject.GetComponent<Text>();
            if (officeText != null)
            {
                officeText.text = NationSalaryRules.Description(); officeText.supportRichText = false;
                officeText.horizontalOverflow = HorizontalWrapMode.Wrap; officeText.verticalOverflow = VerticalWrapMode.Truncate;
                officeText.resizeTextForBestFit = false; officeText.fontSize = 18; officeText.fontStyle = FontStyle.Normal;
                officeText.color = ui.BodyColor; officeText.lineSpacing = 1; officeText.alignment = TextAnchor.UpperLeft;
                ScrollRect rulesScroll;
                var rulesContent = ui.Scroll(officeTextObject.parent, out rulesScroll);
                NationUiFactory.CopyRegion(officeText.rectTransform, (RectTransform)rulesScroll.transform);
                officeText.transform.SetParent(rulesContent, false);
                rulesScroll.verticalNormalizedPosition = 1;
            }
            var officeTitle = country.Find("官职说明/官职信息");
            if (officeTitle != null) 原界面文字样式.标题(officeTitle.GetComponent<Text>());
            var footer = country.Find("界面操作");
            if (footer != null)
            {
                AddFooter(ui, footer, "国家管理", NationPage.管理, .50f);
                AddFooter(ui, footer, "国家排行", NationPage.国家排行, .71f);
            }
            notices = country.gameObject.AddComponent<NationOverviewNotices>();
            NationOriginalTabs.Install(country, Window, () => ViewedCode);
            var tab = country.Find("选项列表/切换/概况"); overviewTab = tab == null ? null : tab.GetComponent<Toggle>();
            foreach (string path in new[] { "个人布局", "选项列表/切换/个人", "概况布局/进入国库", "概况布局/征调兵马", "概况布局/信息列表布局/科技等级/查看按钮", "换国", "界面操作/国家管理" })
            {
                var own = country.Find(path); if (own == null) continue;
                foreach (var control in own.GetComponentsInChildren<Selectable>(true)) if (!ownControls.Contains(control)) ownControls.Add(control);
            }
            foreach (GameObject sceneRoot in country.gameObject.scene.GetRootGameObjects())
            {
                if (sceneRoot.name == "国家列表布局") nationList = sceneRoot.GetComponent<显示国家列表>();
                if (sceneRoot.name != "主界面UI") continue;
                var entry = sceneRoot.transform.Find("主界面_国家"); var button = entry == null ? null : entry.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => OpenOriginalNation(NationDataSource.Current.OwnNationCode));
            }
            return true;
        }

        private bool OpenOriginalNation(string code)
        {
            if (originalCountry == null || overview == null) return false;
            if (NationDataSource.Current.ReadNation(code) == null && (code ?? "") != NationDataSource.Current.OwnNationCode) return false;
            overview.查看国号 = code ?? "";
            bool foreign = overview.当前查看国号 != NationDataSource.Current.OwnNationCode;
            bool noNation = NationDataSource.Current.ReadNation(overview.当前查看国号) == null;
            var changeNation = originalCountry.Find("换国");
            foreach (var control in ownControls)
            {
                if (control == null) continue;
                bool changeEntry = changeNation != null && (control.transform == changeNation || control.transform.IsChildOf(changeNation));
                bool restricted = foreign || (noNation && !changeEntry);
                if (restricted)
                { if (!savedControlStates.ContainsKey(control)) savedControlStates[control] = control.interactable; control.interactable = false; }
                else if (savedControlStates.ContainsKey(control)) { control.interactable = savedControlStates[control]; savedControlStates.Remove(control); }
            }
            if (changeNation != null) NationOriginalControls.Caption(changeNation.GetComponent<Button>(), noNation ? "入国" : "换国", overview.国家名字);
            foreach (string path in new[] { "个人布局", "国库", "科技详细", "官职说明" })
            { var panel = originalCountry.Find(path); if (panel != null) panel.gameObject.SetActive(false); }
            if (overviewTab != null) overviewTab.isOn = true;
            var overviewPanel = originalCountry.Find("概况布局"); if (overviewPanel != null) overviewPanel.gameObject.SetActive(true);
            界面窗口动画.设置显示(originalCountry.gameObject, true); overview.刷新显示(); if (notices != null) notices.Refresh();
            return true;
        }

        private void AddFooter(NationUiFactory ui, Transform footer, string name, NationPage page, float x)
        {
            var button = ui.Button(name, footer, name, () => Window.Open(page, ViewedCode));
            var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(x, .5f);
            rect.sizeDelta = ui.ButtonSize; rect.anchoredPosition = new Vector2(0, -2.910904f);
        }

        private void Bind(Transform root, string path, NationPage page, bool replace = false)
        {
            var target = root.Find(path); var button = target == null ? null : target.GetComponent<Button>();
            if (button == null) { Debug.LogWarning("国家详情入口未找到：" + path); return; }
            if (replace) button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Window.Open(page, ViewedCode));
            界面窗口管理器.注册运行时按钮(button);
        }
    }

    // Refresh announcements when returning to the existing page. No background refresh loop.
    public sealed class NationOverviewNotices : MonoBehaviour
    {
        private Text notice, declaration, welfare;
        private 显示概况脚本 overview;
        private void Awake()
        {
            notice = Find("概况布局/公告布局/标题"); declaration = Find("概况布局/宣告布局/标题");
            welfare = Find("概况布局/信息列表布局/民生值/显示");
            overview = GetComponentInChildren<显示概况脚本>(true);
        }
        private Text Find(string path) { var target = transform.Find(path); return target == null ? null : target.GetComponent<Text>(); }
        private void OnEnable() { Refresh(); }
        public void Refresh()
        {
            var data = NationDataSource.Current; var country = data.ReadNation(overview == null ? data.OwnNationCode : overview.当前查看国号);
            SetSummary(notice, "公告：" + (country == null || country.Notice.Length == 0 ? "尚未发布" : Preview(country.Notice)));
            SetSummary(declaration, "宣告：" + (country == null || country.Declaration.Length == 0 ? "尚未发布" : Preview(country.Declaration)));
            if (welfare != null)
            {
                welfare.text = country == null ? "无国家" : NationDataSource.Number(country.Welfare);
                NationOriginalControls.SingleLine(welfare);
            }
        }
        private static void SetSummary(Text text, string value)
        {
            if (text == null) return;
            text.text = value; NationOriginalControls.SingleLine(text);
        }
        private static string Preview(string text) { text = text.Replace('\n', ' ').Replace('\r', ' '); return text.Length <= 26 ? text : text.Substring(0, 26) + "…"; }
    }
}
