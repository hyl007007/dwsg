using System.Collections.Generic;
using Dwsg.Window3;
using UnityEngine;
using UnityEngine.UI;

public class 选择封地界面脚本 : MonoBehaviour
{
	public GameObject 封地列表对象;

	private int 第几个玩家 { get { return 全局变量.本机身份; } }
	private readonly List<int> 显示封地ID = new List<int>();

	private int 要刷新的数据 = 1;

	public GameObject 全部封地选择对象;

	public 封地界面脚本 封地脚本对象;

	public 将领列表显示 将领脚本对象;

	public 将领编队 编队脚本对象;

	public void 显示所有封地()
	{
        显示封地ID.Clear();
        var player = FiefActions.Player(第几个玩家);
        if (player == null || 封地列表对象 == null || 封地列表对象.transform.childCount == 0) return;
		int count = 全局变量.所有玩家数据表[第几个玩家].封地信息表.Count;
		int childCount = 封地列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			封地列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		for (int j = 0; j < count; j++)
		{
			childCount = 封地列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(封地列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(封地列表对象.transform, false);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 封地列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
            显示封地ID.Add(player.封地信息表[j].ID);
            if (gameObject.transform.childCount > 4) gameObject.transform.GetChild(4).gameObject.SetActive(false);
            foreach (var button in gameObject.GetComponentsInChildren<Button>(true)) 界面窗口管理器.注册运行时按钮(button);
			string 封地名字 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[j].封地名字;
			var building = player.封地信息表[j].建筑信息表.Count == 0 ? null : player.封地信息表[j].建筑信息表[0];
            float num = building == null ? 0 : building.等级;
			坐标 所在城池 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[j].所在城池;
			城池信息库类 城池信息库类 = 所在城池 == null ? null : 所有城池界面脚本.根据坐标获取指定城池(所在城池.x, 所在城池.y);
			string text = 城池信息库类 == null ? "" : 城池信息库类.获取规模名称();
			string 名称 = 城池信息库类 == null ? "城池已迁移" : 城池信息库类.名称;
			int num2 = building == null ? 0 : building.获取建筑头像索引();
			gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.大厅头像资源表[num2];
			gameObject.transform.GetChild(2).GetComponent<Text>().text = 封地名字 + "(" + num.ToString() + "级)";
			gameObject.transform.GetChild(3).GetComponent<Text>().text = 所在城池 == null ? 名称 : 名称 + "(" + text + " " + 所在城池.x.ToString() + "," + 所在城池.y.ToString() + ")";
		}
	}

	public void 打开选择封地列表(int 打开类型)
	{
        var player = FiefActions.Player(第几个玩家);
        if (player == null || player.封地信息表.Count == 0) { 全局变量.提示类.显示信息("暂无封地，请先开辟封地。"); return; }
		要刷新的数据 = 打开类型;
		base.gameObject.SetActive(value: true);
		if (全部封地选择对象 != null) 全部封地选择对象.SetActive(value: false);
		switch (打开类型)
		{
		case 2:
			if (全部封地选择对象 != null) 全部封地选择对象.SetActive(value: true);
			break;
		case 3:
			if (全部封地选择对象 != null) 全部封地选择对象.SetActive(value: true);
			break;
		}
		显示所有封地();
	}

	public void 切换指定封地()
	{
		int childCount = 封地列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			封地列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
		}
		CancelInvoke("切换指定封地1");
        Invoke("切换指定封地1", 0.01f);
	}

	public void 切换指定封地1()
	{
        int selected = -1;
		int childCount = 封地列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			if (封地列表对象.transform.GetChild(i).gameObject.activeSelf && 封地列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf)
			{
				var player = FiefActions.Player(第几个玩家);
                if (player != null && i < 显示封地ID.Count) selected = player.封地信息表.FindIndex(f => f.ID == 显示封地ID[i]);
				break;
			}
		}
		if (selected < 0) { 全局变量.提示类.显示信息("封地已变化，请重新选择。"); 显示所有封地(); return; }
        全局变量.第几个封地 = selected;
		if (要刷新的数据 == 1)
		{
			封地脚本对象.第几个封地 = 全局变量.第几个封地;
			封地脚本对象.显示封地所有建筑();
		}
		else if (要刷新的数据 == 2)
		{
			将领脚本对象.显示第几个封地 = 全局变量.第几个封地;
			将领脚本对象.重置刷新将领列表();
		}
		else if (要刷新的数据 == 3)
		{
			编队脚本对象.显示第几个封地 = 全局变量.第几个封地;
			编队脚本对象.重置刷新将领列表();
		}
        else if (要刷新的数据 == 4)
        {
            var info = CityNavigation.Find<封地信息界面UI脚本>();
            if (info != null) info.绑定当前封地();
        }
        base.gameObject.SetActive(value: false);
    }

    public void 切换全部封地()
	{
		if (要刷新的数据 == 2)
		{
			将领脚本对象.显示第几个封地 = -1;
			将领脚本对象.重置刷新将领列表();
		}
		else if (要刷新的数据 == 3)
		{
			编队脚本对象.显示第几个封地 = -1;
			编队脚本对象.重置刷新将领列表();
		}
		base.gameObject.SetActive(value: false);
	}
}
