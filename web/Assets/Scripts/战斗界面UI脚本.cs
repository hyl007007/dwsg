using UnityEngine;
using UnityEngine.UI;

public class 战斗界面UI脚本 : MonoBehaviour
{
	public GameObject 战斗地图对象;

	public Text 攻方兵力文本;

	public Text 守方兵力文本;

	public Text 战斗时间;

    public 战斗系统 战斗系统脚本对象;

	public GameObject 地图按钮对象;

	public GameObject 封地按钮对象;

	public bool 开始显示兵力;

	private float 战斗计时开始时间;

	private bool 上次显示兵力;

	public void 返回主界面()
	{
		全局变量.战斗地图相机.transform.SetParent(全局变量.战斗地图相机.transform.parent.parent.parent);
		全局变量.大地图相机.SetActive(value: true);
		全局变量.战斗地图相机.SetActive(value: false);
		全局变量.主界面UI对象.SetActive(value: true);
		全局变量.大地图布局对象.SetActive(value: true);
		战斗系统脚本对象.正在观战 = false;
		开始显示兵力 = false;
		base.gameObject.SetActive(value: false);
		地图按钮对象.SetActive(value: false);
		封地按钮对象.SetActive(value: true);
		//返回世界后恢复大地图背景音乐（进战场时被 Stop 掉了）
		主界面UI脚本 世界界面 = 全局变量.主界面UI对象.GetComponent<主界面UI脚本>();
		if (世界界面 != null)
		{
			世界界面.背景音乐对象.clip = 世界界面.大地图背景音乐;
			世界界面.背景音乐对象.Play();
		}
		UnityEngine.Debug.Log("返回");
	}

	public void 获取脚本对象()
	{
		战斗系统脚本对象 = 战斗地图对象.transform.GetChild(0).GetChild(0).GetChild(0)
			.GetChild(0)
			.GetChild(0)
			.GetComponent<战斗系统>();
	}

	public void 全军撤退()
	{
		
		if (战斗系统脚本对象.攻身份!=全局变量.本机身份)
            全局变量.提示类.显示信息("不要走 决战到天亮");
        else
        {
            if (战斗系统脚本对象.服务器战场)
                Dwsg.Combat.CombatClient.Withdraw(战斗系统脚本对象.服务器战场ID);
            else 战斗系统脚本对象.全军撤退 = true;
        }
	}

	public void 显示兵力()
	{
		开始显示兵力 = true;
	}

	private void FixedUpdate()
	{
		if (开始显示兵力)
		{
			//战斗计时改用可缩放的 Time.time：随倍速一起加速；false->true 边沿时复位
			if (!上次显示兵力)
			{
				战斗计时开始时间 = Time.time;
				上次显示兵力 = true;
			}
			long time = (long)Mathf.Floor(Time.time - 战斗计时开始时间);
            if (战斗系统脚本对象.服务器战场 && Dwsg.Network.GameNetwork.CurrentSnapshot != null)
                time = System.Math.Max(0, Dwsg.Network.GameNetwork.CurrentSnapshot.ServerUtcMs / 1000 - 战斗系统脚本对象.创建时间);
            战斗时间.text = TIME.ToTimeFormat(time);
			攻方兵力文本.text = 战斗系统脚本对象.攻方兵力.ToString();
			守方兵力文本.text = 战斗系统脚本对象.守方兵力.ToString();
		}
		else
		{
			上次显示兵力 = false;
		}
	}
	
}
