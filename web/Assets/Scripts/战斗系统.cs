using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using Dwsg.Window3;

public class 战斗系统 : MonoBehaviour
{
	public string 服务器战场ID;

	public bool 服务器战场 => !string.IsNullOrEmpty(服务器战场ID);

	public int 战场类型;

	public int 坐标x;

	public int 坐标y;

	public long 创建时间;

	public GameObject 战斗结果显示对象;

	public GameObject 伤害布局列表;

	public List<GameObject> 伤害显示缓存表 = new List<GameObject>();

	public List<List<将领信息>> 攻方要渲染的编队将领列表 = new List<List<将领信息>>();

	public List<List<将领信息>> 守方要渲染的编队将领列表 = new List<List<将领信息>>();

	public int 攻身份;

	public int 守身份 = 1;

	public double 攻方兵力;

	public double 守方兵力;

	public bool 开始检测战斗结果;

	public bool 战斗结束;

	public bool 正在观战;

	public bool 全军撤退;

	public GameObject 攻方坑位对象;

	public GameObject 守方坑位对象;

	public Text 城墙信息显示;

	public Transform 城墙血条对象;

	private GameObject 坑位模型;

	public List<将领配兵> 消灭敌军列表 = new List<将领配兵>();

	public List<将领配兵> 损失兵力列表 = new List<将领配兵>();

	public 城池信息库类 被攻击的城池;

	public bool 等待销毁战场;

	private readonly Dictionary<将领信息, double> 玩家守军初始兵力 = new Dictionary<将领信息, double>();
	private readonly HashSet<将领信息> 本机驻防将领 = new HashSet<将领信息>();
	private readonly HashSet<军情信息> 参战军情 = new HashSet<军情信息>();

	public void 登记参战军情(军情信息 军情)
	{
		if (军情 != null) 参战军情.Add(军情);
	}

	private void 清理本战军情()
	{
		释放未渲染队员();
		// 同坐标下一批部队可能恰好到时；只能清理实际加入本战场的对象。
		全局变量.军情列表.RemoveAll(军情 => 参战军情.Contains(军情) &&
			!缺失界面.窗口4.和平驻防规则.是驻防军情(军情));
		参战军情.Clear();
	}

	private void 释放未渲染队员()
	{
		// 渲染器生成模型后会清空这两表；已有模型仍走原来的退场回调。
		// 结束若早于下一次渲染，待入场队员没有模型替它释放占用状态。
		var 待释放 = new HashSet<将领信息>();
		foreach (var 编队列表 in new[] { 攻方要渲染的编队将领列表, 守方要渲染的编队将领列表 })
		{
			if (编队列表 == null) continue;
			foreach (var 编队 in 编队列表)
			{
				if (编队 == null) continue;
				foreach (var 将领 in 编队) if (将领 != null) 待释放.Add(将领);
			}
		}
		foreach (var 将领 in 待释放)
		{
			if (将领.详细信息 == null || 将领.详细信息.状态 != 1) continue;
			bool 驻防接管 = 缺失界面.窗口4.和平驻防规则.接管战后返回(将领, TIME.getTime());
			if (!驻防接管 && 将领.详细信息.状态 == 1) 将领.详细信息.状态 = 0;
		}
	}

	public void 登记玩家守军(城池信息库类 城池, List<将领信息> 守军)
	{
		foreach (var 将领 in 守军)
		{
			if (将领 == null || 玩家守军初始兵力.ContainsKey(将领)) continue;
			玩家守军初始兵力.Add(将领, 将领.将领配兵.数量);
			if (将领.详细信息.身份 == 全局变量.本机身份) 本机驻防将领.Add(将领);
			// 尚未生成战场模型的守军也保留完整兵力，不能沿用上次战斗的剩余值。
			将领.详细信息.剩余兵力 = 将领.将领配兵.数量;
		}
		缺失界面.窗口4.和平驻防规则.标记参战(城池, 守军);
	}

	private void Start()
	{
		渲染坑位();
		伤害显示缓存表.Clear();
		for (int i = 0; i < 30; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(全局变量.伤害显示pre);
			gameObject.transform.SetParent(伤害布局列表.transform);
			gameObject.transform.localPosition = new Vector2(0f, 0f);
			gameObject.SetActive(value: false);
			伤害显示缓存表.Add(gameObject);
		}
		被攻击的城池 = 所有城池界面脚本.根据坐标获取指定城池(坐标x, 坐标y);
		if (战场类型 == 1)
		{
			城墙信息显示.text = 被攻击的城池.城墙.ToString();
		}
	}

	private void 渲染坑位()
	{
		float num = 6.5f;
		float num2 = 3.5f;
		加载资源.头像资源();
		加载资源.预制体资源();
		float num3 = -35f;
		float y = 29.8f;
		int num4 = 0;
		int num5 = 0;
		if (战场类型 == 1)
		{
			num3 = -12f;
		}
		for (int i = 0; i < 15; i++)
		{
			坑位模型 = UnityEngine.Object.Instantiate(全局变量.红色坑位pre);
			坑位模型.transform.name = "坑位" + i.ToString();
			坑位模型.transform.SetParent(攻方坑位对象.transform);
			坑位模型.transform.localPosition = new Vector2(0f - (float)num4 * num, 0f - (float)num5 * num2);
			num5++;
			if (num5 >= 5)
			{
				num5 = 0;
				num4++;
				if (num4 >= 3)
				{
					num4 = 0;
				}
			}
		}
		攻方坑位对象.transform.localPosition = new Vector2(num3, y);
		num4 = 0;
		num5 = 0;
		for (int j = 0; j < 15; j++)
		{
			坑位模型 = UnityEngine.Object.Instantiate(全局变量.蓝色坑位pre);
			坑位模型.transform.name = "坑位" + j.ToString();
			坑位模型.transform.SetParent(守方坑位对象.transform);
			坑位模型.transform.localPosition = new Vector2(0f + (float)num4 * num, 0f - (float)num5 * num2);
			num5++;
			if (num5 >= 5)
			{
				num5 = 0;
				num4++;
				if (num4 >= 3)
				{
					num4 = 0;
				}
			}
		}
		守方坑位对象.transform.localPosition = new Vector2(num3 + 8f, y);
	}

	public void 记录击杀兵力信息(double 兵种ID, double 击杀数量)
	{
		将领配兵 将领配兵 = 消灭敌军列表.Find((将领配兵 t) => t.ID == 兵种ID);
		if (将领配兵 == null)
		{
			将领配兵 将领配兵2 = new 将领配兵();
			将领配兵2.ID = 兵种ID;
			将领配兵2.数量 = 击杀数量;
			消灭敌军列表.Add(将领配兵2);
		}
		else
		{
			将领配兵.数量 += 击杀数量;
		}
	}

	public void 记录损失兵力信息(double 兵种ID, double 损失数量)
	{
		将领配兵 将领配兵 = 损失兵力列表.Find((将领配兵 t) => t.ID == 兵种ID);
		if (将领配兵 == null)
		{
			将领配兵 将领配兵2 = new 将领配兵();
			将领配兵2.ID = 兵种ID;
			将领配兵2.数量 = 损失数量;
			损失兵力列表.Add(将领配兵2);
		}
		else
		{
			将领配兵.数量 += 损失数量;
		}
	}

	private void FixedUpdate()
	{
		if (服务器战场) return;
		if (!开始检测战斗结果)
		{
			return;
		}
		if (战场类型 == 1)
		{
			城墙信息显示.text = 被攻击的城池.城墙.ToString();
			double 城墙 = 被攻击的城池.城墙;
			double num = 被攻击的城池.获取城墙上限();
			double num2 = 0.0;
			if (城墙 > 0.0)
			{
				num2 = 城墙 / num;
			}
			float num3 = (float)num2;
			城墙血条对象.localPosition = new Vector2(3f * num3, -0.15f);
		}
		double num4 = 0.0;
		bool 攻方获胜 = false;
		if (守方兵力 <= 0.0 || 攻方兵力 <= 0.0)
		{
			if (全军撤退)
			{
				if (守方兵力 <= 0.0 && 攻方兵力 <= 0.0)
				{
					if (战场类型 == 1)
					{
						UnityEngine.Debug.Log("城池战斗撤退");
						播报城池战报(false);
						战斗结束 = true;
						被攻击的城池.正在交战 = false;
						被攻击的城池.刷新名将驻防();

                    }
					else
					{
                        UnityEngine.Debug.Log(战场类型 == 资源点军情信息.资源战场类型 ? "资源战斗撤退" : "山贼战斗撤退");
						战斗结束 = true;
					}
				}
			}
			else if (战场类型 == 1)
			{
				if (守方兵力 <= 0.0 && 被攻击的城池.城墙 <= 0.0)
				{
					攻方获胜 = true;
					UnityEngine.Debug.Log($"城池战斗结束{攻身份}{坐标x}{坐标y}");
                    num4 = 被攻击的城池.获取攻打战功();
                    播报城池战报(true);
                    if (全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[攻身份].基础信息.国家) != null)
						被攻击的城池.攻下城池(攻身份);
					战斗结束 = true;
					被攻击的城池.正在交战 = false;
                    被攻击的城池.刷新名将驻防();
                }
				else if (攻方兵力 <= 0.0)
				{
					UnityEngine.Debug.Log("城池战斗结束1");
					播报城池战报(false);
					战斗结束 = true;
					被攻击的城池.正在交战 = false;
				}
			}
			else if (战场类型 == 资源点军情信息.资源战场类型)
			{
				攻方获胜 = 守方兵力 <= 0.0 && 攻方兵力 > 0.0;
				战斗结束 = true;
				UnityEngine.Debug.Log(攻方获胜 ? "资源守军已被真实战斗击败" : "资源战斗失利");
			}
			else
			{
				UnityEngine.Debug.Log("山贼战斗结束");
				if (守方兵力 <= 0.0)
				{
					攻方获胜 = true;
					UnityEngine.Debug.Log("山贼已被击败,刷新");
					附近山贼.重新生成指定山贼(坐标x, 坐标y);
				}
				战斗结束 = true;
			}
		}
		if (!战斗结束)
		{
			return;
		}
		// 结算只执行一次；显示战报和退出画面都不能再次触发奖励。
		开始检测战斗结果 = false;
		string 资源战果 = null;
		if (战场类型 == 资源点军情信息.资源战场类型)
		{
			var 结算 = 资源点战斗适配.处理结束(this, 攻方获胜, TIME.getTime());
			资源战果 = 结算.Message;
			if (!结算.Success) UnityEngine.Debug.LogError("真实资源战果未能登记：" + 结算.Message);
		}
		string 本次战报 = null;
		bool 战报已投递 = false;
		string 驻防战报 = 结算玩家驻防(攻方获胜, out 战报已投递);
		if (驻防战报 != null) 本次战报 = 驻防战报;
		if (攻身份 == 全局变量.本机身份)
		{
            List<Dwsg.Shared.Combat.击杀兵种信息> 击杀列表 = new List<Dwsg.Shared.Combat.击杀兵种信息>();
            int count = 消灭敌军列表.Count;
            for (int i = 0; i < count; i++)
            {
                兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(消灭敌军列表[i].ID);
                击杀列表.Add(new Dwsg.Shared.Combat.击杀兵种信息 { 兵种ID = (int)消灭敌军列表[i].ID, 兵种攻击 = 兵种属性库类.攻击力, 数量 = 消灭敌军列表[i].数量 });
            }
            Dwsg.Shared.Combat.战斗奖励 奖励 = Dwsg.Shared.Combat.战斗规则.计算奖励(击杀列表, 全局变量.所有玩家数据表[全局变量.本机身份].获取指定状态加成("资源声望"));
            double num5 = 奖励.声望;
            double num6 = 奖励.国库铜钱;
            double num7 = 奖励.原提示粮食;
            double num8 = 奖励.原提示黄金;
            var 君主 = 全局变量.所有玩家数据表[全局变量.本机身份].基础信息;
            double 原声望 = 君主.声望;
            君主.君主获得经验(Mathf.Floor((float)num5));
            double 实得声望 = System.Math.Max(0, 君主.声望 - 原声望);

			国家信息库类 国家 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[全局变量.本机身份].基础信息.国家);
			double 国库收入 = 国家 == null ? 0 : Mathf.Floor((float)num6);
			if (国家 != null)
			{
				国家.铜钱 += 国库收入;
				国家.粮食 += 国库收入;
			}
            //全局变量.所有玩家数据表[全局变量.本机身份].财产信息.铜钱 = 全局变量.所有玩家数据表[全局变量.本机身份].财产信息.铜钱 + (double)Mathf.Floor((float)num6);
            //全局变量.所有玩家数据表[全局变量.本机身份].财产信息.粮食 = 全局变量.所有玩家数据表[全局变量.本机身份].财产信息.粮食 + (double)Mathf.Floor((float)num7);
            //全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 = 全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 + (double)Mathf.Floor((float)num8);
            全局变量.所有玩家数据表[全局变量.本机身份].基础信息.战功 = 全局变量.所有玩家数据表[全局变量.本机身份].基础信息.战功 + (double)Mathf.Floor((float)num4);
			int 战功 = (int)全局变量.所有玩家数据表[全局变量.本机身份].基础信息.战功;
            if (战功 >=2000 && 战功<3000)
				全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.校尉;
            if (战功 >= 3000 && 战功 < 5000)
                全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.监军;
            if (战功 >= 5000 && 战功 < 7000)
                全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.中郎将;
            if (战功 >= 7000 && 战功 < 8000)
                全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.卫将军;
            if (战功 >= 8000 && 战功 < 15000)
                全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.大将军;
            if (战功 >=15000)
                全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.大都督;
            if (战功 <2000)
                全局变量.所有玩家数据表[全局变量.本机身份].基础信息.官职 = 官职信息.平民;


            string 结果 = 全军撤退 ? "全军撤退" : 攻方获胜 ? "战斗胜利" : "战斗失利";
            double 消灭数 = 0, 损失数 = 0;
            foreach (var 兵力 in 消灭敌军列表) 消灭数 += 兵力.数量;
            foreach (var 兵力 in 损失兵力列表) 损失数 += 兵力.数量;
            string 战场名称 = 战场类型 == 1 ? "攻城" : 战场类型 == 资源点军情信息.资源战场类型 ? "占领资源点" : "剿匪";
            本次战报 = "【" + 结果 + "】" + 战场名称 + "（" + 坐标x + "," + 坐标y + "）\n" +
                "消灭敌军：" + System.Math.Floor(消灭数).ToString("N0") + "　损失：" + System.Math.Floor(损失数).ToString("N0") + "\n" +
                "【个人所得】\n声望 +" + 实得声望.ToString("N0") + "\n战功 +" + Mathf.Floor((float)num4).ToString("N0") + "\n" +
                "【上缴国库】\n铜钱 +" + 国库收入.ToString("N0") + "　粮食 +" + 国库收入.ToString("N0") + "\n所得已结算，可在邮件中查看战报。";
            if (资源战果 != null) 本次战报 += "\n" + 资源战果;
            string 邮件错误;
            战报已投递 = Dwsg.Window1.Window1Module.接收本地邮件(new Dwsg.Window1.LocalMail
            {
                Id = "battle." + System.Guid.NewGuid().ToString("N"), Sender = "军报",
                Title = 结果 + " · " + 战场名称 + "（" + 坐标x + "," + 坐标y + "）",
                Body = 本次战报, SentUtcTicks = System.DateTime.UtcNow.Ticks
            }, out 邮件错误);
            if (!战报已投递) 本次战报 = 本次战报.Replace("可在邮件中查看战报。", "邮箱已满，战报保留在聊天播报中。");
            Dwsg.Window1.Window1Module.通知世界数据变更();
            聊天系统.播报(本次战报);
        }

		if (正在观战)
		{
			全局变量.战斗地图相机.transform.SetParent(全局变量.战斗地图相机.transform.parent.parent.parent);
			全局变量.大地图相机.SetActive(value: true);
			全局变量.大地图布局对象.SetActive(value: true);
			全局变量.战斗地图相机.SetActive(value: false);
			全局变量.主界面UI对象.SetActive(value: true);
			全局变量.战斗界面UI对象.SetActive(value: false);
			//返回世界后恢复大地图背景音乐（进战场时被 Stop 掉了）
			主界面UI脚本 世界界面 = 全局变量.主界面UI对象.GetComponent<主界面UI脚本>();
			if (世界界面 != null)
			{
				世界界面.背景音乐对象.clip = 世界界面.大地图背景音乐;
				世界界面.背景音乐对象.Play();
			}
			if (本次战报 != null) 显示本次战报(本次战报);
		}
		else if (本次战报 != null && 全局变量.提示类 != null)
			全局变量.提示类.显示世界播报(战报已投递 ? "战斗结束，战报已送至邮件。" : "战斗结束，请在聊天播报中查看战报。");
		清理本战军情();
		UnityEngine.Debug.Log("军情清理完毕!");
		守方要渲染的编队将领列表 = null;
		攻方要渲染的编队将领列表 = null;
		伤害显示缓存表 = null;
		开始检测战斗结果 = false;
		UnityEngine.Object.Destroy(base.gameObject.transform.parent.parent.parent.parent.parent.gameObject, 0.2f);
	}

	private string 结算玩家驻防(bool 攻方获胜, out bool 已投递)
	{
		已投递 = false;
		int 本机参战数 = 0, 被俘数 = 0;
		double 初始兵力 = 0, 幸存兵力 = 0;
		foreach (var 记录 in 玩家守军初始兵力)
		{
			var 将领 = 记录.Key;
			bool 本机将领 = 本机驻防将领.Contains(将领);
			缺失界面.窗口4.和平驻防规则.接管战后返回(将领, TIME.getTime());
			if (!本机将领) continue;
			本机参战数++;
			初始兵力 += 记录.Value;
			if (将领.详细信息.状态 == 3) 被俘数++;
			else 幸存兵力 += System.Math.Min(记录.Value, System.Math.Max(0, 将领.将领配兵.数量));
		}
		缺失界面.窗口4.和平驻防规则.推进(TIME.getTime());
		if (本机参战数 == 0) return null;
		string 结果 = 攻方获胜 ? "守城失利" : "守城胜利";
		string 内容 = "【" + 结果 + "】" + (被攻击的城池 == null ? "城池" : 被攻击的城池.名称) + "（" + 坐标x + "," + 坐标y + "）\n" +
			"参战将领：" + 本机参战数 + "　被俘：" + 被俘数 + "\n本机兵力损失：" + System.Math.Floor(初始兵力 - 幸存兵力).ToString("N0") +
			"\n幸存配兵：" + System.Math.Floor(幸存兵力).ToString("N0") + "\n驻防与返程状态已更新，可在军情查看。";
		string 错误;
		已投递 = Dwsg.Window1.Window1Module.接收本地邮件(new Dwsg.Window1.LocalMail
		{
			Id = "defense." + System.Guid.NewGuid().ToString("N"), Sender = "军报",
			Title = 结果 + "（" + 坐标x + "," + 坐标y + "）", Body = 内容, SentUtcTicks = System.DateTime.UtcNow.Ticks
		}, out 错误);
		Dwsg.Window1.Window1Module.通知世界数据变更();
		聊天系统.播报(内容);
		return 内容;
	}

	private void 显示本次战报(string 内容)
	{
		if (战斗结果显示对象 == null)
		{
			foreach (var 根对象 in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
				if (根对象.name == "战斗结束结算界面") { 战斗结果显示对象 = 根对象; break; }
		}
		if (战斗结果显示对象 == null) return;
		var 说明 = 战斗结果显示对象.transform.Find("说明文本");
		if (说明 == null) return;
		var 文字 = 说明.GetComponent<Text>();
		文字.text = 内容;
		文字.supportRichText = false;
		文字.horizontalOverflow = HorizontalWrapMode.Wrap;
		文字.verticalOverflow = VerticalWrapMode.Truncate;
		战斗结果显示对象.SetActive(true);
	}

	//战报：把这场城池战的结果推给聊天（「传闻」频道），格式照参考图——
	//「守方国 在 小城XX 击败 攻方国！」。所有字段都做了兜底，取不到就不播，
	//绝不影响战斗本身。注意要在「攻下城池」改归属之前调用，否则守方国名会被覆盖。
	private void 播报城池战报(bool 攻方胜)
	{
		try
		{
			if (战场类型 != 1 || 被攻击的城池 == null)
			{
				return;
			}
			string 守方国 = 被攻击的城池.国家;
			string 攻势 = 规模称呼(被攻击的城池.规模) + 被攻击的城池.名称;
			string 攻方国 = "";
			if (攻身份 >= 0 && 攻身份 < 全局变量.所有玩家数据表.Count)
			{
				攻方国 = 全局变量.所有玩家数据表[攻身份].基础信息.国家;
			}
			if (string.IsNullOrEmpty(攻方国))
			{
				攻方国 = "某国";
			}
			if (string.IsNullOrEmpty(守方国))
			{
				守方国 = "无主势力";
			}
			string 内容;
			if (攻方胜)
			{
				内容 = 攻方国 + "攻下了" + 守方国 + 攻势 + "！";
			}
			else
			{
				内容 = 守方国 + "在" + 攻势 + "击败" + 攻方国 + "！";
			}
			聊天系统.发送(聊天频道.传闻, "战报", 内容);
		}
		catch (System.Exception)
		{
		}
	}

	private static string 规模称呼(int 规模)
	{
		switch (规模)
		{
		case 1:
			return "小城";
		case 2:
			return "中城";
		case 3:
			return "大城";
		case 4:
			return "重镇";
		default:
			return "";
		}
	}
}
