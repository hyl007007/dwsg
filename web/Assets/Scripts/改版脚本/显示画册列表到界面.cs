using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 显示画册列表到界面 : MonoBehaviour
{
    public GameObject 对象模板;
    public Transform 显示区域;
    private List<GameObject> 名将对象 = new List<GameObject>();
    public GameObject 将领详情界面;
    private double toTime;
    public Text 详情名字;
    public Text 时间显示;
    void Start()
    {

    }

    void Update()
    {
        for (int i = 0; i < 全局变量.画册列表.Count; i++)
        {
            if (全局变量.画册列表[i].名字== 详情名字.text.Trim())
            {
                时间显示.text = TIME.ToTimeFormat(全局变量.画册列表[i].到期时间);
            }
        }
    }

    
    public void 显示画册列表到UI()
    {
        for (int i = 显示区域.childCount - 1; i > 0; i--)
        {
            Destroy(显示区域.GetChild(i).gameObject);
        }

        for (int i = 0; i < 全局变量.画册列表.Count; i++)
        {
            GameObject 将领 = 对象模板;
            将领.transform.GetChild(0).GetComponent<Image>().sprite = 全局将领库.获取指定将领的头像(全局变量.画册列表[i].名字);
            将领.transform.GetChild(1).GetComponent<Text>().text = 全局变量.画册列表[i].名字;
            将领信息 将领详情 = 全局方法类.获取指定名字将领信息(全局变量.画册列表[i].名字);
            将领.transform.GetChild(2).GetComponent<Text>().text = 将领详情.将领属性.成长点数.等级.ToString();
            Instantiate(将领, 显示区域);
            名将对象.Add(将领);
        }
        for (int i = 1; i < 显示区域.childCount; i++)
        {
            显示区域.GetChild(i).gameObject.SetActive(true);
        }
    }

    public void 显示选中将领详情(Text name)
    {
        将领信息 将领详情 = 全局方法类.获取指定名字将领信息(name.text);
        将领详情界面.transform.GetChild(0).GetComponent<Image>().sprite = 全局将领库.获取指定将领的头像(name.text);
        将领详情界面.transform.GetChild(1).GetComponent<Text>().text = 将领详情.将领属性.初始属性.名字;
        long 到期时间 = 0;
        for (int i = 0; i < 全局变量.画册列表.Count; i++)
        {
            if (全局变量.画册列表[i].名字 == 将领详情.将领属性.初始属性.名字)
            {
                到期时间 = 全局变量.画册列表[i].到期时间;
            }
        }

        玩家数据 玩家 = 全局方法类.获取指定名字的玩家(全局方法类.获取指定ID玩家名字((int)将领详情.详细信息.身份));
        将领详情界面.transform.GetChild(2).GetComponent<Text>().text = TIME.ToTimeFormat(到期时间);
        将领详情界面.transform.GetChild(3).GetComponent<Text>().text = 全局方法类.获取指定ID玩家名字((int)将领详情.详细信息.身份);
        将领详情界面.transform.GetChild(4).GetComponent<Text>().text = 全局方法类.获取指定ID状态文本((int)将领详情.详细信息.状态);
        print(玩家.基础信息.名字);
        if (玩家.基础信息.名字 != "野名")
        {
            将领详情界面.transform.GetChild(5).GetComponent<Text>().text = $"城池位置:" +
            $"{所有城池界面脚本.根据坐标获取指定城池(全局方法类.获取指定名字的国家(玩家.基础信息.国家).国都x, 全局方法类.获取指定名字的国家(玩家.基础信息.国家).国都y).名称}" +
            $"({全局方法类.获取指定名字的国家(玩家.基础信息.国家).国都x},{全局方法类.获取指定名字的国家(玩家.基础信息.国家).国都y})";
        }
        else
        {
            for (int i = 0; i < 全局变量.所有城池列表.Count; i++)
            {
                for (int j = 0; j < 全局变量.所有城池列表[i].城池驻防列表.Count; j++)
                {
                    if (全局变量.所有城池列表[i].城池驻防列表[j].将领ID标识 == 将领详情.将领属性.初始属性.ID)
                    {
                        将领详情界面.transform.GetChild(5).GetComponent<Text>().text = $"城池位置:" +
                        $"{全局变量.所有城池列表[i].名称}" +
                        $"({全局变量.所有城池列表[i].坐标x},{全局变量.所有城池列表[i].坐标y})";
                        return;
                    }
                }
            }
        }

    }

}
