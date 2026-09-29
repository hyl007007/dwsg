using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 显示背包物品 : MonoBehaviour
{
	public List<道具信息> 要显示的物品列表;

	public List<将领装备> 要显示的装备列表;

	public GameObject 物品列表对象;

	public GameObject 切换布局对象;

	public GameObject 道具装备切换对象;

	public GameObject 物品详情布局对象;

	public Text 页数显示;

	public Text 容量显示;

	private int 显示第几页 = 1;

	private float 总页数;

	private int 显示类型;

	public Text 已选择道具名字;

	public Text 已选中道具;

	public 调整数量脚本 调整数量脚本对象;

	private bool 重置列表 = true;

	public void 切换道具()
	{
		if (道具装备切换对象.transform.GetChild(0).GetComponent<Toggle>().isOn)
		{
			UnityEngine.Debug.Log("道具");
			切换布局对象.transform.GetChild(0).gameObject.SetActive(value: true);
			切换布局对象.transform.GetChild(1).gameObject.SetActive(value: false);
			切换布局对象.transform.GetChild(0).GetChild(0).GetComponent<Toggle>()
				.isOn = true;
			Toggle component = 切换布局对象.transform.GetChild(0).GetChild(0).GetComponent<Toggle>();
			if (component.isOn)
			{
				切换宝物();
			}
			else
			{
				component.isOn = true;
			}
		}
	}

	public void 切换装备()
	{
		if (道具装备切换对象.transform.GetChild(1).GetComponent<Toggle>().isOn)
		{


			UnityEngine.Debug.Log("装备");
			切换布局对象.transform.GetChild(0).gameObject.SetActive(value: false);
			切换布局对象.transform.GetChild(1).gameObject.SetActive(value: true);
			Toggle component = 切换布局对象.transform.GetChild(1).GetChild(0).GetComponent<Toggle>();
			if (component.isOn)
			{
				切换武器();
			}
			else
			{
				component.isOn = true;
			}
		}
	}

	public void 切换宝物()
	{
		if (切换布局对象.transform.GetChild(0).gameObject.activeSelf && 切换布局对象.transform.GetChild(0).GetChild(0).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.宝物道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换加速()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(1).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.加速道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换生产()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(2).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.生产道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换宝箱()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(3).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.宝箱道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换强化()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(4).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.强化道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换任务()
	{
		if (切换布局对象.transform.GetChild(0).GetChild(5).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.任务道具列表;
			显示类型 = 1;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换武器()
	{
		装备排序("武器");

        if (切换布局对象.transform.GetChild(1).gameObject.activeSelf && 切换布局对象.transform.GetChild(1).GetChild(0).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
            要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.武器装备列表;
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换头盔()
	{
		if (切换布局对象.transform.GetChild(1).GetChild(1).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.头盔装备列表;
			
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换铠甲()
	{
		if (切换布局对象.transform.GetChild(1).GetChild(2).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.铠甲装备列表;
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换坐骑()
	{
		if (切换布局对象.transform.GetChild(1).GetChild(3).GetComponent<Toggle>()
			.isOn)
		{
			int 本机身份 = 全局变量.本机身份;
			要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.坐骑装备列表;
			显示类型 = 2;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 刷新显示()
	{
		物品详情布局对象.SetActive(value: false);
		int childCount = 物品列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			物品列表对象.transform.GetChild(i).GetChild(1).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(1).GetChild(0)
				.gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(5).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(6).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(7).gameObject.SetActive(value: false);
			物品列表对象.transform.GetChild(i).GetChild(8).gameObject.SetActive(value: false);
			Transform child = 物品列表对象.transform.GetChild(i).GetChild(9);
			child.gameObject.SetActive(value: false);
			显示物品详情 component = child.GetComponent<显示物品详情>();
			component.要显示的物品 = null;
			component.要显示的装备 = null;
			child.GetComponent<Toggle>().isOn = false;
		}
		int num = (显示第几页 - 1) * 18;
		int num2 = 0;
		if (显示类型 == 1)
		{
			num2 = 要显示的物品列表.Count;
        }
		else if (显示类型 == 2)
		{
			num2 = 要显示的装备列表.Count;
            要显示的装备列表.Sort((将领装备 a, 将领装备 b) =>
            {
                int tmp = (int)(a.品质) - (int)b.品质;
                if (tmp != 0) return -tmp;
                tmp = (int)a.装备信息.基础值 - (int)b.装备信息.基础值;
                if (tmp != 0) return -tmp;
                return -((int)a.强化等级 - (int)b.强化等级);
            });
        }
		if (num2 > 0)
		{
			物品详情布局对象.SetActive(value: true);
		}
		for (int j = 0; j < 18; j++)
		{
			if (num + j >= num2)
			{
				continue;
			}
			物品列表对象.transform.GetChild(j).GetChild(1).gameObject.SetActive(value: true);
			Image component2 = 物品列表对象.transform.GetChild(j).GetChild(1).GetComponent<Image>();
			物品列表对象.transform.GetChild(j).GetChild(2).gameObject.SetActive(value: true);
			Text component3 = 物品列表对象.transform.GetChild(j).GetChild(2).GetComponent<Text>();
			component3.color = 颜色类.GetColor("#C8C8C8");
			Text component4 = 物品列表对象.transform.GetChild(j).GetChild(3).GetComponent<Text>();
			Transform child2 = 物品列表对象.transform.GetChild(j).GetChild(9);
			显示物品详情 component5 = child2.GetComponent<显示物品详情>();
			component5.选中第几个 = j;
			if (显示类型 == 1)
			{
				道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(要显示的物品列表[num + j].名字);
				component2.sprite = 全局道具库.获取道具头像(道具信息库类.头像);
				component3.text = 要显示的物品列表[num + j].名字;
				物品列表对象.transform.GetChild(j).GetChild(3).gameObject.SetActive(value: true);
				component4.text = (要显示的物品列表[num + j].数量.ToString() ?? "");
				component5.要显示的物品 = 要显示的物品列表[num + j];
				child2.gameObject.SetActive(value: true);
			}
			else if (显示类型 == 2)
			{
				component2.sprite = 要显示的装备列表[num + j].获取装备头像();
				component3.text = 要显示的装备列表[num + j].获取装备名字();
				component3.color = 要显示的装备列表[num + j].获取装备文字颜色();
				物品列表对象.transform.GetChild(j).GetChild(3).gameObject.SetActive(value: false);
				component4.text = "";
				物品列表对象.transform.GetChild(j).GetChild(6).gameObject.SetActive(value: true);
				Image component6 = 物品列表对象.transform.GetChild(j).GetChild(6).GetComponent<Image>();
				double 品质 = 要显示的装备列表[num + j].品质;
				if (品质 > 1.0)
				{
					component6.sprite = 全局变量.装备品质图片资源表[(int)品质];
				}
				if (要显示的装备列表[num + j].将领ID != -1)
				{
					物品列表对象.transform.GetChild(j).GetChild(7).gameObject.SetActive(value: true);
				}
				if (要显示的装备列表[num + j].强化等级 != 0.0)
				{
					物品列表对象.transform.GetChild(j).GetChild(8).gameObject.SetActive(value: true);
					物品列表对象.transform.GetChild(j).GetChild(8).GetComponent<Text>()
						.text = "+" + 要显示的装备列表[num + j].强化等级.ToString();
				}
				if (component3.text.IndexOf("尊") > -1)
				{
					物品列表对象.transform.GetChild(j).GetChild(1).GetChild(0)
						.gameObject.SetActive(value: true);
				}
				component5.要显示的装备 = 要显示的装备列表[num + j];
				child2.gameObject.SetActive(value: true);
			}
			if (j == 0)
			{
				物品列表对象.transform.GetChild(j).GetChild(9).GetComponent<Toggle>()
					.isOn = true;
			}
		}
		int 本机身份 = 全局变量.本机身份;
		容量显示.text = "背包上限  " + 全局变量.所有玩家数据表[本机身份].获取背包物品数量().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.背包容量上限.ToString();
		总页数 = Mathf.Ceil((float)num2 / 18f);
		页数显示.text = 显示第几页.ToString() + "/" + 总页数.ToString();
	}

	public double 获取选中物品数量()
	{
		if (Dwsg.Economy.MaterialPackClient.Supports(已选择道具名字.text))
		{
			int 选中;
			if (!int.TryParse(已选中道具.text, out 选中)) return 0;
			int 索引 = (显示第几页 - 1) * 18 + 选中;
			return 要显示的物品列表 != null && 索引 >= 0 && 索引 < 要显示的物品列表.Count && 要显示的物品列表[索引].名字 == 已选择道具名字.text
				? 要显示的物品列表[索引].数量 : 0;
		}
		if (已选择道具名字.text != "")
		{
			int num = int.Parse(已选中道具.text);
			int num2 = (显示第几页 - 1) * 18;
			return 要显示的物品列表[num2 + num].数量;
		}
		return 1.0;
	}

	public void 批量使用道具()
	{
		if (已选择道具名字.text != "")
		{
			if (Dwsg.Economy.MaterialPackClient.Supports(已选择道具名字.text))
				调整数量脚本对象.第几个玩家 = 全局变量.本机身份;
			调整数量脚本对象.调整类型 = 3;
			调整数量脚本对象.gameObject.SetActive(value: true);
			调整数量脚本对象.显示说明文本();
		}
	}

	public void 使用道具()
	{
		if (!(已选择道具名字.text != ""))
		{
			return;
		}
		if (Dwsg.Economy.MaterialPackClient.Supports(已选择道具名字.text))
		{
			使用材料包(1);
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		string text = 全局变量.所有玩家数据表[本机身份].背包道具列表.使用道具(已选择道具名字.text, 0, 0);
		if (text != "使用失败")
		{
			全局变量.提示类.显示信息("使用成功:\n" + text);
			int index = int.Parse(已选中道具.text);
			刷新显示();
			if (物品列表对象.transform.GetChild(index).GetChild(9).gameObject.activeSelf)
			{
				物品列表对象.transform.GetChild(index).GetChild(9).GetComponent<Toggle>()
					.isOn = true;
			}
		}
		else
		{
			全局变量.提示类.显示信息("使用失败!");
		}
	}

	public void 使用材料包(int 数量, System.Action<Dwsg.Shared.GameResult> 完成 = null)
	{
		if (Dwsg.Economy.MaterialPackClient.Pending)
		{
			if (全局变量.提示类 != null) 全局变量.提示类.显示信息("正在使用宝箱，请稍候");
			return;
		}
		int 选中;
		if (!int.TryParse(已选中道具.text, out 选中)) 选中 = 0;
		Dwsg.Economy.MaterialPackClient.Use(已选择道具名字.text, 数量, result =>
		{
			if (this == null) return;
			if (全局变量.提示类 != null) 全局变量.提示类.显示信息(result.Code == Dwsg.Shared.GameCodes.Ok ? "使用成功:\n" + result.Message : result.Message);
			if (result.Code == Dwsg.Shared.GameCodes.Ok)
			{
				刷新显示();
				if (选中 >= 0 && 选中 < 物品列表对象.transform.childCount && 物品列表对象.transform.GetChild(选中).GetChild(9).gameObject.activeSelf)
					物品列表对象.transform.GetChild(选中).GetChild(9).GetComponent<Toggle>().isOn = true;
			}
			完成?.Invoke(result);
		});
	}

	public void 丢弃道具()
	{
		if (!(已选择道具名字.text != ""))
		{
			return;
		}
		int num = int.Parse(已选中道具.text);
		int 本机身份 = 全局变量.本机身份;
		if (显示类型 == 1)
		{
			if (全局变量.所有玩家数据表[本机身份].背包道具列表.删除道具(已选择道具名字.text))
			{
				全局变量.提示类.显示信息("丢弃" + 已选择道具名字.text + "成功!");
				刷新显示();
			}
			else
			{
				全局变量.提示类.显示信息("丢弃失败!");
			}
		}
		else if (显示类型 == 2)
		{
			int num2 = (显示第几页 - 1) * 18 + num;
			if (全局变量.所有玩家数据表[本机身份].背包装备列表.删除装备(要显示的装备列表[num2], num2))
			{
				全局变量.提示类.显示信息("丢弃" + 已选择道具名字.text + "成功!");
				刷新显示();
			}
			else
			{
				全局变量.提示类.显示信息("丢弃失败!");
			}
		}
		if (物品列表对象.transform.GetChild(num).GetChild(9).gameObject.activeSelf)
		{
			物品列表对象.transform.GetChild(num).GetChild(9).GetComponent<Toggle>()
				.isOn = true;
		}
	}

	private void 获取选中物品()
	{
		int childCount = 切换布局对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			if (!切换布局对象.transform.GetChild(i).gameObject.activeSelf)
			{
				continue;
			}
			int childCount2 = 切换布局对象.transform.GetChild(i).childCount;
			for (int j = 0; j < childCount2; j++)
			{
				if (!切换布局对象.transform.GetChild(i).GetChild(j).gameObject.activeSelf)
				{
					continue;
				}
				int childCount3 = 物品列表对象.transform.GetChild(i).childCount;
				for (int k = 0; k < childCount2; k++)
				{
					int 本机身份 = 全局变量.本机身份;
					switch (i)
					{
						case 0:
							显示类型 = 1;
							switch (j)
							{
								case 0:
									要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.宝物道具列表;
									return;
								case 1:
									要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.加速道具列表;
									return;
								case 2:
									要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.生产道具列表;
									return;
								case 3:
									要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.宝箱道具列表;
									return;
								case 4:
									要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.强化道具列表;
									return;
								case 5:
									要显示的物品列表 = 全局变量.所有玩家数据表[本机身份].背包道具列表.任务道具列表;
									return;
							}
							break;
						case 1:
							显示类型 = 2;
							switch (j)
							{
								case 0:
									要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.武器装备列表;
									return;
								case 1:
									要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.头盔装备列表;
									return;
								case 2:
									要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.铠甲装备列表;
									return;
								case 3:
									要显示的装备列表 = 全局变量.所有玩家数据表[本机身份].背包装备列表.坐骑装备列表;
									return;
							}
							break;
					}
				}
			}
		}
	}

	public void 左翻页()
	{
		if (显示第几页 != 1)
		{
			显示第几页--;
			刷新显示();
		}
	}

	public void 右翻页()
	{
		if ((float)显示第几页 < 总页数)
		{
			显示第几页++;
			刷新显示();
		}
	}

	private void 装备排序(string 装备类型)
	{
		
		if (装备类型 == "武器")
		{
			for (int i = 0; i < 全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表.Count; i++)
			{
				for (int j = 0; j < 全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表.Count-1-i; j++)
				{
					if (全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表[i].品质< 全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表[j].品质)
					{
						将领装备 temp = 全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表[i];
						全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表[i] = 全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表[j];
						全局变量.所有玩家数据表[全局变量.本机身份].背包装备列表.武器装备列表[j] = temp;
                    }
				}
			}
		}
			
	}
}
