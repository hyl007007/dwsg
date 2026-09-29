using System;
using System.Collections.Generic;
using 玩家数据结构;
using Dwsg.Window3;
using UnityEngine;
using UnityEngine.UI;

public class 封地界面脚本 : MonoBehaviour
{
	public Transform 封地建筑列表对象;

	public Transform 建造界面UI对象;

	public GameObject 书院详情UI对象;

	public GameObject 大厅详情UI对象;

	public GameObject 房屋详情UI对象;

	public GameObject 农场详情UI对象;

	public GameObject 兵营详情UI对象;

	private int 第几个玩家 { get { return 全局变量.本机身份; } }
	private 封地信息 打开时封地;
	private 建筑信息 打开时建筑;

	public int 第几个封地;

	private int 已打开第几个建筑;

	public Text 当前封地显示对象;

    private bool 固定入口已绑定;
    private void OnEnable() { 绑定固定建筑入口(); 显示封地所有建筑(); }
    private void 绑定固定建筑入口()
    {
        if (固定入口已绑定) return;
        var forge = transform.Find("固定建筑布局/铁匠铺布局/打开背包");
        var drill = transform.Find("固定建筑布局/校场布局/打开闲兵");
        var forgeButton = forge == null ? null : forge.GetComponent<Button>();
        var drillButton = drill == null ? null : drill.GetComponent<Button>();
        if (forgeButton != null)
        {
            forgeButton.onClick.AddListener(() =>
            {
                if (FiefActions.Player(第几个玩家) == null) { 提示("请先创建角色。"); return; }
                var inventory = CityNavigation.Find<显示背包物品>(v => v.物品列表对象 != null && v.道具装备切换对象 != null);
                if (inventory == null) { 提示("背包尚未就绪，请稍后重试。"); return; }
                inventory.gameObject.SetActive(true);
                inventory.道具装备切换对象.transform.GetChild(0).GetComponent<Toggle>().SetIsOnWithoutNotify(true); inventory.切换道具();
            });
            界面窗口管理器.注册运行时按钮(forgeButton);
        }
        if (drillButton != null)
        {
            drillButton.onClick.AddListener(() =>
            {
                if (FiefActions.Fief(第几个玩家, 第几个封地) == null) { 提示("请先选择有效封地。"); return; }
                var info = CityNavigation.Find<封地信息界面UI脚本>();
                if (info == null) { 提示("兵员信息尚未就绪，请稍后重试。"); return; }
                全局变量.第几个封地 = 第几个封地; info.gameObject.SetActive(true); info.打开兵员分类("闲兵");
            });
            界面窗口管理器.注册运行时按钮(drillButton);
        }
        固定入口已绑定 = forgeButton != null && drillButton != null;
    }

    public void 建造建筑()
    {
        if (!操作对象有效()) return;
        int[] types = { 2, 3, 1, 5, 6, 7, 4 };
        var choices = 建造界面UI对象.GetChild(1).GetChild(1);
        for (int i = 0; i < types.Length && i < choices.childCount; i++)
        {
            var toggle = choices.GetChild(i).GetComponent<Toggle>();
            if (toggle == null || !toggle.isOn) continue;
            var result = FiefActions.Build(第几个玩家, 第几个封地, 已打开第几个建筑, types[i]);
            提示(result.Message);
            if (result.Success) { 显示封地指定建筑(已打开第几个建筑); 建造界面UI对象.gameObject.SetActive(false); }
            return;
        }
        提示("请先选择要建造的建筑。");
    }

    public void 升级建筑()
    {
        if (!操作对象有效()) return;
        var result = FiefActions.Upgrade(第几个玩家, 第几个封地, 已打开第几个建筑);
        提示(result.Message);
        if (!result.Success) return;
        显示封地指定建筑(已打开第几个建筑);
        封地建筑打开操作(已打开第几个建筑);
    }

    public void 拆除建筑()
    {
        if (!操作对象有效()) return;
        var result = FiefActions.Demolish(第几个玩家, 第几个封地, 已打开第几个建筑);
        提示(result.Message);
        if (!result.Success) return;
        显示封地指定建筑(已打开第几个建筑);
        foreach (var pane in new[] { 大厅详情UI对象, 房屋详情UI对象, 农场详情UI对象, 书院详情UI对象, 兵营详情UI对象 })
            if (pane != null) pane.SetActive(false);
        打开时建筑 = null;
    }

	public void 显示建造建筑信息()
    {
        var info = 建造界面UI对象.GetChild(4);
        foreach (var text in info.GetComponentsInChildren<Text>(true))
        {
            if (text.name.StartsWith("需要铜钱显示") || text.name.StartsWith("需要粮食显示")) text.text = "0";
            else if (text.name.StartsWith("建造时间显示")) { text.text = "立即完成"; text.fontSize = 16; }
            else if (text.name.StartsWith("前提建筑显示")) text.text = "空地";
        }
        var choices = 建造界面UI对象.GetChild(1).GetChild(1);
        bool chosen = false;
        foreach (Transform row in choices) { var toggle = row.GetComponent<Toggle>(); if (toggle != null && toggle.isOn) { chosen = true; break; } }
        if (!chosen && choices.childCount > 0) choices.GetChild(0).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
		for (int i = 0; i < 7; i++)
		{
			if (建造界面UI对象.GetChild(1).GetChild(1).GetChild(i)
				.GetComponent<Toggle>()
				.isOn)
			{
				UnityEngine.Debug.Log("类型" + i.ToString());
				Image component = 建造界面UI对象.GetChild(4).GetChild(1).GetComponent<Image>();
				Text component2 = 建造界面UI对象.GetChild(4).GetChild(2).GetComponent<Text>();
				Text component3 = 建造界面UI对象.GetChild(4).GetChild(4).GetComponent<Text>();
				switch (i)
				{
					case 0:
						component.sprite = 全局变量.房屋头像资源表[0];
						component2.text = "增加人口的上限,让你可以招募更多的军队.";
						component3.text = "人口上限 +250";
						break;
					case 1:
						component.sprite = 全局变量.农田头像资源表[0];
						component2.text = "农场可以生产粮食";
						component3.text = "基础粮食产量 +25";
						break;
					case 2:
						component.sprite = 全局变量.书院头像资源表[0];
						component2.text = "书院中可以研究各种科技,研究好的科技可以在各个封地中共享.";
						component3.text = "每座封地限建1座";
						break;
					case 3:
						component.sprite = 全局变量.步兵营头像资源表[0];
						component2.text = "用于招募步兵,提升等级可以招募更强的兵种.";
						component3.text = "可招:民兵";
						break;
					case 4:
						component.sprite = 全局变量.弓兵营头像资源表[0];
						component2.text = "用于招募弓箭部队,提升等级可以招募更强的兵种.";
						component3.text = "可招:弓兵";
						break;
					case 5:
						component.sprite = 全局变量.战车营头像资源表[0];
						component2.text = "用于招募各种大型的器械战车.提升等级可以招募更强的兵种.";
						component3.text = "可招:弩车";
						break;
					case 6:
						component.sprite = 全局变量.骑兵营头像资源表[0];
						component2.text = "用于招募骑兵,提升等级可以招募更强的兵种.";
						component3.text = "可招:轻骑兵";
						break;
				}
                var description = component2.rectTransform;
                float oldHeight = description.rect.height;
                float height = Mathf.Max(oldHeight, component2.preferredHeight + 4);
                if (height > oldHeight)
                {
                    description.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    description.anchoredPosition = new Vector2(description.anchoredPosition.x,
                        description.anchoredPosition.y - (height - oldHeight) * (1 - description.pivot.y));
                }
                return;
			}
		}
	}

    public void 封地建筑打开操作(int 第几个建筑)
    {
        var f = FiefActions.Fief(第几个玩家, 第几个封地);
        var b = FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑);
        if (f == null || b == null) { 提示("暂无可用封地，请先在本国城池开辟封地。"); return; }
        已打开第几个建筑 = 第几个建筑; 打开时封地 = f; 打开时建筑 = b;
        if (b.类型 == -1)
        {
            建造界面UI对象.gameObject.SetActive(true);
            建造界面UI对象.GetChild(0).gameObject.SetActive(true); 显示建造建筑信息();
        }
        else if (b.类型 == 0 || b.类型 == 2 || b.类型 == 3)
        {
            var pane = b.类型 == 0 ? 大厅详情UI对象 : b.类型 == 2 ? 房屋详情UI对象 : 农场详情UI对象;
            var view = pane.GetComponent<显示建筑信息脚本>();
            view.建筑信息对象 = b; view.第几个封地 = 第几个封地; view.第几个建筑 = 第几个建筑;
            view.显示建筑信息(); pane.SetActive(true);
        }
        else if (b.类型 == 1)
        {
            var view = 书院详情UI对象.GetComponent<书院脚本>();
            view.第几个玩家 = 第几个玩家; view.第几个封地 = 第几个封地; view.第几个建筑 = 第几个建筑;
            view.显示书院建筑信息(); view.显示科技列表(); 书院详情UI对象.SetActive(true);
        }
        else if (b.类型 >= 4 && b.类型 <= 7)
        {
            var view = 兵营详情UI对象.GetComponent<兵营脚本>();
            view.第几个玩家 = 第几个玩家; view.第几个封地 = 第几个封地; view.第几个建筑 = 第几个建筑; view.兵营类型 = b.类型;
            view.刷新显示(); 兵营详情UI对象.SetActive(true);
        }
    }

    public void 显示封地所有建筑()
    {
        绑定固定建筑入口();
        var p = FiefActions.Player(第几个玩家);
        if (p != null && p.封地信息表.Count > 0 && (第几个封地 < 0 || 第几个封地 >= p.封地信息表.Count)) 第几个封地 = 0;
        var f = FiefActions.Fief(第几个玩家, 第几个封地);
        if (当前封地显示对象 != null) 当前封地显示对象.text = f == null ? "暂无封地" : f.封地名字;
        if (封地建筑列表对象 == null || f == null) return;
        int count = Math.Min(f.建筑信息表.Count, 封地建筑列表对象.childCount);
        for (int i = 0; i < count; i++) 显示封地指定建筑(i);
    }

    public void 显示封地指定建筑(int 第几个建筑)
    {
        var b = FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑);
        if (b == null || 封地建筑列表对象 == null || 第几个建筑 < 0 || 第几个建筑 >= 封地建筑列表对象.childCount) return;
        var row = 封地建筑列表对象.GetChild(第几个建筑); if (row.childCount < 2) return;
        var modelRoot = row.GetChild(0);
        foreach (Transform child in modelRoot) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
        row.GetChild(1).gameObject.SetActive(false);
        if (b.类型 < 0 || b.类型 >= 全局变量.封地所有建筑模型.Count) return;
        int avatar = b.获取建筑头像索引(); var models = 全局变量.封地所有建筑模型[b.类型];
        if (avatar >= 0 && avatar < models.Length && models[avatar] != null)
        {
            var model = UnityEngine.Object.Instantiate(models[avatar]); model.transform.SetParent(modelRoot, false);
            model.transform.localPosition = Vector3.zero; model.transform.localScale = Vector3.one;
        }
        var badge = row.GetChild(1); badge.gameObject.SetActive(true);
        if (badge.childCount > 2) badge.GetChild(2).GetComponent<Text>().text = b.等级.ToString();
        if (badge.childCount > 3 && b.类型 < 全局变量.封地所有建筑名字.Length) badge.GetChild(3).GetComponent<Image>().sprite = 全局变量.封地所有建筑名字[b.类型];
    }
    private void 提示(string text) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(text); }
    private bool 操作对象有效()
    {
        if (打开时封地 != null && ReferenceEquals(打开时封地, FiefActions.Fief(第几个玩家, 第几个封地)) &&
            ReferenceEquals(打开时建筑, FiefActions.Building(第几个玩家, 第几个封地, 已打开第几个建筑))) return true;
        提示("封地或建筑已变化，请重新选择。"); return false;
    }
}
