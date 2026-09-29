using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口4;

public class 显示画册列表到界面 : MonoBehaviour
{
    public GameObject 对象模板;
    public Transform 显示区域;
    public GameObject 将领详情界面;
    public Text 详情名字;
    public Text 时间显示;
    private readonly List<GameObject> 名将对象 = new List<GameObject>();
    private readonly List<画册信息> 列表快照 = new List<画册信息>();
    private readonly Dictionary<Text, 画册信息> 行索引 = new Dictionary<Text, 画册信息>();
    private 画册信息 选中画册;
    private Text 空状态;
    private bool 已初始化;
    private long 上次刷新 = -1;

    private void OnEnable()
    {
        选中画册 = null;
        if (将领详情界面 != null) 将领详情界面.SetActive(false);
        显示画册列表到UI();
    }

    private void 初始化列表()
    {
        if (已初始化) return;
        已初始化 = true;
        对象模板.SetActive(false);
        foreach (Transform 子 in 显示区域)
        {
            if (子.gameObject == 对象模板) continue;
            子.gameObject.SetActive(false);
            if (子.childCount >= 3 && 子.GetComponent<Button>() != null) 名将对象.Add(子.gameObject);
        }
        var 样式 = new 军事界面样式(transform);
        空状态 = 样式.文本(显示区域.parent, "画册空状态", "暂无名将画册，请先查询并购买。", Vector2.zero, Vector2.zero, 18);
        军事界面样式.拉伸(空状态.rectTransform, 16);
        空状态.alignment = TextAnchor.MiddleCenter;
    }

    public void 显示画册列表到UI()
    {
        初始化列表();
        列表快照.Clear();
        行索引.Clear();
        foreach (var 行 in 名将对象) 行.SetActive(false);
        if (全局变量.画册列表 != null)
            foreach (var 册 in 全局变量.画册列表) if (册 != null) 列表快照.Add(册);
        for (int i = 0; i < 列表快照.Count; i++)
        {
            if (i >= 名将对象.Count) 名将对象.Add(Instantiate(对象模板, 显示区域, false));
            var 行 = 名将对象[i];
            var 册 = 列表快照[i];
            var 模板 = 全局将领库.查询指定ID的将领数据(册.将领id);
            var 记录 = 画册查询规则.查询(册.名字);
            var 头像 = 行.transform.GetChild(0).GetComponent<Image>();
            头像.enabled = 模板 != null;
            if (模板 != null) 头像.sprite = 全局将领库.获取指定将领的头像(模板.名字);
            var 名字 = 行.transform.GetChild(1).GetComponent<Text>();
            名字.supportRichText = false;
            名字.text = 册.名字 + (册.到期时间 <= 0 ? "（已到期）" : "");
            行索引[名字] = 册;
            var 等级 = 行.transform.GetChild(2).GetComponent<Text>();
            等级.text = 记录 != null && 记录.将领 != null ? 记录.将领.将领属性.成长点数.等级.ToString("0") + "级" : "等级未知";
            行.SetActive(true);
            军事界面样式.限定名称(名字);
            foreach (var 按钮 in 行.GetComponentsInChildren<Button>(true)) 界面窗口管理器.注册运行时按钮(按钮);
        }
        空状态.gameObject.SetActive(列表快照.Count == 0);
        if (选中画册 != null && !列表快照.Contains(选中画册))
        {
            选中画册 = null;
            将领详情界面.SetActive(false);
        }
    }

    public void 显示选中将领详情(Text name)
    {
        画册信息 册;
        if (name == null || !行索引.TryGetValue(name, out 册) || 全局变量.画册列表 == null || !全局变量.画册列表.Contains(册))
        { 全局变量.提示类.显示信息("画册已变更，请重新选择。"); 显示画册列表到UI(); return; }
        选中画册 = 册;
        刷新选中详情();
        将领详情界面.SetActive(true);
    }

    private void 写详情(int 序号, string 内容)
    {
        var 字 = 将领详情界面.transform.GetChild(序号).GetComponent<Text>();
        字.supportRichText = false;
        字.text = 内容;
        军事界面样式.限定文本(字);
    }

    private void 刷新选中详情()
    {
        if (选中画册 == null) return;
        var 记录 = 画册查询规则.查询(选中画册.名字);
        var 模板 = 全局将领库.查询指定ID的将领数据(选中画册.将领id);
        var 头像 = 将领详情界面.transform.GetChild(0).GetComponent<Image>();
        头像.enabled = 模板 != null;
        if (模板 != null) 头像.sprite = 全局将领库.获取指定将领的头像(模板.名字);
        写详情(1, 选中画册.名字);
        写详情(2, 选中画册.到期时间 > 0 ? TIME.ToTimeFormat(选中画册.到期时间) : "已到期，请重新购买");
        写详情(3, 选中画册.到期时间 > 0 && 记录 != null ? 记录.归属 : "—");
        写详情(4, 选中画册.到期时间 > 0 ? 记录 != null ? 记录.状态 : "名将去向未知" : "画册已到期");
        写详情(5, 选中画册.到期时间 > 0 && 记录 != null ? "位置：" + 记录.位置 : "—");
    }

    private void Update()
    {
        long 当前 = TIME.getTime();
        if (当前 == 上次刷新) return;
        上次刷新 = 当前;
        bool 变更 = 全局变量.画册列表 == null || 列表快照.Count != 全局变量.画册列表.Count;
        if (!变更)
            for (int i = 0; i < 列表快照.Count; i++)
                if (!ReferenceEquals(列表快照[i], 全局变量.画册列表[i])) { 变更 = true; break; }
        if (变更) 显示画册列表到UI();
        if (选中画册 != null && 将领详情界面.activeSelf) 刷新选中详情();
    }
}
