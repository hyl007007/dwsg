using UnityEngine;
using UnityEngine.UI;

public class 显示状态信息 : MonoBehaviour
{
    public GameObject 状态列表对象;
    public GameObject 选择使用道具界面对象;
    private long 刷新计时;

    private void OnEnable()
    {
        刷新计时 = TIME.getTime() - 2;
        // 原列表行的透明选区伸到了右侧滚动条上；仍由原 Grid 负责排布。
        var 布局 = 状态列表对象 == null ? null : 状态列表对象.GetComponent<GridLayoutGroup>();
        if (布局 != null) 布局.cellSize = new Vector2(610f, 布局.cellSize.y);
        刷新显示();
    }

    public void 刷新显示()
    {
        if (TIME.getTime() - 刷新计时 < 1 || 状态列表对象 == null ||
            全局变量.本机身份 < 0 || 全局变量.本机身份 >= 全局变量.所有玩家数据表.Count) return;
        var 状态表 = 全局变量.所有玩家数据表[全局变量.本机身份].道具状态表;
        if (状态列表对象.transform.childCount == 0) return;
        for (int i = 0; i < 状态表.Count; i++)
        {
            if (状态列表对象.transform.childCount <= i)
                Instantiate(状态列表对象.transform.GetChild(0).gameObject, 状态列表对象.transform, false);
            var 行 = 状态列表对象.transform.GetChild(i);
            行.gameObject.SetActive(true);
            行.GetChild(1).GetComponent<Text>().text = (i + 1) + "、" + 状态表[i].获取状态显示文本();
            bool 已开启 = 状态表[i].获取状态剩余时间() > 0;
            行.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(已开启 ? 455f : 540f, 47f);
            行.GetChild(2).gameObject.SetActive(!已开启);
            行.GetChild(3).gameObject.SetActive(已开启);
            行.GetChild(4).gameObject.SetActive(已开启);
        }
        for (int i = 状态表.Count; i < 状态列表对象.transform.childCount; i++)
            状态列表对象.transform.GetChild(i).gameObject.SetActive(false);
        选中高亮();
        刷新计时 = TIME.getTime();
    }

    public int 获取选中状态()
    {
        if (状态列表对象 == null) return -1;
        for (int i = 0; i < 状态列表对象.transform.childCount; i++)
        {
            var 行 = 状态列表对象.transform.GetChild(i);
            var 选项 = 行.GetComponent<Toggle>();
            if (行.gameObject.activeInHierarchy && 选项 != null && 选项.isOn) return i;
        }
        return -1;
    }

    private int 获取有效状态()
    {
        int 索引 = 获取选中状态();
        if (索引 < 0 || 索引 >= 全局变量.所有玩家数据表[全局变量.本机身份].道具状态表.Count)
        {
            存档脚本.显示操作提示("请先选择一项状态。");
            return -1;
        }
        return 索引;
    }

    public void 开启状态()
    {
        int 索引 = 获取有效状态();
        if (索引 < 0 || 选择使用道具界面对象 == null) return;
        var 状态 = 全局变量.所有玩家数据表[全局变量.本机身份].道具状态表[索引];
        var 道具页 = 选择使用道具界面对象.GetComponent<使用道具脚本>();
        道具页.要显示的列表 = 全局道具库.获取指定类型的道具列表("道具状态_" + 状态.名字);
        选择使用道具界面对象.SetActive(true);
        道具页.刷新显示();
    }

    public void 延期状态() { 开启状态(); }

    public void 取消状态()
    {
        int 索引 = 获取有效状态();
        if (索引 < 0) return;
        var 玩家 = 全局变量.所有玩家数据表[全局变量.本机身份];
        var 状态 = 玩家.道具状态表[索引];
        状态.加成 = 状态.初始加成;
        状态.到期时间 = 0;
        玩家.计算最终属性();
        刷新计时 = TIME.getTime() - 2;
        刷新显示();
    }

    public void 选中高亮()
    {
        if (状态列表对象 == null) return;
        for (int i = 0; i < 状态列表对象.transform.childCount; i++)
        {
            var 行 = 状态列表对象.transform.GetChild(i);
            var 选项 = 行.GetComponent<Toggle>();
            行.GetChild(1).GetComponent<Text>().color = 颜色类.GetColor(选项 != null && 选项.isOn ? "#78FFC8" : "#C8C8C8");
        }
    }

    private void FixedUpdate() { 刷新显示(); }
}
