using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Dwsg.Window3;

public class 城池信息显示脚本 : MonoBehaviour
{
	public GameObject 我方城池背景布局;

	public GameObject 敌方城池背景布局;

	public Image 城池头像;

	public Text 城池名字坐标;

	public Text 城池规模;

	public Text 图腾显示;

	public Text 国家显示;

	public Text 城主显示;

	public Text 天赋显示;

	public Text 税率显示;

	public Text 驻防显示;

	public Text 封地显示;

	public Text 公告显示;

	public GameObject 选择出征将领界面对象;

	public GameObject 开辟封地按钮;

	public GameObject 进入封地按钮;

	public GameObject 修筑城池按钮;

	public GameObject 进入城池按钮;

	public 封地界面脚本 封地界面脚本对象;

	private int 显示第几个城池 = -1;
	private bool 城池入口已绑定;
	private CityPanels 城池面板;
	private int 当前坐标x, 当前坐标y;

	private 城池信息库类 当前城池 { get { return CityLocalAdapter.City(当前坐标x, 当前坐标y); } }
	private void Awake() { 确保城池入口(); }
	private void OnEnable()
	{
		确保城池入口();
		if (显示第几个城池 >= 0 && 当前城池 != null)
		{
			int index = 全局变量.所有城池列表.IndexOf(当前城池);
			显示城池信息(index);
		}
	}

	public void 确保城池入口()
	{
		if (城池入口已绑定) return;
		城池入口已绑定 = true;
		收紧公告摘要框();
		foreach (var 按钮 in GetComponentsInChildren<Button>(true))
		{
			UnityAction 动作 = null;
			switch (按钮.name)
			{
				case "侦查": 动作 = 侦查城池; break;
				case "收藏": 动作 = 收藏城池; break;
				case "进入城池": 动作 = 进入城池; break;
				case "修筑城池": 动作 = 修筑城池; break;
				case "城主征收": 动作 = 城主征收; break;
				case "国家征收": 动作 = 国家征收; break;
				case "竞选": 动作 = 城主竞选; break;
				case "查看按钮": 动作 = 查看国家; break;
				case "查看按钮 (1)": 动作 = 查看城主; break;
				case "查看按钮 (2)": 动作 = 查看封地; break;
				case "进入封地": 动作 = 进入封地; break;
			}
			if (动作 == null) continue;
			// Remove the erroneous serialized actions without replacing the manager's runtime listeners.
			for (int i = 0; i < 按钮.onClick.GetPersistentEventCount(); i++) 按钮.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
			按钮.onClick.AddListener(动作);
			界面窗口管理器.注册运行时按钮(按钮);
		}
	}

	private void 收紧公告摘要框()
	{
		if (公告显示 == null) return;
		var 矩形 = 公告显示.rectTransform;
		float 原高度 = 矩形.rect.height;
		float 摘要高度 = Mathf.Min(原高度, Mathf.Max(24, 公告显示.fontSize * 公告显示.lineSpacing + 8));
		// Preserve the existing text alignment point while removing the unused lower area.
		float 对齐点 = 1 - (int)公告显示.alignment / 3 * .5f;
		矩形.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 摘要高度);
		矩形.anchoredPosition += new Vector2(0, (原高度 - 摘要高度) * (对齐点 - 矩形.pivot.y));
		公告显示.raycastTarget = false;
	}

	private CityPanels 面板 { get { if (城池面板 == null) 城池面板 = new CityPanels(this); return 城池面板; } }
	private void 打开城池页(CityPage page, int tab = 0)
	{
		if (当前城池 == null) { 城池提示("城池已不存在，请重新选择。"); return; }
		面板.Open(page, 当前坐标x, 当前坐标y, tab);
	}
	private void 城池提示(string text) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(text); }
	private void 单行摘要(Text 文本, string 内容, string 后缀 = "")
	{
		内容 = 内容 ?? ""; 文本.supportRichText = false;
		文本.horizontalOverflow = HorizontalWrapMode.Overflow; 文本.verticalOverflow = VerticalWrapMode.Truncate;
		文本.text = 内容 + 后缀;
		while (内容.Length > 0 && 文本.preferredWidth > 文本.rectTransform.rect.width - 2)
		{
			内容 = 内容.Substring(0, 内容.Length - 1); 文本.text = 内容 + "…" + 后缀;
		}
	}
	public void 侦查城池() { 打开城池页(CityPage.Scout); }
	public void 收藏城池()
	{
		var result = CityLocalAdapter.Local.Bookmark(当前坐标x, 当前坐标y, true);
		城池提示(result.Message); if (result.Success) 打开城池页(CityPage.Bookmarks);
	}
	public void 进入城池() { 打开城池页(CityPage.Civic, 1); }
	public void 修筑城池() { 打开城池页(CityPage.Civic, 3); }
	public void 查看封地() { 打开城池页(CityPage.Civic, 2); }
	public void 城主征收() { 打开城池页(CityPage.Civic, 4); }
	public void 国家征收() { 打开城池页(CityPage.Civic, 4); }
	public void 城主竞选() { 打开城池页(CityPage.Civic, 4); }
	public void 查看国家()
	{
		if (当前城池 == null) return;
		if (!string.IsNullOrEmpty(当前城池.国家) && CityNavigation.ShowNation != null && CityNavigation.ShowNation(当前城池.国家)) return;
		if (尝试打开本国界面()) return;
		打开城池页(CityPage.Nation);
	}
	public bool 尝试打开本国界面()
	{
		var 玩家 = CityLocalAdapter.Me;
		if (当前城池 == null || 玩家 == null || 当前城池.国家 != 玩家.基础信息.国家) return false;
		var 国家 = 全局方法类.获取指定名字的国家(当前城池.国家);
		if (国家 == null || CityLocalAdapter.Player(国家.国王) == null || CityLocalAdapter.City(国家.国都x, 国家.国都y) == null) return false;
		var 概况 = CityNavigation.Find<显示概况脚本>(v => CityNavigation.ManagedRoot(v) && v.国家名字 != null && v.国王名字 != null && v.国都名字 != null && v.城池数量 != null && v.成员数量 != null && v.科技等级 != null && v.排名 != null && v.国家名字效率显示 != null);
		if (概况 == null) return false;
		概况.刷新显示(); 概况.transform.root.gameObject.SetActive(true); return true;
	}
	public void 查看城主()
	{
		if (当前城池 == null) return;
		var 城主数据 = CityLocalAdapter.Player(当前城池.城主);
		if (当前城池.城主 == 全局变量.本机身份)
		{
			var 君主 = CityNavigation.Find<显示君主信息>(v => CityNavigation.ManagedRoot(v) && v.君主名字 != null && v.君主等级 != null && v.称号 != null && v.贡献 != null && v.战功 != null && v.产铜 != null && v.产粮 != null && v.将领数量 != null && v.城池数量 != null && v.封地数量 != null && v.官阶 != null && v.声望条显示 != null && v.声望 != null);
			if (君主 != null) { 君主.刷新显示(); 君主.transform.root.gameObject.SetActive(true); return; }
		}
		if (城主数据 != null && CityNavigation.ShowLord != null && CityNavigation.ShowLord(城主数据.基础信息.ID)) return;
		打开城池页(CityPage.Lord);
	}

	public void 显示城池信息(int 第几个城池)
	{
		确保城池入口();
		if (第几个城池 < 0 || 第几个城池 >= 全局变量.所有城池列表.Count || CityLocalAdapter.Me == null) return;
		显示第几个城池 = 第几个城池;
		当前坐标x = 全局变量.所有城池列表[第几个城池].坐标x;
		当前坐标y = 全局变量.所有城池列表[第几个城池].坐标y;
		CityLocalAdapter.Local.Settle();
		敌方城池背景布局.SetActive(value: false);
		我方城池背景布局.SetActive(value: false);
		if (全局变量.所有城池列表[第几个城池].获取城池身份() != 0 && 全局变量.所有城池列表[第几个城池].获取城池身份() != 2)
		{
			敌方城池背景布局.SetActive(value: true);
		}
		else
		{
			我方城池背景布局.SetActive(value: true);
		}
		城池头像.sprite = 全局变量.城池规模头像资源表[全局变量.所有城池列表[第几个城池].规模];
		单行摘要(城池名字坐标, 当前城池.名称, "(" + 当前坐标x + "," + 当前坐标y + ")");
		城池规模.text = 全局变量.所有城池列表[第几个城池].获取规模名称() + "城";
		图腾显示.text = 全局变量.所有城池列表[第几个城池].获取图腾类型名称();
		单行摘要(国家显示, 当前城池.获取国家名字() + "(" + 当前城池.获取国家国号() + ")");
		单行摘要(城主显示, 当前城池.获取城主名字());
		天赋显示.text = 全局变量.所有城池列表[第几个城池].获取天赋类型名称() + 全局变量.所有城池列表[第几个城池].天赋加成.ToString() + "%";
		税率显示.text = 全局变量.所有城池列表[第几个城池].税率.ToString() + "%";
		封地显示.text = 全局变量.所有城池列表[第几个城池].城池封地列表.Count.ToString() + "/" + 全局变量.所有城池列表[第几个城池].获取封地上限().ToString();
		驻防显示.text = CityLocalAdapter.Friendly(当前城池) ? (当前城池.城池驻防列表.Count + 当前城池.城池玩家驻防列表.Count) + "/" + 当前城池.获取驻防上限() : "未公开";
		公告显示.text = string.IsNullOrEmpty(当前城池.公告) ? "本城尚未发布公告" : 当前城池.公告;
		公告显示.supportRichText = false; 公告显示.horizontalOverflow = HorizontalWrapMode.Wrap; 公告显示.verticalOverflow = VerticalWrapMode.Truncate;
		string 公告摘要 = 公告显示.text;
		while (公告摘要.Length > 0 && 公告显示.preferredHeight > 公告显示.rectTransform.rect.height - 2)
		{
			公告摘要 = 公告摘要.Substring(0, 公告摘要.Length - 1); 公告显示.text = 公告摘要 + "…";
		}
		开辟封地按钮.SetActive(value: false);
		进入封地按钮.SetActive(value: false);
		修筑城池按钮.SetActive(value: false);
		进入城池按钮.SetActive(value: false);
		if (CityLocalAdapter.Friendly(当前城池))
		{
			进入城池按钮.SetActive(当前城池.是否属于我的城池());
			修筑城池按钮.SetActive(!当前城池.是否属于我的城池());
		}
		if (全局变量.所有城池列表[第几个城池].是否有我的封地())
		{
			进入封地按钮.SetActive(value: true);
		}
		else if (CityLocalAdapter.Friendly(当前城池))
		{
			开辟封地按钮.SetActive(value: true);
		}
	}

	public void 出征攻打本城池()
	{
		if (!可以从当前封地出征() || 当前城池 == null) return;
		if (CityLocalAdapter.Friendly(当前城池)) { 城池提示("不能攻打自己或本国城池。"); return; }
		A星寻路 a星寻路 = new A星寻路();
		int 第几个封地 = 全局变量.第几个封地;
		int 本机身份 = 全局变量.本机身份;
		坐标 所在城池 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].所在城池;
		int num = a星寻路.开始寻路(所在城池.x - 1, 所在城池.y - 1, 全局变量.所有城池列表[显示第几个城池].坐标x - 1, 全局变量.所有城池列表[显示第几个城池].坐标y - 1);
		if (num != -1 || 全局变量.所有玩家数据表[本机身份].基础信息.名字 == "997788")
		{
			UnityEngine.Debug.Log("距离:" + num.ToString());
			选择出征将领 component = 选择出征将领界面对象.GetComponent<选择出征将领>();
			component.城池坐标x = 全局变量.所有城池列表[显示第几个城池].坐标x;
			component.城池坐标y = 全局变量.所有城池列表[显示第几个城池].坐标y;
			选择出征将领界面对象.SetActive(value: true);
			component.index = 2;
            component.刷新城池();
		}
		else
		{
			全局变量.提示类.显示信息("路径不通!");
		}
	}

    public void 出征驻防本城池()
    {
		if (!可以从当前封地出征() || 当前城池 == null) return;
		if (!CityLocalAdapter.Friendly(当前城池)) { 城池提示("只能向自己或本国城池派出援军。"); return; }
        A星寻路 a星寻路 = new A星寻路();
        int 第几个封地 = 全局变量.第几个封地;
        int 本机身份 = 全局变量.本机身份;
        坐标 所在城池 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].所在城池;
		城池信息库类 城池 = 所有城池界面脚本.根据坐标获取指定城池(全局变量.所有城池列表[显示第几个城池].坐标x, 全局变量.所有城池列表[显示第几个城池].坐标y);
		if (城池.正在交战)
		{
            int num = a星寻路.开始寻路(所在城池.x - 1, 所在城池.y - 1, 全局变量.所有城池列表[显示第几个城池].坐标x - 1, 全局变量.所有城池列表[显示第几个城池].坐标y - 1);
            if (num != -1 || 全局变量.所有玩家数据表[本机身份].基础信息.名字 == "997788")
            {
                UnityEngine.Debug.Log("距离:" + num.ToString());
                选择出征将领 component = 选择出征将领界面对象.GetComponent<选择出征将领>();
                component.城池坐标x = 全局变量.所有城池列表[显示第几个城池].坐标x;
                component.城池坐标y = 全局变量.所有城池列表[显示第几个城池].坐标y;
                component.index = 1;
                选择出征将领界面对象.SetActive(value: true);
                component.刷新城池();
            }
            else
            {
                全局变量.提示类.显示信息("路径不通!");
            }
        }else
            全局变量.提示类.显示信息("当前无法驻防!");

    }

    public void 开辟封地()
	{
		var 城池 = 当前城池; var 玩家 = CityLocalAdapter.Me;
		if (城池 == null || 玩家 == null) { 城池提示("城池或角色不存在。"); return; }
		if (!CityLocalAdapter.Friendly(城池)) { 城池提示("只可在自己或本国城池开辟封地。"); return; }
		if (城池.正在交战) { 城池提示("交战中不可开辟封地。"); return; }
		if (城池.是否有我的封地()) { 城池提示("本城已有你的封地。"); return; }
		if (玩家.封地信息表.Count >= 10) { 城池提示("个人封地已达10座上限。"); return; }
		if (!城池.新建封地(全局变量.本机身份)) { 城池提示("城池封地容量不足，未创建封地。"); return; }
		城池提示("本地封地已开辟。"); 显示城池信息(全局变量.所有城池列表.IndexOf(城池));
    }

	private bool 可以从当前封地出征()
	{
		var 玩家 = CityLocalAdapter.Me;
		if (玩家 == null || 全局变量.第几个封地 < 0 || 全局变量.第几个封地 >= 玩家.封地信息表.Count || 选择出征将领界面对象 == null)
		{ 城池提示("请先选择一个有效的己方封地。"); return false; }
		显示第几个城池 = 当前城池 == null ? -1 : 全局变量.所有城池列表.IndexOf(当前城池);
		return 显示第几个城池 >= 0;
	}

	public void 进入封地()
	{
		if (当前城池 != null && 当前城池.是否有我的封地())
		{
			int num = 当前城池.获取我的封地ID();
			if (num != -1)
			{
				int 本机身份 = 全局变量.本机身份;
				int 索引 = 全局变量.所有玩家数据表[本机身份].获取指定ID标识的封地索引(num);
				if (索引 < 0 || 封地界面脚本对象 == null) { 城池提示("封地记录失效，请重新选择。"); return; }
				var 主界面 = CityNavigation.Find<主界面UI脚本>();
				if (主界面 == null) { 城池提示("封地界面尚未就绪。"); return; }
				全局变量.第几个封地 = 索引;
				封地界面脚本对象.第几个封地 = 索引;
				主界面.加载封地场景();
				封地界面脚本对象.显示封地所有建筑();
			}
		}
		else 城池提示("本城没有你的封地。");
	}
}
