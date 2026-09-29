using System.Linq;
using Dwsg.Window1;
using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

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

	private Vector2 声望条原尺寸;
	private bool 已记录声望条;
	private Text 国库铜钱;
	private Text 国库粮食;
	private Text 俸禄倒计时;
	private Button 领取俸禄按钮;

	private void OnEnable()
	{
		// 场景还将本组件用于两个未绑定信息字段的资源页，只刷新完整君主资料页。
		if (君主名字 == null) return;
		国库铜钱 = 查找俸禄文本("铜钱显示");
		国库粮食 = 查找俸禄文本("粮食显示");
		俸禄倒计时 = 查找俸禄文本("倒计时");
		var 按钮节点 = transform.Find("产能信息布局/国家俸禄布局/领取俸禄");
		领取俸禄按钮 = 按钮节点 == null ? null : 按钮节点.GetComponent<Button>();
		// 原按钮保留显示国库脚本.领取俸禄的场景绑定，此处只在领取动作后刷新资料。
		if (领取俸禄按钮 != null)
		{
			领取俸禄按钮.onClick.RemoveListener(刷新显示);
			领取俸禄按钮.onClick.AddListener(刷新显示);
		}
		刷新显示();
		CancelInvoke(nameof(刷新俸禄显示));
		InvokeRepeating(nameof(刷新俸禄显示), 1f, 1f);
	}

	private void OnDisable()
	{
		CancelInvoke(nameof(刷新俸禄显示));
		if (领取俸禄按钮 != null) 领取俸禄按钮.onClick.RemoveListener(刷新显示);
	}

	private Text 查找俸禄文本(string 名字)
	{
		var 节点 = transform.Find("产能信息布局/国家俸禄布局/" + 名字);
		return 节点 == null ? null : 节点.GetComponent<Text>();
	}

	private static void 填写(Text label, string value) { if (label != null) label.text = value; }

	private void 刷新俸禄显示()
	{
		var 玩家 = ExistingWorldAdapter.CurrentPlayer;
		var 数据 = NationDataSource.Current;
		if (玩家 == null || 玩家.基础信息 == null || 数据 == null)
		{
			填写(国库铜钱, "--"); 填写(国库粮食, "--"); 填写(俸禄倒计时, "--:--:--");
			return;
		}
		var 国家 = 数据.ReadNation(玩家.基础信息.国家);
		填写(国库铜钱, 国家 == null ? "无国家" : NationDataSource.Number(国家.Copper));
		填写(国库粮食, 国家 == null ? "无国家" : NationDataSource.Number(国家.Grain));
		填写(俸禄倒计时, TIME.ToTimeFormat(System.Math.Max(0, 全局变量.领取倒计时)));
	}

	public void 刷新显示()
	{
		刷新俸禄显示();
		var player = ExistingWorldAdapter.CurrentPlayer;
		if (player == null || player.基础信息 == null) return;
		var info = player.基础信息;
		填写(君主名字, info.名字);
		填写(君主等级, info.等级 + "级");
		填写(称号, string.IsNullOrEmpty(info.称号名) || info.称号名 == "无" ? "无称号" : "<" + info.称号名 + ">");
		填写(贡献, info.贡献.ToString());
		填写(战功, info.战功.ToString());
		填写(产铜, player.获取铜钱产量() + "/小时");
		填写(产粮, player.获取粮食产量() + "/小时");
		填写(将领数量, player.获取将领总数() + "/" + info.将领数上限);
		填写(城池数量, 全局变量.所有城池列表 == null ? "--" : 全局变量.所有城池列表.Count(c => c != null && c.城主 == 全局变量.本机身份).ToString());
		填写(封地数量, (player.封地信息表 == null ? 0 : player.封地信息表.Count) + "/10");
		填写(官阶, info.官职.ToString());
		填写(排名, "--");
        填写(资源点数量, Dwsg.Window3.资源点规则.本地.已占数量(info.ID).ToString());
		if (君主头像 != null && 全局变量.所有头像资源表 != null && info.头像 >= 0 && info.头像 < 全局变量.所有头像资源表.Count && 全局变量.所有头像资源表[info.头像] != null)
			君主头像.sprite = 全局变量.所有头像资源表[info.头像];
		float progress = info.获取当前等级经验条比例();
		progress = float.IsNaN(progress) || float.IsInfinity(progress) ? 0 : Mathf.Clamp01(progress);
		if (声望条显示 != null)
		{
			if (!已记录声望条) { 声望条原尺寸 = 声望条显示.sizeDelta; 已记录声望条 = true; }
			声望条显示.sizeDelta = new Vector2(声望条原尺寸.x * progress, 声望条原尺寸.y);
		}
		填写(声望, info.声望 + "/" + info.获取当前等级升级所需经验() + "(" + Mathf.Floor(progress * 100) + "%)");
	}

	public void 增加经验()
	{
		// 保留旧回调签名；当前场景没有此按钮，不再通过资料页发放测试经验。
		刷新显示();
	}
}
