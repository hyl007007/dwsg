using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 炼魂脚本 : MonoBehaviour
{
	public Image 装备头像;

	public Text 装备信息显示;

	public GameObject 炼魂列表对象;

	public 将领装备 装备对象;

	public Text 炼魂材料显示;

	public Text 锁定材料显示;

	private int 锁定数量;

	private double 消耗材料数量;

	private double 凝魂晶石材料数量;

	public Toggle 普通炼魂选中;

	public Toggle 高级炼魂选中;

	public Toggle 高级炼魂选中1;

	private void Start()
	{
	}

	public void 显示装备所有信息()
	{
		锁定数量 = 0;
		装备头像.sprite = 装备对象.获取装备头像();
		装备信息显示.text = 装备对象.获取装备名字() + "+" + 装备对象.强化等级.ToString() + "(" + 装备对象.获取装备等级().ToString() + "级) " + 装备对象.获取装备品质文本() + "\n" + 装备对象.获取装备加成文本();
		装备信息显示.color = 装备对象.获取装备文字颜色();
		int count = 装备对象.炼魂属性.Count;
		for (int i = 0; i < 6; i++)
		{
			炼魂列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
			炼魂列表对象.transform.GetChild(i).GetChild(0).gameObject.SetActive(value: false);
			炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
			if (i < count)
			{
				Text component = 炼魂列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>();
				component.text = 装备对象.炼魂属性[i].获取炼魂加成文本();
				component.color = 装备对象.获取装备文字颜色();
				炼魂列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>()
					.color = 装备对象.获取装备文字颜色();
				炼魂列表对象.transform.GetChild(i).gameObject.SetActive(value: true);
				if (装备对象.炼魂属性[i].锁定)
				{
					炼魂列表对象.transform.GetChild(i).GetChild(0).gameObject.SetActive(value: true);
					炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: true);
					锁定数量++;
				}
				else
				{
					炼魂列表对象.transform.GetChild(i).GetComponent<Toggle>().isOn = false;
				}
			}
		}
		int 本机身份 = 全局变量.本机身份;
		string text = 装备对象.获取装备炼魂材料名字();
		消耗材料数量 = 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量(text);
		凝魂晶石材料数量 = 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("凝魂晶石");
		炼魂材料显示.text = text + 消耗材料数量.ToString() + "/" + 装备对象.品质.ToString();
		锁定材料显示.text = "凝魂晶石" + 凝魂晶石材料数量.ToString() + "/" + 锁定数量.ToString();
	}

	public void 锁定指定炼魂()
	{
		锁定数量 = 0;
		int count = 装备对象.炼魂属性.Count;
		for (int i = 0; i < 6; i++)
		{
			if (i < count)
			{
				if (炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf)
				{
					装备对象.炼魂属性[i].锁定 = true;
					锁定数量++;
				}
				else
				{
					装备对象.炼魂属性[i].锁定 = false;
				}
			}
		}
		锁定材料显示.text = "凝魂晶石" + 凝魂晶石材料数量.ToString() + "/" + 锁定数量.ToString();
	}

	public void 开始炼魂()
	{
		List<炼魂属性> list = new List<炼魂属性>();
		bool flag = false;
		if (消耗材料数量 >= 装备对象.品质 && 凝魂晶石材料数量 >= (double)锁定数量)
		{
			int 本机身份 = 全局变量.本机身份;
			string 道具名字 = 装备对象.获取装备炼魂材料名字();
			string a = 全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具(道具名字, (int)装备对象.品质, 0, 0);
			if (a != "使用失败")
			{
				flag = true;
			}
			if (锁定数量 > 0)
			{
				全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具("凝魂晶石", 锁定数量, 0, 0);
				flag = ((a != "使用失败") ? true : false);
			}
			if (高级炼魂选中.isOn)
			{
				if (全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0 >= 0.0)
				{
					全局变量.所有玩家数据表[本机身份].财产信息.黄金 = 全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0;
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			if (高级炼魂选中1.isOn)
			{
				if (全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0 >= 5.0)
				{
					全局变量.所有玩家数据表[本机身份].财产信息.黄金 = 全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0;
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
		}
		if (!flag)
		{
			全局变量.提示类.显示信息("材料不足");
		}
		else
		{
			int count = 装备对象.炼魂属性.Count;
			for (int i = 0; i < count; i++)
			{
				if (装备对象.炼魂属性[i].锁定)
				{
					list.Add(装备对象.炼魂属性[i]);
				}
			}
			if (list.Count < 6)
			{
				int num = 1;
				if (高级炼魂选中.isOn || 高级炼魂选中1.isOn)
				{
					num = 3;
					if (num >= 7 - list.Count)
					{
						num = 7 - list.Count - 1;
					}
				}
				double num2 = UnityEngine.Random.Range(num, 7 - list.Count);
				for (int j = 0; (double)j < num2; j++)
				{
					double 类型 = UnityEngine.Random.Range(1, 6);
					float num3 = (float)装备对象.获取炼魂加成上限(类型);
					int num4 = (int)UnityEngine.Random.Range(1f, num3 + 1f);
					if (普通炼魂选中.isOn && (double)UnityEngine.Random.Range(1, 101) < 30.0)
					{
						num4 = -num4;
					}
					炼魂属性 炼魂属性 = new 炼魂属性();
					炼魂属性.类型 = 类型;
					炼魂属性.炼魂值 = num4;
					炼魂属性.锁定 = false;
					list.Add(炼魂属性);
				}
				装备对象.炼魂属性.Clear();
				装备对象.炼魂属性 = list;
			}
		}
		显示装备所有信息();
	}
    public List<炼魂属性> 开始炼魂一次()
    {
        List<炼魂属性> list = new List<炼魂属性>();
        bool flag = false;
        int count = this.装备对象.炼魂属性.Count;
        for (int i = 0; i < count; i++)
        {
            if (this.装备对象.炼魂属性[i].锁定)
            {
                list.Add(this.装备对象.炼魂属性[i]);
            }
        }
        if (list.Count == 6)
        {
            全局变量.提示类.显示信息("已经六条属性了");
            return list;
        }
        if (this.消耗材料数量 >= this.装备对象.品质 && this.凝魂晶石材料数量 >= (double)this.锁定数量)
        {
            int 本机身份 = 全局变量.本机身份;
            string 道具名字 = this.装备对象.获取装备炼魂材料名字();
            string a = 全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具(道具名字, (int)this.装备对象.品质, 0, 0);
            if (a != "使用失败")
            {
                flag = true;
            }
            if (this.锁定数量 > 0)
            {
                全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具("凝魂晶石", this.锁定数量, 0, 0);
                flag = (a != "使用失败");
            }
            if (this.高级炼魂选中.isOn)
            {
                if (全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0 >= 0.0)
                {
                    全局变量.所有玩家数据表[本机身份].财产信息.黄金 = 全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0;
                    flag = true;
                }
                else
                {
                    flag = false;
                }
            }
            if (this.高级炼魂选中1.isOn)
            {
                if (全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0 >= 5.0)
                {
                    全局变量.所有玩家数据表[本机身份].财产信息.黄金 = 全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 5.0;
                    flag = true;
                }
                else
                {
                    flag = false;
                }
            }
        }
        if (!flag)
        {
            全局变量.提示类.显示信息("材料不足");
            List<炼魂属性> tmp = new List<炼魂属性>();
            this.装备对象.炼魂属性.ForEach(item => tmp.Add(item));
            return tmp;
        }
        else
        {
            if (list.Count < 6)
            {
                int num = 1;
                if (this.高级炼魂选中.isOn || this.高级炼魂选中1.isOn)
                {
                    num = 3;
                    if (num >= 7 - list.Count)
                    {
                        num = 7 - list.Count - 1;
                    }
                }
                double num2 = (double)UnityEngine.Random.Range(num, 7 - list.Count);
                int num3 = 0;
                while ((double)num3 < num2)
                {
                    double 类型 = (double)UnityEngine.Random.Range(1, 6);
                    float num4 = (float)this.装备对象.获取炼魂加成上限(类型);
                    int num5 = (int)UnityEngine.Random.Range(1f, num4 + 1f);
                    if (this.普通炼魂选中.isOn && (double)UnityEngine.Random.Range(1, 101) < 30.0)
                    {
                        num5 = 0 - num5;
                    }
                    list.Add(new 炼魂属性
                    {
                        类型 = 类型,
                        炼魂值 = (double)num5,
                        锁定 = false
                    });
                    num3++;
                }
                //this.装备对象.炼魂属性.Clear();
                //this.装备对象.炼魂属性 = list;
            }
        }
        //this.显示装备所有信息();
        return list;
    }

    public void 开始炼魂30次()
    {
        int i = 0;
        while (i++ < 30)
        {
            List<炼魂属性> tmp = this.开始炼魂一次();
            this.锁定数量 = 0;
            for (int j = 0; j < tmp.Count; j++)
            {
				

				if ((int)tmp[j].炼魂值 >= 50)
                {
                    //锁定条件
                    tmp[j].锁定 = true;
                }
                else if ((int)tmp[j].炼魂值 > 20 && (int)tmp[j].类型 == 2)
                {
                    tmp[j].锁定 = true;
                }
                if (tmp[j].锁定)
                {
                    this.锁定数量++;
                }
            }
            this.装备对象.炼魂属性.Clear();
            this.装备对象.炼魂属性 = tmp;


        }
        this.显示装备所有信息();
    }


	enum leix
	{
		a=1,b,c,d,e
	}
}
