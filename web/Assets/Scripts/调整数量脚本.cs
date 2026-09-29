using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using Dwsg.Window3;

public class 调整数量脚本 : MonoBehaviour
{
    public 显示背包物品 显示背包物品脚本对象;
    public 兵营脚本 兵营脚本对象;
    public 封地信息界面UI脚本 封地信息界面UI脚本对象;
    public 市场脚本 市场脚本对象;
    public Text 数量显示对象;
    public Text 输入数量对象;
    public Slider 数量滑条对象;
    public int 调整类型;
    public int 兵种ID;
    public double 兵种数量;
    public int 第几个玩家;
    public int 第几个封地;
    public int 第几个建筑 = -1;
    public Text 说明文本;
    private double 调整数量, 数量上限;
    private bool 更新输入中, 已提交;
    private 玩家数据 打开时玩家;
    private 封地信息 打开时封地;
    private 建筑信息 打开时兵营;
    private string 打开时道具, 基础说明;

    private void OnEnable()
    {
        原界面文字样式.单行输入(输入数量对象, 数量显示对象, TextAnchor.MiddleCenter, 17);
        var input = 获取数量输入框();
        if (input != null)
        {
            var label = input.transform.parent.Find("购买");
            原界面文字样式.对齐单行标签(label == null ? null : label.GetComponent<Text>(), (RectTransform)input.transform);
            input.onValueChanged.RemoveListener(输入数量变化);
            input.onValueChanged.AddListener(输入数量变化);
        }
        已提交 = false;
        设置数量(0);
    }
    private void OnDisable()
    {
        var input = 获取数量输入框();
        if (input != null) input.onValueChanged.RemoveListener(输入数量变化);
    }
    private void 输入数量变化(string value) { 输入改变购买数量(); }
    private void 提示(string value) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(value); }
    private InputField 获取数量输入框()
    {
        return 输入数量对象 == null ? null : 输入数量对象.GetComponentInParent<InputField>(true);
    }
    private void 设置数量(double value)
    {
        调整数量 = Math.Min(数量上限, FiefActions.Whole(value));
        更新输入中 = true;
        string text = 调整数量.ToString("0", CultureInfo.InvariantCulture);
        if (数量显示对象 != null) 数量显示对象.text = text;
        if (数量滑条对象 != null) 数量滑条对象.SetValueWithoutNotify((float)调整数量);
        var input = 获取数量输入框();
        if (input != null) input.SetTextWithoutNotify(text);
        else if (输入数量对象 != null) 输入数量对象.text = text;
        更新输入中 = false;
        更新费用说明();
    }
    public void 滑条改变购买数量()
    {
        if (!更新输入中 && 数量滑条对象 != null) 设置数量(数量滑条对象.value);
    }
    public void 输入改变购买数量()
    {
        if (!更新输入中) 同步当前输入(false);
    }
    private bool 同步当前输入(bool 提示错误)
    {
        var input = 获取数量输入框();
        int number;
        string error;
        if (input == null) error = "未找到数量输入框，请重新打开页面。";
        else if (!int.TryParse((input.text ?? "").Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out number) || number <= 0)
            error = "请输入大于0的整数数量，且不超过当前可用上限。";
        else
        {
            数量上限 = Math.Min(int.MaxValue, 获取当前上限());
            if (number <= 数量上限)
            {
                // 编辑期间只更新数量和费用，不重写输入，保留光标、选区和正在输入的内容。
                调整数量 = number;
                if (数量显示对象 != null) 数量显示对象.text = number.ToString(CultureInfo.InvariantCulture);
                if (数量滑条对象 != null) 数量滑条对象.SetValueWithoutNotify(number);
                更新费用说明();
                return true;
            }
            error = "数量超过当前可用上限：" + 数量上限.ToString("0") + "。";
        }
        // 保留用户正在编辑的原始输入，清空预览；确认时不能沿用上次有效数量。
        调整数量 = 0;
        if (数量显示对象 != null) 数量显示对象.text = "0";
        if (数量滑条对象 != null) 数量滑条对象.SetValueWithoutNotify(0);
        更新费用说明(error);
        if (提示错误) 提示(error);
        return false;
    }
    private double 获取当前上限()
    {
        if (调整类型 == 1) return FiefActions.RecruitLimit(第几个玩家, 第几个封地, 第几个建筑, 兵种ID);
        if (调整类型 == 3) return 显示背包物品脚本对象 == null ? 0 : FiefActions.Whole(显示背包物品脚本对象.获取选中物品数量());
        if (调整类型 == 4) return FiefActions.HealLimit(第几个玩家, 第几个封地, 兵种ID);
        if (调整类型 >= 5 && 调整类型 <= 8) return FiefActions.TradeLimit(第几个玩家, 调整类型);
        return 0;
    }
    public void 显示说明文本()
    {
        第几个玩家 = 全局变量.本机身份;
        打开时玩家 = FiefActions.Player(第几个玩家);
        打开时封地 = 调整类型 == 1 || 调整类型 == 4 ? FiefActions.Fief(第几个玩家, 第几个封地) : null;
        打开时兵营 = 调整类型 == 1 ? FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑) : null;
        打开时道具 = 显示背包物品脚本对象 == null ? null : 显示背包物品脚本对象.已选择道具名字.text;
        已提交 = false; 数量上限 = Math.Min(int.MaxValue, 获取当前上限());
        if (数量滑条对象 != null)
        { 数量滑条对象.wholeNumbers = true; 数量滑条对象.minValue = 0; 数量滑条对象.maxValue = (float)数量上限; }
        var u = 全局兵种库.查询指定ID的数据(兵种ID);
        if (打开时玩家 == null) 基础说明 = "角色不存在，请重新打开页面。";
        else if (调整类型 == 1)
        {
            string error = FiefActions.RecruitmentError(第几个玩家, 第几个封地, 第几个建筑, 兵种ID);
            double idle = 打开时封地 == null ? 0 : 打开时封地.闲兵信息表.Where(x => x.ID == 兵种ID).Sum(x => x.数量);
            基础说明 = error ?? "招募" + u.名称 + " · 本封地闲兵" + idle.ToString("0") + "\n可招募" + 数量上限.ToString("0") + " · 空余人口" + Math.Max(0, 打开时玩家.获取人口上限() - 打开时玩家.获取已占用人口()).ToString("0");
        }
        else if (调整类型 == 3) 基础说明 = "批量使用：" + 打开时道具 + "\n现有" + 数量上限.ToString("0") + "件";
        else if (调整类型 == 4) 基础说明 = "治疗" + (u == null ? "伤兵" : u.名称) + "\n资源可治疗" + 数量上限.ToString("0") + "名，治疗后返回本封地闲兵。";
        else if (调整类型 >= 5 && 调整类型 <= 8)
            基础说明 = "获得" + (调整类型 == 5 || 调整类型 == 8 ? "铜钱" : "粮食") + "数量 · 最多" + 数量上限.ToString("0") + "\n现有" + (调整类型 == 5 || 调整类型 == 6 ? "黄金" : 调整类型 == 7 ? "铜钱" : "粮食") + FiefActions.TradeBalance(第几个玩家, 调整类型).ToString("0.##");
        else 基础说明 = "请重新选择操作。";
        设置数量(调整类型 >= 5 ? 0 : 数量上限);
    }
    private void 更新费用说明(string error = null)
    {
        if (说明文本 == null || 基础说明 == null) return;
        if (error != null) { 说明文本.text = 基础说明 + "\n" + error; return; }
        string fee = "";
        if (调整类型 == 1 || 调整类型 == 4)
        {
            var u = 全局兵种库.查询指定ID的数据(兵种ID);
            if (u != null)
            {
                double factor = 调整类型 == 4 ? .5 : 1;
                fee = "\n消耗铜钱" + (调整数量 * u.需要铜钱 * factor).ToString("0.##") + "、粮食" + (调整数量 * u.需要粮食 * factor).ToString("0.##");
            }
        }
        else if (调整类型 >= 5 && 调整类型 <= 8)
        {
            double cost = FiefActions.TradeCost(第几个玩家, 调整类型, 调整数量);
            fee = "\n消耗" + (调整类型 == 5 || 调整类型 == 6 ? "黄金" : 调整类型 == 7 ? "铜钱" : "粮食") + (FiefActions.Finite(cost) ? cost.ToString("0") : "—") + "（不足1按1计）";
        }
        说明文本.text = 基础说明 + fee;
    }
    public void 确认调整()
    {
        if (已提交) return;
        if (打开时玩家 == null || !ReferenceEquals(打开时玩家, FiefActions.Player(第几个玩家)) ||
            ((调整类型 == 1 || 调整类型 == 4) && !ReferenceEquals(打开时封地, FiefActions.Fief(第几个玩家, 第几个封地))) ||
            (调整类型 == 1 && !ReferenceEquals(打开时兵营, FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑))))
        { 提示("角色或封地已变化，请重新选择操作。"); return; }
        if (!同步当前输入(true)) return;
        CityResult result;
        if (调整类型 == 1) result = FiefActions.Recruit(第几个玩家, 第几个封地, 第几个建筑, 兵种ID, 调整数量);
        else if (调整类型 == 4) result = FiefActions.Heal(第几个玩家, 第几个封地, 兵种ID, 调整数量);
        else if (调整类型 >= 5 && 调整类型 <= 8) result = FiefActions.Trade(第几个玩家, 调整类型, 调整数量);
        else if (调整类型 == 3)
        {
            if (显示背包物品脚本对象 == null || 打开时道具 != 显示背包物品脚本对象.已选择道具名字.text || 调整数量 > 获取当前上限())
            { 提示("道具或数量已变化，请重新选择。"); return; }
            string use = 打开时玩家.背包道具列表.批量使用道具(打开时道具, (int)调整数量, 0, 0);
            result = use == "使用失败" ? CityResult.Fail("批量使用失败。") : CityResult.Ok("批量使用成功。");
        }
        else { 提示("请重新选择操作。"); return; }
        提示(result.Message);
        if (!result.Success) return;
        已提交 = true; 设置数量(0); gameObject.SetActive(false);
        if (调整类型 == 1 && 兵营脚本对象 != null) 兵营脚本对象.刷新显示();
        else if (调整类型 == 4 && 封地信息界面UI脚本对象 != null) 封地信息界面UI脚本对象.显示伤兵列表();
        else if (调整类型 >= 5 && 市场脚本对象 != null) 市场脚本对象.刷新显示();
        else if (调整类型 == 3 && 显示背包物品脚本对象 != null)
        {
            显示背包物品脚本对象.刷新显示();
            int index;
            if (int.TryParse(显示背包物品脚本对象.已选中道具.text, out index) && index >= 0 && index < 显示背包物品脚本对象.物品列表对象.transform.childCount)
            {
                var row = 显示背包物品脚本对象.物品列表对象.transform.GetChild(index);
                if (row.childCount > 9 && row.GetChild(9).gameObject.activeSelf)
                { var toggle = row.GetChild(9).GetComponent<Toggle>(); if (toggle != null) toggle.isOn = true; }
            }
        }
    }
    public void 调整最大数量() { 数量上限 = Math.Min(int.MaxValue, 获取当前上限()); 设置数量(数量上限); }
    public void 显示调整界面() { gameObject.SetActive(true); }
}
