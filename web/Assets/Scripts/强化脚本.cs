using Dwsg.Generals;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 强化脚本 : MonoBehaviour
{
	public Image 装备头像;

	public Text 装备名字属性品质显示;

	public Text 需要材料;

	public Text 强化效果;

	public Text 强化成功率;

	private double 材料数量;

	private string 材料名字 = "";

	public 将领装备 装备对象;

	private void Start()
	{
	}

	public void 显示指定装备()
	{
		装备头像.sprite = 装备对象.获取装备头像();
		装备名字属性品质显示.text = 装备对象.获取装备名字() + "+" + 装备对象.强化等级.ToString() + "(" + 装备对象.获取装备等级().ToString() + "级)\n" + 装备对象.获取装备加成文本() + "\n" + 装备对象.获取装备品质文本();
		int 本机身份 = 全局变量.本机身份;
		材料名字 = 装备对象.获取装备强化材料名字();
		材料数量 = 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量(材料名字);
		需要材料.text = 装备对象.获取装备强化材料名字() + " " + 材料数量.ToString() + "/" + (装备对象.强化等级 + 1.0).ToString();
		强化效果.text = "强化效果：" + 装备对象.获取装备强化效果文本() + "↑" + 装备对象.获取装备强化加成().ToString();
		强化成功率.text = "强化成功率：" + (装备对象.获取装备强化成功率() / 100.0).ToString() + "%(保底次数：" + 装备对象.已强化次数.ToString() + "/" + 装备对象.获取装备强化保底次数().ToString() + ")";
	}

	public void 强化装备()
	{
		if (装备对象 == null || 装备对象.装备信息.名称 == "空") return;
		GeneralsClientAdapter.EnhanceEquipment(装备对象, 1, 显示指定装备);
	}

	public void 批量强化()
	{
		if (装备对象 == null || 装备对象.装备信息.名称 == "空") return;
		GeneralsClientAdapter.EnhanceEquipment(装备对象, 10, 显示指定装备);
	}
}
