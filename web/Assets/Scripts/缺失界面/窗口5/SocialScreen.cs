using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dwsg.Social
{
    // 生命周期订阅随窗口开关；后台数据变化不会重建隐藏窗口的列表。
    public sealed class SocialScreen : MonoBehaviour
    {
        internal RectTransform Body;
        internal RectTransform Frame;
        internal Text Title;
        internal Text Status;
        internal Action<SocialScreen> Render;
        internal ISocialAdapter Adapter;
        internal int RenderCount;
        private void OnEnable()
        {
            FitToSafeArea();
            if (Adapter != null) { Adapter.Changed -= Refresh; Adapter.Changed += Refresh; }
            Refresh();
        }
        private Vector2 lastResolution;
        private Rect lastSafeArea;
        private void Update()
        {
            if (lastResolution.x != UnityEngine.Screen.width || lastResolution.y != UnityEngine.Screen.height || lastSafeArea != UnityEngine.Screen.safeArea)
                FitToSafeArea();
        }
        private void FitToSafeArea()
        {
            if (Frame == null || UnityEngine.Screen.height <= 0 || UnityEngine.Screen.width <= 0) return;
            float width = UnityEngine.Screen.width, height = UnityEngine.Screen.height;
            Rect safe = UnityEngine.Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, width, height);
            float factor = height / 540f;
            float scale = Mathf.Min(1f, (safe.width / factor - 16) / 700, (safe.height / factor - 12) / 476);
            Frame.localScale = Vector3.one * Mathf.Max(.1f, scale);
            Frame.anchoredPosition = (safe.center - new Vector2(width / 2, height / 2)) / factor;
            lastResolution = new Vector2(width, height); lastSafeArea = UnityEngine.Screen.safeArea;
        }
        private void OnDisable()
        {
            if (Adapter != null) Adapter.Changed -= Refresh;
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;
            if (EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
        }
        private void OnDestroy() { if (Adapter != null) Adapter.Changed -= Refresh; }
        internal void Refresh()
        {
            if (!gameObject.activeInHierarchy || Render == null) return;
            SocialUi.Clear(Body); RenderCount++; Render(this);
            TruncateTitle();
        }
        private void TruncateTitle()
        {
            if (Title == null) return;
            Title.resizeTextForBestFit = false;
            Title.horizontalOverflow = HorizontalWrapMode.Overflow;
            float availableWidth = Mathf.Max(0, Title.rectTransform.rect.width - 8);
            string fullTitle = Title.text ?? string.Empty;
            if (Title.preferredWidth <= availableWidth) return;
            const string ellipsis = "…";
            Title.text = ellipsis;
            if (Title.preferredWidth > availableWidth) { Title.text = string.Empty; return; }
            var boundaries = StringInfo.ParseCombiningCharacters(fullTitle);
            int low = 0, high = boundaries.Length - 1;
            while (low < high)
            {
                int count = (low + high + 1) / 2;
                Title.text = fullTitle.Substring(0, boundaries[count]) + ellipsis;
                if (Title.preferredWidth <= availableWidth) low = count;
                else high = count - 1;
            }
            Title.text = fullTitle.Substring(0, boundaries[low]) + ellipsis;
        }
        internal void Feedback(SocialResult result)
        {
            Status.text = result.Message;
            // 状态位于黄色纸面，沿用任务页的深墨/深红反馈色。
            Status.color = result.Succeeded ? SocialUi.PaperInk : new Color(.36f, .035f, .02f);
        }
    }
}
