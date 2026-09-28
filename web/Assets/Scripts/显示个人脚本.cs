using UnityEngine;
using UnityEngine.UI;

public class 显示个人脚本 : MonoBehaviour
{
	public Text 战功显示;

	public Text 贡献显示;

	public Text 官职显示;

	public Text 轮选剩余时间;

	private long 刷新计时 = TIME.getTime();

	public void 刷新显示()
	{
		if (TIME.getTime() - 刷新计时 >= 1)
		{
			int 本机身份 = 全局变量.本机身份;
			国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[本机身份].基础信息.国家);
			if (国家信息库类 != null)
			{
				战功显示.text = (全局变量.所有玩家数据表[本机身份].基础信息.战功.ToString() ?? "");
				贡献显示.text = (全局变量.所有玩家数据表[本机身份].基础信息.贡献.ToString() ?? "");
				官职显示.text = (全局变量.所有玩家数据表[本机身份].基础信息.官职.ToString() ?? "");
				//long time = 国家信息库类.上次轮选时间 + 国家信息库类.轮选时间间隔 - TIME.getTime();
				轮选剩余时间.text =TIME.ToTimeFormat(全局变量.领取倒计时); ;
			}
			刷新计时 = TIME.getTime();
		}
	}

	private void FixedUpdate()
	{
		刷新显示();
	}
}
