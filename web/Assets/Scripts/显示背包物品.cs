using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using Dwsg.Window1;

public class 显示背包物品 : MonoBehaviour
{
	public List<道具信息> 要显示的物品列表;

	public List<将领装备> 要显示的装备列表;

	public GameObject 物品列表对象;

	public GameObject 切换布局对象;

	public GameObject 道具装备切换对象;

	public GameObject 物品详情布局对象;

	public Text 页数显示;

	public Text 容量显示;

	private int 显示第几页 = 1;

	private float 总页数;

	private int 显示类型;

	public Text 已选择道具名字;

	public Text 已选中道具;

	public 调整数量脚本 调整数量脚本对象;

	private Text 空态文字;

	private void OnEnable()
	{
		if (物品列表对象 != null && 切换布局对象 != null) 刷新显示();
	}

	public void 切换道具()
	{
		if (道具装备切换对象.transform.GetChild(0).GetComponent<Toggle>().isOn)
		{
			UnityEngine.Debug.Log("道具");
			切换布局对象.transform.GetChild(0).gameObject.SetActive(value: true);
			切换布局对象.transform.GetChild(1).gameObject.SetActive(value: false);
			切换布局对象.transform.GetChild(0).GetChild(0).GetComponent<Toggle>()
				.isOn = true;
			Toggle component = 切换布局对象.transform.GetChild(0).GetChild(0).GetComponent<Toggle>();
			if (component.isOn)
			{
				切换宝物();
			}
			else
			{
				component.isOn = true;
			}
		}
	}

	public void 切换装备()
	{
		if (道具装备切换对象.transform.GetChild(1).GetComponent<Toggle>().isOn)
		{


			UnityEngine.Debug.Log("装备");
			切换布局对象.transform.GetChild(0).gameObject.SetActive(value: false);
			切换布局对象.transform.GetChild(1).gameObject.SetActive(value: true);
			Toggle component = 切换布局对象.transform.GetChild(1).GetChild(0).GetComponent<Toggle>();
			if (component.isOn)
			{
				切换武器();
			}
			else
			{
				component.isOn = true;
			}
		}
	}

	public void 切换宝物()
	{
		if (切换布局对象.transform.GetChild(0).gameObject.activeSelf && 切换布局对象.transform.GetChild(0).GetChild(0).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.宝物道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换加速()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(1).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.加速道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换生产()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(2).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.生产道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换宝箱()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(3).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.宝箱道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换强化()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(4).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.强化道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换任务()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(5).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.任务道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换武器()
	{

        if (切换布局对象.transform.GetChild(1).gameObject.activeSelf && 切换布局对象.transform.GetChild(1).GetChild(0).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
            要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.武器装备列表;
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换头盔()
	{
		if (切换布局对象.transform.GetChild(1).GetChild(1).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.头盔装备列表;
			
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换铠甲()
	{
		if (切换布局对象.transform.GetChild(1).GetChild(2).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.铠甲装备列表;
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换坐骑()
	{
		if (切换布局对象.transform.GetChild(1).GetChild(3).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.坐骑装备列表;
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 刷新显示()
	{
		var 旧选择 = 当前选中详情();
		var 旧物品 = 旧选择 == null ? null : 旧选择.要显示的物品;
		var 旧装备 = 旧选择 == null ? null : 旧选择.要显示的装备;
		int 选中行 = 旧选择 == null ? 0 : 旧选择.选中第几个;
		获取选中物品();
		物品详情布局对象.SetActive(value: false);
		已选择道具名字.text = "";
		已选中道具.text = "0";
		int childCount = 物品列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			物品列表对象.transform.GetChild(i).GetChild(1).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(1).GetChild(0)
				.gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(5).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(6).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(6).GetComponent<Image>().sprite = null;
			物品列表对象.transform.GetChild(i).GetChild(7).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(8).gameObject.SetActive(value: false);
			Transform child = 物品列表对象.transform.GetChild(i).GetChild(9);
			child.gameObject.SetActive(value: false);
			显示物品详情 component = child.GetComponent<显示物品详情>();
			component.要显示的物品 = null;
			component.要显示的装备 = null;
			child.GetComponent<Toggle>().SetIsOnWithoutNotify(false);
		}
		int num;
		int num2 = 0;
		if (显示类型 == 1)
		{
			num2 = 要显示的物品列表 == null ? 0 : 要显示的物品列表.Count;
        }
		else if (显示类型 == 2)
		{
			num2 = 要显示的装备列表 == null ? 0 : 要显示的装备列表.Count;
            if (要显示的装备列表 != null) 要显示的装备列表.Sort((将领装备 a, 将领装备 b) =>
            {
                if (a == null || a.装备信息 == null) return b == null || b.装备信息 == null ? 0 : 1;
                if (b == null || b.装备信息 == null) return -1;
                int tmp = b.品质.CompareTo(a.品质);
                if (tmp != 0) return tmp;
                tmp = b.装备信息.基础值.CompareTo(a.装备信息.基础值);
                return tmp != 0 ? tmp : b.强化等级.CompareTo(a.强化等级);
            });
        }
		总页数 = JournalService.PageCount(num2, 18);
		显示第几页 = JournalService.ClampPage(显示第几页 - 1, num2, 18) + 1;
		num = (显示第几页 - 1) * 18;
		选中行 = Mathf.Clamp(选中行, 0, Mathf.Max(0, Mathf.Min(18, num2 - num) - 1));
		if (num2 > 0)
		{
			物品详情布局对象.SetActive(value: true);
		}
		for (int j = 0; j < Mathf.Min(18, childCount); j++)
		{
			if (num + j >= num2)
			{
				continue;
			}
			物品列表对象.transform.GetChild(j).GetChild(1).gameObject.SetActive(value: true);
			Image component2 = 物品列表对象.transform.GetChild(j).GetChild(1).GetComponent<Image>();
			物品列表对象.transform.GetChild(j).GetChild(2).gameObject.SetActive(value: true);
			Text component3 = 物品列表对象.transform.GetChild(j).GetChild(2).GetComponent<Text>();
			component3.color = 颜色类.GetColor("#C8C8C8");
			Text component4 = 物品列表对象.transform.GetChild(j).GetChild(3).GetComponent<Text>();
			Transform child2 = 物品列表对象.transform.GetChild(j).GetChild(9);
			显示物品详情 component5 = child2.GetComponent<显示物品详情>();
			component5.选中第几个 = j;
			if (显示类型 == 1)
			{
				if (要显示的物品列表[num + j] == null) continue;
				道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(要显示的物品列表[num + j].名字);
				component2.sprite = 道具信息库类 == null || 全局变量.所有道具头像资源表 == null ? null : 全局道具库.获取道具头像(道具信息库类.头像);
				component3.text = 要显示的物品列表[num + j].名字;
				物品列表对象.transform.GetChild(j).GetChild(3).gameObject.SetActive(value: true);
				component4.text = (要显示的物品列表[num + j].数量.ToString() ?? "");
				component5.要显示的物品 = 要显示的物品列表[num + j];
				child2.gameObject.SetActive(value: true);
			}
			else if (显示类型 == 2)
			{
				if (要显示的装备列表[num + j] == null || 要显示的装备列表[num + j].装备信息 == null) continue;
				component2.sprite = 要显示的装备列表[num + j].获取装备头像();
				component3.text = 要显示的装备列表[num + j].获取装备名字();
				component3.color = 要显示的装备列表[num + j].获取装备文字颜色();
				物品列表对象.transform.GetChild(j).GetChild(3).gameObject.SetActive(value: false);
				component4.text = "";
				Image component6 = 物品列表对象.transform.GetChild(j).GetChild(6).GetComponent<Image>();
				double 品质 = 要显示的装备列表[num + j].品质;
				if (品质 > 1.0 && 全局变量.装备品质图片资源表 != null && 品质 < 全局变量.装备品质图片资源表.Length)
				{
					component6.sprite = 全局变量.装备品质图片资源表[(int)品质];
					component6.gameObject.SetActive(component6.sprite != null);
				}
				if (要显示的装备列表[num + j].将领ID != -1)
				{
					物品列表对象.transform.GetChild(j).GetChild(7).gameObject.SetActive(value: true);
				}
				if (要显示的装备列表[num + j].强化等级 != 0.0)
				{
					物品列表对象.transform.GetChild(j).GetChild(8).gameObject.SetActive(value: true);
					物品列表对象.transform.GetChild(j).GetChild(8).GetComponent<Text>()
						.text = "+" + 要显示的装备列表[num + j].强化等级.ToString();
				}
				if (component3.text.IndexOf("尊") > -1)
				{
					物品列表对象.transform.GetChild(j).GetChild(1).GetChild(0)
						.gameObject.SetActive(value: true);
				}
				component5.要显示的装备 = 要显示的装备列表[num + j];
				child2.gameObject.SetActive(value: true);
			}
			InventoryUi.显示格子名称(component3);
			if ((旧物品 != null && component5.要显示的物品 == 旧物品) || (旧装备 != null && component5.要显示的装备 == 旧装备)) 选中行 = j;
		}
		var player = ExistingWorldAdapter.CurrentPlayer;
		容量显示.text = player == null ? "背包数据暂不可用" : "背包上限  " + player.获取背包物品数量() + "/" + player.基础信息.背包容量上限;
		页数显示.text = 显示第几页 + "/" + 总页数;
		空态文字 = InventoryUi.Empty(空态文字, 物品列表对象.GetComponent<RectTransform>(), 页数显示,
			player == null ? "背包数据暂不可用" : num2 == 0 ? (显示类型 == 2 ? "此分类暂无装备" : "此分类暂无道具") : "");
		if (num2 > 0 && 选中行 < childCount)
		{
			var row = 物品列表对象.transform.GetChild(选中行).GetChild(9);
			if (row.gameObject.activeSelf)
			{
				row.GetComponent<Toggle>().SetIsOnWithoutNotify(true);
				row.GetComponent<显示物品详情>().显示物品信息();
			}
		}
	}

	private 显示物品详情 当前选中详情()
	{
		if (物品列表对象 == null) return null;
		foreach (Transform row in 物品列表对象.transform)
		{
			if (row.childCount <= 9) continue;
			var selector = row.GetChild(9);
			var toggle = selector.GetComponent<Toggle>();
			if (selector.gameObject.activeSelf && toggle != null && toggle.isOn) return selector.GetComponent<显示物品详情>();
		}
		return null;
	}

	public double 获取选中物品数量()
	{
		var selection = 当前选中详情();
		return selection != null && selection.要显示的物品 != null && 要显示的物品列表.Contains(selection.要显示的物品) ? selection.要显示的物品.数量 : 0;
	}

	public void 批量使用道具()
	{
		var selection = 当前选中详情();
		var player = ExistingWorldAdapter.CurrentPlayer;
		var item = selection == null ? null : selection.要显示的物品;
		if (player == null || item == null || !全局道具库.可在背包开启(item.名字) || item.数量 < 1 || !要显示的物品列表.Contains(item)) return;
		调整数量脚本对象.第几个玩家 = 全局变量.本机身份;
		调整数量脚本对象.调整类型 = 3;
		调整数量脚本对象.gameObject.SetActive(true);
		调整数量脚本对象.显示说明文本();
		调整数量脚本对象.数量滑条对象.wholeNumbers = true;
		调整数量脚本对象.数量滑条对象.minValue = 1;
		调整数量脚本对象.数量滑条对象.value = 1;
		调整数量脚本对象.滑条改变购买数量();
	}

	public void 使用道具()
	{
		var selection = 当前选中详情();
		var player = ExistingWorldAdapter.CurrentPlayer;
		if (player == null || selection == null || selection.要显示的物品 == null ||
			!全局道具库.可在背包开启(selection.要显示的物品.名字) || !要显示的物品列表.Contains(selection.要显示的物品)) return;
		string result = player.背包道具列表.使用道具(selection.要显示的物品.名字, 0, 0);
		全局变量.提示类.显示信息(result == "使用失败" ? "使用失败，请检查道具与使用条件" : "使用成功:\n" + result);
		刷新显示();
	}

	public void 丢弃道具()
	{
		var selection = 当前选中详情();
		var player = ExistingWorldAdapter.CurrentPlayer;
		if (player == null || selection == null) return;
		string name;
		bool removed;
		if (显示类型 == 1 && selection.要显示的物品 != null)
		{
			name = selection.要显示的物品.名字;
			removed = player.背包道具列表.删除道具(selection.要显示的物品);
		}
		else if (显示类型 == 2 && selection.要显示的装备 != null)
		{
			if (selection.要显示的装备.将领ID != -1)
			{ 全局变量.提示类.显示信息("请先卸下装备，再丢弃"); return; }
			name = selection.要显示的装备.获取装备名字();
			int index = 要显示的装备列表.IndexOf(selection.要显示的装备);
			removed = index >= 0 && player.背包装备列表.删除装备(selection.要显示的装备, index);
		}
		else return;
		全局变量.提示类.显示信息(removed ? "已丢弃：" + name : "丢弃失败，请重新选择物品");
		刷新显示();
	}

	private void 获取选中物品()
	{
		var player = ExistingWorldAdapter.CurrentPlayer;
		要显示的物品列表 = null; 要显示的装备列表 = null;
		if (player == null) return;
		for (int group = 0; group < 切换布局对象.transform.childCount; group++)
		{
			var tabs = 切换布局对象.transform.GetChild(group);
			if (!tabs.gameObject.activeSelf) continue;
			int selected = 0;
			for (int i = 0; i < tabs.childCount; i++)
				if (tabs.GetChild(i).GetComponent<Toggle>().isOn) { selected = i; break; }
			if (group == 0)
			{
				显示类型 = 1;
				var lists = new[] { player.背包道具列表.宝物道具列表, player.背包道具列表.加速道具列表, player.背包道具列表.生产道具列表,
					player.背包道具列表.宝箱道具列表, player.背包道具列表.强化道具列表, player.背包道具列表.任务道具列表 };
				要显示的物品列表 = lists[Mathf.Min(selected, lists.Length - 1)];
			}
			else
			{
				显示类型 = 2;
				var lists = new[] { player.背包装备列表.武器装备列表, player.背包装备列表.头盔装备列表, player.背包装备列表.铠甲装备列表, player.背包装备列表.坐骑装备列表 };
				要显示的装备列表 = lists[Mathf.Min(selected, lists.Length - 1)];
			}
			return;
		}
	}

	public void 左翻页()
	{
		if (显示第几页 <= 1) return;
		显示第几页--; 刷新显示();
	}

	public void 右翻页()
	{
		if (显示第几页 >= 总页数) return;
		显示第几页++; 刷新显示();
	}
}
