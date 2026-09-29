using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 画册脚本 : MonoBehaviour
{
    public Text 查询名将名字对象;
    public 将领属性库类 查询到的名将;
    public GameObject 名将画册消耗显示界面对象;
    public Text 名将画册材料文本对象;
    public Text 名将画册价格文本对象;
    public GameObject 名将画册列表界面对象;
    public GameObject 名将画册列表对象;
    public Transform 显示区域;
    public GameObject 画册对象模板;
    public Image 名将画册详情头像对象;
    public Text 名将画册详情名字对象;
    public Text 名将画册详情剩余时间对象;
    public Text 名将画册详情归属对象;
    public Text 名将画册详情状态对象;
    public Text 名将画册详情位置对象;
    public Text 玩家对话对象;
    public 显示画册列表到界面 显示画册脚本;
    private 名将查询记录 查询记录;

    private void OnEnable()
    {
        查询记录 = null;
        查询到的名将 = null;
        if (玩家对话对象 != null)
        {
            var 玩家 = 军事缺口入口.当前玩家();
            玩家对话对象.text = (玩家 != null && 玩家.基础信息 != null ? 玩家.基础信息.名字 + "：" : "") + "请查询名将姓名，购买画册查看归属和位置。";
        }
    }

    public void 确定黄金购买() { 添加将领到画册列表(true); }
    public void 确定材料交换() { 添加将领到画册列表(false); }

    private void 添加将领到画册列表(bool 是否为黄金)
    {
        var 结果 = 画册查询规则.购买(军事缺口入口.当前玩家(), 全局变量.画册列表, 查询记录, 是否为黄金);
        全局变量.提示类.显示信息(结果.说明);
        if (!结果.成功) return;
        名将画册消耗显示界面对象.SetActive(false);
        名将画册列表界面对象.SetActive(true);
        显示画册脚本.显示画册列表到UI();
    }

    public void 点击查询()
    {
        var 输入 = 查询名将名字对象.GetComponentInParent<InputField>();
        string 名字 = (输入 != null ? 输入.text : 查询名将名字对象.text).Trim();
        查询记录 = null;
        查询到的名将 = null;
        if (名字.Length == 0) { 全局变量.提示类.显示信息("请输入名将姓名。"); return; }
        查询记录 = 画册查询规则.查询(名字);
        if (查询记录 == null)
        {
            名将画册消耗显示界面对象.SetActive(false);
            全局变量.提示类.显示信息("当前未找到该名将，请核对姓名。");
            return;
        }
        查询到的名将 = 查询记录.模板;
        int 材料数量 = 全局方法类.根据突围获取材料数量((int)查询记录.模板.突围);
        名将画册材料文本对象.supportRichText = 名将画册价格文本对象.supportRichText = false;
        名将画册材料文本对象.text = "名将：" + 查询记录.名字 + "\n画册有效期24小时\n交换需要勇士令" + 材料数量 + "个";
        名将画册价格文本对象.text = "黄金购买需要" + (材料数量 * 10) + "黄金";
        名将画册消耗显示界面对象.SetActive(true);
    }
}
