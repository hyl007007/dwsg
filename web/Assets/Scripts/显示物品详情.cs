using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 显示物品详情 : MonoBehaviour
{
	public Text 名字显示;

	public Text 说明显示;

	public GameObject 使用按钮对象;

	public GameObject 丢弃按钮对象;

	public GameObject 切换布局对象;

	public GameObject 物品列表对象;

	public 道具信息 要显示的物品;

	public 将领装备 要显示的装备;

	public Text 选中显示;

	public int 选中第几个;

	public void 显示物品信息()
	{
		选中显示.text = 选中第几个.ToString();
		if (要显示的物品 != null)
		{
			名字显示.text = 要显示的物品.名字;
			名字显示.color = 颜色类.GetColor("#FAFA00");
			道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(要显示的物品.名字);
			说明显示.text = 道具信息库类.说明;
			说明显示.color = 颜色类.GetColor("#FAFA00");
			丢弃按钮对象.SetActive(value: true);
			使用按钮对象.SetActive(value: false);
			if (道具信息库类.分类 == "宝箱" || (Dwsg.Network.GameNetwork.Enabled && 道具信息库类.名字 == Dwsg.Shared.Generals.GeneralExperienceBookRules.ItemName))
			{
				使用按钮对象.SetActive(value: true);
			}
		}
		else if (要显示的装备 != null)
		{
			名字显示.text = 要显示的装备.获取装备名字();
			名字显示.color = 要显示的装备.获取装备文字颜色();
			说明显示.color = 要显示的装备.获取装备文字颜色();
			int 本机身份 = 全局变量.本机身份;
			string str = "";
			if (要显示的装备.将领ID != -1)
			{
				返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[本机身份].获取指定ID标识的将领索引(要显示的装备.将领ID);
				str = "装备将领:" + 全局变量.所有玩家数据表[本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].将领属性.初始属性.名字 + "\n";
			}
			说明显示.text = str + 要显示的装备.获取装属性说明文本();
			使用按钮对象.SetActive(value: false);
			丢弃按钮对象.SetActive(value: true);
		}
		else
		{
			名字显示.text = "";
			说明显示.text = "";
			丢弃按钮对象.SetActive(value: false);
		}
	}
}
