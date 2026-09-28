using UnityEngine;
using UnityEngine.UI;

public class 城池信息显示脚本 : MonoBehaviour
{
	public GameObject 我方城池背景布局;

	public GameObject 敌方城池背景布局;

	public Image 城池头像;

	public Text 城池名字坐标;

	public Text 城池规模;

	public Text 图腾显示;

	public Text 国家显示;

	public Text 城主显示;

	public Text 天赋显示;

	public Text 税率显示;

	public Text 驻防显示;

	public Text 封地显示;

	public Text 公告显示;

	public GameObject 选择出征将领界面对象;

	public GameObject 开辟封地按钮;

	public GameObject 进入封地按钮;

	public GameObject 修筑城池按钮;

	public GameObject 进入城池按钮;

	public 封地界面脚本 封地界面脚本对象;

	private int 显示第几个城池;

	public void 显示城池信息(int 第几个城池)
	{
		显示第几个城池 = 第几个城池;
		敌方城池背景布局.SetActive(value: false);
		我方城池背景布局.SetActive(value: false);
		if (全局变量.所有城池列表[第几个城池].获取城池身份() != 0 && 全局变量.所有城池列表[第几个城池].获取城池身份() != 2)
		{
			敌方城池背景布局.SetActive(value: true);
		}
		else
		{
			我方城池背景布局.SetActive(value: true);
		}
		城池头像.sprite = 全局变量.城池规模头像资源表[全局变量.所有城池列表[第几个城池].规模];
		城池名字坐标.text = 全局变量.所有城池列表[第几个城池].名称 + "(" + 全局变量.所有城池列表[第几个城池].坐标x.ToString() + "," + 全局变量.所有城池列表[第几个城池].坐标y.ToString() + ")";
		城池规模.text = 全局变量.所有城池列表[第几个城池].获取规模名称() + "城";
		图腾显示.text = 全局变量.所有城池列表[第几个城池].获取图腾类型名称();
		国家显示.text = 全局变量.所有城池列表[第几个城池].获取国家名字() + "(" + 全局变量.所有城池列表[第几个城池].获取国家国号() + ")";
		城主显示.text = 全局变量.所有城池列表[第几个城池].获取城主名字();
		天赋显示.text = 全局变量.所有城池列表[第几个城池].获取天赋类型名称() + 全局变量.所有城池列表[第几个城池].天赋加成.ToString() + "%";
		税率显示.text = 全局变量.所有城池列表[第几个城池].税率.ToString() + "%";
		封地显示.text = 全局变量.所有城池列表[第几个城池].城池封地列表.Count.ToString() + "/" + 全局变量.所有城池列表[第几个城池].获取封地上限().ToString();
		开辟封地按钮.SetActive(value: false);
		进入封地按钮.SetActive(value: false);
		修筑城池按钮.SetActive(value: false);
		进入城池按钮.SetActive(value: false);
		if (全局变量.所有城池列表[第几个城池].是否属于我的城池())
		{
			进入城池按钮.SetActive(value: true);
		}
		else
		{
			修筑城池按钮.SetActive(value: true);
		}
		if (全局变量.所有城池列表[第几个城池].是否有我的封地())
		{
			进入封地按钮.SetActive(value: true);
		}
		else
		{
			开辟封地按钮.SetActive(value: true);
		}
	}

	public void 出征攻打本城池()
	{
		A星寻路 a星寻路 = new A星寻路();
		int 第几个封地 = 全局变量.第几个封地;
		int 本机身份 = 全局变量.本机身份;
		坐标 所在城池 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].所在城池;
		int num = a星寻路.开始寻路(所在城池.x - 1, 所在城池.y - 1, 全局变量.所有城池列表[显示第几个城池].坐标x - 1, 全局变量.所有城池列表[显示第几个城池].坐标y - 1);
		if (num != -1 || 全局变量.所有玩家数据表[本机身份].基础信息.名字 == "997788")
		{
			UnityEngine.Debug.Log("距离:" + num.ToString());
			选择出征将领 component = 选择出征将领界面对象.GetComponent<选择出征将领>();
			component.城池坐标x = 全局变量.所有城池列表[显示第几个城池].坐标x;
			component.城池坐标y = 全局变量.所有城池列表[显示第几个城池].坐标y;
			选择出征将领界面对象.SetActive(value: true);
			component.index = 2;
            component.刷新城池();
		}
		else
		{
			全局变量.提示类.显示信息("路径不通!");
		}
	}

    public void 出征驻防本城池()
    {
		print(123456);
        A星寻路 a星寻路 = new A星寻路();
        int 第几个封地 = 全局变量.第几个封地;
        int 本机身份 = 全局变量.本机身份;
        坐标 所在城池 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].所在城池;
		城池信息库类 城池 = 所有城池界面脚本.根据坐标获取指定城池(全局变量.所有城池列表[显示第几个城池].坐标x, 全局变量.所有城池列表[显示第几个城池].坐标y);
		if (城池.正在交战)
		{
            int num = a星寻路.开始寻路(所在城池.x - 1, 所在城池.y - 1, 全局变量.所有城池列表[显示第几个城池].坐标x - 1, 全局变量.所有城池列表[显示第几个城池].坐标y - 1);
            if (num != -1 || 全局变量.所有玩家数据表[本机身份].基础信息.名字 == "997788")
            {
                UnityEngine.Debug.Log("距离:" + num.ToString());
                选择出征将领 component = 选择出征将领界面对象.GetComponent<选择出征将领>();
                component.城池坐标x = 全局变量.所有城池列表[显示第几个城池].坐标x;
                component.城池坐标y = 全局变量.所有城池列表[显示第几个城池].坐标y;
                component.index = 1;
                选择出征将领界面对象.SetActive(value: true);
                component.刷新城池();
            }
            else
            {
                全局变量.提示类.显示信息("路径不通!");
            }
        }else
            全局变量.提示类.显示信息("当前无法驻防!");

    }

    public void 开辟封地()
	{
		if (全局变量.所有玩家数据表[全局变量.本机身份].封地信息表.Count<=10)
		{
            if (!全局变量.所有城池列表[显示第几个城池].是否有我的封地())
            {
                全局变量.所有城池列表[显示第几个城池].新建封地(全局变量.本机身份);
                全局变量.提示类.显示信息("开辟封地成功!");
                开辟封地按钮.SetActive(value: false);
                进入封地按钮.SetActive(value: true);
            }
        }
        else
        {
            全局变量.提示类.显示信息("开辟封地失败!");
        }

    }

	public void 进入封地()
	{
		if (全局变量.所有城池列表[显示第几个城池].是否有我的封地())
		{
			int num = 全局变量.所有城池列表[显示第几个城池].获取我的封地ID();
			if (num != -1)
			{
				int 本机身份 = 全局变量.本机身份;
				封地界面脚本对象.第几个封地 = 全局变量.所有玩家数据表[本机身份].获取指定ID标识的封地索引(num);
				封地界面脚本对象.显示封地所有建筑();
			}
		}
	}
}
