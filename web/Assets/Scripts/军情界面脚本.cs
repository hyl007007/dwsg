using UnityEngine;
using UnityEngine.UI;

public class 军情界面脚本 : MonoBehaviour
{
	public GameObject 战斗地图列表;

	public GameObject 军情列表;

	public Text 页数显示;

	private int 第几页军情;

	private float 总页数;

	private long time1 = TIME.getTime();

	public AudioSource 背景音乐对象;

	public AudioClip 山贼背景音乐;

	public AudioClip 城池背景音乐;

	public void 列表左翻页()
	{
		if (第几页军情 != 0)
		{
			第几页军情--;
			time1 = TIME.getTime() - 2;
		}
	}

	public void 列表右翻页()
	{
		if ((float)第几页军情 < 总页数 - 1f)
		{
			第几页军情++;
			time1 = TIME.getTime() - 2;
		}
	}

	public void 显示军情列表()
	{
		int count = 全局变量.军情列表.Count;
		总页数 = Mathf.Ceil((float)count / 5f);
		int num = 0;
		int num2 = 0;
		num2 = 第几页军情 * 5;
		页数显示.text = (第几页军情 + 1).ToString() + "/" + 总页数.ToString();
		for (int i = 0; i < 5; i++)
		{
			num = num2 + i;
			军情列表.transform.GetChild(i).gameObject.SetActive(value: false);
			if (num >= count)
			{
				continue;
			}
			军情列表.transform.GetChild(i).gameObject.SetActive(value: true);
			Text component = 军情列表.transform.GetChild(i).GetChild(1).GetComponent<Text>();
			if (全局变量.军情列表[num].战场类型 == 0)
			{
				山贼属性信息 山贼属性信息 = 附近山贼.获取指定坐标的山贼(全局变量.军情列表[num].坐标x, 全局变量.军情列表[num].坐标y);
				if (山贼属性信息 != null)
				{
					component.text = "【消灭】" + 全局变量.军情列表[num].队列将领列表[0].将领属性.初始属性.名字 + "消灭" + 山贼属性信息.等级.ToString() + "级山贼(" + 全局变量.军情列表[num].坐标x.ToString() + "," + 全局变量.军情列表[num].坐标y.ToString() + ")";
				}
			}
			else if (全局变量.军情列表[num].战场类型 == 1)
			{
				城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(全局变量.军情列表[num].坐标x, 全局变量.军情列表[num].坐标y);
				component.text = "【攻占】" + 全局变量.军情列表[num].队列将领列表[0].将领属性.初始属性.名字 + "攻占" + 城池信息库类.名称 + "(" + 全局变量.军情列表[num].坐标x.ToString() + "," + 全局变量.军情列表[num].坐标y.ToString() + ")";
			}
			Text component2 = 军情列表.transform.GetChild(i).GetChild(2).GetComponent<Text>();
			Text component3 = 军情列表.transform.GetChild(i).GetChild(3).GetComponent<Text>();
			军情列表.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: false);
			军情列表.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
			long time = TIME.getTime();
			if (全局变量.军情列表[num].到达时间 <= time)
			{
				component2.text = "状态:战斗中";
				军情列表.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: true);
				continue;
			}
			component2.text = "状态:行军中";
			军情列表.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: true);
			long time2 = 全局变量.军情列表[num].到达时间 - time;
			component3.text = "剩余时间:" + TIME.ToTimeFormat(time2);
		}
	}

	public void 进入战场()
	{
		int num = 0;
		int num2 = 0;
		num2 = 第几页军情 * 5;
		for (int i = 0; i < 5; i++)
		{
			if (军情列表.transform.GetChild(i).gameObject.activeSelf && 军情列表.transform.GetChild(i).GetChild(5).gameObject.activeSelf)
			{
				num = num2 + i;
				int num3 = 0;
				foreach (Transform item in 战斗地图列表.transform)
				{
					战斗系统 component = item.GetChild(0).GetChild(0).GetChild(0)
						.GetChild(0)
						.GetChild(0)
						.GetComponent<战斗系统>();
					if (全局变量.军情列表[num].坐标x == component.坐标x && 全局变量.军情列表[num].坐标y == component.坐标y && 全局变量.军情列表[num].战场类型 == component.战场类型)
					{
						全局变量.战斗地图相机.transform.SetParent(item.transform);
						全局变量.战斗地图相机.transform.localPosition = new Vector3(0f, 0f, -10f);
						战斗界面UI脚本 component2 = 全局变量.战斗界面UI对象.transform.GetComponent<战斗界面UI脚本>();
						component2.战斗地图对象 = item.gameObject;
						component2.获取脚本对象();
						component2.开始显示兵力 = true;
						全局变量.主相机.SetActive(value: false);
						全局变量.战斗地图相机.SetActive(value: true);
						全局变量.主界面UI对象.SetActive(value: false);
						全局变量.战斗界面UI对象.SetActive(value: true);
						全局变量.大地图布局对象.SetActive(value: false);
						全局变量.封地布局对象.SetActive(value: false);
						component.正在观战 = true;
						//停掉主界面大地图/封地的实际背景音，否则它会一直盖住战场音乐
						主界面UI脚本 主界面 = 全局变量.主界面UI对象.GetComponent<主界面UI脚本>();
						if (主界面 != null)
						{
							主界面.背景音乐对象.Stop();
						}
						//战场音乐：放到战斗界面上一定激活的音频源播放，避免挂在被隐藏的布局里放不出声
						AudioSource 战场音乐 = 全局变量.战斗界面UI对象.GetComponent<AudioSource>();
						if (战场音乐 == null)
						{
							战场音乐 = 全局变量.战斗界面UI对象.AddComponent<AudioSource>();
						}
						战场音乐.loop = true;
						if (component.战场类型 == 1)
						{
							战场音乐.clip = 城池背景音乐;
						}
						else
						{
							战场音乐.clip = 山贼背景音乐;
						}
						战场音乐.Play();
					}
					num3++;
				}
			}
		}
	}

	private void FixedUpdate()
	{
		if (TIME.getTime() - time1 >= 1)
		{
			显示军情列表();
			time1 = TIME.getTime();
		}
	}
}
