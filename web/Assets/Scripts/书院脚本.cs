using System;
using Dwsg.Window3;
using 玩家数据结构;
using UnityEngine;
using UnityEngine.UI;

public class 书院脚本 : MonoBehaviour
{
	public GameObject 书院列表对象;

	public GameObject 书院信息对象;

	public int 第几个玩家;

	public int 第几个封地;

	public int 第几个建筑;
	private 建筑信息 打开时书院;
	private Text 研究费用文本;
	private ScrollRect 科技滚动列表;
	private float 科技列表原顶部;
	private float 已布局费用高度 = -1;
    private Text 研究按钮文字;

	public void 显示书院建筑信息()
    {
        var building = FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑);
        if (building == null || building.类型 != 1 || 书院信息对象 == null) return;
        打开时书院 = building;
		float num = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].等级;
		int num2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].获取建筑头像索引();
		书院信息对象.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.书院头像资源表[num2];
		书院信息对象.transform.GetChild(2).GetComponent<Text>().text = "书院(" + num.ToString() + "级)";
	}

	public void 显示科技列表()
    {
        var building = FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑);
        if (building == null || building.类型 != 1 || 书院列表对象 == null || 书院列表对象.transform.childCount < 21) return;
		double 工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.工程设计;
		Text component = 书院列表对象.transform.GetChild(0).GetChild(3).GetComponent<Text>();
		Text component2 = 书院列表对象.transform.GetChild(0).GetChild(4).GetComponent<Text>();
		component.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 15.0)
		{
			component2.text = "升级:人口上限增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:人口上限增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.征召技巧;
		Text component3 = 书院列表对象.transform.GetChild(1).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(1).GetChild(4).GetComponent<Text>();
		component3.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵招募速度加快" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵招募速度加快" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.种植技术;
		Text component4 = 书院列表对象.transform.GetChild(2).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(2).GetChild(4).GetComponent<Text>();
		component4.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:粮食产量增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:粮食产量增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.行军技巧;
		Text component5 = 书院列表对象.transform.GetChild(3).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(3).GetChild(4).GetComponent<Text>();
		component5.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:军队行军速度加快" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:军队行军速度加快" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.市场贸易;
		Text component6 = 书院列表对象.transform.GetChild(4).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(4).GetChild(4).GetComponent<Text>();
		component6.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:铜钱产量增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:铜钱产量增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.建筑学;
		Text component7 = 书院列表对象.transform.GetChild(5).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(5).GetChild(4).GetComponent<Text>();
		component7.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:建筑升级建造拆除速度加快" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:建筑升级建造拆除速度加快" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.铸铁技术;
		Text component8 = 书院列表对象.transform.GetChild(6).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(6).GetChild(4).GetComponent<Text>();
		component8.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵攻击增加" + ((工程设计 + 1.0) * 4.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵攻击增加" + (工程设计 * 4.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.甲胄制造;
		Text component9 = 书院列表对象.transform.GetChild(7).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(7).GetChild(4).GetComponent<Text>();
		component9.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵的防御增加" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵的防御增加" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.药草研究;
		Text component10 = 书院列表对象.transform.GetChild(8).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(8).GetChild(4).GetComponent<Text>();
		component10.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵的生命增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵的生命增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.阵法技巧;
		Text component11 = 书院列表对象.transform.GetChild(9).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(9).GetChild(4).GetComponent<Text>();
		component11.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:步兵的防御增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:步兵的防御增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.抛射技巧;
		Text component12 = 书院列表对象.transform.GetChild(10).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(10).GetChild(4).GetComponent<Text>();
		component12.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:弓兵的攻击增加" + ((工程设计 + 1.0) * 6.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:弓兵的攻击增加" + (工程设计 * 6.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.驾驭技巧;
		Text component13 = 书院列表对象.transform.GetChild(11).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(11).GetChild(4).GetComponent<Text>();
		component13.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:骑兵的攻防增加" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:骑兵的攻防增加" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.战车设计;
		Text component14 = 书院列表对象.transform.GetChild(12).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(12).GetChild(4).GetComponent<Text>();
		component14.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:战车的移速+" + ((工程设计 + 1.0) * 10.0).ToString() + "%,攻速+" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:战车的移速+" + (工程设计 * 10.0).ToString() + "%,攻速+" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.统帅能力;
		Text component15 = 书院列表对象.transform.GetChild(13).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(13).GetChild(4).GetComponent<Text>();
		component15.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:将领的统兵数量增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:将领的统兵数量增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.信仰;
		Text component16 = 书院列表对象.transform.GetChild(14).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(14).GetChild(4).GetComponent<Text>();
		component16.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:减少将领提升忠诚度费用的" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:减少将领提升忠诚度费用的" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.仓储;
		Text component17 = 书院列表对象.transform.GetChild(15).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(15).GetChild(4).GetComponent<Text>();
		component17.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:遭受掠夺时减少" + ((工程设计 + 1.0) * 10.0).ToString() + "%的资源损失";
		}
		else
		{
			component2.text = "已满级:遭受掠夺时减少" + (工程设计 * 10.0).ToString() + "%的资源损失";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.安置;
		Text component18 = 书院列表对象.transform.GetChild(16).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(16).GetChild(4).GetComponent<Text>();
		component18.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:每个房屋增加" + ((工程设计 + 1.0) * 40.0).ToString() + "个人口上限";
		}
		else
		{
			component2.text = "已满级:每个房屋增加" + (工程设计 * 40.0).ToString() + "个人口上限";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.格斗;
		Text component19 = 书院列表对象.transform.GetChild(17).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(17).GetChild(4).GetComponent<Text>();
		component19.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:步兵格挡骑兵或步兵攻击的几率" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:步兵格挡骑兵或步兵攻击的几率" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.精准;
		Text component20 = 书院列表对象.transform.GetChild(18).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(18).GetChild(4).GetComponent<Text>();
		component20.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:弓兵攻击时穿透的几率" + ((工程设计 + 1.0) * 6.0).ToString() + "%无视对方防御";
		}
		else
		{
			component2.text = "已满级:弓兵攻击时穿透的几率" + (工程设计 * 6.0).ToString() + "%无视对方防御";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.驯马;
		Text component21 = 书院列表对象.transform.GetChild(19).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(19).GetChild(4).GetComponent<Text>();
		component21.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:骑兵闪避弓兵或战车攻击的几率" + ((工程设计 + 1.0) * 7.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:骑兵闪避弓兵或战车攻击的几率" + (工程设计 * 7.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.精工;
		Text component22 = 书院列表对象.transform.GetChild(20).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(20).GetChild(4).GetComponent<Text>();
		component22.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:招募战车资源时间-" + ((工程设计 + 1.0) * 5.0).ToString() + "%,人口占用-" + ((工程设计 + 1.0) * 10.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:招募战车资源时间-" + (工程设计 * 5.0).ToString() + "%,人口占用-" + (工程设计 * 10.0).ToString() + "%";
		}
		显示研究费用();
	}

    public void 升级选中科技()
    {
        var building = FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑);
        if (building == null || !ReferenceEquals(building, 打开时书院)) { 全局变量.提示类.显示信息("书院已变化，请重新打开。"); return; }
        int selected = 选中科技();
        if (selected < 0) { 全局变量.提示类.显示信息("请先选择要研究的科技。"); return; }
        var result = FiefActions.Research(第几个玩家, 第几个封地, 第几个建筑, selected);
        全局变量.提示类.显示信息(result.Message); 显示科技列表(); 显示研究费用();
    }
    private int 选中科技()
    {
        if (书院列表对象 == null) return -1;
        for (int i = 0; i < 21 && i < 书院列表对象.transform.childCount; i++)
        {
            var row = 书院列表对象.transform.GetChild(i);
            if (row.gameObject.activeSelf && row.childCount > 5 && row.GetChild(5).gameObject.activeSelf) return i;
        }
        return -1;
    }
    private void 显示研究费用()
    {
        if (书院信息对象 == null) return;
        if (研究按钮文字 == null)
        {
            var action = transform.Find("书院操作/升级");
            var source = 书院信息对象.transform.childCount > 2 ? 书院信息对象.transform.GetChild(2).GetComponent<Text>() : null;
            if (action != null && source != null)
            {
                var oldCaption = action.Find("Image"); if (oldCaption != null) oldCaption.gameObject.SetActive(false);
                研究按钮文字 = CityMapPresentation.CopyText(source, action, "研究动作文字");
                var caption = 研究按钮文字.rectTransform; caption.anchorMin = Vector2.zero; caption.anchorMax = Vector2.one;
                caption.offsetMin = new Vector2(4, 2); caption.offsetMax = new Vector2(-4, -2);
                研究按钮文字.text = "研究"; 研究按钮文字.alignment = TextAnchor.MiddleCenter;
                原界面文字样式.按钮(研究按钮文字);
            }
        }
        if (研究费用文本 == null)
        {
            var go = new GameObject("研究费用", typeof(RectTransform)); go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(405, 52); rect.anchoredPosition = new Vector2(16.75f, -172);
            研究费用文本 = go.AddComponent<Text>();
            var source = 书院信息对象.transform.GetChild(2).GetComponent<Text>();
            研究费用文本.font = source.font; 研究费用文本.fontSize = 16; 研究费用文本.color = source.color;
            研究费用文本.alignment = TextAnchor.MiddleLeft; 研究费用文本.raycastTarget = false;
            研究费用文本.horizontalOverflow = HorizontalWrapMode.Wrap; 研究费用文本.verticalOverflow = VerticalWrapMode.Truncate;
        }
        int index = 选中科技();
        if (index < 0) { 更新研究费用("选择科技，查看费用后点“研究”。"); return; }
        var p = FiefActions.Player(第几个玩家); if (p == null) { 更新研究费用("请重新打开书院。"); return; }
        double level = FiefActions.ResearchLevel(p.科技信息, index);
        更新研究费用(level >= FiefActions.ResearchCap(index) ? "科技已满级。" :
            "需书院" + FiefActions.ResearchRequirement(index, level) + "级 · " + (index >= 15 ? "黄金" : "铜钱") + FiefActions.ResearchCost(index, level).ToString("0") + "\n" + (FiefActions.ResearchError(第几个玩家, 第几个封地, 第几个建筑, index) ?? "可研究，点“研究”完成。"));
    }
    private void 更新研究费用(string text)
    {
        研究费用文本.text = text;
        float height = Mathf.Max(52, 研究费用文本.preferredHeight + 4);
        if (Math.Abs(height - 已布局费用高度) < .1f) return;
        var fee = 研究费用文本.rectTransform;
        fee.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        if (科技滚动列表 == null)
        {
            foreach (var scroll in GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.content == null || scroll.content.gameObject != 书院列表对象 || scroll.viewport == null) continue;
                科技滚动列表 = scroll;
                var original = (RectTransform)scroll.transform;
                科技列表原顶部 = original.anchoredPosition.y + original.rect.height * (1 - original.pivot.y);
                break;
            }
        }
        if (科技滚动列表 == null) return;
        var list = (RectTransform)科技滚动列表.transform;
        float position = 科技滚动列表.verticalNormalizedPosition;
        // 原裁剪在整个 ScrollRect 上；费用独立留在它下方，视口再隔开内容与原滚动条。
        float bottom = list.parent.InverseTransformPoint(fee.TransformPoint(new Vector3(0, fee.rect.yMax, 0))).y + 8;
        float listHeight = Mathf.Max(64, 科技列表原顶部 - bottom);
        list.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listHeight);
        list.anchoredPosition = new Vector2(list.anchoredPosition.x, 科技列表原顶部 - listHeight * (1 - list.pivot.y));
        var viewport = 科技滚动列表.viewport;
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero; viewport.offsetMax = new Vector2(-28, 0);
        if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();
        var content = 科技滚动列表.content;
        var grid = content.GetComponent<GridLayoutGroup>();
        float width = Mathf.Max(64, viewport.rect.width - 12);
        float delta = grid == null ? 0 : width - grid.cellSize.x;
        content.anchorMin = content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 1);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        content.anchoredPosition = new Vector2(6, 0);
        if (grid != null)
        {
            grid.cellSize = new Vector2(width, grid.cellSize.y);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 1;
            for (int i = 0; i < content.childCount; i++)
            {
                var row = content.GetChild(i);
                if (row.childCount < 6) continue;
                for (int j = 0; j < 6; j++)
                {
                    if (j != 0 && j != 3 && j != 4 && j != 5) continue;
                    var child = row.GetChild(j) as RectTransform;
                    if (child == null) continue;
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1, child.rect.width + delta));
                    if (j == 3 || j == 4) child.anchoredPosition += new Vector2(delta / 2, 0);
                }
            }
        }
        var scrollbar = 科技滚动列表.verticalScrollbar;
        if (scrollbar != null)
        {
            var bar = (RectTransform)scrollbar.transform;
            float barWidth = bar.rect.width;
            bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(.5f, .5f);
            bar.sizeDelta = new Vector2(barWidth, -46); bar.anchoredPosition = new Vector2(-barWidth / 2, 0);
            bar.SetAsLastSibling();
            // 原轨道只有滑块和箭头 Graphic；透明命中面让空轨道也能直接点击。
            var track = bar.GetComponent<Graphic>();
            if (track == null)
            {
                var image = bar.gameObject.AddComponent<Image>(); image.color = new Color(1, 1, 1, 0); track = image;
            }
            track.raycastTarget = true; scrollbar.targetGraphic = track;
            摆放滚动箭头(bar.Find("Sliding Area/Image") as RectTransform, true);
            摆放滚动箭头(bar.Find("Sliding Area/Image (1)") as RectTransform, false);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        科技滚动列表.verticalNormalizedPosition = position;
        已布局费用高度 = height;
    }
    private static void 摆放滚动箭头(RectTransform arrow, bool up)
    {
        if (arrow == null) return;
        arrow.anchorMin = arrow.anchorMax = new Vector2(.5f, up ? 1 : 0); arrow.pivot = new Vector2(.5f, .5f);
        arrow.anchoredPosition = new Vector2(0, (up ? 1 : -1) * arrow.rect.height / 2);
    }

	public void 选中高亮()
	{
        显示研究费用();
		int childCount = 书院列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			GameObject gameObject = 书院列表对象.transform.GetChild(i).gameObject;
			Text component = gameObject.transform.GetChild(2).GetComponent<Text>();
			Text component2 = gameObject.transform.GetChild(3).GetComponent<Text>();
			Text component3 = gameObject.transform.GetChild(4).GetComponent<Text>();
			if (gameObject.transform.GetChild(5).gameObject.activeSelf)
			{
				component.color = 颜色类.GetColor("#329696");
				component2.color = 颜色类.GetColor("#FFF019");
				component3.color = 颜色类.GetColor("#78FFC8");
			}
			else
			{
				component.color = 颜色类.GetColor("#C8C8C8");
				component2.color = 颜色类.GetColor("#C8C8C8");
				component3.color = 颜色类.GetColor("#C8C8C8");
			}
		}
	}

}
