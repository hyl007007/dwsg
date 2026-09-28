using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class 建国脚本 : MonoBehaviour
{
	public Text 国名对象;

	public Text 国号对象;

	public Text 国都对象;

	public Text 国家宣言对象;

	public Text 国名输入对象;

	public Text 国号输入对象;

	public Text 宣言输入对象;

	public 城池信息库类 国都城池信息;

	public GameObject 国家列表布局;

	public void 输入国家名字()
	{
		if (国名输入对象.text != null && !(国名输入对象.text == ""))
		{
			if (Encoding.Default.GetBytes(国名输入对象.text).Length < 17)
			{
				string text = 国名输入对象.text;
				UnityEngine.Debug.Log(text);
				国名对象.text = text;
			}
			else
			{
				全局变量.提示类.显示信息("国名错误,重新输入!");
			}
		}
	}

	public void 输入国家国号()
	{
		if (国号输入对象.text != null && !(国号输入对象.text == ""))
		{
			if (Encoding.Default.GetBytes(国号输入对象.text).Length > 3)
			{
				全局变量.提示类.显示信息("国号错误,重新输入!");
				return;
			}
			string text = 国号输入对象.text;
			UnityEngine.Debug.Log(text);
			国号对象.text = text;
		}
	}

	public void 输入国家宣言()
	{
		if (宣言输入对象.text != null && !(宣言输入对象.text == ""))
		{
			string text = 宣言输入对象.text;
			UnityEngine.Debug.Log(text);
			国家宣言对象.text = text;
		}
	}

	public void 确定建国()
	{
		if (!(国名对象.text == "") && 国名对象.text != null && !(国号对象.text == "") && 国号对象.text != null && !(国都对象.text == "") && 国都对象.text != null && !(国家宣言对象.text == "") && 国家宣言对象.text != null && 国都城池信息 != null)
		{
			int 本机身份 = 全局变量.本机身份;
			if (全局变量.所有玩家数据表[本机身份].财产信息.铜钱 > 10000000.0 && 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("玉玺") > 0.0 && 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("印绶") > 99.0 && 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("虎符") > 9.0 && 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("令牌") > 999.0 && 全局变量.所有玩家数据表[本机身份].背包道具列表.使用道具("玉玺", 0, 0) != "使用失败" && 全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具("虎符", 10, 0, 0) != "使用失败" && 全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具("印绶", 100, 0, 0) != "使用失败" && 全局变量.所有玩家数据表[本机身份].背包道具列表.批量使用道具("令牌", 1000, 0, 0) != "使用失败")
			{
				全局变量.所有玩家数据表[本机身份].财产信息.铜钱 = 全局变量.所有玩家数据表[本机身份].财产信息.铜钱 - 10000000.0;
				国家信息库类 国家信息库类 = new 国家信息库类();
				国家信息库类.初始化国家(国名对象.text, 国号对象.text, 国都城池信息.坐标x, 国都城池信息.坐标y);
				国都城池信息.规模 = 4;
				全局变量.所有国家列表.Add(国家信息库类);
				全局变量.所有玩家数据表[本机身份].加入指定国家(国号对象.text);
				国都城池信息.更换指定城主(全局变量.所有玩家数据表[本机身份].基础信息.名字);
				国家信息库类.国王 = 全局变量.所有玩家数据表[本机身份].基础信息.ID;
				全局变量.所有玩家数据表[本机身份].基础信息.官阶 = "国王";
				全局变量.提示类.显示信息("建国成功!");
				base.gameObject.SetActive(value: false);
				国家列表布局.SetActive(value: false);
			}
			else
			{
				全局变量.提示类.显示信息("建国失败,材料不足!");
			}
		}
	}
}
