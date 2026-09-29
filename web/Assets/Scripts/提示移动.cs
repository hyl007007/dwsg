using System.Collections;
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

	private void OnDisable()
	{
		StopAllCoroutines();
		if (当前提示 != null) UnityEngine.Object.Destroy(当前提示);
		当前提示 = null;
	}
}
