using System;
using UnityEngine;
using UnityEngine.UI;

namespace 缺失界面.窗口4
{
    // 只复制图像/矩形的视觉属性，绝不克隆旧界面逻辑、事件或 Animator。
    public sealed class 军事界面样式
    {
        public Font 字体;
        public Sprite 按钮图;
        public Transform 参考;
        public readonly Color 正文色 = new Color(0.88f, 0.95f, 0.89f);
        public readonly Color 金色 = new Color(1f, 0.9f, 0.48f);

        public 军事界面样式(Transform 参考)
        {
            this.参考 = 参考;
            if (参考 != null)
            {
                foreach (var 文本 in 参考.GetComponentsInChildren<Text>(true))
                    if (文本.font != null) { 字体 = 文本.font; break; }
                foreach (var 按钮 in 参考.GetComponentsInChildren<Button>(true))
                {
                    var 图 = 按钮.GetComponent<Image>();
                    if (图 != null && 图.sprite != null && 按钮.name == "查看按钮") { 按钮图 = 图.sprite; break; }
                }
            }
            if (字体 == null) 字体 = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static RectTransform 矩形(string 名字, Transform 父级, Vector2 尺寸, Vector2 位置)
        {
            var 对象 = new GameObject(名字, typeof(RectTransform));
            var 矩形 = 对象.GetComponent<RectTransform>();
            矩形.SetParent(父级, false);
            矩形.anchorMin = 矩形.anchorMax = new Vector2(0.5f, 0.5f);
            矩形.sizeDelta = 尺寸;
            矩形.anchoredPosition = 位置;
            return 矩形;
        }

        public static void 拉伸(RectTransform 矩形, float 留边 = 0)
        {
            矩形.anchorMin = Vector2.zero;
            矩形.anchorMax = Vector2.one;
            矩形.offsetMin = new Vector2(留边, 留边);
            矩形.offsetMax = new Vector2(-留边, -留边);
        }

        public static Transform 原改名窗口(Transform 参考)
        {
            if (参考 == null || !参考.gameObject.scene.IsValid()) return null;
            foreach (var 根 in 参考.gameObject.scene.GetRootGameObjects())
                if (根.name == "将领改名界面") return 根.transform;
            return null;
        }

        public InputField 原样输入(RectTransform 框, string 初始值, bool 整数, int 字数)
        {
            var 接收点击 = 框.gameObject.AddComponent<Image>();
            接收点击.color = Color.clear;
            接收点击.raycastTarget = true;
            var 原窗口 = 原改名窗口(参考);
            var 原皮肤 = 原窗口 != null ? 原窗口.Find("输入框布局") : null;
            Image 中图 = null;
            if (原皮肤 != null)
            {
                foreach (string 名 in new[] { "左", "中", "右" })
                {
                    var 原图 = 原皮肤.Find(名);
                    var 图框 = 复制视觉(原图, 框);
                    if (图框 == null) continue;
                    图框.pivot = new Vector2(.5f, .5f);
                    if (名 == "中")
                    {
                        图框.anchorMin = new Vector2(0, .5f);
                        图框.anchorMax = new Vector2(1, .5f);
                        图框.sizeDelta = new Vector2(-62, 26);
                        图框.anchoredPosition = Vector2.zero;
                        中图 = 图框.GetComponent<Image>();
                    }
                    else
                    {
                        图框.anchorMin = 图框.anchorMax = new Vector2(名 == "左" ? 0 : 1, .5f);
                        图框.sizeDelta = new Vector2(31, 26);
                        图框.anchoredPosition = new Vector2(名 == "左" ? 15.5f : -15.5f, 0);
                    }
                }
            }
            var 输入 = 框.gameObject.AddComponent<InputField>();
            输入.targetGraphic = 中图 != null ? 中图 : 接收点击;
            var 字 = 文本(框, "输入文字", "", Vector2.zero, Vector2.zero, 16);
            拉伸(字.rectTransform);
            字.rectTransform.offsetMin = new Vector2(8, 0);
            字.rectTransform.offsetMax = new Vector2(-8, 0);
            var 原字 = 原窗口 != null ? 原窗口.Find("原名字").GetComponent<Text>() : null;
            if (原字 != null) { 字.font = 原字.font; 字.color = 原字.color; }
            输入.textComponent = 字;
            输入.contentType = 整数 ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
            输入.characterLimit = 字数;
            原界面文字样式.单行输入(字, null, TextAnchor.MiddleLeft, 16);
            输入.SetTextWithoutNotify(初始值 ?? "");
            return 输入;
        }

        public RectTransform 复制视觉(Transform 来源, Transform 父级, bool 包含按钮图像 = false)
        {
            if (来源 == null || (!包含按钮图像 && 来源.GetComponent<Button>() != null) || 来源.GetComponent<Text>() != null) return null;
            var 来源图 = 来源.GetComponent<Image>();
            // 8099是原界面的“君主”文字图，标题由新Text绘制，保留两侧装饰即可。
            if (来源图 != null && 来源图.sprite != null && 来源图.sprite.name.StartsWith("8099.dat", StringComparison.Ordinal)) return null;
            var 原框 = 来源 as RectTransform;
            if (原框 == null) return null;
            var 框 = 矩形(来源.name, 父级, 原框.sizeDelta, 原框.anchoredPosition);
            框.anchorMin = 原框.anchorMin;
            框.anchorMax = 原框.anchorMax;
            框.pivot = 原框.pivot;
            // 旧背景有 z=0 缩放，UI视觉复制固定z，避免交互矩形退化。
            框.localScale = new Vector3(原框.localScale.x, 原框.localScale.y, 1);
            框.localRotation = 原框.localRotation;
            var 原图 = 来源.GetComponent<Image>();
            if (原图 != null)
            {
                var 图 = 框.gameObject.AddComponent<Image>();
                图.sprite = 原图.sprite;
                图.type = 原图.type;
                图.color = 原图.color;
                图.preserveAspect = 原图.preserveAspect;
                图.raycastTarget = false;
            }
            if (来源.GetComponent<RectMask2D>() != null) 框.gameObject.AddComponent<RectMask2D>();
            var 原遮罩 = 来源.GetComponent<Mask>();
            if (原遮罩 != null && 原图 != null) 框.gameObject.AddComponent<Mask>().showMaskGraphic = 原遮罩.showMaskGraphic;
            foreach (Transform 子 in 来源) 复制视觉(子, 框);
            return 框;
        }

        public Text 文本(Transform 父级, string 名字, string 内容, Vector2 尺寸, Vector2 位置, int 字号 = 16)
        {
            var 框 = 矩形(名字, 父级, 尺寸, 位置);
            var 文本 = 框.gameObject.AddComponent<Text>();
            文本.font = 字体;
            文本.fontSize = 字号;
            文本.color = 正文色;
            文本.text = 内容;
            文本.alignment = TextAnchor.MiddleLeft;
            文本.horizontalOverflow = HorizontalWrapMode.Wrap;
            文本.verticalOverflow = VerticalWrapMode.Truncate;
            文本.raycastTarget = false;
            return 文本;
        }

        public static void 限定文本(Text 字)
        {
            限定内容(字, false);
        }

        public static void 限定名称(Text 字)
        {
            限定内容(字, true);
        }

        private static void 限定内容(Text 字, bool 单行)
        {
            if (字 == null || 字.font == null || 字.GetComponentInParent<InputField>() != null) return;
            float 宽 = 字.rectTransform.rect.width, 高 = 字.rectTransform.rect.height;
            if (宽 <= 0 || 高 <= 0 || string.IsNullOrEmpty(字.text)) return;
            string 原文 = 单行 ? 字.text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ') : 字.text;
            var 设置 = 字.GetGenerationSettings(new Vector2(宽, 0));
            设置.resizeTextForBestFit = false;
            设置.horizontalOverflow = 单行 ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
            设置.verticalOverflow = VerticalWrapMode.Overflow;
            var 测量 = 字.cachedTextGeneratorForLayout;
            // Names occupy one line: the font's baseline height must not erase
            // an otherwise fitting name. Multiline copy keeps its allocated height.
            Func<string, bool> 可容纳 = 内容 => 单行 ?
                测量.GetPreferredWidth(内容, 设置) / 字.pixelsPerUnit <= 宽 + .5f :
                测量.GetPreferredHeight(内容, 设置) / 字.pixelsPerUnit <= 高 + .5f;
            if (可容纳(原文)) { 字.text = 原文; return; }
            var 边界 = System.Globalization.StringInfo.ParseCombiningCharacters(原文);
            int 左 = 0, 右 = 边界.Length;
            while (左 < 右)
            {
                int 中 = (左 + 右 + 1) / 2;
                int 终点 = 中 < 边界.Length ? 边界[中] : 原文.Length;
                if (可容纳(原文.Substring(0, 终点) + "…")) 左 = 中;
                else 右 = 中 - 1;
            }
            字.text = 原文.Substring(0, 左 < 边界.Length ? 边界[左] : 原文.Length) + "…";
        }

        public Button 按钮(Transform 父级, string 内容, Vector2 尺寸, Vector2 位置, Action 点击)
        {
            var 框 = 矩形(内容, 父级, 尺寸, 位置);
            var 图 = 框.gameObject.AddComponent<Image>();
            图.sprite = 按钮图;
            图.type = Image.Type.Sliced;
            图.color = 按钮图 != null ? Color.white : new Color(0.18f, 0.36f, 0.30f);
            var 按钮 = 框.gameObject.AddComponent<Button>();
            按钮.targetGraphic = 图;
            var 色 = 按钮.colors;
            色.highlightedColor = new Color(1f, 1f, 0.8f);
            色.pressedColor = new Color(0.75f, 0.8f, 0.65f);
            色.disabledColor = new Color(0.42f, 0.48f, 0.43f);
            按钮.colors = 色;
            var 字 = 文本(框, "文字", 内容, 尺寸 - new Vector2(8, 4), Vector2.zero, 16);
            字.alignment = TextAnchor.MiddleCenter;
            原界面文字样式.按钮(字);
            if (点击 != null) 按钮.onClick.AddListener(() => 点击());
            界面窗口管理器.注册运行时按钮(按钮);
            return 按钮;
        }
    }

    public sealed class 军事详情面板 : MonoBehaviour
    {
        public 军事界面样式 样式;
        public Text 标题;
        public Text 状态;
        public Text 反馈;
        public RectTransform 内容;
        public ScrollRect 滚动;
        public Action<军事详情面板> 构造内容;
        public long 展示代次 { get; private set; }
        private bool 已构造;
        private bool 战斗专属;

        public static 军事详情面板 创建(string 名字, 军事界面样式 样式, bool 战斗内 = false, Transform 战斗根 = null)
        {
            var 根 = new GameObject(名字, typeof(RectTransform));
            根.SetActive(false);
            if (战斗内 && 战斗根 != null) 根.transform.SetParent(战斗根, false);
            if (战斗内)
            {
                var 根框 = 根.GetComponent<RectTransform>();
                根框.anchorMin = 根框.anchorMax = new Vector2(0.5f, 0.5f);
                根框.anchoredPosition = Vector2.zero;
                根框.sizeDelta = new Vector2(960, 540);
            }
            var 画布 = 根.AddComponent<Canvas>();
            画布.renderMode = RenderMode.ScreenSpaceOverlay;
            画布.overrideSorting = 战斗内;
            画布.sortingOrder = 战斗内 ? 9 : 8;
            var 缩放 = 根.AddComponent<CanvasScaler>();
            缩放.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            缩放.referenceResolution = new Vector2(960, 540);
            缩放.matchWidthOrHeight = 1;
            if (战斗内) 缩放.enabled = false; // 战斗父Canvas有旧缩放，由下面的屏幕适配抵消。
            根.AddComponent<GraphicRaycaster>();
            var 面板 = 根.AddComponent<军事详情面板>();
            面板.样式 = 样式;
            面板.战斗专属 = 战斗内;
            var 遮罩 = 军事界面样式.矩形("遮罩", 根.transform, Vector2.zero, Vector2.zero);
            军事界面样式.拉伸(遮罩);
            遮罩.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            var 框 = 军事界面样式.矩形("军事详情", 根.transform, new Vector2(700, 500), Vector2.zero);
            框.gameObject.AddComponent<Image>().color = Color.clear;
            if (样式.参考 != null)
                foreach (string 名 in new[] { "黄色背景图", "标题栏背景", "信息背景", "信息边框" })
                    样式.复制视觉(样式.参考.Find(名), 框);
            面板.标题 = 样式.文本(框, "标题", "军事详情", new Vector2(550, 36), new Vector2(0, 221), 22);
            面板.标题.alignment = TextAnchor.MiddleCenter;
            原界面文字样式.标题(面板.标题);
            var 原关闭 = 样式.参考 != null ? 样式.参考.Find("标题栏背景/关闭") : null;
            var 标题栏 = 框.Find("标题栏背景");
            if (原关闭 != null && 标题栏 != null)
            {
                var 关闭框 = 样式.复制视觉(原关闭, 标题栏, true);
                var 图 = 关闭框.GetComponent<Image>();
                图.raycastTarget = true;
                var 关闭 = 关闭框.gameObject.AddComponent<Button>();
                关闭.targetGraphic = 图;
                var 原按钮 = 原关闭.GetComponent<Button>();
                if (原按钮 != null) { 关闭.transition = 原按钮.transition; 关闭.colors = 原按钮.colors; 关闭.spriteState = 原按钮.spriteState; }
                关闭.onClick.AddListener(() => 根.SetActive(false));
                界面窗口管理器.注册运行时按钮(关闭);
            }
            面板.状态 = 样式.文本(框, "状态说明", "本地世界 · 未连接多人服务器", new Vector2(620, 38), new Vector2(0, 180), 14);
            面板.状态.color = new Color32(50, 60, 40, 255);
            var 滚动框 = 军事界面样式.矩形("详情列表", 框, new Vector2(616, 298), new Vector2(0, -13));
            面板.滚动 = 滚动框.gameObject.AddComponent<ScrollRect>();
            var 视口 = 军事界面样式.矩形("视口", 滚动框, Vector2.zero, Vector2.zero);
            军事界面样式.拉伸(视口);
            // 仅写裁剪遮罩，让原信息底纹和边框透出，不叠加纯色内容底。
            视口.gameObject.AddComponent<Image>().color = Color.white;
            视口.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            面板.内容 = 军事界面样式.矩形("内容", 视口, Vector2.zero, Vector2.zero);
            面板.内容.anchorMin = new Vector2(0, 1);
            面板.内容.anchorMax = new Vector2(1, 1);
            面板.内容.pivot = new Vector2(0.5f, 1);
            var 布局 = 面板.内容.gameObject.AddComponent<VerticalLayoutGroup>();
            布局.padding = new RectOffset(10, 10, 8, 8);
            布局.spacing = 8;
            布局.childControlHeight = true;
            布局.childControlWidth = true;
            布局.childForceExpandHeight = false;
            布局.childForceExpandWidth = true;
            面板.内容.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            面板.滚动.viewport = 视口;
            面板.滚动.content = 面板.内容;
            面板.滚动.horizontal = false;
            面板.滚动.movementType = ScrollRect.MovementType.Clamped;
            面板.滚动.scrollSensitivity = 24;
            var 原返回 = 样式.参考 != null ? 样式.参考.Find("界面操作/返回") as RectTransform : null;
            Vector2 按钮尺寸 = 原返回 != null ? 原返回.sizeDelta : new Vector2(73, 39);
            Vector2 返回位置 = 原返回 != null ? (Vector2)样式.参考.InverseTransformPoint(原返回.TransformPoint(原返回.rect.center)) : new Vector2(286, -202);
            面板.反馈 = 样式.文本(框, "操作反馈", "", new Vector2(460, 34), new Vector2(0, 返回位置.y), 13);
            var 刷新按钮 = 样式.按钮(框, "刷新", 按钮尺寸, new Vector2(-返回位置.x, 返回位置.y), 面板.刷新);
            var 返回按钮 = 样式.按钮(框, "返回", 按钮尺寸, 返回位置, () => 根.SetActive(false));
            var 原返回图 = 原返回 != null ? 原返回.GetComponent<Image>() : null;
            if (原返回图 != null)
            {
                刷新按钮.GetComponent<Image>().sprite = 原返回图.sprite;
                返回按钮.GetComponent<Image>().sprite = 原返回图.sprite;
            }
            面板.已构造 = true;
            if (!战斗内 && !界面窗口管理器.注册运行时窗口(根))
                Debug.LogWarning("军事详情面板未注册：需先初始化主场景窗口管理器。");
            return 面板;
        }

        public void 打开(string 标题内容, Action<军事详情面板> 构造)
        {
            展示代次++;
            标题.text = 标题内容;
            构造内容 = 构造;
            反馈.text = "";
            if (gameObject.activeSelf) 刷新();
            else gameObject.SetActive(true);
        }
        private void OnEnable()
        {
            适配战斗缩放();
            if (已构造) 刷新();
        }
        private void OnRectTransformDimensionsChange() { 适配战斗缩放(); }
        private void 适配战斗缩放()
        {
            if (!战斗专属 || transform.parent == null || !gameObject.activeInHierarchy) return;
            float 父缩放 = Mathf.Abs(transform.parent.lossyScale.y);
            if (父缩放 > 0)
            {
                float 屏幕缩放 = Screen.height / 540f;
                float 缩放 = 屏幕缩放 / 父缩放;
                if (Mathf.Abs(transform.localScale.y - 缩放) > 0.001f) transform.localScale = new Vector3(缩放, 缩放, 1);
                var 根框 = (RectTransform)transform;
                Vector2 尺寸 = new Vector2(Screen.width / 屏幕缩放, 540);
                if ((根框.sizeDelta - 尺寸).sqrMagnitude > 0.01f) 根框.sizeDelta = 尺寸;
            }
        }
        private void OnDisable() { 展示代次++; if (战斗专属 && gameObject.activeSelf) gameObject.SetActive(false); }
        public void 刷新()
        {
            清空();
            if (构造内容 != null) 构造内容(this);
            LayoutRebuilder.ForceRebuildLayoutImmediate(内容);
            foreach (var 字 in GetComponentsInChildren<Text>(true))
            {
                // 滚动区中的正文由布局组件撑高，不能把规则永久改成省略号。
                if (字.transform.IsChildOf(内容) && (字.name == "说明文本" || 字.name == "详情")) continue;
                军事界面样式.限定文本(字);
            }
            滚动.verticalNormalizedPosition = 1;
        }
        public void 清空()
        {
            foreach (Transform 子 in 内容)
            {
                子.gameObject.SetActive(false);
                Destroy(子.gameObject);
            }
        }
        public void 提示(军事结果 结果)
        {
            反馈.text = 结果.说明;
            反馈.color = 结果.成功 ? 样式.正文色 : new Color(1f, 0.74f, 0.46f);
            军事界面样式.限定文本(反馈);
        }
        private RectTransform 创建行(string 名字, float 高度)
        {
            var 行 = 军事界面样式.矩形(名字, 内容, new Vector2(596, 高度), Vector2.zero);
            行.gameObject.AddComponent<LayoutElement>().preferredHeight = 高度;
            return 行;
        }
        public Text 说明(string 文案, float 高度 = 60, int 字号 = 15)
        {
            var 行 = 创建行("说明", 高度);
            var 行尺寸 = 行.GetComponent<LayoutElement>();
            行尺寸.minHeight = 高度;
            行尺寸.preferredHeight = -1;
            var 排版 = 行.gameObject.AddComponent<VerticalLayoutGroup>();
            排版.padding = new RectOffset(3, 3, 0, 0);
            排版.childAlignment = TextAnchor.MiddleLeft;
            排版.childControlWidth = 排版.childControlHeight = true;
            排版.childForceExpandWidth = true;
            排版.childForceExpandHeight = false;
            var 字 = 样式.文本(行, "说明文本", 文案, new Vector2(590, 高度), Vector2.zero, 字号);
            字.verticalOverflow = VerticalWrapMode.Overflow;
            return 字;
        }
        public Button 操作行(string 名称, string 说明, string 动作, Action 点击, bool 可用 = true, Sprite 图标 = null)
        {
            var 行 = 创建行(名称, 80);
            float 宽 = 图标 != null ? 356 : 432;
            var 行尺寸 = 行.GetComponent<LayoutElement>();
            行尺寸.minHeight = 80;
            行尺寸.preferredHeight = -1;
            var 排版 = 行.gameObject.AddComponent<HorizontalLayoutGroup>();
            排版.padding = new RectOffset(图标 != null ? 8 : 38, 10, 0, 0);
            排版.childAlignment = TextAnchor.MiddleLeft;
            排版.childControlWidth = 排版.childControlHeight = true;
            排版.childForceExpandWidth = 排版.childForceExpandHeight = false;
            if (图标 != null)
            {
                var 图框 = 军事界面样式.矩形("图标", 行, new Vector2(48, 48), Vector2.zero);
                var 图尺寸 = 图框.gameObject.AddComponent<LayoutElement>();
                图尺寸.minWidth = 图尺寸.preferredWidth = 48;
                图尺寸.minHeight = 图尺寸.preferredHeight = 48;
                var 图 = 图框.gameObject.AddComponent<Image>();
                图.sprite = 图标;
                图.preserveAspect = true;
                图.raycastTarget = false;
                间隔(行, 34);
            }
            var 文案框 = 军事界面样式.矩形("文案", 行, new Vector2(宽, 76), Vector2.zero);
            var 文案尺寸 = 文案框.gameObject.AddComponent<LayoutElement>();
            文案尺寸.minWidth = 文案尺寸.preferredWidth = 宽;
            var 纵排 = 文案框.gameObject.AddComponent<VerticalLayoutGroup>();
            纵排.childAlignment = TextAnchor.MiddleLeft;
            纵排.childControlWidth = 纵排.childControlHeight = true;
            纵排.childForceExpandWidth = true;
            纵排.childForceExpandHeight = false;
            var 标 = 样式.文本(文案框, "名称", 名称, new Vector2(宽, 24), Vector2.zero, 16);
            标.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
            标.color = 样式.金色;
            var 详情 = 样式.文本(文案框, "详情", 说明, new Vector2(宽, 52), Vector2.zero, 13);
            详情.verticalOverflow = VerticalWrapMode.Overflow;
            详情.gameObject.AddComponent<LayoutElement>().minHeight = 52;
            间隔(行, 图标 != null ? 40 : 16);
            var 按 = 样式.按钮(行, 动作, new Vector2(100, 34), Vector2.zero, 点击);
            var 按尺寸 = 按.gameObject.AddComponent<LayoutElement>();
            按尺寸.minWidth = 按尺寸.preferredWidth = 100;
            按尺寸.minHeight = 按尺寸.preferredHeight = 34;
            按.interactable = 可用;
            return 按;
        }
        private static void 间隔(Transform 父级, float 宽度)
        {
            var 空 = 军事界面样式.矩形("间隔", 父级, new Vector2(宽度, 0), Vector2.zero);
            var 尺寸 = 空.gameObject.AddComponent<LayoutElement>();
            尺寸.minWidth = 尺寸.preferredWidth = 宽度;
        }
        public Button 数据行(string 名称, string 数值, string 动作, Action 点击, bool 可用 = true)
        {
            var 行 = 创建行(名称, 48);
            var 名 = 样式.文本(行, "名称", 名称, new Vector2(172, 32), new Vector2(-204, 0), 16);
            名.color = 样式.金色;
            样式.文本(行, "数据", 数值, new Vector2(282, 32), new Vector2(22, 0), 16);
            var 按 = 样式.按钮(行, 动作, new Vector2(100, 34), new Vector2(238, 0), 点击);
            按.interactable = 可用;
            return 按;
        }
        public InputField 输入(string 提示, string 初始值, bool 整数, Action<string> 改变 = null)
        {
            var 行 = 创建行("输入区域", 40);
            样式.文本(行, "输入标签", 提示, new Vector2(206, 38), new Vector2(-182, 0), 15);
            var 框 = 军事界面样式.矩形("输入框", 行, new Vector2(354, 38), new Vector2(108, 0));
            var 输入 = 样式.原样输入(框, 初始值, 整数, 整数 ? 9 : 40);
            if (改变 != null) 输入.onEndEdit.AddListener(v => 改变(v));
            return 输入;
        }
    }
}
