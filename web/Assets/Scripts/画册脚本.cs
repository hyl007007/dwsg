using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 画册脚本 : MonoBehaviour
{
    public Text 查询名将名字对象;

    private List<画册信息> 画册列表;

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

    private int 材料数量;
    public 显示画册列表到界面 显示画册脚本;
    private TimeSpan ts;

    private void Start()
    {
        //玩家对话对象.text = $"{全局变量.所有玩家数据表[全局变量.本机身份].基础信息.名字}:" +
        //  $"我意会战诸侯,中原逐鹿,以成帝王之霸业,无奈帐下缺能征善战之大将,勇冠三军之雄才";
    }

    public void 确定黄金购买()
    {
        添加将领到画册列表(true);
    }
    public void 确定材料交换()
    {
        添加将领到画册列表();
    }

    private void 添加将领到画册列表(bool 是否为黄金 = false)
    {
        for (int i = 0; i < 全局变量.画册列表.Count; i++)
        {
            if (全局变量.画册列表[i].名字 == 查询到的名将.名字)
            {
                全局变量.提示类.显示信息("购买/交换失败\n无法重复购买/交换");
                return;
            }
        }

        if (全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 >= 材料数量 * 10 && 是否为黄金 == true)
        {
            ts = (DateTime.Now.AddDays(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0)) - (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0));
            全局变量.画册列表.Add(new 画册信息(Convert.ToInt64(ts.TotalSeconds), 查询到的名将.名字, (int)查询到的名将.ID));
            名将画册列表界面对象.SetActive(true);
            显示画册脚本.显示画册列表到UI();
            全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 -= 材料数量 * 10;
        }
        else if (全局变量.所有玩家数据表[全局变量.本机身份].背包道具列表.获取指定道具数量("勇士令") >= 材料数量)
        {
            ts = (DateTime.Now.AddDays(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0)) - (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0));
            全局变量.画册列表.Add(new 画册信息(Convert.ToInt64(ts.TotalSeconds), 查询到的名将.名字, (int)查询到的名将.ID));
            名将画册列表界面对象.SetActive(true);
            显示画册脚本.显示画册列表到UI();
            int 道具数量 = (int)全局变量.所有玩家数据表[全局变量.本机身份].背包道具列表.获取指定道具数量("勇士令") - 材料数量;
            全局变量.所有玩家数据表[全局变量.本机身份].背包道具列表.删除道具("勇士令");
            全局变量.所有玩家数据表[全局变量.本机身份].背包道具列表.添加道具("勇士令", 道具数量);

        }
        else
        {
            全局变量.提示类.显示信息("购买/交换失败\n黄金/勇士令不足");
        }
    }

    public void 点击查询()
    {
        if (查询名将名字对象.text.Trim() != "")
        {
            bool 是否找到 = false;
            将领信息 将领 = new 将领信息();
            for (int i = 0; i < 全局变量.所有城池列表.Count; i++)
            {
                for (int j = 0; j < 全局变量.所有城池列表[i].城池驻防列表.Count; j++)

                    查询到的名将 = 全局将领库.查询指定ID的将领数据(全局变量.所有城池列表[i].城池驻防列表[j].将领ID标识);
                if (查询名将名字对象.text.Trim() == 查询到的名将.名字)
                {
                    将领 = 全局方法类.获取指定名字将领信息(查询到的名将.名字);
                    print($"" +
                        $"将领名称:{将领.将领属性.初始属性.名字}" +
                        $"将领状态:{将领.详细信息.状态}" +
                        $"将领身份:{将领.详细信息.身份}" +
                        $"城池名:{全局变量.所有城池列表[i].名称}");
                    是否找到 = true;
                    break;
                }
            }
            for (int i = 0; i < 全局变量.所有玩家数据表.Count; i++)
            {
                for (int j = 0; j < 全局变量.所有玩家数据表[i].封地信息表[0].将领信息表.Count; j++)
                {
                    if (全局变量.所有玩家数据表[i].封地信息表[0].将领信息表[j].将领属性.初始属性.名字 == 查询名将名字对象.text.Trim())
                    {
                        查询到的名将 = 全局将领库.查询指定ID的将领数据(全局变量.所有玩家数据表[i].封地信息表[0].将领信息表[j].将领属性.初始属性.ID);
                        将领 = 全局方法类.获取指定名字将领信息(全局变量.所有玩家数据表[i].封地信息表[0].将领信息表[j].将领属性.初始属性.名字);
                        print($"" +
                            $"将领名称:{将领.将领属性.初始属性.名字}" +
                            $"将领状态:{将领.详细信息.状态}" +
                            $"将领身份:{将领.详细信息.身份}" +
                            $"归属:{全局变量.所有玩家数据表[i].基础信息.名字}");
                        是否找到 = true;
                        break;
                    }
                }
            }

            if (是否找到)
            {
                名将画册消耗显示界面对象.gameObject.SetActive(true);
                材料数量 = 全局方法类.根据突围获取材料数量((int)将领.将领属性.初始属性.突围);

                名将画册材料文本对象.text = $"查询名将:{将领.将领属性.初始属性.名字}\r\n包打听:{将领.将领属性.初始属性.名字}画册需要{材料数量}个勇士令来换\r\n确定交换吗？";
                名将画册价格文本对象.text = $"包打听：\r\n如果你没有勇士令，也可以用{材料数量 * 10}个黄金来";
            }
        }
    }
}

