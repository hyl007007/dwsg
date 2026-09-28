using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 全局任务脚本 : MonoBehaviour
{
    public int year;

    public int mouth;

    public int day;

    public int hour;

    public int min;

    public int sec;

    public Text 国家信息对象;

    public Text 等级信息对象;

    public Text 粮食信息对象;

    public Text 铜钱信息对象;

    public Text 人口信息对象;

    public Text 时间信息对象;

    public Text 声望显示;

    public GameObject 提示;

    public RectTransform 声望条显示;

    public GameObject 战斗地图列表;

    public GameObject 所有城池界面布局;

    public 提示移动 提示类对象;

    private GameObject 要创建的战斗地图对象;

    private 战斗系统 战斗系统脚本对象;

    public Text 说明文本;

    public Text 群号文本;

    public 招募将领 招募将领脚本对象;

    public GameObject 画册列表显示;

        private int 验证计次;

        public Text 赌场结果显示;


    private void Start()
    {
        UnityEngine.Debug.Log("任务初始化");
        附近山贼.生成山贼数据列表();
        StartCoroutine(检测军情列表());
        StartCoroutine(刷新大地图列表());
        StartCoroutine(刷新基础信息());
        if (全局变量.所有玩家数据表[全局变量.本机身份].基础信息.名字 != "997788")
        {
            StartCoroutine(判断将领忠诚());
            StartCoroutine(减少将领忠诚());
        }
        StartCoroutine(AI推城());
        StartCoroutine(增加资源());
        StartCoroutine(Ai军情检测());
        StartCoroutine(赌场());
        StartCoroutine(刷新商城());
        全局变量.提示类 = 提示类对象;
        军情通知角标.装配(提示);
        // 聊天系统：世界/封地界面挂「主界面_国家」后面的聊天按钮，战斗界面挂左下角播报条
        聊天系统.初始化();
        UnityEngine.Debug.Log("初始化结束");
    }

    private IEnumerator 赌场()
    {
        while (true)
        {
            if (全局变量.当局赌场下注列表.Count > 0 && 全局变量.赌场是否开始 == false)
            {
                全局变量.赌场是否开始 = true;
                赌场结果显示.text = "";
                int 骰子1 = UnityEngine.Random.Range(1, 7);
                yield return new WaitForSeconds(5);
                int 骰子2 = UnityEngine.Random.Range(1, 7);
                yield return new WaitForSeconds(5);
                int 骰子3 = UnityEngine.Random.Range(1, 7);
                print($"{骰子1},{骰子2},{骰子3}");
                if (骰子1 + 骰子2 + 骰子3 < 11)
                    赌场结算(0);
                if (骰子1 + 骰子2 + 骰子3 > 10)
                    赌场结算(1);
                if (骰子1 == 1 && 骰子2 == 1 && 骰子3 == 1)
                    赌场结算(2);
                if (骰子1 == 2 && 骰子2 == 2 && 骰子3 == 2)
                    赌场结算(2);
                if (骰子1 == 3 && 骰子2 == 3 && 骰子3 == 3)
                    赌场结算(2);
                if (骰子1 == 4 && 骰子2 == 4 && 骰子3 == 4)
                    赌场结算(2);
                if (骰子1 == 5 && 骰子2 == 5 && 骰子3 == 5)
                    赌场结算(2);
                if (骰子1 == 6 && 骰子2 == 6 && 骰子3 == 6)
                    赌场结算(2);
                else
                {
                    string 赌场结果 = "";
                    for (int i = 0; i < 全局变量.当局赌场下注列表.Count; i++)
                    {
                        switch (全局变量.当局赌场下注列表[i].下注类型)
                        {
                            case 0:
                                赌场结果 += $"压小输了{全局变量.当局赌场下注列表[i].下注金额}\n";
                                break;
                            case 1:
                                赌场结果 += $"压大输了{全局变量.当局赌场下注列表[i].下注金额}\n";
                                break;
                            case 2:
                                赌场结果 += $"压豹子输了{全局变量.当局赌场下注列表[i].下注金额}\n";
                                break;
                        }
                    }
                    if (赌场结果 != "")
                    {
                        赌场结果显示.text += 赌场结果;
                    }
                }
                全局变量.赌场是否开始 = false;
                全局变量.当局赌场下注列表.Clear();
            }
            yield return new WaitForSeconds(60);
        }
    }

    private void 赌场结算(int 类型)
    {
        string 赌场结果 = "";
        switch (类型)
        {
            case 0:
                for (int i = 0; i < 全局变量.当局赌场下注列表.Count; i++)
                {
                    if (全局变量.当局赌场下注列表[i].下注类型 == 0)
                    {
                        赌场结果 += $"压小获得了{全局变量.当局赌场下注列表[i].下注金额 * 1.2}\n";
                        全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 += 全局变量.当局赌场下注列表[i].下注金额 * 1.2;
                        全局变量.当局赌场下注列表.RemoveAt(i);
                    }
                }
                break;
            case 1:
                for (int i = 0; i < 全局变量.当局赌场下注列表.Count; i++)
                {
                    if (全局变量.当局赌场下注列表[i].下注类型 == 1)
                    {
                        赌场结果 += $"压大获得了{全局变量.当局赌场下注列表[i].下注金额 * 1.2}\n";
                        全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 += 全局变量.当局赌场下注列表[i].下注金额 * 1.2;
                        全局变量.当局赌场下注列表.RemoveAt(i);
                    }
                }
                break;
            case 2:
                for (int i = 0; i < 全局变量.当局赌场下注列表.Count; i++)
                {
                    if (全局变量.当局赌场下注列表[i].下注类型 == 2)
                    {
                        赌场结果 += $"压豹子获得了{全局变量.当局赌场下注列表[i].下注金额 * 1.5}\n";
                        全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 += 全局变量.当局赌场下注列表[i].下注金额 * 1.5;
                        全局变量.当局赌场下注列表.RemoveAt(i);
                    }
                }
                break;
        }

        if (赌场结果 != "")
        {
            赌场结果显示.text += $"恭喜你在本局\n{赌场结果}";
        }
    }

    private IEnumerator AI推城()
    {
        long 刷新计时 = TIME.getTime() - 2L;
        bool demo = false;
        long[] 各国下次出手时间 = new long[0];
        long 上次出手时间 = 0L;
        int 出手间隔最小 = 120;
        int 出手间隔最大 = 300;
        int 出手错开秒数 = 10;
        int 临近距离 = 4;
        int 寻路次数上限 = 12;
        for (; ; )
        {
            int 数量 = 全局变量.所有国家列表.Count;
            if (各国下次出手时间.Length != 数量)
            {
                long[] 临时排期 = new long[数量];
                for (int k = 0; k < 数量; k++)
                {
                    if (k < 各国下次出手时间.Length)
                    {
                        临时排期[k] = 各国下次出手时间[k];
                    }
                    if (临时排期[k] <= 0L)
                    {
                        临时排期[k] = TIME.getTime() + UnityEngine.Random.Range(0, 出手间隔最大);
                    }
                }
                各国下次出手时间 = 临时排期;
            }

            int num5;
            for (int i = 0; i < 数量; i = num5 + 1)
            {
                if (i < 各国下次出手时间.Length && TIME.getTime() >= 各国下次出手时间[i] && TIME.getTime() - 上次出手时间 >= (long)出手错开秒数)
                {
                    各国下次出手时间[i] = TIME.getTime() + UnityEngine.Random.Range(出手间隔最小, 出手间隔最大);
                    上次出手时间 = TIME.getTime();
                    List<int> 候选索引 = new List<int>();
                    List<int> 候选距离 = new List<int>();
                    List<int> 起点x表 = new List<int>();
                    List<int> 起点y表 = new List<int>();
                    int count = 全局变量.所有城池列表.Count;
                    int num3;
                    for (int j = 0; j < count; j = num3 + 1)
                    {
                        城池信息库类 目标城池 = 全局变量.所有城池列表[j];
                        if (目标城池.规模 < 8 && 目标城池.国家 != 全局变量.所有国家列表[i].国号 && 目标城池.城主 != 全局变量.所有国家列表[i].国王 && !目标城池.正在交战)
                        {
                            int count2 = 全局变量.所有国家列表[i].城池列表.Count;
                            for (int q = 0; q < count2; q++)
                            {
                                坐标 本国城池 = 全局变量.所有国家列表[i].城池列表[q];
                                int 横向间距 = Math.Abs(目标城池.坐标x - 本国城池.x);
                                int 纵向间距 = Math.Abs(目标城池.坐标y - 本国城池.y);
                                int 间距 = Math.Max(横向间距, 纵向间距);
                                if (间距 <= 临近距离)
                                {
                                    候选索引.Add(j);
                                    候选距离.Add(间距);
                                    起点x表.Add(本国城池.x);
                                    起点y表.Add(本国城池.y);
                                    break;
                                }
                            }
                        }
                        num3 = j;
                    }
                    int 候选数量 = 候选索引.Count;
                    List<int> 可达城池 = new List<int>();
                    int 寻路次数 = 0;
                    while (寻路次数 < 寻路次数上限 && 可达城池.Count < 5)
                    {
                        int 最近序号 = -1;
                        int 最近距离 = 999;
                        for (int q = 0; q < 候选数量; q++)
                        {
                            if (候选距离[q] >= 0 && 候选距离[q] < 最近距离)
                            {
                                最近距离 = 候选距离[q];
                                最近序号 = q;
                            }
                        }
                        if (最近序号 < 0)
                        {
                            break;
                        }
                        候选距离[最近序号] = -1;
                        寻路次数++;
                        int 目标索引 = 候选索引[最近序号];
                        int num2 = new A星寻路
                        {
                            指定身份 = 全局变量.所有国家列表[i].国号
                        }.开始寻路(起点x表[最近序号] - 1, 起点y表[最近序号] - 1, 全局变量.所有城池列表[目标索引].坐标x - 1, 全局变量.所有城池列表[目标索引].坐标y - 1);
                        if (num2 > 0 && num2 < 999)
                        {
                            可达城池.Add(目标索引);
                        }
                    }
                    if (可达城池.Count != 0)
                    {
                        int 出征目标索引 = 可达城池[UnityEngine.Random.Range(0, 可达城池.Count)];
                        城池信息库类 出征目标 = 全局变量.所有城池列表[出征目标索引];
                        List<将领信息> list2 = new List<将领信息>();
                        int 将领数量 = 出征目标.规模 * 20;
                        if (将领数量 < 5)
                        {
                            将领数量 = 5;
                        }
                        print($"{全局变量.所有玩家数据表[全局变量.所有国家列表[i].国王].基础信息.名字}开始攻打：{出征目标.名称}坐标：{出征目标.坐标x}{出征目标.坐标y}");
                        for (int gg = 0; gg < 将领数量; gg++)
                        {
                            将领信息 jl = 随机一个城池驻防将领(99, 全局变量.所有国家列表[i].国号);
                            jl.详细信息.坑位颜色 = 0.0;
                            // 带兵
                            jl.将领配兵.数量 = (int)((出征目标.规模 + 全局变量.难度 + 全局变量.所有玩家数据表[全局变量.本机身份].科技信息.统帅能力 / 20) * 2500 + UnityEngine.Random.Range(出征目标.规模 * 100, 出征目标.规模 * 500));
                            if (jl.将领配兵.ID == 404.0)
                            {
                                jl.将领配兵.数量 = (int)(jl.将领配兵.数量 / 2.5);
                            }
                            list2.Add(jl);
                        }
                        int 队伍数量 = (int)Mathf.Floor(list2.Count / 5);
                        int 计数器 = 0;
                        for (int fff = 0; fff < 队伍数量; fff++)
                        {
                            List<将领信息> range = list2.GetRange(计数器, 5);
                            添加Ai攻城(出征目标.坐标x, 出征目标.坐标y, range);
                            计数器 += 5;
                        }
                        全局变量.提示类.显示信息(全局变量.所有玩家数据表[全局变量.所有国家列表[i].国王].基础信息.名字 + "正在进攻" + 出征目标.名称);
                    }
                }
                if (i < 全局变量.所有国家列表.Count && TIME.getTime() - 刷新计时 > 2L)
                {
                    if (TIME.getTime() - 全局变量.所有国家列表[i].上次轮选时间 > 全局变量.所有国家列表[i].轮选时间间隔)
                    {
                        全局变量.所有国家列表[i].上次轮选时间 = TIME.getTime();
                    }
                    全局变量.所有国家列表[i].获取国家城池列表();
                    刷新计时 = TIME.getTime();
                }
                yield return null;
                num5 = i;
            }
            yield return null;
        }
    }

    private IEnumerator 增加资源()
    {
        while (true)
        {
            if (全局变量.所有玩家数据表[全局变量.本机身份].基础信息.粮食增加 >= 0)
            {
                double 科技等级 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[0].基础信息.国家).资源科技;
                double 加成 = 全局变量.所有玩家数据表[全局变量.本机身份].基础信息.粮食增加 * (科技等级 > 0 ? 科技等级 / 100 : 0);
                全局变量.所有玩家数据表[全局变量.本机身份].财产信息.粮食 += 全局变量.所有玩家数据表[全局变量.本机身份].基础信息.粮食增加 + 加成;
            }

            for (int i = 0; i < 全局变量.画册列表.Count; i++)
            {
                全局变量.画册列表[i].到期时间 -= 1;
            }

            // 军情按钮上的通知角标改由 军情通知角标 自己刷新（统计全部军情条数），
            // 原来这段「有身份!=0的军情就把红点开关一下」的逻辑撤掉，免得两边规则不一致互相打架。

            if (全局变量.领取倒计时>0)
            {
                全局变量.领取倒计时 -= 1;
            }
            yield return new WaitForSeconds(1);
        }

    }

    private IEnumerator 减少将领忠诚()
    {
        while (true)
        {
            for (int i = 全局变量.所有玩家数据表[0].封地信息表[0].将领信息表.Count - 1; i >= 0; i--)
            {
                全局变量.所有玩家数据表[0].封地信息表[0].将领信息表[i].详细信息.忠诚 -= 1;
                if ((int)全局变量.所有玩家数据表[0].封地信息表[0].将领信息表[i].详细信息.忠诚 < 0)
                {
                    全局变量.所有玩家数据表[0].封地信息表[0].将领信息表[i].详细信息.忠诚 = 0;
                }

            }
            yield return new WaitForSeconds(60);
        }
    }

    private IEnumerator 判断将领忠诚()
    {
        int 第几个玩家 = 全局变量.本机身份;
        while (true)
        {
            for (int i = 全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表.Count - 1; i >= 0; i--)
            {
                if (全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.忠诚 < 60)
                {
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.将领叛逃计时器 -= 1;
                    if (全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.将领叛逃计时器 <= 0)
                    {
                        int num = UnityEngine.Random.Range(0, 1000);
                        if (num < 50 + (60 - 全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.忠诚))
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                将领装备 将领装备 = 全局变量.所有玩家数据表[第几个玩家].背包装备列表.寻找指定将领的装备(j, 全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[j].ID);
                                if (将领装备 != null)
                                {
                                    将领装备.将领ID = -1;
                                }
                            }

                            int 第几个封地 = 0;
                            int 第几个将领 = i;
                            bool flag = false;
                            int count = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表.Count;
                            for (int j = 0; j < count; j++)
                            {
                                if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.ID == (double)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].ID)
                                {
                                    flag = true;
                                    全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                闲兵信息 闲兵信息 = new 闲兵信息();
                                闲兵信息.ID = (int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.ID;
                                闲兵信息.数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量;
                                全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表.Add(闲兵信息);
                            }
                            全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.ID = 0.0;
                            全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量 = 0.0;


                            if (全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].将领属性.初始属性.系列 != "名将")
                            {
                                全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表.RemoveAt(i);
                                UnityEngine.Debug.Log(" 删除将领  " + i.ToString());
                                全局变量.提示类.显示信息("你的一名将领已回归大自然 再也找不到了!");
                            }
                            else
                            {
                                全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.忠诚 = 100.0;
                                全局变量.所有玩家数据表[2].添加将领信息到列表(0, 全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i]);
                                全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表.RemoveAt(i);
                                UnityEngine.Debug.Log(" 删除名将  " + i.ToString());
                                全局变量.提示类.显示信息("名将已回归大自然!");
                            }
                        }
                        else
                        {
                            全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.将领叛逃计时器 = 5;
                            全局变量.提示类.显示信息("你的一名将领对你的领导有些意见 请注意!");
                            全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].详细信息.忠诚 -= 1;
                        }
                    }

                }
            }
            yield return new WaitForSeconds(1);
        }

    }

    private IEnumerator 刷新基础信息()
    {
        long 刷新计时 = TIME.getTime() - 4;
        UnityEngine.Random.Range(60, 120);
        TIME.getTime();
        int 第几个玩家 = 全局变量.本机身份;
        while (true)
        {
            if (TIME.getTime() - 刷新计时 > 3)
            {
                国家信息对象.text = 全局变量.所有玩家数据表[第几个玩家].基础信息.国家;
                等级信息对象.text = 全局变量.所有玩家数据表[第几个玩家].基础信息.等级.ToString();
                float num5 = 全局变量.所有玩家数据表[第几个玩家].基础信息.获取当前等级经验条比例();
                声望条显示.sizeDelta = new Vector2(103.309f * num5, 11f);
                声望显示.text = Mathf.Floor(num5 * 100f).ToString() + "%";
                double 粮食 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食;
                string text3;
                if (粮食 >= 10000.0)
                {
                    if (粮食 > 300000000.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 300000000.0;
                    }
                    text3 = Mathf.Floor((float)粮食 / 10000f).ToString() + "W";
                }
                else
                {
                    text3 = 粮食.ToString();
                }
                double 铜钱 = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱;
                string text2;
                if (铜钱 >= 10000.0)
                {
                    if (铜钱 > 100000000.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 100000000.0;
                    }
                    text2 = Mathf.Floor((float)铜钱 / 10000f).ToString() + "W";
                }
                else
                {
                    text2 = 铜钱.ToString();
                }
                double num4 = 全局变量.所有玩家数据表[第几个玩家].获取人口上限();
                double num3 = 全局变量.所有玩家数据表[第几个玩家].获取已占用人口();
                string str3 = (!(num4 >= 100000.0)) ? num4.ToString() : (Mathf.Floor((float)num4 / 10000f).ToString() + "W");
                string str2 = (!(num3 >= 100000.0)) ? num3.ToString() : (Mathf.Floor((float)num3 / 10000f).ToString() + "W");
                人口信息对象.text = str2 + "/" + str3;
                粮食信息对象.text = text3;
                铜钱信息对象.text = text2;
                if (全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 > 6666666.0)
                {
                    全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 = 6666666.0;
                }
                if (全局变量.所有玩家数据表[第几个玩家].财产信息.白银 > 6666666.0)
                {
                    全局变量.所有玩家数据表[第几个玩家].财产信息.白银 = 6666666.0;
                }
                bool flag = false;
                if (全局变量.所有玩家数据表[第几个玩家].基础信息.ID < 2)
                {
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.工程设计 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.工程设计 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.征召技巧 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.征召技巧 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.铸铁技术 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.铸铁技术 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.甲胄制造 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.甲胄制造 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.药草研究 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.药草研究 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.阵法技巧 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.阵法技巧 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.抛射技巧 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.抛射技巧 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.驾驭技巧 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.驾驭技巧 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.战车设计 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.战车设计 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.统帅能力 > 15.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.统帅能力 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.格斗 > 5.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.格斗 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.精准 > 5.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.精准 = 1.0;
                        flag = true;
                    }
                    if (全局变量.所有玩家数据表[第几个玩家].科技信息.驯马 > 5.0)
                    {
                        全局变量.所有玩家数据表[第几个玩家].科技信息.驯马 = 1.0;
                        flag = true;
                    }
                }
                if (flag)
                {
                    全局变量.提示类.显示信息("科技数据异常,退出!");
                    Application.Quit();
                }
                刷新计时 = TIME.getTime();
            }
            else if (TIME.getTime() - 刷新计时 >= 1)
            {
                时间信息对象.text = TIME.转时间格式2();
            }
            if (TIME.getTime() - 全局变量.酒馆刷新时间 > 3600)
            {
                招募将领脚本对象.自动刷新招募将领();
                全局变量.酒馆刷新时间 = TIME.getTime();
            }
            yield return null;
        }
    }

    private IEnumerator 刷新大地图列表()
    {
        所有城池界面脚本 脚本对象 = 所有城池界面布局.transform.GetComponent<所有城池界面脚本>();
        long 刷新计时 = TIME.getTime() - 4;
        while (true)
        {
            if (TIME.getTime() - 刷新计时 > 3)
            {
                刷新计时 = TIME.getTime();
                if (所有城池界面布局.transform.parent.parent.parent.parent.gameObject.activeSelf)
                {
                    脚本对象.刷新所有城池();
                }
            }
            yield return null;
        }
    }

    private IEnumerator 检测军情列表()
    {
        while (true)
        {
            for (int i = 0; i < 全局变量.军情列表.Count; i++)
            {
                if (全局变量.军情列表[i].身份 == 0)
                {
                    long time = TIME.getTime();
                    if (全局变量.军情列表[i].已进入战场)
                    {
                        continue;
                    }
                    if (全局变量.军情列表[i].到达时间 > time)
                    {
                        continue;
                    }
                    bool flag = true;
                    int num27 = 0;

                    foreach (object obj in 战斗地图列表.transform)
                    {
                        Transform transform = (Transform)obj;
                        战斗系统脚本对象 = transform.GetChild(0).GetChild(0).GetChild(0)
                            .GetChild(0)
                            .GetChild(0)
                            .GetComponent<战斗系统>();
                        if (全局变量.军情列表[i].坐标x == 战斗系统脚本对象.坐标x && 全局变量.军情列表[i].坐标y == 战斗系统脚本对象.坐标y && 全局变量.军情列表[i].战场类型 == 战斗系统脚本对象.战场类型)
                        {
                            flag = false;
                        }
                        num27++;
                    }
                    if (flag && !全局变量.军情列表[i].已进入战场)
                    {
                        if (全局变量.军情列表[i].战场类型 == 0)
                        {
                            要创建的战斗地图对象 = UnityEngine.Object.Instantiate(全局变量.山贼战斗场景pre);
                            要创建的战斗地图对象.transform.SetParent(战斗地图列表.transform);
                        }
                        else if (全局变量.军情列表[i].战场类型 == 1)
                        {
                            要创建的战斗地图对象 = UnityEngine.Object.Instantiate(全局变量.城池战斗场景pre);
                            要创建的战斗地图对象.transform.SetParent(战斗地图列表.transform);
                        }
                        战斗系统脚本对象 = 要创建的战斗地图对象.transform.GetChild(0).GetChild(0).GetChild(0)
                            .GetChild(0)
                            .GetChild(0)
                            .GetComponent<战斗系统>();

                        战斗系统脚本对象.坐标x = 全局变量.军情列表[i].坐标x;
                        战斗系统脚本对象.坐标y = 全局变量.军情列表[i].坐标y;
                        战斗系统脚本对象.战场类型 = 全局变量.军情列表[i].战场类型;
                        战斗系统脚本对象.创建时间 = TIME.getTime();
                        int 坐标x = 全局变量.军情列表[i].坐标x;
                        int 坐标y = 全局变量.军情列表[i].坐标y;
                        if (全局变量.军情列表[i].战场类型 == 0)
                        {
                            List<将领信息> list3 = new List<将领信息>();
                            UnityEngine.Debug.Log(坐标x.ToString() + "坐标" + 坐标y.ToString());
                            山贼属性信息 山贼属性信息 = 附近山贼.获取指定坐标的山贼(坐标x, 坐标y);
                            if (山贼属性信息 != null)
                            {
                                int count5 = 山贼属性信息.将领数据列表.Count;
                                for (int n = 0; n < count5; n++)
                                {
                                    山贼属性信息.将领数据列表[n].详细信息.坑位颜色 = 1.0;
                                    list3.Add(山贼属性信息.将领数据列表[n]);
                                }
                            }
                            战斗系统脚本对象.守方要渲染的编队将领列表.Add(list3);
                        }
                        else if (全局变量.军情列表[i].战场类型 == 1)
                        {
                            城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(坐标x, 坐标y);
                            城池信息库类.正在交战 = true;
                            List<将领信息> list2 = new List<将领信息>();

                            if (城池信息库类.规模 == 0)
                            {
                                int num26 = UnityEngine.Random.Range(20, 30);
                                for (int m = 0; m < num26; m++)
                                {
                                    int 要生成的等级9 = UnityEngine.Random.Range(20, 30);
                                    list2.Add(随机一个城池驻防将领(要生成的等级9, 城池信息库类.国家));
                                }
                            }
                            else if (城池信息库类.规模 == 1)
                            {
                                int num25 = UnityEngine.Random.Range(30, 40);
                                for (int l = 0; l < num25; l++)
                                {
                                    int 要生成的等级8 = UnityEngine.Random.Range(30, 50);
                                    list2.Add(随机一个城池驻防将领(要生成的等级8, 城池信息库类.国家));
                                }
                            }
                            else if (城池信息库类.规模 == 2)
                            {
                                int num24 = UnityEngine.Random.Range(40, 60);
                                for (int k = 0; k < num24; k++)
                                {
                                    int 要生成的等级7 = UnityEngine.Random.Range(60, 70);
                                    list2.Add(随机一个城池驻防将领(要生成的等级7, 城池信息库类.国家));
                                }
                            }
                            else if (城池信息库类.规模 == 3)
                            {
                                int num23 = UnityEngine.Random.Range(100, 150);
                                for (int j = 0; j < num23; j++)
                                {
                                    int 要生成的等级6 = UnityEngine.Random.Range(70, 80);
                                    list2.Add(随机一个城池驻防将领(要生成的等级6, 城池信息库类.国家));
                                }
                            }
                            else if (城池信息库类.规模 == 4)
                            {
                                if (全局变量.所有玩家数据表[全局变量.本机身份].基础信息.名字 != "997788")
                                {
                                    int num22 = UnityEngine.Random.Range(200, 230);
                                    for (int num21 = 0; num21 < num22; num21++)
                                    {
                                        int 要生成的等级5 = UnityEngine.Random.Range(90, 99);
                                        list2.Add(随机一个城池驻防将领(要生成的等级5, 城池信息库类.国家));
                                    }
                                }


                            }

                            int num20 = UnityEngine.Random.Range(0, 101);

                            if ((double)num20 <= 城池信息库类.协防几率)
                            {
                                if (城池信息库类.获取国家名字() != "无")
                                {
                                    int num19 = (int)城池信息库类.获取名将驻防数量();
                                    for (int num18 = 0; num18 < num19; num18++)
                                    {
                                        将领信息 将领信息 = 随机一个名将驻防城池(城池信息库类.国家, 城池信息库类.规模);
                                        if (将领信息 == null)
                                        {
                                            break;
                                        }
                                        int count4 = list2.Count;
                                        将领信息.详细信息.坑位颜色 = 1.0;
                                        将领信息.详细信息.状态 = 1.0;
                                        list2.Insert(UnityEngine.Random.Range(0, count4), 将领信息);
                                    }
                                }
                            }

                            #region 名将驻防列表
                            for (int k = 0; k < 城池信息库类.城池驻防列表.Count; k++)
                            {
                                将领属性库类 将领属性 = 全局将领库.查询指定ID的将领数据(城池信息库类.城池驻防列表[k].将领ID标识);
                                将领信息 将领信息 = 全局方法类.获取指定名字将领信息(将领属性.名字);
                                if (将领信息 != null)
                                {
                                    if (将领信息.详细信息.状态 != 0.0)
                                        continue;
                                    print("名字:" + 将领信息.将领属性.初始属性.名字 + "状态:" + 将领信息.详细信息.状态);
                                    将领信息.将领配兵.ID = 104.0;
                                    将领信息.将领配兵.数量 = 将领信息.将领属性.最终属性.统兵;
                                    int count2 = list2.Count;
                                    将领信息.详细信息.坑位颜色 = 1.0;
                                    将领信息.详细信息.状态 = 1.0;
                                    UnityEngine.Debug.Log(将领信息.将领属性.初始属性.名字 + "入场");
                                    list2.Insert(UnityEngine.Random.Range(0, count2), 将领信息);
                                }

                            }
                            #endregion

                            int count3 = list2.Count;
                            int num17 = (int)Mathf.Floor(count3 / 5);
                            int num16 = 0;
                            for (int num15 = 0; num15 < num17; num15++)
                            {
                                List<将领信息> range = list2.GetRange(num16, 5);
                                if (num15 < 8)
                                {
                                    战斗系统脚本对象.守方要渲染的编队将领列表.Add(range);
                                }
                                else
                                {
                                    StartCoroutine(加入战场(战斗系统脚本对象, range));
                                }
                                num16 += 5;
                            }
                            int num14 = num17 * 5;
                            战斗系统脚本对象.守方要渲染的编队将领列表.Add(list2.GetRange(num14, count3 - num14));
                        }
                    }
                    if (!全局变量.军情列表[i].已进入战场)
                    {
                        全局变量.军情列表[i].已进入战场 = true;
                        战斗系统脚本对象.攻方要渲染的编队将领列表.Add(全局变量.军情列表[i].队列将领列表);
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator Ai军情检测()
    {
        while (true)
        {
            for (int i = 0; i < 全局变量.军情列表.Count; i++)
            {
                if (全局变量.军情列表[i].身份 != 0)
                {
                    long time = TIME.getTime();
                    if (全局变量.军情列表[i].已进入战场)
                        continue;
                    if (全局变量.军情列表[i].到达时间 > time)
                        continue;

                    bool flag = true;
                    foreach (object obj in 战斗地图列表.transform)
                    {
                        Transform transform = (Transform)obj;
                        战斗系统脚本对象 = transform.GetChild(0).GetChild(0).GetChild(0)
                            .GetChild(0)
                            .GetChild(0)
                            .GetComponent<战斗系统>();
                        if (全局变量.军情列表[i].坐标x == 战斗系统脚本对象.坐标x && 全局变量.军情列表[i].坐标y == 战斗系统脚本对象.坐标y && 全局变量.军情列表[i].战场类型 == 战斗系统脚本对象.战场类型)
                        {
                            flag = false;
                        }
                    }
                    // 初始化驻防信息
                    if (flag && !全局变量.军情列表[i].已进入战场)
                    {

                        要创建的战斗地图对象 = UnityEngine.Object.Instantiate(全局变量.城池战斗场景pre);
                        要创建的战斗地图对象.transform.SetParent(战斗地图列表.transform);

                        战斗系统脚本对象 = 要创建的战斗地图对象.transform.GetChild(0).GetChild(0).GetChild(0)
                            .GetChild(0)
                            .GetChild(0)
                            .GetComponent<战斗系统>();

                        战斗系统脚本对象.坐标x = 全局变量.军情列表[i].坐标x;
                        战斗系统脚本对象.坐标y = 全局变量.军情列表[i].坐标y;
                        战斗系统脚本对象.战场类型 = 全局变量.军情列表[i].战场类型;
                        战斗系统脚本对象.创建时间 = TIME.getTime();
                        int 坐标x = 全局变量.军情列表[i].坐标x;
                        int 坐标y = 全局变量.军情列表[i].坐标y;
                        城池信息库类 城池 = 所有城池界面脚本.根据坐标获取指定城池(坐标x, 坐标y);
                        int 攻城方身份 = 2;
                        if (全局变量.军情列表[i].队列将领列表.Count > 0 && 全局变量.军情列表[i].队列将领列表[0] != null)
                        {
                            攻城方身份 = (int)全局变量.军情列表[i].队列将领列表[0].详细信息.身份;
                        }
                        战斗系统脚本对象.攻身份 = 攻城方身份;
                        城池.正在交战 = true;
                        List<将领信息> list2 = new List<将领信息>();
                        if (城池.规模 == 1)
                        {
                            int num25 = UnityEngine.Random.Range(30, 40);
                            for (int l = 0; l < num25; l++)
                            {
                                int 要生成的等级8 = UnityEngine.Random.Range(30, 50);
                                list2.Add(随机一个城池驻防将领(要生成的等级8, 城池.国家));
                            }
                        }
                        else if (城池.规模 == 2)
                        {
                            int num24 = UnityEngine.Random.Range(40, 60);
                            for (int k = 0; k < num24; k++)
                            {
                                int 要生成的等级7 = UnityEngine.Random.Range(60, 70);
                                list2.Add(随机一个城池驻防将领(要生成的等级7, 城池.国家));
                            }
                        }
                        else if (城池.规模 == 3)
                        {
                            int num23 = UnityEngine.Random.Range(20, 35);
                            for (int j = 0; j < num23; j++)
                            {
                                int 要生成的等级6 = UnityEngine.Random.Range(70, 80);
                                list2.Add(随机一个城池驻防将领(要生成的等级6, 城池.国家));
                            }
                        }
                        else if (城池.规模 == 4)
                        {
                            if (全局变量.所有玩家数据表[全局变量.本机身份].基础信息.名字 != "9977886")
                            {
                                int num22 = UnityEngine.Random.Range(10, 30);
                                for (int num21 = 0; num21 < num22; num21++)
                                {
                                    int 要生成的等级5 = UnityEngine.Random.Range(90, 99);
                                    list2.Add(随机一个城池驻防将领(要生成的等级5, 城池.国家));
                                }
                            }
                        }

                        int num20 = UnityEngine.Random.Range(0, 101);

                        for (int kk = 0; kk < 城池.城池玩家驻防列表.Count; kk++)
                        {
                            list2.Add(城池.城池玩家驻防列表[kk]);
                        }

                        int count3 = list2.Count;
                        int num17 = (int)Mathf.Floor(count3 / 5);
                        int num16 = 0;
                        for (int num15 = 0; num15 < num17; num15++)
                        {
                            List<将领信息> 队伍 = list2.GetRange(num16, 5);
                            if (num15 < 8)
                            {
                                战斗系统脚本对象.守方要渲染的编队将领列表.Add(队伍);
                            }
                            else
                            {
                                StartCoroutine(加入战场(战斗系统脚本对象, 队伍));
                            }
                            num16 += 5;
                        }
                        int num14 = num17 * 5;
                        战斗系统脚本对象.守方要渲染的编队将领列表.Add(list2.GetRange(num14, count3 - num14));
                    }
                    if (!全局变量.军情列表[i].已进入战场)
                    {
                        if (全局变量.军情列表[i].身份 == 78)
                        {
                            if (!flag)
                            {
                                foreach (object obj in 战斗地图列表.transform)
                                {
                                    Transform transform = (Transform)obj;
                                    战斗系统脚本对象 = transform.GetChild(0).GetChild(0).GetChild(0)
                                        .GetChild(0)
                                        .GetChild(0)
                                        .GetComponent<战斗系统>();
                                    if (全局变量.军情列表[i].坐标x == 战斗系统脚本对象.坐标x && 全局变量.军情列表[i].坐标y == 战斗系统脚本对象.坐标y && 全局变量.军情列表[i].战场类型 == 战斗系统脚本对象.战场类型)
                                    {
                                        战斗系统脚本对象.守方要渲染的编队将领列表.Add(全局变量.军情列表[i].队列将领列表);
                                        print($"要加入的战场{全局变量.军情列表[i].坐标x},{全局变量.军情列表[i].坐标y}实际加入的战场{战斗系统脚本对象.坐标x},{战斗系统脚本对象.坐标y}");
                                        break;
                                    }
                                }
                            }

                        }
                        else
                        {
                            foreach (object obj in 战斗地图列表.transform)
                            {
                                Transform transform = (Transform)obj;
                                战斗系统脚本对象 = transform.GetChild(0).GetChild(0).GetChild(0)
                                    .GetChild(0)
                                    .GetChild(0)
                                    .GetComponent<战斗系统>();
                                if (全局变量.军情列表[i].坐标x == 战斗系统脚本对象.坐标x && 全局变量.军情列表[i].坐标y == 战斗系统脚本对象.坐标y && 全局变量.军情列表[i].战场类型 == 战斗系统脚本对象.战场类型)
                                {
                                    战斗系统脚本对象.攻方要渲染的编队将领列表.Add(全局变量.军情列表[i].队列将领列表);
                                    print($"要加入的战场{全局变量.军情列表[i].坐标x},{全局变量.军情列表[i].坐标y}实际加入的战场{战斗系统脚本对象.坐标x},{战斗系统脚本对象.坐标y}");
                                    break;
                                }
                            }

                        }
                        全局变量.军情列表[i].已进入战场 = true;
                    }
                }
            }

            yield return null;
        }
    }

    private IEnumerator 加入战场(战斗系统 要加入的战场, List<将领信息> 要加入的编队)
    {
        long 加入计时 = TIME.getTime();
        double 守方兵力 = 要加入的战场.守方兵力;
        int 计数器 = 0;
        long 加入间隔 = UnityEngine.Random.Range(5, 20);
        while (TIME.getTime() - 加入计时 <= 加入间隔 || 守方兵力 >= 500000.0)
        {
            计数器++;
            yield return null;
        }
        if (要加入的战场 != null && !要加入的战场.战斗结束)
        {
            要加入的战场.守方要渲染的编队将领列表.Add(要加入的编队);
        }
    }

    private IEnumerator 刷新商城()
    {
        while (true)
        {
           int 价格 = UnityEngine.Random.Range(1000, 8000);
            for (int i = 0; i < 全局商城库.其他商品列表.Count; i++)
            {
                print($"{全局商城库.其他商品列表[i].道具名}{全局商城库.其他商品列表[i].黄金售价}");
                价格 = UnityEngine.Random.Range(1011, 1326);
                全局商城库.其他商品列表[i].黄金售价 = 价格;
                全局商城库.其他商品列表[i].限购数量 = UnityEngine.Random.Range(1, 10);
            }
            yield return new WaitForSeconds(300);
        }
    }

    private void 添加Ai攻城(int x, int y, List<将领信息> list)
    {

        军情信息 ai = new 军情信息();
        ai.身份 = 666;
        ai.战场类型 = 1;
        ai.坐标x = x;
        ai.坐标y = y;
        ai.队列将领列表 = list;
        ai.到达时间 = TIME.getTime() + 30;
        全局变量.军情列表.Add(ai);
    }

    private 将领信息 随机一个名将驻防城池(string 国家名字, int 规模)
    {
        国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(国家名字);
        List<将领信息> list = (国家信息库类 == null) ? 全局变量.所有玩家数据表[2].封地信息表[0].将领信息表 : 全局变量.所有玩家数据表[国家信息库类.国王].封地信息表[0].将领信息表;
        List<将领信息> list2 = new List<将领信息>();
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            if (list[i].详细信息.状态 != 0.0)
            {
                continue;
            }
            bool flag = true;
            if (list[i].将领属性.初始属性.系列 != "名将")
            {
                if (规模 < 3)
                {
                    flag = false;
                }
                if (list[i].将领属性.初始属性.系列 == "君王" && 规模 < 4)
                {
                    flag = false;
                }
                if (list[i].将领属性.初始属性.系列 == "尊将" && 规模 < 4)
                {
                    flag = false;
                }
                if (list[i].将领属性.初始属性.系列 == "战将" && 规模 < 4)
                {
                    flag = false;
                }
                if (list[i].将领属性.初始属性.系列 == "禧将" && 规模 < 4)
                {
                    flag = false;
                }
            }
            if (flag)
            {
                list[i].将领配兵.ID = 104.0;
                list[i].将领配兵.数量 = list[i].将领属性.最终属性.统兵;
                list2.Add(list[i]);
            }
        }
        if (list2.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, list2.Count);
            return list2[index];
        }
        return null;
    }

    private 将领信息 随机一个城池驻防将领(int 要生成的等级, string 国家名字)
    {
        int index = 全局方法类.获取指定名字的国家(国家名字)?.国王 ?? 2;
        if (要生成的等级 > 90)
        {
            if (全局变量.难度 == 4)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 65.0;
            }
            else if (全局变量.难度 == 3)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 45.0;
            }
            else if (全局变量.难度 == 2)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 35.0;
            }
            else
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 25.0;
            }
        }
        else if (要生成的等级 > 70)
        {
            if (全局变量.难度 == 4)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 55.0;
            }
            else if (全局变量.难度 == 3)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 45.0;
            }
            else if (全局变量.难度 == 2)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 35.0;
            }
            else
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 25.0;
            }
        }
        else if (要生成的等级 > 50)
        {
            if (全局变量.难度 == 4)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 55.0;
            }
            else if (全局变量.难度 == 3)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 45.0;
            }
            else if (全局变量.难度 == 2)
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 35.0;
            }
            else
            {
                全局变量.所有玩家数据表[index].科技信息.统帅能力 = 25.0;
            }
        }
        else if (全局变量.难度 == 2)
        {
            全局变量.所有玩家数据表[index].科技信息.统帅能力 = 15.0;
        }
        else if (全局变量.难度 == 3)
        {
            全局变量.所有玩家数据表[index].科技信息.统帅能力 = 25.0;
        }
        else if (全局变量.难度 == 4)
        {
            全局变量.所有玩家数据表[index].科技信息.统帅能力 = 35.0;
        }
        else
        {
            全局变量.所有玩家数据表[index].科技信息.统帅能力 = 5.0;
        }
        将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(UnityEngine.Random.Range(1, 7));
        if (将领属性库类 != null)
        {
            将领属性库类.获取随机属性();
            将领信息 将领信息 = new 将领信息();
            将领信息.生成将领数据(将领属性库类);
            将领信息.将领属性.初始属性.名字 = 随机姓名.生成随机姓名();
            将领信息.将领获取经验值(将领信息.获取升级需要经验(要生成的等级));
            将领信息.详细信息.坑位颜色 = 1.0;
            int count = 全局变量.所有玩家数据表[index].封地信息表[0].将领信息表.Count;
            全局变量.所有玩家数据表[index].封地信息表[0].将领信息表.Add(将领信息);
            全局变量.所有玩家数据表[index].计算最终属性();
            全局变量.所有玩家数据表[index].封地信息表[0].将领信息表.RemoveAt(count);
            int num = 100 * UnityEngine.Random.Range(1, 5);
            int num2 = UnityEngine.Random.Range(1, 5);
            num2 = ((要生成的等级 <= 50) ? UnityEngine.Random.Range(1, 4) : 4);
            if (num == 400 && num2 == 3)
            {
                num2 = 4;
            }
            int num3 = num + num2;
            将领信息.将领配兵.ID = num3;
            将领信息.将领配兵.数量 = 将领信息.将领属性.最终属性.统兵;
            全局变量.所有玩家数据表[index].科技信息.统帅能力 = 8.0;
            return 将领信息;
        }
        return null;
    }

    public void 调整普通难度()
    {
        全局变量.难度 = 1;
        int count = 全局变量.所有玩家数据表.Count;
        全局变量.所有玩家数据表[1].科技信息.升级全部科技(2);
        for (int i = 2; i < count; i++)
        {
            全局变量.所有玩家数据表[i].科技信息.升级全部科技(5);
        }
        全局变量.提示类.显示信息("已调整为普通难度!");
    }

    public void 调整挑战难度()
    {
        全局变量.难度 = 2;
        int count = 全局变量.所有玩家数据表.Count;
        全局变量.所有玩家数据表[1].科技信息.升级全部科技(5);
        for (int i = 2; i < count; i++)
        {
            全局变量.所有玩家数据表[i].科技信息.升级全部科技(15);
        }
        全局变量.提示类.显示信息("已调整为挑战难度!");
    }
}
