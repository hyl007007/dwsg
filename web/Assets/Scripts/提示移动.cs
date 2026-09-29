using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 提示移动 : MonoBehaviour
{
	private GameObject 当前提示;
	private bool 当前为世界播报;

	public void 显示信息(string 要显示的文本)
	{
		// 同一句话同时推给聊天：全游戏的播报都从聊天里走一遍
		聊天系统.播报(要显示的文本);
		StartCoroutine(toast(要显示的文本));
	}

	public IEnumerator toast(string 要显示的文本)
	{
		return 显示浮动提示(要显示的文本, false);
	}

    // 世界自动播报仍进入聊天；阅读功能页时不从页面中央飘过。
    public void 显示世界播报(string 要显示的文本)
    {
        聊天系统.播报(要显示的文本);
        if (!界面窗口管理器.有活动功能窗口)
            StartCoroutine(显示浮动提示(要显示的文本, true));
    }

	private IEnumerator 显示浮动提示(string 要显示的文本, bool 世界播报)
	{
		if (string.IsNullOrWhiteSpace(要显示的文本) || transform.childCount == 0) yield break;
		// 反馈历史保留在聊天中；画面只显示最近一条，连续点击不会堆叠文字。
		// 自动播报也不能覆盖尚未读完的操作反馈。
		if (世界播报 && 当前提示 != null && !当前为世界播报) yield break;
		if (当前提示 != null)
		{
			当前提示.SetActive(false);
			UnityEngine.Object.Destroy(当前提示);
		}
		GameObject 提示对象 = UnityEngine.Object.Instantiate(base.gameObject.transform.GetChild(0).gameObject);
		当前提示 = 提示对象;
		当前为世界播报 = 世界播报;
		提示对象.transform.SetParent(base.transform, false);
		提示对象.transform.localPosition = new Vector2(0f, 0f);
		提示对象.transform.localScale = new Vector3(1f, 1f, 1f);
		提示对象.SetActive(value: true);
        var 文字 = 提示对象.GetComponent<Text>();
        文字.text = 要显示的文本;
        文字.raycastTarget = false;
		if (!世界播报 && 界面窗口管理器.有活动功能窗口)
		{
			yield return 显示页面提示(提示对象, 文字);
			yield break;
		}
		float 移动距离 = 100f;
		while (提示对象 != null && 当前提示 == 提示对象 && 提示对象.transform.localPosition.y < 移动距离)
		{
            if (世界播报 && 界面窗口管理器.有活动功能窗口) break;
			float maxDistanceDelta = 移动距离 * Time.deltaTime * 1f;
			提示对象.transform.localPosition = Vector2.MoveTowards(提示对象.transform.localPosition, new Vector2(提示对象.transform.localPosition.x, 移动距离), maxDistanceDelta);
			yield return null;
		}
        float 消失时间 = Time.time + 1f;
        while (提示对象 != null && 当前提示 == 提示对象 && Time.time < 消失时间 && !(世界播报 && 界面窗口管理器.有活动功能窗口))
            yield return null;
		if (当前提示 == 提示对象) 当前提示 = null;
		if (提示对象 != null) UnityEngine.Object.Destroy(提示对象);
	}

    private IEnumerator 显示页面提示(GameObject 提示对象, Text 文字)
    {
        // 聊天保留完整换行；单行提示按同样的内容顺序滚动，不能裁掉第二行的操作结果。
        文字.text = 文字.text.Replace("\r\n", " · ").Replace("\r", " · ").Replace("\n", " · ");
        // 功能页的反馈沿用原文字皮肤，放在页面上方的 HUD 空隙，避免从正文和按钮上飘过。
        var canvas = GetComponent<Canvas>();
        float scale = canvas == null ? 1 : Mathf.Max(.01f, canvas.scaleFactor);
        float padding = 文字.fontSize * .25f;
        float height = 文字.fontSize + padding * 2;
        float y = Screen.height - (padding + height * .5f) * scale;
        var gaps = new List<Vector2> { new Vector2(padding * scale, Screen.width - padding * scale) };
        var corners = new Vector3[4];
        if (全局变量.主界面UI对象 != null)
        foreach (var graphic in 全局变量.主界面UI对象.GetComponentsInChildren<Graphic>(false))
        {
            if (!graphic.isActiveAndEnabled || graphic.color.a <= .01f || graphic.canvasRenderer.cull) continue;
            graphic.rectTransform.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            if (max.y < y - height * scale * .5f || min.y > y + height * scale * .5f) continue;
            // 原主界面中透明的全屏点击区域不占可见 HUD 空间。
            if (max.x - min.x >= Screen.width * .98f && max.y - min.y >= Screen.height * .98f) continue;
            for (int i = gaps.Count - 1; i >= 0; i--)
            {
                var gap = gaps[i];
                float left = Mathf.Max(gap.x, min.x - padding * scale);
                float right = Mathf.Min(gap.y, max.x + padding * scale);
                if (right <= left) continue;
                gaps.RemoveAt(i);
                if (left > gap.x) gaps.Add(new Vector2(gap.x, left));
                if (right < gap.y) gaps.Add(new Vector2(right, gap.y));
            }
        }
        Vector2 available = new Vector2(Screen.width * .3f, Screen.width * .7f);
        float widest = 0;
        foreach (var gap in gaps)
            if (gap.y - gap.x > widest) { available = gap; widest = gap.y - gap.x; }
        var viewport = new GameObject("页面操作提示", typeof(RectTransform), typeof(RectMask2D));
        var area = (RectTransform)viewport.transform;
        area.SetParent(transform, false);
        var parent = (RectTransform)transform;
        area.anchorMin = area.anchorMax = parent.pivot;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2((available.x + available.y) * .5f, y), null, out local);
        area.anchoredPosition = local;
        area.sizeDelta = new Vector2(Mathf.Max(1, (available.y - available.x) / scale), height);
        提示对象.transform.SetParent(area, false);
        当前提示 = viewport;
        文字.alignment = TextAnchor.MiddleCenter;
        文字.alignByGeometry = true;
        文字.horizontalOverflow = HorizontalWrapMode.Overflow;
        文字.verticalOverflow = VerticalWrapMode.Overflow;
        var line = 文字.rectTransform;
        line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
        line.pivot = new Vector2(.5f, .5f);
        float width = Mathf.Max(area.rect.width, 文字.preferredWidth);
        line.sizeDelta = new Vector2(width, height);
        float overflow = Mathf.Max(0, width - area.rect.width);
        float duration = 2.5f + overflow / 60f;
        for (float elapsed = 0; 当前提示 == viewport && elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            line.anchoredPosition = new Vector2(overflow * .5f - Mathf.Clamp((elapsed - 1) * 60, 0, overflow), 0);
            yield return null;
        }
        if (当前提示 == viewport) 当前提示 = null;
        if (viewport != null) UnityEngine.Object.Destroy(viewport);
    }

	private void OnDisable()
	{
		StopAllCoroutines();
		if (当前提示 != null) UnityEngine.Object.Destroy(当前提示);
		当前提示 = null;
	}
}
