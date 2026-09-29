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

	private bool 正在服务器招兵;

	private Newtonsoft.Json.Linq.JObject 市场报价;

	private int 市场显示次数;

	private int 材料包显示次数;

	public void 滑条改变购买数量()
	{
		数量显示对象.text = 数量滑条对象.value.ToString();
		调整数量 = 数量滑条对象.value;
	}

	public void 输入改变购买数量()
	{
		if (调整类型 == 1 && Dwsg.Network.GameNetwork.Enabled)
		{
			double 数量;
			if (!double.TryParse(输入数量对象.text, out 数量) || double.IsNaN(数量) || double.IsInfinity(数量) || 数量 <= 0 || 数量 > int.MaxValue || 数量 != System.Math.Truncate(数量))
			{
				数量滑条对象.value = 0f;
				调整数量 = 0;
				数量显示对象.text = "0";
				if (全局变量.提示类 != null) 全局变量.提示类.显示信息("请输入有效的整数招兵数量");
				return;
			}
			调整数量 = System.Math.Min(数量, 数量滑条对象.maxValue);
			数量显示对象.text = 调整数量.ToString(); 数量滑条对象.value = (float)调整数量;
			return;
		}
		if (调整类型 == 3 && Dwsg.Network.GameNetwork.Enabled && 显示背包物品脚本对象 != null && 显示背包物品脚本对象.已选择道具名字.text == Dwsg.Shared.Generals.GeneralExperienceBookRules.ItemName)
		{
			double 数量;
			if (!double.TryParse(输入数量对象.text, out 数量) || double.IsNaN(数量) || double.IsInfinity(数量) || 数量 <= 0 || 数量 > int.MaxValue || 数量 != System.Math.Truncate(数量))
			{
				if (全局变量.提示类 != null) 全局变量.提示类.显示信息("请输入有效的整数道具数量");
				return;
			}
			调整数量 = System.Math.Min(数量, 数量滑条对象.maxValue);
			数量显示对象.text = 调整数量.ToString(); 数量滑条对象.value = (float)调整数量;
			return;
		}
		if (调整类型 == 3 && 显示背包物品脚本对象 != null && Dwsg.Economy.MaterialPackClient.Supports(显示背包物品脚本对象.已选择道具名字.text))
		{
			float 数量;
			if (!float.TryParse(输入数量对象.text, out 数量) || float.IsNaN(数量) || float.IsInfinity(数量) || 数量 <= 0)
			{
				if (全局变量.提示类 != null) 全局变量.提示类.显示信息("请输入有效的道具数量");
				return;
			}
			数量 = Mathf.Min(数量, 数量滑条对象.maxValue);
			调整数量 = 数量;
			数量显示对象.text = 数量.ToString();
			数量滑条对象.value = 数量;
			return;
		}
		if (调整类型 >= 5 && 调整类型 <= 8)
		{
			float 数量;
			if (!float.TryParse(输入数量对象.text, out 数量) || float.IsNaN(数量) || float.IsInfinity(数量) || 数量 <= 0)
			{
				if (全局变量.提示类 != null) 全局变量.提示类.显示信息("请输入有效的兑换数量");
				return;
			}
			数量 = Mathf.Min(数量, 数量滑条对象.maxValue);
			调整数量 = 数量;
			数量显示对象.text = 数量.ToString();
			数量滑条对象.value = 数量;
			return;
		}
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
		材料包显示次数++;
		if (调整类型 >= 5 && 调整类型 <= 8)
		{
			市场显示次数++;
			刷新市场数量(true);
			return;
		}
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
		}
	}

	public void 确认调整()
	{
		if (调整类型 == 3 && Dwsg.Network.GameNetwork.Enabled && 显示背包物品脚本对象.已选择道具名字.text == Dwsg.Shared.Generals.GeneralExperienceBookRules.ItemName)
		{
			int 本次经验书显示 = 材料包显示次数;
			显示背包物品脚本对象.使用经验书(调整数量, result =>
			{
				if (this == null || 调整类型 != 3 || 本次经验书显示 != 材料包显示次数) return;
				if (result.Code == Dwsg.Shared.GameCodes.Ok) base.gameObject.SetActive(value: false);
				else 说明文本.text = "【批量使用道具】\r\n" + result.Message;
			});
			return;
		}
		if (调整类型 == 3 && 显示背包物品脚本对象 != null && Dwsg.Economy.MaterialPackClient.Supports(显示背包物品脚本对象.已选择道具名字.text))
		{
			if (double.IsNaN(调整数量) || double.IsInfinity(调整数量) || 调整数量 < 1 || 调整数量 > int.MaxValue)
			{
				if (全局变量.提示类 != null) 全局变量.提示类.显示信息("请选择有效的道具数量");
				return;
			}
			int 本次材料包显示 = 材料包显示次数;
			显示背包物品脚本对象.使用材料包((int)调整数量, result =>
			{
				if (this == null || 调整类型 != 3 || 本次材料包显示 != 材料包显示次数) return;
				if (result.Code == Dwsg.Shared.GameCodes.Ok) base.gameObject.SetActive(value: false);
				else
				{
					说明文本.text = "【批量使用道具】\r\n" + result.Message;
					数量滑条对象.maxValue = (float)显示背包物品脚本对象.获取选中物品数量();
					数量滑条对象.value = Mathf.Clamp((float)调整数量, 0f, 数量滑条对象.maxValue);
					调整数量 = 数量滑条对象.value;
					数量显示对象.text = 数量滑条对象.value.ToString();
				}
			});
			return;
		}
		if (调整类型 >= 5 && 调整类型 <= 8)
		{
			if (Dwsg.Economy.MarketClient.Pending)
			{
				if (全局变量.提示类 != null) 全局变量.提示类.显示信息("正在兑换，请稍候");
				return;
			}
			int 本次显示 = 市场显示次数, 本次类型 = 调整类型;
			说明文本.text += "\r\n正在兑换，请稍候";
			Dwsg.Economy.MarketClient.Exchange(本次类型, 调整数量, 市场报价, result =>
			{
				if (this == null) return;
				市场脚本对象.刷新显示();
				if (本次显示 != 市场显示次数 || 本次类型 != 调整类型) return;
				if (result.Code == Dwsg.Shared.GameCodes.Ok) base.gameObject.SetActive(value: false);
				else
				{
					刷新市场数量(false, result.Message);
					if (全局变量.提示类 != null) 全局变量.提示类.显示信息(result.Message);
				}
			});
			return;
		}
		if (调整类型 == 1)
		{
			if (Dwsg.Network.GameNetwork.Enabled)
			{
				if (正在服务器招兵) return;
				int 操作身份 = 全局变量.本机身份, 本次招兵显示 = 材料包显示次数, 招兵封地 = 第几个封地, 招兵兵种 = 兵种ID;
				if (第几个玩家 != 操作身份 || 操作身份 < 0 || 操作身份 >= 全局变量.所有玩家数据表.Count)
				{ 全局变量.提示类.显示信息("只能招募本人封地的兵士"); return; }
				正在服务器招兵 = true;
				ProductionClient.Recruit(全局变量.所有玩家数据表[操作身份], 招兵封地, 兵营脚本对象.第几个建筑, 招兵兵种, 调整数量, 结果 =>
				{
					if (this == null) return;
					正在服务器招兵 = false;
					if (全局变量.本机身份 != 操作身份 || 第几个玩家 != 操作身份 || 调整类型 != 1 || 本次招兵显示 != 材料包显示次数 || 第几个封地 != 招兵封地 || 兵种ID != 招兵兵种) return;
					全局变量.提示类.显示信息(结果?.Message ?? "服务器未确认招兵，请重试");
					if (结果 != null && 结果.Code == Dwsg.Shared.GameCodes.Ok)
					{ gameObject.SetActive(false); 兵营脚本对象.刷新显示(); }
				});
				return;
			}
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
				Dwsg.Generals.TroopTreatmentClientAdapter.Heal(全局变量.本机身份, 第几个封地, 兵种ID, 调整数量, () =>
				{
					封地信息界面UI脚本对象.显示伤兵列表();
					base.gameObject.SetActive(value: false);
				});
			}
		}

		private void 刷新市场数量(bool 重置, string 信息 = null)
		{
			第几个玩家 = 全局变量.本机身份;
			市场报价 = Dwsg.Economy.MarketClient.GetQuote();
			var 财产 = 全局变量.所有玩家数据表[第几个玩家].财产信息;
			说明文本.text = 调整类型 == 5 || 调整类型 == 6
				? "【现有资源】\r\n黄金:" + 财产.黄金 + "\r\n白银:" + 财产.白银
				: "【现有资源】\r\n铜钱:" + 财产.铜钱 + "\r\n粮食:" + 财产.粮食;
			if (市场报价 == null) 说明文本.text += "\r\n市场报价尚未就绪";
			if (!string.IsNullOrEmpty(信息)) 说明文本.text += "\r\n" + 信息;
			float 原数量 = (float)调整数量;
			数量滑条对象.maxValue = (float)Dwsg.Economy.MarketClient.Maximum(市场报价, 调整类型);
			数量滑条对象.value = 重置 ? 0f : Mathf.Clamp(原数量, 0f, 数量滑条对象.maxValue);
			调整数量 = 数量滑条对象.value;
			数量显示对象.text = 数量滑条对象.value.ToString();
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
