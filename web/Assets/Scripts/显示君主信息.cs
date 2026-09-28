using UnityEngine;
using UnityEngine.UI;

public class 显示君主信息 : MonoBehaviour
{
	public Image 君主头像;

	public Text 君主名字;

	public Text 君主等级;

	public Text 称号;

	public Text 排名;

	public Text 贡献;

	public Text 官阶;

	public Text 战功;

	public Text 声望;

	public Text 产铜;

	public Text 产粮;

	public Text 将领数量;

	public Text 封地数量;

	public Text 城池数量;

	public Text 资源点数量;

	public RectTransform 声望条显示;

	public void 刷新显示()
	{
		int 本机身份 = 全局变量.本机身份;
		君主名字.text = 全局变量.所有玩家数据表[本机身份].基础信息.名字;
		君主等级.text = 全局变量.所有玩家数据表[本机身份].基础信息.等级.ToString() + "级";
		称号.text = "<" + 全局变量.所有玩家数据表[本机身份].基础信息.称号名 + ">";
		贡献.text = 全局变量.所有玩家数据表[本机身份].基础信息.贡献.ToString();
		战功.text = 全局变量.所有玩家数据表[本机身份].基础信息.战功.ToString();
		产铜.text = 全局变量.所有玩家数据表[本机身份].获取铜钱产量().ToString() + "/小时";
		产粮.text = 全局变量.所有玩家数据表[本机身份].获取粮食产量().ToString() + "/小时";
		将领数量.text = 全局变量.所有玩家数据表[本机身份].获取将领总数().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限.ToString();
		城池数量.text = "0";
		封地数量.text = 全局变量.所有玩家数据表[本机身份].封地信息表.Count.ToString() + "/10";
		官阶.text = 全局变量.所有玩家数据表[本机身份].基础信息.官职.ToString();
		double 声望2 = 全局变量.所有玩家数据表[本机身份].基础信息.声望;
		全局变量.所有玩家数据表[本机身份].基础信息.获取当前等级升级所需经验();
		全局变量.所有玩家数据表[本机身份].基础信息.获取指定等级升级所需经验(全局变量.所有玩家数据表[本机身份].基础信息.等级 - 1f);
		float num = 全局变量.所有玩家数据表[本机身份].基础信息.获取当前等级经验条比例();
		声望条显示.sizeDelta = new Vector2(307.07f * num, 15f);
		声望.text = 全局变量.所有玩家数据表[本机身份].基础信息.声望.ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.获取当前等级升级所需经验().ToString() + "(" + Mathf.Floor(num * 100f).ToString() + "%)";
	}

	public void 增加经验()
	{
		int 本机身份 = 全局变量.本机身份;
		全局变量.所有玩家数据表[本机身份].基础信息.君主获得经验(10000.0);
		刷新显示();
	}
}
