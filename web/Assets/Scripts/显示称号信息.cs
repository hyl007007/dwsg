using UnityEngine;
using UnityEngine.UI;
using Dwsg.Network;
using Dwsg.Auxiliary;
using Dwsg.Shared;

public class 显示称号信息 : MonoBehaviour
{
	public Text 当前称号;

	public Text 称号达成;

	public Text 页数显示;

	public GameObject 称号列表对象;

	public long 上次切换时间;

	public int 显示第几页 = 1;

	private float 总页数;

	private int 第几个玩家;
	private bool 联机切换中;
	private string 联机称号签名;
	private void OnEnable() { GameNetwork.SnapshotReceived -= 联机快照更新; GameNetwork.SnapshotReceived += 联机快照更新; }
	private void OnDisable() { GameNetwork.SnapshotReceived -= 联机快照更新; }
	private void 联机快照更新(WorldSnapshot snapshot)
	{
		if (!GameNetwork.Enabled || !isActiveAndEnabled || snapshot == null || !GameNetwork.HasRole) return;
		string 签名 = snapshot.PrivatePlayer["称号信息表"]?.ToString(Newtonsoft.Json.Formatting.None);
		if (联机称号签名 == 签名) return;
		联机称号签名 = 签名; 刷新显示();
	}

	public void 刷新显示()
	{
		第几个玩家 = 全局变量.本机身份;
		int count = 全局变量.所有玩家数据表[第几个玩家].称号信息表.Count;
		总页数 = Mathf.Max(1, Mathf.CeilToInt(count / 12f));
		显示第几页 = Mathf.Clamp(显示第几页, 1, (int)总页数);
		int num = (显示第几页 - 1) * 12;
		for (int i = 0; i < 12; i++)
		{
			称号列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
			if (num + i >= count)
			{
				continue;
			}
			string 名字 = 全局变量.所有玩家数据表[第几个玩家].称号信息表[num + i].名字;
			int num2 = 全局变量.称号头像资源表.Length;
			for (int j = 0; j < num2; j++)
			{
				if (全局变量.称号头像资源表[j].name == 名字)
				{
					称号列表对象.transform.GetChild(i).GetChild(1).GetComponent<Image>()
						.sprite = 全局变量.称号头像资源表[j];
						break;
					}
				}
				Text component = 称号列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>();
				component.text = 名字;
				component.color = 全局变量.所有玩家数据表[第几个玩家].称号信息表[num + i].获取称号颜色();
				int 等级 = 全局变量.所有玩家数据表[第几个玩家].称号信息表[num + i].等级;
				if (全局变量.所有玩家数据表[第几个玩家].称号信息表[num + i].状态 == 0)
				{
					称号列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: true);
				}
				else
				{
					称号列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
				}
				if (全局变量.所有玩家数据表[第几个玩家].称号信息表[num + i].类型 == 1)
				{
					称号列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: true);
				}
				else
				{
					称号列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: false);
				}
				称号列表对象.transform.GetChild(i).gameObject.SetActive(value: true);
			}
			当前称号.text = "<无>";
			当前称号.color = 颜色类.GetColor("#FDA400");
			int num3 = 0;
			for (int k = 0; k < count; k++)
			{
				if (全局变量.所有玩家数据表[第几个玩家].称号信息表[k].状态 != 0)
				{
					num3++;
				}
				if (全局变量.所有玩家数据表[第几个玩家].称号信息表[k].状态 == 2)
				{
					当前称号.text = "<" + 全局变量.所有玩家数据表[第几个玩家].称号信息表[k].名字 + ">";
					当前称号.color = 全局变量.所有玩家数据表[第几个玩家].称号信息表[k].获取称号颜色();
				}
			}
			称号达成.text = "称号达成:" + num3.ToString() + "/" + count.ToString();
			页数显示.text = 显示第几页.ToString() + "/" + 总页数.ToString();
		}

		public void 点击激活称号(int 第几个称号)
		{
			第几个玩家 = 全局变量.本机身份;
			var 称号表 = 全局变量.所有玩家数据表[第几个玩家].称号信息表;
			int 选中索引 = (显示第几页 - 1) * 12 + 第几个称号;
			if (第几个称号 < 0 || 第几个称号 >= 12 || 选中索引 < 0 || 选中索引 >= 称号表.Count) return;
			if (GameNetwork.Enabled)
			{
				if (联机切换中) return;
				if (!GameNetwork.Connected) { 全局变量.提示类.显示信息("尚未连接服务器，请等待重连"); return; }
				string 名字 = 称号表[选中索引].名字;
				联机切换中 = true;
				设置称号交互(false);
				AuxiliaryClient.EquipTitle(名字, result => {
					if (this == null) return;
					联机切换中 = false;
					设置称号交互(true);
					全局变量.提示类.显示信息(result.Code == GameCodes.Ok ? "已激活:" + 名字 : result.Message);
					if (isActiveAndEnabled) 刷新显示();
				});
				return;
			}
			if (称号表[选中索引].状态 == 0)
			{
				全局变量.提示类.显示信息("尚未获得此称号。");
				return;
			}
			if (称号表[选中索引].状态 == 2) return;
			if (上次切换时间 > 0)
			{
				long num = TIME.getTime() - 上次切换时间;
				if (num < 20)
				{
					全局变量.提示类.显示信息("冷却中,剩余:" + (20 - num).ToString());
					return;
				}
			}
			int num2 = (显示第几页 - 1) * 12;
			int count = 全局变量.所有玩家数据表[第几个玩家].称号信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (全局变量.所有玩家数据表[第几个玩家].称号信息表[i].状态 != 0)
				{
					if (i == num2 + 第几个称号)
					{
						全局变量.提示类.显示信息("已激活:" + 全局变量.所有玩家数据表[第几个玩家].称号信息表[num2 + 第几个称号].名字);
						全局变量.所有玩家数据表[第几个玩家].称号信息表[num2 + 第几个称号].状态 = 2;
						全局变量.所有玩家数据表[第几个玩家].基础信息.称号名 = 全局变量.所有玩家数据表[第几个玩家].称号信息表[num2 + 第几个称号].名字;
						上次切换时间 = TIME.getTime();
					}
					else
					{
						全局变量.所有玩家数据表[第几个玩家].称号信息表[i].状态 = 1;
					}
				}
			}
			刷新显示();
		}
		private void 设置称号交互(bool 可用)
		{
			if (称号列表对象 == null) return;
			var 交互 = 称号列表对象.GetComponent<CanvasGroup>();
			if (交互 == null) 交互 = 称号列表对象.AddComponent<CanvasGroup>();
			交互.interactable = 可用;
		}

		public void 左翻页()
		{
			if (显示第几页 != 1)
			{
				显示第几页--;
				刷新显示();
			}
		}

		public void 右翻页()
		{
			if ((float)显示第几页 < 总页数)
			{
				显示第几页++;
				刷新显示();
			}
		}
	}
