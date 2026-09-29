using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 将领编队 : MonoBehaviour
{
    public int 显示第几个封地 = -1;
    public List<将领索引信息> 要显示的将领列表 = new List<将领索引信息>();
    public Text 选择的封地显示对象;
    public GameObject 将领列表对象;
    public GameObject 编队列表对象;
    public Text 页数显示对象;

    private int 第几页将领;
    private int 总页数 = 1;
    private 将领信息 选中将领;
    private readonly List<将领信息> 将领快照 = new List<将领信息>();

    private void OnEnable()
    {
        选中将领 = null;
        重置刷新将领列表();
    }

    public void 默认显示全部封地将领() { 显示第几个封地 = -1; }

    public void 重置刷新将领列表()
    {
        获取要显示的将领列表();
        刷新编队();
    }

    public void 获取要显示的将领列表()
    {
        var 玩家 = 军事缺口入口.当前玩家();
        要显示的将领列表.Clear();
        将领快照.Clear();
        第几页将领 = 0;
        if (玩家 == null || 玩家.封地信息表 == null) { 选择的封地显示对象.text = "暂无封地"; return; }
        if (显示第几个封地 < -1 || 显示第几个封地 >= 玩家.封地信息表.Count) 显示第几个封地 = -1;
        for (int i = 0; i < 玩家.封地信息表.Count; i++)
        {
            if (显示第几个封地 != -1 && 显示第几个封地 != i) continue;
            var 地 = 玩家.封地信息表[i];
            if (地 == null || 地.将领信息表 == null) continue;
            for (int j = 0; j < 地.将领信息表.Count; j++)
            {
                var 将 = 地.将领信息表[j];
                if (将 == null || 将.将领属性 == null || 将.详细信息 == null) continue;
                要显示的将领列表.Add(new 将领索引信息(i, j));
                将领快照.Add(将);
            }
        }
        if (!将领快照.Contains(选中将领)) 选中将领 = null;
        选择的封地显示对象.text = 显示第几个封地 == -1 ? "全部封地" : 玩家.封地信息表[显示第几个封地].封地名字;
    }

    public void 列表左翻页()
    {
        if (第几页将领 > 0) { 第几页将领--; 选中将领 = null; 刷新编队(); }
    }
    public void 列表右翻页()
    {
        if (第几页将领 < 总页数 - 1) { 第几页将领++; 选中将领 = null; 刷新编队(); }
    }

    public void 刷新编队()
    {
        显示将领列表();
        显示编队列表();
    }

    private void 显示将领列表()
    {
        总页数 = Mathf.Max(1, Mathf.CeilToInt(将领快照.Count / 4f));
        第几页将领 = Mathf.Clamp(第几页将领, 0, 总页数 - 1);
        页数显示对象.text = (第几页将领 + 1) + "/" + 总页数;
        var 玩家 = 军事缺口入口.当前玩家();
        for (int i = 0; i < 4 && i < 将领列表对象.transform.childCount; i++)
        {
            var 行 = 将领列表对象.transform.GetChild(i);
            int 索引 = 第几页将领 * 4 + i;
            bool 存在 = 玩家 != null && 索引 < 将领快照.Count;
            行.gameObject.SetActive(存在);
            var 勾选 = 行.GetComponent<Toggle>();
            if (勾选 != null) 勾选.SetIsOnWithoutNotify(存在 && ReferenceEquals(选中将领, 将领快照[索引]));
            if (!存在) continue;
            var 将 = 将领快照[索引];
            var 属性 = 将.将领属性.初始属性;
            var 库 = 全局将领库.查询指定ID的将领数据(属性.ID);
            行.GetChild(0).GetChild(1).gameObject.SetActive(库 != null && 库.头像特效 != 0);
            if (库 != null)
            {
                行.GetChild(0).GetChild(0).GetComponent<Image>().sprite = 全局将领库.获取指定将领的头像(库.名字);
                var 动画 = 行.GetChild(0).GetChild(1).GetComponent<Animator>();
                if (动画 != null) 动画.SetInteger("特效类型", (int)库.头像特效);
            }
            int 编队 = (int)将.详细信息.编队;
            行.GetChild(1).gameObject.SetActive(编队 >= 1 && 编队 <= 5);
            if (编队 >= 1 && 编队 <= 5) 行.GetChild(1).GetComponent<Image>().sprite = 全局变量.将领编队图标资源表[编队 - 1];
            var 名字 = 行.GetChild(2).GetComponent<Text>();
            名字.text = 属性.名字;
            名字.color = 属性.获取将领名字颜色();
            军事界面样式.限定名称(名字);
            行.GetChild(3).GetComponent<Text>().text = "(" + 将.将领属性.成长点数.等级.ToString("0") + "级" + 属性.获取职业名字() + ")";
        }
    }

    private void 显示编队列表()
    {
        var 玩家 = 军事缺口入口.当前玩家();
        将领流程规则.补齐编队(玩家);
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
            {
                var 槽 = 编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j);
                槽.GetChild(1).gameObject.SetActive(false);
                槽.GetChild(2).gameObject.SetActive(false);
                if (玩家 == null) continue;
                int ID = 玩家.编队信息表[i][j];
                if (ID == -1) continue;
                封地信息 地;
                var 将 = 将领流程规则.查找将领(玩家, ID, out 地);
                var 名字 = 槽.GetChild(1).GetComponent<Text>();
                名字.text = 将 == null ? "将领已离开" : 将.将领属性.初始属性.名字;
                槽.GetChild(1).gameObject.SetActive(true);
                军事界面样式.限定名称(名字);
                if (将 == null) continue;
                名字.color = 将.将领属性.初始属性.获取将领名字颜色();
                var 级别 = 槽.GetChild(2).GetComponent<Text>();
                级别.text = "(" + 将.将领属性.成长点数.等级.ToString("0") + "级" + 将.将领属性.初始属性.获取职业名字() + ")";
                级别.color = 名字.color;
                槽.GetChild(2).gameObject.SetActive(true);
            }
    }

    public void 点击列表将领(int 点击第几个)
    {
        int 索引 = 第几页将领 * 4 + 点击第几个;
        if (点击第几个 < 0 || 点击第几个 >= 4 || 索引 >= 将领快照.Count) return;
        选中将领 = 将领快照[索引];
        int 队 = -1;
        for (int i = 0; i < 5; i++)
            if (编队列表对象.transform.GetChild(i).GetChild(2).gameObject.activeSelf) { 队 = i; break; }
        加入(队, -1);
        刷新编队();
    }

    private void 加入(int 队, int 槽)
    {
        var 玩家 = 军事缺口入口.当前玩家();
        封地信息 地 = null;
        if (玩家 != null && 选中将领 != null)
            foreach (var 项 in 玩家.封地信息表) if (项.将领信息表.Contains(选中将领)) { 地 = 项; break; }
        var 结果 = 选中将领 == null ? 军事结果.拒绝(军事错误.无将领, "请先选择将领。")
            : 将领流程规则.加入编队(玩家, 地, 选中将领, 队, 槽);
        全局变量.提示类.显示信息(结果.说明);
    }

    public void 点击加入选中将领到编队()
    {
        for (int i = 0; i < 4; i++)
        {
            var 行 = 将领列表对象.transform.GetChild(i);
            var 勾选 = 行.GetComponent<Toggle>();
            if (行.gameObject.activeSelf && 勾选 != null && 勾选.isOn) { 点击列表将领(i); return; }
        }
        全局变量.提示类.显示信息("请先选择将领。");
    }

    public void 编队列表读写()
    {
        var 玩家 = 军事缺口入口.当前玩家();
        将领流程规则.补齐编队(玩家);
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
            {
                var 标记 = 编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j).GetChild(3);
                if (!标记.gameObject.activeSelf) continue;
                标记.gameObject.SetActive(false);
                if (玩家 != null && 玩家.编队信息表[i][j] != -1)
                    全局变量.提示类.显示信息(将领流程规则.移出编队(玩家, i, j).说明);
                else 加入(i, j);
                刷新编队();
                return;
            }
        全局变量.提示类.显示信息("请选择编队中的位置。");
    }
}
