using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class 创建角色脚本 : MonoBehaviour
{
    public Text 君主名对象;

    public Text 国家名对象;

    public GameObject 国家列表对象;

    public Text 国名对象;

    public Text 国王对象;

    public Text 名将列表对象;

    private int 第几个玩家 = 全局变量.本机身份;

    public Text 君主名输入结果;

    public Text 说明文本;

    public Text 群号文本;
        
    private void Start()
    {
        if (全局变量.是否为登录==false)
        {
            君主名对象.text = "";
        }
    }

    public void 创建角色进入游戏()
    {
        if (全局变量.验证变量 ==0)
        {
            return;
        }
        string 名字 = 君主名对象.text.Trim();
        if (名字.Length == 0 || 名字.Length > 20 || 名字.IndexOfAny(new[] { '<', '>', '\r', '\n' }) >= 0)
        {
            说明文本.text = "请输入 1 到 20 字的君主名，不能包含换行或尖括号。";
            return;
        }
        if (!(国家名对象.text != ""))
        {
            return;
        }
        var 玩家 = 全局变量.所有玩家数据表[第几个玩家];
        string 原名字 = 玩家.基础信息.名字;
        玩家.基础信息.名字 = 名字;
        if (全局变量.所有玩家数据表[第几个玩家].加入指定国家(国家名对象.text))
        {
            #if UNITY_EDITOR
            #region 测试账号
            if (全局变量.所有玩家数据表[第几个玩家].基础信息.名字 == "997788")
            {
                全局道具库.使用道具("99级装备套装箱", 0, 0);
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.坐骑装备列表[0].强化满级装备();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.坐骑装备列表[0].装备炼魂全满();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.武器装备列表[0].强化满级装备();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.武器装备列表[0].装备炼魂全满();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.铠甲装备列表[0].强化满级装备();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.铠甲装备列表[0].装备炼魂全满();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.头盔装备列表[0].强化满级装备();
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.头盔装备列表[0].装备炼魂全满();
                for (int i = 0; i < 10; i++)
                {
                    全局变量.所有玩家数据表[第几个玩家].添加指定将领到列表("献帝");
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].将领获取经验值(全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].获取升级需要经验(99.0));
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].将领配兵.ID = 104.0;
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[i].将领配兵.数量 = 999999.0;
                }
                for (int j = 10; j < 20; j++)
                {
                    全局变量.所有玩家数据表[第几个玩家].添加指定将领到列表("尊·小乔");
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[j].将领获取经验值(全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[j].获取升级需要经验(99.0));
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[j].将领配兵.ID = 104.0;
                    全局变量.所有玩家数据表[第几个玩家].封地信息表[0].将领信息表[j].将领配兵.数量 = 99999;
                }
                全局变量.所有玩家数据表[第几个玩家].科技信息.升级全部科技(10);
                全局变量.所有玩家数据表[第几个玩家].科技信息.驯马 = 5.0;
                所有城池界面脚本.根据坐标获取指定城池(74, 19).更换指定城主(全局变量.所有玩家数据表[第几个玩家].基础信息.名字);
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.添加指定装备数据到背包(全局装备库.获取指定名字的装备("尊·皇冠"), 4.0);
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.添加指定装备数据到背包(全局装备库.获取指定名字的装备("尊·龙剑"), 4.0);
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.添加指定装备数据到背包(全局装备库.获取指定名字的装备("尊·龙袍"), 4.0);
                全局变量.所有玩家数据表[第几个玩家].背包装备列表.添加指定装备数据到背包(全局装备库.获取指定名字的装备("尊·龙椅"), 4.0);
                全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 5;
                全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 5;
                全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 = 5;
                全局变量.所有玩家数据表[第几个玩家].基础信息.战功 = 2500;

                所有城池界面脚本.根据坐标获取指定城池(61, 29).更换指定城主(全局变量.所有玩家数据表[第几个玩家].基础信息.名字);
                所有城池界面脚本.根据坐标获取指定城池(61, 33).更换指定城主(全局变量.所有玩家数据表[第几个玩家].基础信息.名字);


            }
            #endregion
            #endif
        }
        else
        {
            玩家.基础信息.名字 = 原名字;
            return;
        }
        界面扩展存档.新建世界();
        SceneManager.LoadScene(1);
    }

    public void 显示选择的国家名()
    {
        国家名对象.text = 全局变量.所有玩家数据表[第几个玩家].基础信息.国家;
    }

    public void 显示国家列表()
    {
        int childCount = 国家列表对象.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            国家列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
        }
        int count = 全局变量.所有国家列表.Count;
        for (int j = 0; j < count; j++)
        {
            childCount = 国家列表对象.transform.childCount;
            GameObject gameObject;
            if (childCount <= j)
            {
                gameObject = UnityEngine.Object.Instantiate(国家列表对象.transform.GetChild(0).gameObject);
                gameObject.transform.SetParent(国家列表对象.transform);
                gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                gameObject = 国家列表对象.transform.GetChild(j).gameObject;
            }
            gameObject.SetActive(value: true);
            国家列表对象.transform.GetChild(j).GetChild(1).GetComponent<Text>()
                .text = 全局变量.所有国家列表[j].国号;
        }
    }

    public void 显示选中国家信息()
    {
        int num = 获取选中国家索引();
        if (num == -1)
        {
            return;
        }
        int 国王 = 全局变量.所有国家列表[num].国王;
        if (国王 != -1)
        {
            string 名字 = 全局变量.所有玩家数据表[国王].基础信息.名字;
            double 科技等级 = 全局变量.所有国家列表[num].科技等级;
            国名对象.text = 全局变量.所有国家列表[num].国名;
            国王对象.text = 名字;
            string text = "";
            int count = 全局变量.所有玩家数据表[国王].封地信息表[0].将领信息表.Count;
            for (int i = 0; i < count; i++)
            {
                text = text + 全局变量.所有玩家数据表[国王].封地信息表[0].将领信息表[i].将领属性.初始属性.名字 + "(" + 全局变量.所有玩家数据表[国王].封地信息表[0].将领信息表[i].将领属性.初始属性.突围.ToString() + "),";
            }
            名将列表对象.text = text;
        }
    }

    public void 选择指定国家()
    {
        int num = 获取选中国家索引();
        if (num != -1)
        {
            全局变量.所有玩家数据表[第几个玩家].基础信息.国家 = 全局变量.所有国家列表[num].国号;
        }
    }

    public int 获取选中国家索引()
    {
        int childCount = 国家列表对象.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            if (国家列表对象.transform.GetChild(i).GetComponent<Toggle>().isOn)
            {
                return i;
            }
        }
        return -1;
    }

    public void 随机君主名()
    {
        string text = 随机姓名.生成随机姓名();
        君主名对象.text = text;
    }

    public void 正在修改君主名()
    {
        君主名对象.text = "";
    }

    public void 修改君主名()
    {
        君主名对象.text = 君主名输入结果.text;
        君主名输入结果.gameObject.SetActive(value: false);
    }

    public void 创建武将()
    {
        print($"添加前{全局将领库.属性表.Count}");
        将领属性库类 新武将 = new 将领属性库类();
        int id = 全局将领库.属性表.Count;
        List<int> list = new List<int>();
        for (int i = 0; i < 全局将领库.属性表.Count; i++)
            list.Add((int)全局将领库.属性表[i].ID);

        while (true)
        {
            if (list.IndexOf(id) != -1)
                id = Random.Range(0, int.MaxValue);
            else
                break;
        }

        新武将.快捷生成名将(id, "新的将领","野","统","骑",96);
        全局将领库.属性表.Add(新武将);
        print(新武将.ID);
    }

}
