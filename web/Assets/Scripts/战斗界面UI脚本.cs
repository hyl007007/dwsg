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

    private Transform 相机原父级;
    private bool 已记录相机父级;

    private void Awake()
    {
        foreach (Text 文字 in new[] { 攻方兵力文本, 守方兵力文本 })
        {
            if (文字 == null) continue;
            文字.horizontalOverflow = HorizontalWrapMode.Overflow;
            var 布局 = 文字.GetComponent<ContentSizeFitter>();
            if (布局 == null) 布局 = 文字.gameObject.AddComponent<ContentSizeFitter>();
            布局.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            布局.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if ((文字.alignment == TextAnchor.MiddleCenter || 文字.alignment == TextAnchor.MiddleLeft ||
                文字.alignment == TextAnchor.MiddleRight) && 文字.GetComponent<按钮字形垂直居中>() == null)
                文字.gameObject.AddComponent<按钮字形垂直居中>();
        }
    }

    public void 准备战场()
    {
        if (已记录相机父级 || 全局变量.战斗地图相机 == null) return;
        相机原父级 = 全局变量.战斗地图相机.transform.parent;
        已记录相机父级 = true;
    }

	public void 返回主界面()
	{
		if (全局变量.战斗地图相机 != null)
        {
            if (已记录相机父级) 全局变量.战斗地图相机.transform.SetParent(相机原父级);
            全局变量.战斗地图相机.SetActive(false);
        }
		已记录相机父级 = false;
		var 战场音乐 = GetComponent<AudioSource>();
		if (战场音乐 != null) 战场音乐.Stop();
		全局变量.大地图相机.SetActive(value: true);
		全局变量.主界面UI对象.SetActive(value: true);
		全局变量.大地图布局对象.SetActive(value: true);
		if (战斗系统脚本对象 != null) 战斗系统脚本对象.正在观战 = false;
		开始显示兵力 = false;
		上次显示兵力 = false;
		base.gameObject.SetActive(value: false);
		地图按钮对象.SetActive(value: false);
		封地按钮对象.SetActive(value: true);
		//返回世界后恢复大地图背景音乐（进战场时被 Stop 掉了）
		主界面UI脚本 世界界面 = 全局变量.主界面UI对象.GetComponent<主界面UI脚本>();
		if (世界界面 != null && 世界界面.背景音乐对象 != null)
		{
			世界界面.背景音乐对象.clip = 世界界面.大地图背景音乐;
			世界界面.背景音乐对象.Play();
		}
	}

	public void 获取脚本对象()
	{
		战斗系统脚本对象 = 战斗地图对象 != null ? 战斗地图对象.GetComponentInChildren<战斗系统>(true) : null;
	}

	public void 全军撤退()
	{
		if (战斗系统脚本对象 == null)
            全局变量.提示类.显示信息("战场已结束。");
        else if (战斗系统脚本对象.攻身份 != 全局变量.本机身份)
            全局变量.提示类.显示信息("仅本方攻城部队可以全军撤退。");
        else
		{
            战斗系统脚本对象.全军撤退 = true;
            全局变量.提示类.显示信息("已下令全军撤退。");
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
			if (战斗系统脚本对象 == null)
            {
                开始显示兵力 = false;
                上次显示兵力 = false;
                战斗时间.text = "战斗已结束";
                攻方兵力文本.text = 守方兵力文本.text = "—";
                return;
            }
			//战斗计时改用可缩放的 Time.time：随倍速一起加速；false->true 边沿时复位
			if (!上次显示兵力)
			{
				战斗计时开始时间 = Time.time;
				上次显示兵力 = true;
			}
			long time = (long)Mathf.Floor(Time.time - 战斗计时开始时间);
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
