using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dwsg.Social
{
    public static class 社交界面入口
    {
        public static ISocialAdapter Adapter { get; private set; }
        internal static SocialPanel Panel;
        private static bool externallyConfigured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
            SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            bool main = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<SocialPanel>() != null) return;
                if (root.GetComponent<主界面UI脚本>() != null) main = true;
            }
            if (!main) { Panel = null; return; }
            var host = new GameObject("窗口5社交控制器");
            SceneManager.MoveGameObjectToScene(host, scene);
            Panel = host.AddComponent<SocialPanel>();
            Panel.StartCoroutine(Initialize(Panel));
        }
        private static IEnumerator Initialize(SocialPanel panel)
        {
            // 主场景全局任务和导航管理器先完成初始化，不修改它们的脚本。
            yield return null;
            ConfigureUpdateNotice(panel.gameObject.scene);
            for (int frame = 0; frame < 120; frame++)
            {
                int index = 全局变量.本机身份;
                if (全局变量.主界面UI对象 != null && 全局变量.所有玩家数据表 != null && index >= 0 && index < 全局变量.所有玩家数据表.Count)
                {
                    if (!externallyConfigured)
                    {
                        var info = 全局变量.所有玩家数据表[index].基础信息;
                        // 仅读取本机身份，绝不遍历世界玩家表把 NPC 添加为联系人。
                        Adapter = new LocalSocialAdapter(new SocialPlayerDto
                        {
                            Id = "local-" + info.ID, Name = info.名字,
                            Level = Mathf.Clamp(Mathf.FloorToInt(info.等级), 1, 999),
                            Country = string.IsNullOrEmpty(info.国家) ? "无" : info.国家
                        }, Guid.NewGuid().ToString("N"));
                    }
                    externallyConfigured = false;
                    panel.Initialize(Adapter);
                    yield break;
                }
                yield return null;
            }
            Debug.LogWarning("社交界面未初始化：主场景本机身份尚未准备好");
        }
        private static void ConfigureUpdateNotice(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "更新内容") continue;
                var region = root.transform.Find("道具布局信息") as RectTransform;
                var scroll = region == null ? null : region.GetComponent<ScrollRect>();
                if (scroll == null || scroll.content == null || scroll.viewport == null || scroll.verticalScrollbar == null) return;
                var content = scroll.content;
                var viewport = scroll.viewport;
                var textNode = content.Find("说明文本");
                var text = textNode == null ? null : textNode.GetComponent<Text>();
                if (text == null) return;

                // 保留原正文左边界，为原滚动条列留出半列宽的正文间距。
                Rect bodyBounds = NoticeBounds(region, content);
                Rect barBounds = NoticeBounds(region, scroll.verticalScrollbar.transform as RectTransform);
                float bodyRight = barBounds.xMin - barBounds.width * .5f;
                float bodyWidth = bodyRight - bodyBounds.xMin;
                if (bodyWidth <= 0f) return;
                viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bodyWidth);
                var viewportPosition = viewport.localPosition;
                viewportPosition.x = (bodyBounds.xMin + bodyRight) * .5f - viewport.rect.center.x;
                viewport.localPosition = viewportPosition;
                var mask = viewport.GetComponent<RectMask2D>();
                if (mask == null) mask = viewport.gameObject.AddComponent<RectMask2D>();
                mask.enabled = true;

                // 原透明内容 Image 继续承接正文拖动，裁剪只作用于正文，不覆盖右侧箭头。
                float previousHeight = content.rect.height;
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = new Vector2(0f, previousHeight);
                text.raycastTarget = false;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                var layout = content.GetComponent<VerticalLayoutGroup>();
                if (layout == null) layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset();
                layout.spacing = 0f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                var fitter = content.GetComponent<ContentSizeFitter>();
                if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                // 隐藏公告在首次启用时由原生布局计算全文高度，不能在 inactive 时强制算成零高。
                ConfigureUpdateScrollbar(scroll.verticalScrollbar);
                return;
            }
        }
        private static void ConfigureUpdateScrollbar(Scrollbar scrollbar)
        {
            var bar = scrollbar.transform as RectTransform;
            var handle = scrollbar.handleRect;
            var area = handle == null ? null : handle.parent as RectTransform;
            if (bar == null || area == null) return;
            var arrows = area.GetComponentsInChildren<Button>(true);
            var pieces = handle.GetComponentsInChildren<Image>(true);
            if (arrows.Length != 2 || pieces.Length != 3) return;
            Array.Sort(arrows, (a, b) => b.transform.localPosition.y.CompareTo(a.transform.localPosition.y));
            Array.Sort(pieces, (a, b) => b.transform.localPosition.y.CompareTo(a.transform.localPosition.y));
            var topArrow = arrows[0].transform as RectTransform;
            var bottomArrow = arrows[1].transform as RectTransform;
            var top = pieces[0].rectTransform;
            var middle = pieces[1].rectTransform;
            var bottom = pieces[2].rectTransform;
            Rect topBounds = NoticeBounds(bar, topArrow);
            Rect bottomBounds = NoticeBounds(bar, bottomArrow);
            float topHeight = top.rect.height, bottomHeight = bottom.rect.height;
            float minimumHeight = topHeight + bottomHeight;
            const float arrowGap = 4f;
            float trackTop = topBounds.yMin - arrowGap;
            float trackBottom = bottomBounds.yMax + arrowGap;
            float innerHeight = trackTop - trackBottom - minimumHeight;
            if (innerHeight <= 0f) return;

            // 两端留出拼片高度，让最小 Handle 也能容纳原顶/底贴图；箭头保留原位置和事件。
            Vector3 topPosition = topArrow.position, bottomPosition = bottomArrow.position;
            area.anchorMin = area.anchorMax = area.pivot = new Vector2(.5f, .5f);
            area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, innerHeight);
            var areaPosition = area.localPosition;
            areaPosition.y = (trackTop + trackBottom) * .5f;
            area.localPosition = areaPosition;
            topArrow.position = topPosition;
            bottomArrow.position = bottomPosition;
            handle.pivot = new Vector2(.5f, .5f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(0f, minimumHeight);

            float topWidth = top.rect.width, middleWidth = middle.rect.width, bottomWidth = bottom.rect.width;
            top.anchorMin = top.anchorMax = top.pivot = new Vector2(.5f, 1f);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(topWidth, topHeight);
            bottom.anchorMin = bottom.anchorMax = bottom.pivot = new Vector2(.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = new Vector2(bottomWidth, bottomHeight);
            middle.anchorMin = new Vector2(.5f, 0f);
            middle.anchorMax = new Vector2(.5f, 1f);
            middle.pivot = new Vector2(.5f, .5f);
            middle.offsetMin = new Vector2(-middleWidth * .5f, bottomHeight);
            middle.offsetMax = new Vector2(middleWidth * .5f, -topHeight);
        }
        private static Rect NoticeBounds(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                Vector2 point = parent.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        public static bool 打开(string page)
        {
            if (Panel == null || !Panel.Ready) return false;
            var local = Adapter as LocalSocialAdapter;
            int index = 全局变量.本机身份;
            if (local != null && 全局变量.所有玩家数据表 != null && index >= 0 && index < 全局变量.所有玩家数据表.Count)
            {
                var info = 全局变量.所有玩家数据表[index].基础信息;
                if (local.CurrentPlayerId == "local-" + info.ID)
                    local.UpdateLocalPlayer(new SocialPlayerDto
                    {
                        Id = local.CurrentPlayerId, Name = info.名字, Level = Mathf.Clamp(Mathf.FloorToInt(info.等级), 1, 999),
                        Country = string.IsNullOrEmpty(info.国家) ? "无" : info.国家
                    });
            }
            Panel.Open(page); return true;
        }
        // 主窗口存档接线：将 ExportJson 放入世界存档的可选 social 字段，随同槽位原子写入。
        // 读档后使用世界自己的稳定 worldKey 和当前角色资料调用 RestoreForWorld；旧档 json 为空。
        // 新游戏/换槽位同样调用此方法，避免不同世界/角色之间泄漏关系。此模块不写独立文件。
        public static string ExportJson() { return Adapter == null ? null : Adapter.ExportJson(); }
        public static SocialResult ImportJson(string json)
        { return Adapter == null ? SocialResult.Fail("identity", "请先绑定当前世界和角色") : Adapter.ImportJson(json); }
        public static SocialResult RestoreForWorld(string worldKey, SocialPlayerDto self, string json)
        {
            if (string.IsNullOrEmpty(worldKey) || worldKey.Length > 128 || self == null || !LocalSocialAdapter.ValidId(self.Id) || !LocalSocialAdapter.ValidName(self.Name, 20))
                return SocialResult.Fail("invalid", "社交世界或角色身份无效");
            var adapter = new LocalSocialAdapter(self, worldKey);
            var result = string.IsNullOrEmpty(json) ? SocialResult.Local("已初始化空社交记录") : adapter.ImportJson(json);
            if (!result.Succeeded) return result;
            var profileResult = adapter.UpdateLocalPlayer(self);
            if (!profileResult.Succeeded) return profileResult;
            SetAdapter(adapter); return result;
        }
        // M02 使用认证会话实现 ISocialAdapter 后在主线程调用，界面随适配器重绑。
        public static void SetAdapter(ISocialAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");
            Adapter = adapter;
            // 允许存档加载器在切场景之前配置；配置明确携带到下一次主场景初始化。
            externallyConfigured = true;
            if (Panel != null && Panel.Ready) Panel.Initialize(adapter);
        }
        public static void Reset()
        { if (Adapter != null) Adapter.Reset(); }
        internal static void Detached(SocialPanel panel)
        {
            if (Panel != panel) return;
            Panel = null;
            // 场景切换后的本地关系由世界存档 RestoreForWorld 接回，不能误复用上一局。
            if (!externallyConfigured) Adapter = null;
        }
    }
}
