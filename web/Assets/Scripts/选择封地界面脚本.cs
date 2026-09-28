using UnityEngine;
using UnityEngine.UI;

public class 选择封地界面脚本 : MonoBehaviour
{
	public GameObject 封地列表对象;

	private int 第几个玩家 = 全局变量.本机身份;

	private int 要刷新的数据 = 1;

	public GameObject 全部封地选择对象;

	public 封地界面脚本 封地脚本对象;

	public 将领列表显示 将领脚本对象;

	public 将领编队 编队脚本对象;

	public void 显示所有封地()
	{
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
				gameObject.transform.SetParent(封地列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 封地列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
			string 封地名字 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[j].封地名字;
			float num = 全局变量.所有玩家数据表[第几个玩家].封地信息表[j].建筑信息表[0].等级;
			坐标 所在城池 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[j].所在城池;
			城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(所在城池.x, 所在城池.y);
			string text = 城池信息库类.获取规模名称();
			string 名称 = 城池信息库类.名称;
			int num2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[j].建筑信息表[0].获取建筑头像索引();
			gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.大厅头像资源表[num2];
			gameObject.transform.GetChild(2).GetComponent<Text>().text = 封地名字 + "(" + num.ToString() + "级)";
			gameObject.transform.GetChild(3).GetComponent<Text>().text = 名称 + "(" + text + " " + 所在城池.x.ToString() + "," + 所在城池.y.ToString() + ")";
		}
	}

	public void 打开选择封地列表(int 打开类型)
	{
		要刷新的数据 = 打开类型;
		base.gameObject.SetActive(value: true);
		全部封地选择对象.SetActive(value: false);
		switch (打开类型)
		{
		case 2:
			全部封地选择对象.SetActive(value: true);
			break;
		case 3:
			全部封地选择对象.SetActive(value: true);
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
		Invoke("切换指定封地1", 0.2f);
	}

	public void 切换指定封地1()
	{
		int childCount = 封地列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			if (封地列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf)
			{
				全局变量.第几个封地 = i;
				break;
			}
		}
		UnityEngine.Debug.Log("选择了封地" + 全局变量.第几个封地.ToString());
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
