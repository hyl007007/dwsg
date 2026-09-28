using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class 提示移动 : MonoBehaviour
{
	public void 显示信息(string 要显示的文本)
	{
		// 同一句话同时推给聊天：全游戏的播报都从聊天里走一遍
		聊天系统.播报(要显示的文本);
		StartCoroutine(toast(要显示的文本));
	}

	public IEnumerator toast(string 要显示的文本)
	{
		GameObject 提示对象 = UnityEngine.Object.Instantiate(base.gameObject.transform.GetChild(0).gameObject);
		提示对象.transform.SetParent(base.transform);
		提示对象.transform.localPosition = new Vector2(0f, 0f);
		提示对象.transform.localScale = new Vector3(1f, 1f, 1f);
		提示对象.SetActive(value: true);
		提示对象.transform.GetComponent<Text>().text = 要显示的文本;
		float 移动距离 = 100f;
		while (提示对象.transform.localPosition.y < 移动距离)
		{
			float maxDistanceDelta = 移动距离 * Time.deltaTime * 1f;
			提示对象.transform.localPosition = Vector2.MoveTowards(提示对象.transform.localPosition, new Vector2(提示对象.transform.localPosition.x, 移动距离), maxDistanceDelta);
			yield return null;
		}
		UnityEngine.Object.Destroy(提示对象, 1f);
	}
}
