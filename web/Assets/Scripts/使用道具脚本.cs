using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dwsg.Window1;

public class 使用道具脚本 : MonoBehaviour
{
	public GameObject 显示列表对象;

	public List<道具信息库类> 要显示的列表;

	public int 第几个封地;

	public int 第几个将领;

	public GameObject 将领列表对象;

	public GameObject 君主状态列表对象;

	private RectTransform 滚动信息;
	private float 结束位置;
	private bool 开始滚动;
	private Text 空态文字;
	private Button 使用按钮;
	private string 选中名字;

	private void OnEnable()
	{
		if (显示列表对象 != null && 要显示的列表 != null) 刷新显示();
	}

	private void OnDisable()
	{
		CancelInvoke(nameof(开启滚动)); 开始滚动 = false;
	}

	private int 选中索引()
	{
		if (要显示的列表 == null || 显示列表对象 == null) return -1;
		int count = Mathf.Min(要显示的列表.Count, 显示列表对象.transform.childCount);
		for (int i = 0; i < count; i++)
		{
			var row = 显示列表对象.transform.GetChild(i);
			var toggle = row.GetComponent<Toggle>();
			if (row.gameObject.activeSelf && toggle != null && toggle.isOn && 要显示的列表[i] != null) return i;
		}
		return -1;
	}

	private bool 目标有效(道具信息库类 item)
	{
		var player = ExistingWorldAdapter.CurrentPlayer;
		if (player == null || item == null || player.背包道具列表.获取指定道具数量(item.名字) < 1) return false;
		if (item.类型 != "经验书" && item.类型 != "统帅道具") return true;
		return player.封地信息表 != null && 第几个封地 >= 0 && 第几个封地 < player.封地信息表.Count &&
			player.封地信息表[第几个封地] != null && player.封地信息表[第几个封地].将领信息表 != null &&
			第几个将领 >= 0 && 第几个将领 < player.封地信息表[第几个封地].将领信息表.Count &&
			player.封地信息表[第几个封地].将领信息表[第几个将领] != null;
	}

	public void 刷新显示()
	{
		CancelInvoke(nameof(开启滚动)); 开始滚动 = false;
		int count = 要显示的列表 == null ? 0 : 要显示的列表.Count;
		var player = ExistingWorldAdapter.CurrentPlayer;
		var template = 显示列表对象.transform.childCount == 0 ? null : 显示列表对象.transform.GetChild(0);
		if (template == null) return;
		foreach (Transform row in 显示列表对象.transform)
		{
			row.GetComponent<Toggle>().SetIsOnWithoutNotify(false);
			row.gameObject.SetActive(false);
		}
		while (显示列表对象.transform.childCount < count)
			Instantiate(template.gameObject, 显示列表对象.transform, false).SetActive(false);
		int selected = -1, firstOwned = -1, firstVisible = -1;
		for (int i = 0; i < count; i++)
		{
			var item = 要显示的列表[i];
			if (item == null) continue;
			var row = 显示列表对象.transform.GetChild(i);
			row.gameObject.SetActive(true);
			row.GetComponent<Toggle>().SetIsOnWithoutNotify(false);
			row.GetChild(1).GetComponent<Image>().sprite = 全局变量.所有道具头像资源表 == null ? null : 全局道具库.获取道具头像(item.头像);
			row.GetChild(2).GetComponent<Text>().text = item.名字 + " ";
			double owned = player == null ? 0 : player.背包道具列表.获取指定道具数量(item.名字);
			row.GetChild(2).GetChild(0).GetComponent<Text>().text = "(" + owned + ")";
			var description = row.GetChild(3).GetChild(0).GetComponent<Text>();
			description.text = item.说明;
			description.horizontalOverflow = HorizontalWrapMode.Overflow;
			description.rectTransform.anchoredPosition = Vector2.zero;
			description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(row.GetChild(3).GetComponent<RectTransform>().rect.width, description.preferredWidth));
			if (firstVisible < 0) firstVisible = i;
			if (firstOwned < 0 && owned > 0) firstOwned = i;
			if (item.名字 == 选中名字) selected = i;
		}
		if (selected < 0) selected = firstOwned >= 0 ? firstOwned : firstVisible;
		if (selected >= 0) 显示列表对象.transform.GetChild(selected).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
		if (使用按钮 == null)
		{
			var button = transform.Find("道具操作/使用");
			if (button != null) 使用按钮 = button.GetComponent<Button>();
		}
		空态文字 = InventoryUi.Empty(空态文字, 显示列表对象.GetComponent<RectTransform>(), template.GetChild(2).GetComponent<Text>(), firstVisible < 0 ? "暂无可用道具" : "");
		选中高亮();
	}

	public void 使用选中道具()
	{
		int selected = 选中索引();
		if (selected < 0) { 全局变量.提示类.显示信息("请先选择道具"); return; }
		var item = 要显示的列表[selected];
		if (!目标有效(item)) { 全局变量.提示类.显示信息("道具数量不足或使用目标已变化，请重新选择"); 刷新显示(); return; }
		string result = ExistingWorldAdapter.CurrentPlayer.背包道具列表.使用道具(item.名字, 第几个封地, 第几个将领);
		全局变量.提示类.显示信息(result == "使用失败" ? "使用失败，请检查使用条件" : "使用成功:\n" + result);
		if (result != "使用失败")
		{
			if ((item.类型 == "经验书" || item.类型 == "统帅道具") && 将领列表对象 != null)
			{
				var generals = 将领列表对象.GetComponent<将领列表显示>();
				if (generals != null) { generals.刷新列表信息(); generals.刷新将领属性信息(); }
			}
			else if (君主状态列表对象 != null)
			{
				var status = 君主状态列表对象.GetComponent<显示状态信息>();
				if (status != null) status.刷新显示();
			}
		}
		刷新显示();
	}

	public void 选中高亮()
	{
		CancelInvoke(nameof(开启滚动)); 开始滚动 = false; 滚动信息 = null;
		int selected = 选中索引();
		选中名字 = selected < 0 ? null : 要显示的列表[selected].名字;
		if (使用按钮 != null) 使用按钮.interactable = selected >= 0 && 目标有效(要显示的列表[selected]);
		for (int i = 0; i < 显示列表对象.transform.childCount; i++)
		{
			var row = 显示列表对象.transform.GetChild(i);
			if (!row.gameObject.activeSelf) continue;
			bool chosen = i == selected;
			Color nameColor = 颜色类.GetColor(chosen ? "#DBD98A" : "#C8C8C8");
			row.GetChild(2).GetComponent<Text>().color = nameColor;
			row.GetChild(2).GetChild(0).GetComponent<Text>().color = 颜色类.GetColor(chosen ? "#FDA400" : "#C8C8C8");
			row.GetChild(2).GetChild(0).GetChild(0).GetComponent<Text>().color = nameColor;
			var description = row.GetChild(3).GetChild(0).GetComponent<Text>();
			description.color = 颜色类.GetColor(chosen ? "#329696" : "#C8C8C8");
			description.rectTransform.anchoredPosition = Vector2.zero;
			if (!chosen) continue;
			滚动信息 = description.rectTransform;
			float width = 滚动信息.rect.width;
			float visibleWidth = row.GetChild(3).GetComponent<RectTransform>().rect.width;
			if (width > visibleWidth) { 结束位置 = -width; Invoke(nameof(开启滚动), 1f); }
		}
	}

	private void 开启滚动() { 开始滚动 = 滚动信息 != null && isActiveAndEnabled; }

	private void FixedUpdate()
	{
		if (!开始滚动 || 滚动信息 == null) return;
		if (滚动信息.anchoredPosition.x < 结束位置) 滚动信息.anchoredPosition = Vector2.zero;
		else 滚动信息.anchoredPosition -= new Vector2(50f * Time.fixedDeltaTime, 0);
	}
}
