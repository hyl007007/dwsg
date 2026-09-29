using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Dwsg.Window1;

public class 显示商城列表 : MonoBehaviour
{
	public GameObject 商品列表对象;

	public Text 页数显示;

	private List<商品属性类> 要显示的物品列表;

	public GameObject 切换列表对象;

	public 购买道具脚本 购买道具脚本对象;

	public Text 黄金显示;

	public Text 白银显示;

	private int 显示第几页 = 1;

	private float 总页数;
	private Text 空态文字;

	private void OnEnable()
	{
		if (商品列表对象 != null && 切换列表对象 != null) 刷新显示();
	}

	public void 切换热卖()
	{
		if (切换列表对象.transform.GetChild(0).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.热卖商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换特价()
	{
		if (切换列表对象.transform.GetChild(1).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.特价商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换装备()
	{
		if (切换列表对象.transform.GetChild(2).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.装备商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换生产()
	{
		if (切换列表对象.transform.GetChild(3).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.生产商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换加速()
	{
		if (切换列表对象.transform.GetChild(4).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.加速商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换宝物()
	{
		if (切换列表对象.transform.GetChild(5).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.宝物商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换宝箱()
	{
		if (切换列表对象.transform.GetChild(6).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.宝箱商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换其他()
	{
		if (切换列表对象.transform.GetChild(7).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.其他商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	private void 绑定当前分类()
	{
		var lists = new[] { 全局商城库.热卖商品列表, 全局商城库.特价商品列表, 全局商城库.装备商品列表, 全局商城库.生产商品列表,
			全局商城库.加速商品列表, 全局商城库.宝物商品列表, 全局商城库.宝箱商品列表, 全局商城库.其他商品列表 };
		int selected = 0;
		for (int i = 0; i < Mathf.Min(切换列表对象.transform.childCount, lists.Length); i++)
			if (切换列表对象.transform.GetChild(i).GetComponent<Toggle>().isOn) { selected = i; break; }
		要显示的物品列表 = lists[selected];
	}

	public void 刷新显示()
	{
		绑定当前分类();
		int count = 要显示的物品列表 == null ? 0 : 要显示的物品列表.Count;
		总页数 = JournalService.PageCount(count, 12);
		显示第几页 = JournalService.ClampPage(显示第几页 - 1, count, 12) + 1;
		int first = (显示第几页 - 1) * 12;
		for (int i = 0; i < 商品列表对象.transform.childCount; i++)
		{
			var row = 商品列表对象.transform.GetChild(i);
			for (int j = 1; j <= 4; j++) row.GetChild(j).gameObject.SetActive(false);
			var trigger = row.GetComponent<EventTrigger>();
			trigger.triggers.Clear();
			var icon = row.GetChild(1).GetComponent<Image>(); icon.sprite = null;
			if (i >= 12 || first + i >= count) continue;
			var goods = 要显示的物品列表[first + i];
			if (goods == null) continue;
			var definition = 全局道具库.获取指定名字的道具(goods.道具名);
			row.GetChild(1).gameObject.SetActive(definition != null);
			row.GetChild(2).gameObject.SetActive(true);
			row.GetChild(2).GetComponent<Text>().text = goods.道具名;
			string unavailable = definition == null ? "未上架" :
				definition.分类 == "宝箱" && !全局道具库.可在背包开启(goods.道具名) ? "不可购" : null;
			// 名称与售卖状态分别使用原名称栏、价格栏，避免长后缀换行压住价格。
			row.GetChild(3).gameObject.SetActive(true);
			row.GetChild(3).GetChild(0).gameObject.SetActive(unavailable != null || goods.黄金售价 > 0);
			row.GetChild(3).GetChild(0).GetChild(0).gameObject.SetActive(unavailable == null);
			row.GetChild(3).GetChild(0).GetChild(1).GetComponent<Text>().text = unavailable ?? goods.黄金售价.ToString();
			row.GetChild(3).GetChild(1).gameObject.SetActive(unavailable == null && goods.白银售价 > 0);
			row.GetChild(3).GetChild(1).GetChild(1).GetComponent<Text>().text = goods.白银售价.ToString();
			if (definition == null) continue;
			icon.sprite = 全局道具库.获取道具头像(definition.头像);
			string name = goods.道具名;
			注册事件(trigger, EventTriggerType.PointerDown, e => row.GetChild(4).gameObject.SetActive(true));
			注册事件(trigger, EventTriggerType.PointerUp, e => row.GetChild(4).gameObject.SetActive(false));
			注册事件(trigger, EventTriggerType.PointerExit, e => row.GetChild(4).gameObject.SetActive(false));
			注册事件(trigger, EventTriggerType.PointerClick, e => 点击购买道具(name));
		}
		var player = ExistingWorldAdapter.CurrentPlayer;
		黄金显示.text = player == null ? "--" : player.财产信息.黄金.ToString();
		白银显示.text = player == null ? "--" : player.财产信息.白银.ToString();
		页数显示.text = 显示第几页 + "/" + 总页数;
		空态文字 = InventoryUi.Empty(空态文字, 商品列表对象.GetComponent<RectTransform>(), 页数显示, count == 0 ? "此分类暂无商品" : "");
	}

	public void 点击购买道具(string 道具名字)
	{
		购买道具脚本对象.道具名字 = 道具名字;
		购买道具脚本对象.打开购买界面();
		购买道具脚本对象.刷新显示();
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

	private void 注册事件(EventTrigger 事件系统, EventTriggerType 事件类型, UnityAction<BaseEventData> 绑定方法)
	{
		var entry = new EventTrigger.Entry { eventID = 事件类型, callback = new EventTrigger.TriggerEvent() };
		entry.callback.AddListener(绑定方法);
		事件系统.triggers.Add(entry);
	}
}
