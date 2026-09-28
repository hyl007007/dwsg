using UnityEngine;
using UnityEngine.UI;

public class 调整数量脚本 : MonoBehaviour
{
	public 显示背包物品 显示背包物品脚本对象;

	public 兵营脚本 兵营脚本对象;

	public 封地信息界面UI脚本 封地信息界面UI脚本对象;

	public 市场脚本 市场脚本对象;

	public Text 数量显示对象;

	public Text 输入数量对象;

	public Slider 数量滑条对象;

	public int 调整类型;

	public int 兵种ID;

	public double 兵种数量;

	public int 第几个玩家;

	public int 第几个封地;

	public Text 说明文本;

	private double 调整数量;

	private double 已占人口;

	private double 人口上限;

	private double 剩余人口;

	public void 滑条改变购买数量()
	{
		数量显示对象.text = 数量滑条对象.value.ToString();
		调整数量 = 数量滑条对象.value;
	}

	public void 输入改变购买数量()
	{
		if (输入数量对象.text != null && !(输入数量对象.text == ""))
		{
			float num = float.Parse(输入数量对象.text);
			if (num<=0)
			{
				Application.Quit();
				print("你TMD");
			}
			UnityEngine.Debug.Log("输入数量:" + num.ToString());
			if (num > 数量滑条对象.maxValue)
			{
				num = 数量滑条对象.maxValue;
			}
			调整数量 = num;
			数量显示对象.text = num.ToString();
			数量滑条对象.value = num;
		}
	}

	public void 显示说明文本()
	{
		if (调整类型 == 1)
		{
			已占人口 = 全局变量.所有玩家数据表[第几个玩家].获取已占用人口();
			人口上限 = 全局变量.所有玩家数据表[第几个玩家].获取人口上限();
			剩余人口 = 人口上限 - 已占人口;
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(兵种ID);
			if (兵种属性库类 != null)
			{
				double num = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 / 兵种属性库类.需要铜钱;
				double num2 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 / 兵种属性库类.需要粮食;
				double num3 = 全局变量.所有玩家数据表[第几个玩家].获取指定兵种ID总数(兵种ID);
				double num4 = 剩余人口 / 兵种属性库类.占用人口;
				double num5 = 0.0;
				num5 = ((!(num <= num2)) ? num2 : num);
				num5 = Mathf.Floor((float)num5);
				num4 = Mathf.Floor((float)num4);
				if (num4 < 0.0)
				{
					num4 = 0.0;
				}
				if (num5 < 0.0)
				{
					num5 = 0.0;
				}
				说明文本.text = "【招募数量输入】\r\n当前空闲:" + 兵种属性库类.名称 + " " + num3.ToString() + "\r\n资源可招:" + num5.ToString() + "\r\n人口可招:" + num4.ToString();
				数量滑条对象.maxValue = 0f;
				if (num5 <= num4)
				{
					数量滑条对象.maxValue = (int)num5;
				}
				if (num4 <= num5)
				{
					数量滑条对象.maxValue = (int)num4;
				}
				数量滑条对象.value = 数量滑条对象.maxValue;
			}
		}
		else if (调整类型 != 2)
		{
			if (调整类型 == 3)
			{
				说明文本.text = "【批量使用道具】\r\n调整使用 " + 显示背包物品脚本对象.已选择道具名字.text + " 的数量";
				数量滑条对象.maxValue = (float)显示背包物品脚本对象.获取选中物品数量();
				数量滑条对象.value = 数量滑条对象.maxValue;
			}
			else if (调整类型 == 4)
			{
				说明文本.text = "【治疗伤兵】\r\n治疗数量:" + 兵种数量.ToString();
				数量滑条对象.maxValue = (float)兵种数量;
				数量滑条对象.value = 数量滑条对象.maxValue;
			}
			else if (调整类型 == 5)
			{
				说明文本.text = "【现有资源】\r\n黄金:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.黄金.ToString() + "\r\n白银:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.白银.ToString();
				数量滑条对象.maxValue = (float)(全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 * 市场脚本对象.铜钱单价);
				数量滑条对象.value = 0f;
			}
			else if (调整类型 == 6)
			{
				说明文本.text = "【现有资源】\r\n黄金:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.黄金.ToString() + "\r\n白银:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.白银.ToString();
				数量滑条对象.maxValue = (float)(全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 * 市场脚本对象.粮食单价);
				数量滑条对象.value = 0f;
			}
			else if (调整类型 == 7)
			{
				说明文本.text = "【现有资源】\r\n铜钱:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱.ToString() + "\r\n粮食:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食.ToString();
				数量滑条对象.maxValue = Mathf.Floor((float)(全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 * 2.5));
				数量滑条对象.value = 0f;
			}
			else if (调整类型 == 8)
			{
				说明文本.text = "【现有资源】\r\n铜钱:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱.ToString() + "\r\n粮食:" + 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食.ToString();
				数量滑条对象.maxValue = Mathf.Floor((float)(全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 * 0.3));
				数量滑条对象.value = 0f;
			}
		}
	}

	public void 确认调整()
	{
		if (调整类型 == 1)
		{
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(兵种ID);
			全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 - 兵种属性库类.需要铜钱 * 调整数量;
			全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 - 兵种属性库类.需要粮食 * 调整数量;
			if (全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 < 0.0)
			{
				全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 0.0;
			}
			if (全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 < 0.0)
			{
				全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 0.0;
			}
			全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].添加闲兵(兵种ID, 调整数量);
			base.gameObject.SetActive(value: false);
			兵营脚本对象.刷新显示();
		}
		else if (调整类型 == 3)
		{
			if (全局变量.所有玩家数据表[第几个玩家].背包道具列表.批量使用道具(显示背包物品脚本对象.已选择道具名字.text, (int)调整数量, 0, 0) != "使用失败")
			{
				全局变量.提示类.显示信息("批量使用成功!");
			}
			else
			{
				全局变量.提示类.显示信息("批量使用失败!");
			}
			base.gameObject.SetActive(value: false);
			显示背包物品脚本对象.刷新显示();
			int index = int.Parse(显示背包物品脚本对象.已选中道具.text);
			if (显示背包物品脚本对象.物品列表对象.transform.GetChild(index).GetChild(9).gameObject.activeSelf)
			{
				显示背包物品脚本对象.物品列表对象.transform.GetChild(index).GetChild(9).GetComponent<Toggle>()
					.isOn = true;
				}
			}
			else if (调整类型 == 4)
			{
				兵种属性库类 兵种属性库类2 = 全局兵种库.查询指定ID的数据(兵种ID);
				if (兵种属性库类2 != null)
				{
					double num = 调整数量 * (兵种属性库类2.需要铜钱 / 2.0);
					double num2 = 调整数量 * (兵种属性库类2.需要粮食 / 2.0);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 >= num && 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 >= num2)
					{
						全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 - num;
						全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 - num2;
						全局变量.提示类.显示信息("治疗成功!\n花费铜钱:" + num.ToString() + "\n花费粮食:" + num2.ToString());
						全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].添加闲兵(兵种ID, 调整数量);
						全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].删除伤兵(兵种ID, 调整数量);
						封地信息界面UI脚本对象.显示伤兵列表();
						base.gameObject.SetActive(value: false);
					}
					else
					{
						全局变量.提示类.显示信息("治疗失败!\n需要铜钱:" + num.ToString() + "\n需要粮食:" + num2.ToString());
					}
				}
			}
			else if (调整类型 == 5)
			{
				全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 + 调整数量;
				全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 = 全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 - (double)Mathf.Floor((float)(调整数量 / 市场脚本对象.铜钱单价));
				base.gameObject.SetActive(value: false);
				市场脚本对象.刷新显示();
			}
			else if (调整类型 == 6)
			{
				全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 + 调整数量;
				全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 = 全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 - (double)Mathf.Floor((float)(调整数量 / 市场脚本对象.粮食单价));
				base.gameObject.SetActive(value: false);
				市场脚本对象.刷新显示();
			}
			else if (调整类型 == 7)
			{
				全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 + 调整数量;
				全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 - (double)Mathf.Floor((float)(调整数量 / 2.5));
				base.gameObject.SetActive(value: false);
				市场脚本对象.刷新显示();
			}
			else if (调整类型 == 8)
			{
				全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 = 全局变量.所有玩家数据表[第几个玩家].财产信息.铜钱 + 调整数量;
				全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 = 全局变量.所有玩家数据表[第几个玩家].财产信息.粮食 - (double)Mathf.Floor((float)(调整数量 / 0.3));
				base.gameObject.SetActive(value: false);
				市场脚本对象.刷新显示();
			}
		}

		public void 调整最大数量()
		{
			数量显示对象.text = 数量滑条对象.maxValue.ToString();
			调整数量 = 数量滑条对象.maxValue;
			数量滑条对象.value = 数量滑条对象.maxValue;
		}

		public void 显示调整界面()
		{
			base.gameObject.SetActive(value: true);
		}
	}
