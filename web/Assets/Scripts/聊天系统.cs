using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dwsg.Client.Chat;
using Dwsg.Network;
using Newtonsoft.Json.Linq;

// ================================================================
// 聊天系统：把全游戏的播报统一收进聊天里
//
// 之前所有提示都是 提示移动.显示信息() 的飘字，脚本里有一百多处调用。
// 现在 显示信息() 会顺手把同一句话推进聊天，于是所有播报都从聊天里走：
//   · 世界 / 封地界面：聊天按钮摆在「主界面_国家」后面，点开是频道面板
//   · 战斗界面：左下角一条青色播报条，点一下也能展开同一个频道面板
//
// 界面全部是运行时用代码搭的，场景文件一个字节都不用动。
// 图片沿用工程里已有的：聊天/主界面_聊天（入口图标）、7966（面板底）、
// 7967/7968/7969（金色边框）、873（返回箭头）、7992（设置按钮）、
// 通用界面/军情红点（未读红点）。
//
// 频道按内容自动分流：
//   国家 —— AI 国战播报（谁在打谁）
//   传闻 —— 战斗结果、城池易主、将领归天这一类
//   系统 —— 其余全部（买、卖、升级、强化、出征……）
//   其余频道保留入口，目前还没有内容来源。
//
// 只接了两根线：
//   1. 全局任务脚本.Start() 里   聊天系统.初始化();
//   2. 提示移动.显示信息() 里    聊天系统.播报(要显示的文本);
//
// 想手动调整样式，直接改下面那组常量即可。
// ================================================================

// 频道顺序就是面板里从左到右、从上到下的顺序
public enum 聊天频道
{
	全部 = 0,
	世界 = 1,
	国家 = 2,
	传闻 = 3,
	军团 = 4,
	私聊 = 5,
	城池 = 6,
	师徒 = 7,
	结拜 = 8,
	系统 = 9
}

public class 聊天消息
{
	public string 通知ID;
	public 聊天频道 频道;

	public string 发送者;

	public string 内容;

	public long 时间;

	public long 序号;

	//玩家自己说的话（显示时跟系统播报区分开：名字缀「我」、颜色不同）
	public bool 自己;
}

public class 聊天系统 : MonoBehaviour
{
	// ==================== 可调参数 ====================

	// 面板尺寸按“屏幕像素”来写（760x462）。主界面UI 和 战斗界面UI 两个画布都是
	// CanvasScaler 参考 960x540、按高度匹配，1080p 下 1 画布单位 = 2 屏幕像素，
	// 所以两个面板都整体缩到 0.5，屏幕上就是这里写的尺寸。
	private const float 面板宽 = 760f;

	private const float 面板高 = 462f;

	private const float 边框厚度 = 8f;

	private const float 消息区左 = 136f;

	private const float 消息区右留白 = 8f;

	private const float 消息区上留白 = 10f;

	private const float 消息区下留白 = 66f;

	private const float 内容留白 = 4f;

	private const float 频道按钮宽 = 116f;

	private const float 频道按钮高 = 30f;

	private const float 频道间距 = 36f;

	private const float 频道首个纵坐标 = -8f;

	private const float 头像尺寸 = 46f;

	private const float 头像右间距 = 8f;

	private const float 名字行高 = 20f;

	private const float 框内边距 = 6f;

	private const float 消息视口宽 = 面板宽 - 消息区左 - 消息区右留白;

	private const float 行宽 = 消息视口宽 - 内容留白 * 2f;

	private const float 行文字左偏移 = 头像尺寸 + 头像右间距;

	private const float 行文本宽 = 行宽 - 行文字左偏移 - 框内边距 * 2f;

	// 战斗播报条（挂在战斗界面UI 左下角；战斗界面UI 放大 2 倍，所以屏幕上是 1090x98）
	private const float 播报条宽 = 545f;

	private const float 播报条高 = 49f;

	// 世界／封地界面里聊天按钮的位置：接在「主界面_国家」后面
	private const float 入口按钮间距 = 91.46f;

	private const float 面板横坐标 = 80f;

	private const float 面板纵坐标 = 85f;

	// 战斗界面里面板的位置（贴在播报条上方）
	private const float 战斗面板纵坐标 = 56f;

	private const int 消息总上限 = 300;

	private const int 单页显示上限 = 60;

	//底部输入行的尺寸（跟面板一样按屏幕像素写）
	private const float 输入行高 = 40f;

	private const float 输入框左 = 244f;

	private const float 输入框宽 = 392f;

	private const float 发送按钮左 = 644f;

	private const float 发送按钮宽 = 100f;

	private const float 输入行纵坐标 = 19f;

	//玩家自己一条最多打几个字
	private const int 单条字数上限 = 40;

	// ==================== 存档键 ====================

	private const string 频道存档键 = "聊天_当前频道";

	private const string 播报条存档键 = "聊天_战斗播报条";

	// ==================== 配色（照参考图取色） ====================

	private static readonly Color 选中字色 = new Color(1f, 0.949f, 0.667f, 1f);

	private static readonly Color 未选中字色 = new Color(0.804f, 0.760f, 0.502f, 1f);

	private static readonly Color 青字 = new Color(0.188f, 0.874f, 0.776f, 1f);

	private static readonly Color 名字色 = new Color(0.925f, 0.769f, 0.322f, 1f);

	private static readonly Color 描边色 = new Color(0.043f, 0.055f, 0.031f, 0.95f);

	private static readonly Color 频道普通底 = new Color(0.055f, 0.063f, 0.047f, 0.92f);

	private static readonly Color 频道选中底 = new Color(0.145f, 0.400f, 0.353f, 0.96f);

	private static readonly Color 金色线 = new Color(0.749f, 0.635f, 0.400f, 0.85f);

	private static readonly Color 消息框底 = new Color(0.024f, 0.024f, 0.024f, 0.878f);

	private static readonly Color 播报条底 = new Color(0.043f, 0.051f, 0.031f, 0.596f);

	private static readonly Color 红点色 = Color.white;

	//自己说的话：消息框底比系统播报稍微偏绿一点
	private static readonly Color 自己框底 = new Color(0.055f, 0.098f, 0.078f, 0.9f);

	private static readonly string[] 频道名称 = new string[10] { "全部", "世界", "国家", "传闻", "军团", "私聊", "城池", "师徒", "结拜", "系统" };

	// 分流用的关键词
	private static readonly string[] 传闻关键词 = new string[11] { "击败", "攻下", "攻占", "占领", "夺回", "守城", "战报", "灭亡", "抓到", "劝降", "回归大自然" };

	private static readonly string[] 国家关键词 = new string[6] { "正在进攻", "来袭", "袭来", "攻打", "进攻", "讨伐" };

	// ==================== 数据（静态，世界里外读同一份） ====================

	private static readonly List<聊天消息> 全部消息 = new List<聊天消息>();

	private static long 自增序号 = 0L;

	private static int 数据版本 = 0;

	//输入框正在打字：这时候 1/2/3/4 之类的快捷键要让给输入框
	public static bool 正在输入;

	private static 聊天系统 实例;

	private static Font 界面字体;

	private static Dictionary<string, Sprite> 素材表;

	// ==================== 实例状态 ====================

	private GameObject 世界界面;

	private GameObject 战斗界面;

	private GameObject 战斗播报条对象;

	private 面板部件 世界部件;

	private 面板部件 战斗部件;

	private 聊天频道 当前频道 = 聊天频道.全部;

	private long 已读序号 = 0L;

	private int 已刷新数据版本 = -1;

	private float 下次角标刷新;

	private class 面板部件
	{
		public GameObject 面板;

		public RectTransform 内容;

		public ScrollRect 滚动;

		public GameObject 设置面板;

		public Text 播报条开关文字;

		public Text 播报文本;

		public GameObject 未读红点;

		public InputField 输入框;

		public Text 输入提示;

		public Button 发送按钮;

		public Text 空提示;

		public readonly List<Button> 频道按钮 = new List<Button>();

		public readonly List<Text> 频道文字 = new List<Text>();

		public readonly Dictionary<RectTransform, string> 通知行 = new Dictionary<RectTransform, string>();

		public int 已刷新版本 = -1;

		public 聊天频道 已刷新频道 = 聊天频道.全部;
	}

	// ==================== 对外接口 ====================

	//挂在 全局任务脚本.Start() 里。世界界面 / 战斗界面 会被反复补挂，
	//拿不到对象就下一帧再试，所以调用时机不用挑。
	public static void 初始化()
	{
		if (实例 == null)
		{
			GameObject 载体 = new GameObject("聊天系统");
			UnityEngine.Object.DontDestroyOnLoad(载体);
			实例 = 载体.AddComponent<聊天系统>();
			实例.读取存档();
		}
		ChatClient.Initialize(接收联机消息, 播报, 清空聊天);
		实例.挂界面();
	}

	//全游戏播报的统一入口：内容一样的一条会同时进飘字和聊天
	public static void 播报(string 内容)
	{
		if (string.IsNullOrEmpty(内容))
		{
			return;
		}
		发送(判断频道(内容), null, 内容);
	}

	//指定频道手动发一条
	public static void 发送(聊天频道 频道, string 发送者, string 内容)
	{
		if (string.IsNullOrEmpty(内容))
		{
			return;
		}
		聊天消息 消息 = new 聊天消息();
		消息.频道 = 频道;
		消息.发送者 = (string.IsNullOrEmpty(发送者) ? 取默认发送者(频道) : 发送者);
		消息.内容 = 内容.Replace("\r\n", "  ").Replace('\n', ' ').Replace('\r', ' ');
		消息.发送者 = 消息.发送者.Replace("\r\n", "  ").Replace('\n', ' ').Replace('\r', ' ');
		消息.时间 = 0L;
		try
		{
			消息.时间 = TIME.getTime();
		}
		catch (System.Exception)
		{
			消息.时间 = 0L;
		}
		加入消息(消息);
	}

	//玩家自己说话：发到当前选中的频道（「全部」落到「世界」）。
	//发出去返回 true；传闻/系统这些只读频道会被挡下，返回 false。
	public static bool 玩家发言(string 内容)
	{
		if (实例 == null || 内容 == null)
		{
			return false;
		}
		内容 = 内容.Replace("\r", "").Replace("\n", " ").Trim();
		if (内容.Length <= 0)
		{
			return false;
		}
		if (内容.Length > 单条字数上限)
		{
			内容 = 内容.Substring(0, 单条字数上限);
		}
		聊天频道 落点;
		if (!取发言落点(实例.当前频道, out 落点))
		{
			return false;
		}
		if (GameNetwork.Enabled)
		{
			return ChatClient.Send(落点 == 聊天频道.国家 ? "nation" : 落点 == 聊天频道.城池 ? "city" : "world", 内容);
		}
		聊天消息 消息 = new 聊天消息();
		消息.频道 = 落点;
		消息.发送者 = 取本机名字();
		消息.内容 = 内容;
		消息.自己 = true;
		消息.时间 = 0L;
		try
		{
			消息.时间 = TIME.getTime();
		}
		catch (System.Exception)
		{
			消息.时间 = 0L;
		}
		加入消息(消息);
		//自己说的话不算未读，免得聊天按钮上凭空冒红点
		实例.已读序号 = 自增序号;
		return true;
	}

	private static void 接收联机消息(JObject 数据, bool 自己)
	{
		聊天消息 消息 = new 聊天消息();
		string 频道 = 数据.Value<string>("channel");
		消息.频道 = 频道 == "nation" ? 聊天频道.国家 : 频道 == "city" ? 聊天频道.城池 : 频道 == "rumor" ? 聊天频道.传闻 : 频道 == "system" ? 聊天频道.系统 : 聊天频道.世界;
		消息.通知ID = 数据.Value<string>("notificationId");
		消息.发送者 = 数据.Value<string>("senderName").Replace("<", "＜").Replace(">", "＞");
		消息.内容 = 数据.Value<string>("content").Replace("<", "＜").Replace(">", "＞");
		消息.时间 = 数据.Value<long>("serverUtcMs") / 1000;
		消息.自己 = 自己;
		加入消息(消息);
		if (自己 && 实例 != null) 实例.已读序号 = 自增序号;
	}

	//本机玩家的名字；取不到就退成「我」，不因为拿不到名字就不让说话
	public static string 取本机名字()
	{
		try
		{
			int 身份 = 全局变量.本机身份;
			if (全局变量.所有玩家数据表 != null && 身份 >= 0 && 身份 < 全局变量.所有玩家数据表.Count)
			{
				string 名字 = 全局变量.所有玩家数据表[身份].基础信息.名字;
				if (!string.IsNullOrEmpty(名字))
				{
					return 名字;
				}
			}
		}
		catch (System.Exception)
		{
		}
		return "我";
	}

	//哪些频道玩家能说话：「全部」当成「世界」发出去，传闻/系统/私聊这些暂时只读
	private static bool 取发言落点(聊天频道 频道, out 聊天频道 落点)
	{
		switch (频道)
		{
		case 聊天频道.全部:
		case 聊天频道.世界:
			落点 = 聊天频道.世界;
			return true;
		case 聊天频道.国家:
			落点 = 频道;
			return true;
		case 聊天频道.城池:
			落点 = 频道;
			return true;
		default:
			落点 = 频道;
			return false;
		}
	}

	//输入框里的灰字提示
	private static string 取输入提示(聊天频道 频道)
	{
		聊天频道 落点;
		if (取发言落点(频道, out 落点))
		{
			if (频道 == 聊天频道.全部)
			{
				return "说点什么…（会发到「世界」频道）";
			}
			return "说点什么…";
		}
		if (频道 == 聊天频道.传闻)
		{
			return "「传闻」只能查看战报";
		}
		if (频道 == 聊天频道.系统)
		{
			return "「系统」只能查看";
		}
		return "「" + 频道名称[(int)频道] + "」暂未开放发言";
	}

	//消息统一进列表，超上限就顶掉最旧的
	private static void 加入消息(聊天消息 消息)
	{
		自增序号++;
		消息.序号 = 自增序号;
		全部消息.Add(消息);
		while (全部消息.Count > 消息总上限)
		{
			全部消息.RemoveAt(0);
		}
		数据版本++;
	}

	public static void 清空聊天()
	{
		全部消息.Clear();
		数据版本++;
	}

	//没读过的条数：世界界面的聊天按钮上就是靠它决定红点显不显示
	public static int 取未读条数()
	{
		if (实例 == null)
		{
			return 0;
		}
		long 未读 = ChatClient.UnreadNotifications;
		foreach (聊天消息 消息 in 全部消息)
			if (消息.序号 > 实例.已读序号 && string.IsNullOrEmpty(消息.通知ID)) 未读++;
		if (未读 <= 0L)
		{
			return 0;
		}
		if (未读 > 99L)
		{
			return 99;
		}
		return (int)未读;
	}

	private static 聊天频道 判断频道(string 内容)
	{
		if (含关键词(内容, 传闻关键词))
		{
			return 聊天频道.传闻;
		}
		if (含关键词(内容, 国家关键词))
		{
			return 聊天频道.国家;
		}
		return 聊天频道.系统;
	}

	private static bool 含关键词(string 内容, string[] 关键词表)
	{
		for (int i = 0; i < 关键词表.Length; i++)
		{
			if (内容.Contains(关键词表[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static string 取默认发送者(聊天频道 频道)
	{
		switch (频道)
		{
		case 聊天频道.国家:
			return "国战";
		case 聊天频道.传闻:
			return "战报";
		case 聊天频道.世界:
			return "世界";
		default:
			return "系统";
		}
	}

	// ==================== 运行期 ====================

	private void 读取存档()
	{
		int 频道序号 = PlayerPrefs.GetInt(频道存档键, 0);
		if (频道序号 < 0 || 频道序号 >= 频道名称.Length)
		{
			频道序号 = 0;
		}
		当前频道 = (聊天频道)频道序号;
	}

	private void Update()
	{
		ChatClient.RefreshSelection();
		挂界面();
		同步面板(世界部件);
		同步面板(战斗部件);
		刷新输入状态();
		if (已刷新数据版本 != 数据版本)
		{
			已刷新数据版本 = 数据版本;
			刷新战斗条();
			刷新未读红点();
		}
		if (Time.unscaledTime >= 下次角标刷新)
		{
			下次角标刷新 = Time.unscaledTime + 0.5f;
			标记可见通知(世界部件);
			标记可见通知(战斗部件);
			刷新未读红点();
		}
	}

	//输入框被选中时：告诉别的系统「正在打字」，回车就当发送。
	//（没有用 InputField.onEndEdit，那个连「点到别处失焦」也会触发，会误发。）
	private void 刷新输入状态()
	{
		面板部件 目标 = null;
		if (世界部件 != null && 世界部件.输入框 != null && 世界部件.输入框.isFocused)
		{
			目标 = 世界部件;
		}
		else if (战斗部件 != null && 战斗部件.输入框 != null && 战斗部件.输入框.isFocused)
		{
			目标 = 战斗部件;
		}
		正在输入 = (目标 != null);
		if (目标 == null)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			发送输入(目标);
		}
	}

	//回车和「发 送」按钮都走这里
	private void 发送输入(面板部件 部件)
	{
		if (部件 == null || 部件.输入框 == null)
		{
			return;
		}
		string 内容 = 部件.输入框.text;
		if (string.IsNullOrEmpty(内容) || 内容.Trim().Length == 0)
		{
			部件.输入框.text = "";
			刷新提示(部件);
			return;
		}
		if (!玩家发言(内容))
		{
			return;
		}
		部件.输入框.text = "";
		刷新提示(部件);
		StartCoroutine(延迟聚焦(部件));
	}

	//回车提交会让输入框失焦，下一帧抢回来，方便连着打下一句
	private IEnumerator 延迟聚焦(面板部件 部件)
	{
		yield return null;
		if (部件 != null && 部件.输入框 != null && 部件.面板 != null && 部件.输入框.interactable && 部件.面板.activeInHierarchy)
		{
			部件.输入框.ActivateInputField();
		}
	}

	//世界界面和战斗界面各挂一份；对象被销毁（比如重进场景）就重新补
	private void 挂界面()
	{
		if (世界界面 == null) 世界部件 = null;
		if (战斗界面 == null) 战斗部件 = null;
		if (世界界面 == null && 全局变量.主界面UI对象 != null)
		{
			建世界界面();
		}
		if (战斗界面 == null && 全局变量.战斗界面UI对象 != null)
		{
			建战斗界面();
		}
	}

	// ==================== 世界 / 封地界面 ====================

	private void 建世界界面()
	{
		Transform 父物体 = 全局变量.主界面UI对象.transform;
		Vector2 基准 = new Vector2(142.7f, 45f);
		Transform 国家按钮 = 找子物体(父物体, "主界面_国家");
		if (国家按钮 != null)
		{
			RectTransform 国家框 = 国家按钮.GetComponent<RectTransform>();
			if (国家框 != null)
			{
				基准 = 国家框.anchoredPosition;
			}
		}
		GameObject 按钮对象 = 新建节点("主界面_聊天", 父物体, new Vector2(50f, 69f), 基准 + new Vector2(入口按钮间距, 0f), Vector2.zero, Vector2.zero, 居中轴心);
		Image 按钮图 = 按钮对象.AddComponent<Image>();
		Sprite 图标 = 取图("聊天/主界面_聊天", "主界面_聊天");
		按钮图.sprite = 图标;
		按钮图.preserveAspect = true;
		按钮图.color = ((图标 == null) ? 选中字色 : Color.white);
		按钮图.raycastTarget = true;
		Button 按钮 = 按钮对象.AddComponent<Button>();
		按钮.transition = Selectable.Transition.ColorTint;
		按钮.targetGraphic = 按钮图;
		按钮.onClick.AddListener(delegate
		{
			开关面板(世界部件);
		});
		if (国家按钮 != null)
		{
			按钮对象.transform.SetSiblingIndex(国家按钮.GetSiblingIndex() + 1);
		}
		世界界面 = 按钮对象;
		世界部件 = 建聊天面板(父物体, 0.5f, new Vector2(面板横坐标, 面板纵坐标), false);
		世界部件.未读红点 = 建未读红点(按钮对象.transform);
		世界部件.面板.SetActive(value: false);
		刷新未读红点();
	}

	private GameObject 建未读红点(Transform 父物体)
	{
		GameObject 红点 = 新建节点("未读红点", 父物体, new Vector2(20f, 20f), Vector2.zero, Vector2.one, Vector2.one, 居中轴心);
		Image 图 = 红点.AddComponent<Image>();
		图.sprite = 取图("通用界面/军情红点", "军情红点");
		图.type = Image.Type.Simple;
		图.color = 红点色;
		图.raycastTarget = false;
		Text 数字 = 建文本(红点.transform, "数字", "", 12, Color.white, new Vector2(20f, 20f), Vector2.zero, TextAnchor.MiddleCenter, true, 居中锚点, 居中锚点, 居中轴心);
		加描边(数字, new Color(0.42f, 0.05f, 0.04f, 1f), 1f);
		红点.SetActive(value: false);
		return 红点;
	}

	private void 刷新未读红点()
	{
		if (世界部件 != null) 刷新频道高亮(世界部件);
		if (战斗部件 != null) 刷新频道高亮(战斗部件);
		if (世界部件 == null || 世界部件.未读红点 == null)
		{
			return;
		}
		int 条数 = 取未读条数();
		bool 要显示 = 条数 > 0;
		if (世界部件.未读红点.activeSelf != 要显示)
		{
			世界部件.未读红点.SetActive(要显示);
		}
		if (!要显示)
		{
			return;
		}
		Text 数字 = 世界部件.未读红点.GetComponentInChildren<Text>(includeInactive: true);
		if (数字 != null)
		{
			数字.text = ((条数 >= 99) ? "99+" : 条数.ToString());
		}
	}

	// ==================== 战斗界面 ====================

	private void 建战斗界面()
	{
		Transform 父物体 = 全局变量.战斗界面UI对象.transform;
		战斗部件 = 建聊天面板(父物体, 0.5f, new Vector2(0f, 战斗面板纵坐标), false);
		战斗部件.面板.SetActive(value: false);
		战斗界面 = 建战斗播报条(父物体);
	}

	private GameObject 建战斗播报条(Transform 父物体)
	{
		GameObject 条 = 新建节点("战斗聊天播报条", 父物体, new Vector2(播报条宽, 播报条高), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
		Image 条底 = 条.AddComponent<Image>();
		条底.color = 播报条底;
		条底.raycastTarget = true;
		Button 按钮 = 条.AddComponent<Button>();
		按钮.transition = Selectable.Transition.None;
		按钮.targetGraphic = 条底;
		按钮.onClick.AddListener(delegate
		{
			开关面板(战斗部件);
		});
		建纯色块(条.transform, "上线", new Vector2(播报条宽, 2f), new Vector2(0f, 播报条高 - 2f), 金色线);
		建纯色块(条.transform, "下线", new Vector2(播报条宽, 2f), Vector2.zero, 金色线);
		GameObject 图标对象 = 新建节点("聊天图标", 条.transform, new Vector2(44f, 52f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
		Image 图标图 = 图标对象.AddComponent<Image>();
		Sprite 图标 = 取图("聊天/主界面_聊天", "主界面_聊天");
		图标图.sprite = 图标;
		图标图.preserveAspect = true;
		图标图.color = ((图标 == null) ? 选中字色 : Color.white);
		图标图.raycastTarget = false;
		Text 文本 = 建文本(条.transform, "播报文本", "暂无播报", 20, 青字, new Vector2(播报条宽 - 56f, 播报条高 - 4f), new Vector2(52f, 2f), TextAnchor.MiddleLeft, true, Vector2.zero, Vector2.zero, Vector2.zero);
		文本.horizontalOverflow = HorizontalWrapMode.Wrap;
		文本.verticalOverflow = VerticalWrapMode.Truncate;
		加描边(文本, 描边色, 1.6f);
		战斗播报条对象 = 条;
		条.SetActive(PlayerPrefs.GetInt(播报条存档键, 1) != 0);
		if (战斗部件 != null)
		{
			战斗部件.播报文本 = 文本;
		}
		return 条;
	}

	//播报条只显示最新一条，内容过长就自动换行截断
	private void 刷新战斗条()
	{
		if (战斗部件 == null || 战斗部件.播报文本 == null)
		{
			return;
		}
		string 文本 = "暂无播报";
		for (int i = 全部消息.Count - 1; i >= 0; i--)
		{
			if (当前频道 != 聊天频道.全部 && 全部消息[i].频道 != 当前频道)
			{
				continue;
			}
			文本 = 全部消息[i].内容;
			break;
		}
		if (战斗部件.播报文本.text != 文本)
		{
			战斗部件.播报文本.text = 文本;
		}
	}

	// ==================== 面板 ====================

	private 面板部件 建聊天面板(Transform 父物体, float 缩放, Vector2 位置, bool 默认打开)
	{
		面板部件 部件 = new 面板部件();
		GameObject 面板 = 新建节点("聊天面板", 父物体, new Vector2(面板宽, 面板高), 位置, Vector2.zero, Vector2.zero, Vector2.zero);
		面板.transform.localScale = new Vector3(缩放, 缩放, 1f);
		部件.面板 = 面板;

		// 底板：通用绿色背景 + 金色四角边框
		GameObject 底板 = 新建节点("面板底", 面板.transform, new Vector2(面板宽, 面板高), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
		Image 底板图 = 底板.AddComponent<Image>();
		底板图.sprite = 取图("7966.dat", "7966.dat");
		底板图.type = Image.Type.Simple;
		底板图.color = ((底板图.sprite == null) ? new Color(0.071f, 0.110f, 0.090f, 0.98f) : Color.white);
		底板图.raycastTarget = true;
		建边框(底板.transform);

		// 左侧频道列
		for (int i = 0; i < 频道名称.Length; i++)
		{
			int 索引 = i;
			Button 频道按钮 = 建频道按钮(面板.transform, 频道名称[i], 频道首个纵坐标 - 频道间距 * (float)i);
			频道按钮.onClick.AddListener(delegate
			{
				切换频道((聊天频道)索引);
			});
			部件.频道按钮.Add(频道按钮);
			部件.频道文字.Add(频道按钮.GetComponentInChildren<Text>(includeInactive: true));
		}

		// 消息区（可滚动的列表）
		GameObject 视口 = 新建节点("消息视口", 面板.transform, new Vector2(消息视口宽, 面板高 - 消息区上留白 - 消息区下留白), new Vector2(消息区左, 消息区下留白), Vector2.zero, Vector2.zero, Vector2.zero);
		Image 视口图 = 视口.AddComponent<Image>();
		视口图.color = new Color(0f, 0f, 0f, 0f);
		视口图.raycastTarget = true;
		视口.AddComponent<RectMask2D>();
		ScrollRect 滚动 = 视口.AddComponent<ScrollRect>();
		滚动.horizontal = false;
		滚动.vertical = true;
		滚动.movementType = ScrollRect.MovementType.Clamped;
		滚动.scrollSensitivity = 32f;
		滚动.viewport = (RectTransform)视口.transform;
		GameObject 内容 = 新建节点("消息列表", 视口.transform, Vector2.zero, Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
		VerticalLayoutGroup 排布 = 内容.AddComponent<VerticalLayoutGroup>();
		排布.spacing = 8f;
		排布.padding = new RectOffset((int)内容留白, (int)内容留白, 6, 6);
		排布.childAlignment = TextAnchor.UpperLeft;
		排布.childControlWidth = true;
		排布.childControlHeight = true;
		排布.childForceExpandWidth = true;
		排布.childForceExpandHeight = false;
		ContentSizeFitter 适配 = 内容.AddComponent<ContentSizeFitter>();
		适配.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		适配.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		滚动.content = (RectTransform)内容.transform;
		部件.滚动 = 滚动;
		部件.内容 = (RectTransform)内容.transform;

		// 左下角返回箭头
		Button 返回按钮 = 建按钮(面板.transform, "返回", "", 取图("873.dat", "873.dat"), new Vector2(52f, 52f), new Vector2(8f, 26f), 16, Vector2.zero, Vector2.zero, Vector2.zero);
		返回按钮.onClick.AddListener(delegate
		{
			部件.面板.SetActive(value: false);
		});

		// 设置按钮 + 设置小面板
		Button 设置按钮 = 建按钮(面板.transform, "设置", "设 置", 取图("7992.dat", "7992.dat"), new Vector2(86f, 40f), new Vector2(150f, 22f), 16, Vector2.zero, Vector2.zero, Vector2.zero);
		设置按钮.onClick.AddListener(delegate
		{
			if (部件.设置面板 != null)
			{
				部件.设置面板.SetActive(!部件.设置面板.activeSelf);
			}
		});
		建设置面板(部件);

		// 底部输入行：输入框 + 发 送
		建输入行(部件);

		// 空频道时中间那行提示
		建空提示(部件);

		刷新输入行(部件);

		刷新频道高亮(部件);
		部件.面板.SetActive(默认打开);
		return 部件;
	}

	private void 建设置面板(面板部件 部件)
	{
		GameObject 面板 = 新建节点("聊天设置", 部件.面板.transform, new Vector2(240f, 146f), new Vector2(148f, 68f), Vector2.zero, Vector2.zero, Vector2.zero);
		Image 底 = 面板.AddComponent<Image>();
		底.color = new Color(0.055f, 0.078f, 0.063f, 0.98f);
		底.raycastTarget = true;
		加细边框(面板.transform, 240f, 146f, 金色线, 1f);
		Sprite 按钮底 = 取图("7992.dat", "7992.dat");
		Button 播报条开关 = 建按钮(面板.transform, "播报条开关", 播报条文案(), 按钮底, new Vector2(224f, 36f), new Vector2(8f, -8f), 15, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		播报条开关.onClick.AddListener(切换播报条);
		Button 清空按钮 = 建按钮(面板.transform, "清空聊天", "清空聊天", 按钮底, new Vector2(224f, 36f), new Vector2(8f, -50f), 15, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		清空按钮.onClick.AddListener(delegate
		{
			清空聊天();
			已读序号 = 自增序号;
			刷新未读红点();
		});
		Button 关闭按钮 = 建按钮(面板.transform, "关闭", "关 闭", 按钮底, new Vector2(224f, 36f), new Vector2(8f, -92f), 15, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		关闭按钮.onClick.AddListener(delegate
		{
			面板.SetActive(value: false);
		});
		部件.设置面板 = 面板;
		部件.播报条开关文字 = 播报条开关.GetComponentInChildren<Text>(includeInactive: true);
		面板.SetActive(value: false);
	}

	//底部输入行：黑底金边的输入框 + 「发 送」。提示灰字是自己管的（不挂给 InputField），
	//免得跟着它内部那套 placeholder 刷新时机抖。
	private void 建输入行(面板部件 部件)
	{
		GameObject 输入框对象 = 新建节点("聊天输入", 部件.面板.transform, new Vector2(输入框宽, 输入行高), new Vector2(输入框左, 输入行纵坐标), Vector2.zero, Vector2.zero, Vector2.zero);
		Image 输入底 = 输入框对象.AddComponent<Image>();
		输入底.color = 消息框底;
		输入底.raycastTarget = true;
		加细边框(输入框对象.transform, 输入框宽, 输入行高, new Color(0.451f, 0.396f, 0.235f, 0.9f), 1f);
		Text 提示 = 建文本(输入框对象.transform, "提示", "", 15, 未选中字色, new Vector2(输入框宽 - 20f, 输入行高), new Vector2(10f, 0f), TextAnchor.MiddleLeft, false, Vector2.zero, Vector2.zero, Vector2.zero);
		Text 文字 = 建文本(输入框对象.transform, "输入文字", "", 16, 青字, new Vector2(输入框宽 - 20f, 输入行高), new Vector2(10f, 0f), TextAnchor.MiddleLeft, false, Vector2.zero, Vector2.zero, Vector2.zero);
		文字.horizontalOverflow = HorizontalWrapMode.Overflow;
		文字.verticalOverflow = VerticalWrapMode.Truncate;
		InputField 输入 = 输入框对象.AddComponent<InputField>();
		输入.targetGraphic = 输入底;
		输入.textComponent = 文字;
		输入.lineType = InputField.LineType.SingleLine;
		输入.characterLimit = 单条字数上限;
		输入.customCaretColor = true;
		输入.caretColor = 青字;
		输入.selectionColor = new Color(0.145f, 0.4f, 0.353f, 0.6f);
		输入.onValueChanged.AddListener(delegate(string 文本)
		{
			刷新提示(部件);
		});
		部件.输入框 = 输入;
		部件.输入提示 = 提示;
		Button 发送 = 建按钮(部件.面板.transform, "发送", "发 送", 取图("7992.dat", "7992.dat"), new Vector2(发送按钮宽, 输入行高), new Vector2(发送按钮左, 输入行纵坐标), 16, Vector2.zero, Vector2.zero, Vector2.zero);
		发送.onClick.AddListener(delegate
		{
			发送输入(部件);
		});
		部件.发送按钮 = 发送;
	}

	//切频道时同步输入行：能说话的换提示文字，只读频道把输入框锁上
	private void 刷新输入行(面板部件 部件)
	{
		if (部件 == null || 部件.输入框 == null)
		{
			return;
		}
		聊天频道 落点;
		bool 可发言 = 取发言落点(当前频道, out 落点);
		if (部件.输入框.interactable != 可发言)
		{
			部件.输入框.interactable = 可发言;
		}
		if (部件.输入框.isFocused && !可发言)
		{
			部件.输入框.DeactivateInputField();
		}
		if (部件.发送按钮 != null && 部件.发送按钮.interactable != 可发言)
		{
			部件.发送按钮.interactable = 可发言;
		}
		string 提示 = 取输入提示(当前频道);
		if (部件.输入提示 != null && 部件.输入提示.text != 提示)
		{
			部件.输入提示.text = 提示;
		}
		刷新提示(部件);
	}

	//输入框空着才显示提示文字
	private void 刷新提示(面板部件 部件)
	{
		if (部件 == null || 部件.输入提示 == null || 部件.输入框 == null)
		{
			return;
		}
		bool 显示 = string.IsNullOrEmpty(部件.输入框.text);
		if (部件.输入提示.gameObject.activeSelf != 显示)
		{
			部件.输入提示.gameObject.SetActive(显示);
		}
	}

	//消息区中间那行「暂无消息」
	private void 建空提示(面板部件 部件)
	{
		float 视口高 = 面板高 - 消息区上留白 - 消息区下留白;
		Text 空提示 = 建文本(部件.面板.transform, "空提示", "", 17, 未选中字色, new Vector2(消息视口宽, 视口高), new Vector2(消息区左, 消息区下留白), TextAnchor.MiddleCenter, false, Vector2.zero, Vector2.zero, Vector2.zero);
		空提示.gameObject.SetActive(value: false);
		部件.空提示 = 空提示;
	}

	private void 刷新空提示(面板部件 部件, bool 显示)
	{
		if (部件 == null || 部件.空提示 == null)
		{
			return;
		}
		if (部件.空提示.gameObject.activeSelf != 显示)
		{
			部件.空提示.gameObject.SetActive(显示);
		}
		if (!显示)
		{
			return;
		}
		聊天频道 落点;
		bool 可发言 = 取发言落点(当前频道, out 落点);
		string 文案 = ((当前频道 == 聊天频道.全部) ? "还没有任何消息" : ("「" + 频道名称[(int)当前频道] + "」还没有消息"));
		if (可发言)
		{
			文案 += "，可以在下面说话";
		}
		部件.空提示.text = 文案;
	}

	private static string 播报条文案()
	{
		return ((PlayerPrefs.GetInt(播报条存档键, 1) != 0) ? "战斗播报条：开" : "战斗播报条：关");
	}

	private void 切换播报条()
	{
		bool 要开 = PlayerPrefs.GetInt(播报条存档键, 1) == 0;
		PlayerPrefs.SetInt(播报条存档键, 要开 ? 1 : 0);
		if (战斗播报条对象 != null)
		{
			战斗播报条对象.SetActive(要开);
		}
		string 文案 = 播报条文案();
		if (世界部件 != null && 世界部件.播报条开关文字 != null)
		{
			世界部件.播报条开关文字.text = 文案;
		}
		if (战斗部件 != null && 战斗部件.播报条开关文字 != null)
		{
			战斗部件.播报条开关文字.text = 文案;
		}
	}

	private void 开关面板(面板部件 部件)
	{
		if (部件 == null || 部件.面板 == null)
		{
			return;
		}
		bool 要打开 = !部件.面板.activeSelf;
		部件.面板.SetActive(要打开);
		if (要打开)
		{
			已读序号 = 自增序号;
			部件.已刷新版本 = -1;
			刷新输入行(部件);
			刷新未读红点();
			同步面板(部件);
		}
		else if (部件.设置面板 != null)
		{
			部件.设置面板.SetActive(value: false);
		}
	}

	private void 切换频道(聊天频道 频道)
	{
		当前频道 = 频道;
		PlayerPrefs.SetInt(频道存档键, (int)频道);
		刷新战斗条();
		刷新输入行(世界部件);
		刷新输入行(战斗部件);
		同步面板(世界部件);
		同步面板(战斗部件);
	}

	private void 同步面板(面板部件 部件)
	{
		if (部件 == null || 部件.面板 == null || 部件.内容 == null)
		{
			return;
		}
		if (!部件.面板.activeInHierarchy)
		{
			部件.已刷新版本 = -1;
			return;
		}
		if (部件.已刷新版本 != 数据版本 || 部件.已刷新频道 != 当前频道)
		{
			部件.已刷新版本 = 数据版本;
			部件.已刷新频道 = 当前频道;
			刷新频道高亮(部件);
			重建消息(部件);
		}
	}

	private void 刷新频道高亮(面板部件 部件)
	{
		if (部件 == null || 部件.面板 == null)
		{
			return;
		}
		for (int i = 0; i < 部件.频道按钮.Count; i++)
		{
			if (部件.频道按钮[i] == null) continue;
			bool 选中 = ((int)当前频道 == i);
			Image 图 = 部件.频道按钮[i].GetComponent<Image>();
			if (图 != null)
			{
				图.color = (选中 ? 频道选中底 : 频道普通底);
			}
			if (i < 部件.频道文字.Count && 部件.频道文字[i] != null)
			{
				if (i == (int)聊天频道.系统)
				{
					int 未读 = ChatClient.UnreadNotifications;
					部件.频道文字[i].text = 未读 > 0 ? "系统(" + (未读 > 99 ? "99+" : 未读.ToString()) + ")" : "系统";
				}
				部件.频道文字[i].color = (选中 ? 选中字色 : 未选中字色);
			}
		}
	}

	private void 重建消息(面板部件 部件)
	{
		部件.通知行.Clear();
		for (int i = 部件.内容.childCount - 1; i >= 0; i--)
		{
			GameObject 旧行 = 部件.内容.GetChild(i).gameObject;
			旧行.SetActive(value: false);
			UnityEngine.Object.Destroy(旧行);
		}
		List<聊天消息> 待显示 = new List<聊天消息>();
		for (int j = 0; j < 全部消息.Count; j++)
		{
			if (当前频道 != 聊天频道.全部 && 全部消息[j].频道 != 当前频道)
			{
				continue;
			}
			待显示.Add(全部消息[j]);
		}
		if (待显示.Count > 单页显示上限) 待显示.RemoveRange(0, 待显示.Count - 单页显示上限);
		for (int k = 0; k < 待显示.Count; k++)
		{
			建消息行(部件, 待显示[k]);
		}
		if (部件.滚动 != null && 待显示.Count > 0)
		{
			Canvas.ForceUpdateCanvases();
			部件.滚动.verticalNormalizedPosition = 0f;
		}
		刷新空提示(部件, 待显示.Count == 0);
	}

	private void 标记可见通知(面板部件 部件)
	{
		if (!GameNetwork.Enabled || 部件 == null || 部件.面板 == null || !部件.面板.activeInHierarchy || 部件.滚动 == null || 部件.滚动.viewport == null ||
			(当前频道 != 聊天频道.全部 && 当前频道 != 聊天频道.系统)) return;
		List<string> 可见通知 = new List<string>();
		Rect 视口 = 部件.滚动.viewport.rect;
		foreach (var 行 in 部件.通知行)
		{
			if (行.Key == null) continue;
			Bounds 范围 = RectTransformUtility.CalculateRelativeRectTransformBounds(部件.滚动.viewport, 行.Key);
			if (范围.min.y < 视口.yMax && 范围.max.y > 视口.yMin && 范围.min.x < 视口.xMax && 范围.max.x > 视口.xMin) 可见通知.Add(行.Value);
		}
		ChatClient.MarkNotificationsRead(可见通知);
	}

	private void 建消息行(面板部件 部件, 聊天消息 消息)
	{
		GameObject 行 = 新建节点("消息", 部件.内容, new Vector2(行宽, 60f), Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
		if (!string.IsNullOrEmpty(消息.通知ID)) 部件.通知行[(RectTransform)行.transform] = 消息.通知ID;
		LayoutElement 占位 = 行.AddComponent<LayoutElement>();
		占位.preferredHeight = 60f;

		// 头像
		GameObject 头像对象 = 新建节点("头像", 行.transform, new Vector2(头像尺寸, 头像尺寸), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		Image 头像图 = 头像对象.AddComponent<Image>();
		Sprite 头像资源 = 取图("主界面_头像2", "主界面_头像2");
		头像图.sprite = 头像资源;
		头像图.type = Image.Type.Simple;
		头像图.color = ((头像资源 == null) ? new Color(0.24f, 0.26f, 0.24f, 1f) : Color.white);
		头像图.raycastTarget = false;
		加细边框(头像对象.transform, 头像尺寸, 头像尺寸, new Color(0.451f, 0.396f, 0.235f, 0.9f), 1f);

		// 发送者（自己说的：名字用青色并缀「我」）
		Text 名字 = 建文本(行.transform, "名字", (消息.自己 ? (消息.发送者 + "（我）") : 消息.发送者), 18, (消息.自己 ? 青字 : 名字色), new Vector2(行宽 - 行文字左偏移, 名字行高), new Vector2(行文字左偏移, 0f), TextAnchor.MiddleLeft, true, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		加描边(名字, 描边色, 1.4f);

		// 正文先建出来量高度，量完再塞进消息框里
		GameObject 正文对象 = 新建节点("正文", 行.transform, new Vector2(行文本宽, 24f), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		Text 正文文本 = 正文对象.AddComponent<Text>();
		正文文本.font = 取字体();
		正文文本.fontSize = 18;
		正文文本.fontStyle = FontStyle.Normal;
		正文文本.alignment = TextAnchor.UpperLeft;
		正文文本.color = (消息.自己 ? 选中字色 : 青字);
		正文文本.text = 消息.内容;
		正文文本.raycastTarget = false;
		正文文本.supportRichText = false;
		正文文本.horizontalOverflow = HorizontalWrapMode.Wrap;
		正文文本.verticalOverflow = VerticalWrapMode.Overflow;
		加描边(正文文本, 描边色, 1.2f);
		float 文本高 = 正文文本.preferredHeight;
		if (float.IsNaN(文本高) || float.IsInfinity(文本高) || 文本高 < 18f)
		{
			文本高 = 18f;
		}
		float 框高 = 文本高 + 框内边距 * 2f;

		// 黑底金边的消息框
		float 框宽 = 行宽 - 行文字左偏移;
		GameObject 框对象 = 新建节点("消息框", 行.transform, new Vector2(框宽, 框高), new Vector2(行文字左偏移, -(名字行高 + 2f)), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		Image 框图 = 框对象.AddComponent<Image>();
		框图.color = (消息.自己 ? 自己框底 : 消息框底);
		框图.raycastTarget = false;
		加细边框(框对象.transform, 框宽, 框高, new Color(0.451f, 0.396f, 0.235f, 0.9f), 1f);

		// 正文挂到框里（挂到框底下，这样一定画在框背景上面）
		RectTransform 正文框 = (RectTransform)正文对象.transform;
		正文框.SetParent(框对象.transform, worldPositionStays: false);
		正文框.anchorMin = new Vector2(0f, 1f);
		正文框.anchorMax = new Vector2(0f, 1f);
		正文框.pivot = new Vector2(0f, 1f);
		正文框.anchoredPosition = new Vector2(框内边距, -框内边距);
		正文框.sizeDelta = new Vector2(行文本宽, 文本高);

		float 行高 = 名字行高 + 2f + 框高 + 6f;
		if (行高 < 头像尺寸 + 6f)
		{
			行高 = 头像尺寸 + 6f;
		}
		占位.preferredHeight = 行高;
		((RectTransform)行.transform).sizeDelta = new Vector2(0f, 行高);
	}

	// ==================== 小零件 ====================

	private static readonly Vector2 居中锚点 = new Vector2(0.5f, 0.5f);

	private static readonly Vector2 居中轴心 = new Vector2(0.5f, 0.5f);

	private Button 建频道按钮(Transform 父物体, string 名字, float 纵坐标)
	{
		GameObject 物体 = 新建节点("频道_" + 名字, 父物体, new Vector2(频道按钮宽, 频道按钮高), new Vector2(8f, 纵坐标), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
		Image 底 = 物体.AddComponent<Image>();
		底.color = 频道普通底;
		底.raycastTarget = true;
		Button 按钮 = 物体.AddComponent<Button>();
		按钮.transition = Selectable.Transition.None;
		按钮.targetGraphic = 底;
		建纯色块(物体.transform, "金线", new Vector2(频道按钮宽 - 8f, 2f), new Vector2(4f, 3f), 金色线);
		Text 文本 = 建文本(物体.transform, "文字", 名字, 16, 未选中字色, new Vector2(频道按钮宽, 频道按钮高), Vector2.zero, TextAnchor.MiddleCenter, true, 居中锚点, 居中锚点, 居中轴心);
		加描边(文本, 描边色, 1.2f);
		return 按钮;
	}

	private static Button 建按钮(Transform 父物体, string 名字, string 文案, Sprite 底图, Vector2 尺寸, Vector2 位置, int 字号, Vector2 锚点最小, Vector2 锚点最大, Vector2 轴心)
	{
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, 锚点最小, 锚点最大, 轴心);
		Image 图 = 物体.AddComponent<Image>();
		图.sprite = 底图;
		图.type = Image.Type.Simple;
		图.color = ((底图 == null) ? new Color(0.110f, 0.310f, 0.278f, 1f) : Color.white);
		图.raycastTarget = true;
		Button 按钮 = 物体.AddComponent<Button>();
		按钮.transition = Selectable.Transition.ColorTint;
		按钮.targetGraphic = 图;
		if (!string.IsNullOrEmpty(文案))
		{
			Text 文本 = 建文本(物体.transform, "文字", 文案, 字号, 选中字色, new Vector2(尺寸.x, 尺寸.y), Vector2.zero, TextAnchor.MiddleCenter, true, 居中锚点, 居中锚点, 居中轴心);
			加描边(文本, 描边色, 1.2f);
		}
		return 按钮;
	}

	private static Image 建纯色块(Transform 父物体, string 名字, Vector2 尺寸, Vector2 位置, Color 颜色)
	{
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, Vector2.zero, Vector2.zero, Vector2.zero);
		Image 图 = 物体.AddComponent<Image>();
		图.color = 颜色;
		图.raycastTarget = false;
		return 图;
	}

	private static void 加细边框(Transform 父物体, float 宽, float 高, Color 颜色, float 厚度)
	{
		建纯色块(父物体, "框上", new Vector2(宽, 厚度), new Vector2(0f, 高 - 厚度), 颜色);
		建纯色块(父物体, "框下", new Vector2(宽, 厚度), Vector2.zero, 颜色);
		建纯色块(父物体, "框左", new Vector2(厚度, 高), Vector2.zero, 颜色);
		建纯色块(父物体, "框右", new Vector2(厚度, 高), new Vector2(宽 - 厚度, 0f), 颜色);
	}

	//通用背景边框（和场景里「增援界面UI / 通用背景边框」用同一套图：四角 7967、上下 7968、左右 7969，
	//右上/左下/右下靠负缩放镜像）
	private void 建边框(Transform 父物体)
	{
		Sprite 角图 = 取图("7967.dat", "7967.dat");
		Sprite 横图 = 取图("7968.dat", "7968.dat");
		Sprite 竖图 = 取图("7969.dat", "7969.dat");
		Vector2 角尺寸 = new Vector2(边框厚度, 边框厚度);
		if (角图 != null)
		{
			建图(父物体, "边框左上", 角图, 角尺寸, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector3(1f, 1f, 1f));
			建图(父物体, "边框右上", 角图, 角尺寸, Vector2.zero, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector3(-1f, 1f, 1f));
			建图(父物体, "边框左下", 角图, 角尺寸, Vector2.zero, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector3(1f, -1f, 1f));
			建图(父物体, "边框右下", 角图, 角尺寸, Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector3(-1f, -1f, 1f));
		}
		Vector2 横尺寸 = new Vector2(边框厚度 * -2f, 边框厚度);
		Vector2 竖尺寸 = new Vector2(边框厚度, 边框厚度 * -2f);
		if (横图 != null)
		{
			建图(父物体, "边框上", 横图, 横尺寸, Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector3(1f, 1f, 1f));
			建图(父物体, "边框下", 横图, 横尺寸, Vector2.zero, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector3(1f, -1f, 1f));
		}
		if (竖图 != null)
		{
			建图(父物体, "边框左", 竖图, 竖尺寸, Vector2.zero, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector3(1f, 1f, 1f));
			建图(父物体, "边框右", 竖图, 竖尺寸, Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector3(-1f, 1f, 1f));
		}
	}

	private static Image 建图(Transform 父物体, string 名字, Sprite 图, Vector2 尺寸, Vector2 位置, Vector2 锚点最小, Vector2 锚点最大, Vector2 轴心, Vector3 缩放)
	{
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, 锚点最小, 锚点最大, 轴心);
		物体.transform.localScale = 缩放;
		Image 图片 = 物体.AddComponent<Image>();
		图片.sprite = 图;
		图片.type = Image.Type.Simple;
		图片.raycastTarget = false;
		return 图片;
	}

	private static Text 建文本(Transform 父物体, string 名字, string 内容, int 字号, Color 颜色, Vector2 尺寸, Vector2 位置, TextAnchor 对齐, bool 粗体, Vector2 锚点最小, Vector2 锚点最大, Vector2 轴心)
	{
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, 锚点最小, 锚点最大, 轴心);
		Text 文本 = 物体.AddComponent<Text>();
		文本.text = 内容;
		文本.font = 取字体();
		文本.fontSize = 字号;
		文本.fontStyle = (粗体 ? FontStyle.Bold : FontStyle.Normal);
		文本.alignment = 对齐;
		文本.color = 颜色;
		文本.raycastTarget = false;
		文本.supportRichText = false;
		文本.horizontalOverflow = HorizontalWrapMode.Overflow;
		文本.verticalOverflow = VerticalWrapMode.Overflow;
		return 文本;
	}

	private static GameObject 新建节点(string 名字, Transform 父物体, Vector2 尺寸, Vector2 位置, Vector2 锚点最小, Vector2 锚点最大, Vector2 轴心)
	{
		GameObject 物体 = new GameObject(名字, typeof(RectTransform));
		RectTransform 框 = 物体.GetComponent<RectTransform>();
		框.SetParent(父物体, worldPositionStays: false);
		框.localRotation = Quaternion.identity;
		框.localScale = Vector3.one;
		框.anchorMin = 锚点最小;
		框.anchorMax = 锚点最大;
		框.pivot = 轴心;
		框.anchoredPosition = 位置;
		框.sizeDelta = 尺寸;
		return 物体;
	}

	private static void 加描边(Graphic 图形, Color 颜色, float 距离)
	{
		if (图形 == null)
		{
			return;
		}
		Outline 描边 = 图形.gameObject.AddComponent<Outline>();
		描边.effectColor = 颜色;
		描边.effectDistance = new Vector2(距离, -距离);
		描边.useGraphicAlpha = true;
	}

	private static Transform 找子物体(Transform 父物体, string 名字)
	{
		foreach (Transform 子物体 in 父物体.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (子物体 != 父物体 && 子物体.name == 名字)
			{
				return 子物体;
			}
		}
		return null;
	}

	// ==================== 找图 / 找字体 ====================

	//先按 Resources 路径找；找不到就退一步，按贴图名在已加载的精灵里翻（工程里的图名就是文件名去掉 .png）
	private static Sprite 取图(string 资源路径, string 精灵名)
	{
		Sprite 图 = Resources.Load<Sprite>(资源路径);
		if (图 != null)
		{
			return 图;
		}
		if (素材表 == null)
		{
			素材表 = new Dictionary<string, Sprite>();
			foreach (Sprite 精灵 in Resources.FindObjectsOfTypeAll<Sprite>())
			{
				if (精灵 == null || string.IsNullOrEmpty(精灵.name) || 素材表.ContainsKey(精灵.name))
				{
					continue;
				}
				素材表.Add(精灵.name, 精灵);
			}
		}
		Sprite 结果;
		if (素材表.TryGetValue(精灵名, out 结果))
		{
			return 结果;
		}
		foreach (KeyValuePair<string, Sprite> 条目 in 素材表)
		{
			if (条目.Key.StartsWith(精灵名))
			{
				return 条目.Value;
			}
		}
		return null;
	}

	//中文字体：借界面上任意一个 Text 正在用的字体，拿不到再退到 Unity 内置字体
	private static Font 取字体()
	{
		if (界面字体 != null)
		{
			return 界面字体;
		}
		foreach (Text 文本 in Resources.FindObjectsOfTypeAll<Text>())
		{
			if (文本 != null && 文本.font != null)
			{
				界面字体 = 文本.font;
				return 界面字体;
			}
		}
		try
		{
			界面字体 = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		}
		catch (System.Exception)
		{
			界面字体 = null;
		}
		if (界面字体 == null)
		{
			try
			{
				界面字体 = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
			catch (System.Exception)
			{
				界面字体 = null;
			}
		}
		return 界面字体;
	}
}
