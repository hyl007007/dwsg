using UnityEngine;
using UnityEngine.UI;

public class 显示动态脚本 : MonoBehaviour
{
	public Text 将领显示;

	public Text 俘虏显示;

	public Text 兵力显示;

	public Text 伤兵显示;

	public void 刷新显示()
	{
		int 本机身份 = 全局变量.本机身份;
		将领显示.text = 全局变量.所有玩家数据表[本机身份].获取将领总数().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限.ToString();
		俘虏显示.text = 全局变量.所有玩家数据表[本机身份].获取俘虏总数().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限.ToString();
		兵力显示.text = 全局变量.所有玩家数据表[本机身份].获取兵力总数().ToString();
		伤兵显示.text = 全局变量.所有玩家数据表[本机身份].获取伤兵总数().ToString();
	}
}
