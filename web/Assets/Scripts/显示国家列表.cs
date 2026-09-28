using UnityEngine;
using UnityEngine.UI;

public class 显示国家列表 : MonoBehaviour
{
	public GameObject 显示列表对象;

	public GameObject 建国按钮对象;

	public GameObject 更换加入对象;

	private void Start()
	{
	}

	public void 刷新显示()
	{
		建国按钮对象.SetActive(value: true);
		更换加入对象.SetActive(value: true);
		int 本机身份 = 全局变量.本机身份;
		国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[本机身份].基础信息.国家);
		if (国家信息库类 != null && 国家信息库类.国王 == 全局变量.所有玩家数据表[本机身份].基础信息.ID)
		{
			建国按钮对象.SetActive(value: false);
			更换加入对象.SetActive(value: false);
		}
		int childCount = 显示列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			显示列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		int count = 全局变量.所有国家列表.Count;
		for (int j = 0; j < count; j++)
		{
			int childCount2 = 显示列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(显示列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(显示列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 显示列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
			Image component = gameObject.transform.GetChild(1).GetComponent<Image>();
			Text component2 = gameObject.transform.GetChild(2).GetComponent<Text>();
			Text component3 = gameObject.transform.GetChild(3).GetComponent<Text>();
			component.sprite = 全局变量.所有国家列表[j].获取国家头像();
			component2.text = 全局变量.所有国家列表[j].国名 + "(" + 全局变量.所有国家列表[j].国号 + ")";
			component3.text = "城池数量:" + 全局变量.所有国家列表[j].城池列表.Count.ToString();
		}
	}

	public void 更换国家()
	{
		int childCount = 显示列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			if (!显示列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf)
			{
				continue;
			}
			int 本机身份 = 全局变量.本机身份;
			string 国家 = 全局变量.所有玩家数据表[本机身份].基础信息.国家;
			if (国家 != 全局变量.所有国家列表[i].国号)
			{
				国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(国家);
				if (国家信息库类 != null)
				{
					if (国家信息库类.国王 != 全局变量.所有玩家数据表[本机身份].基础信息.ID)
					{
						全局变量.所有玩家数据表[本机身份].更换指定国家(全局变量.所有国家列表[i].国号);
						全局变量.所有玩家数据表[本机身份].基础信息.战功 = 0;
						全局变量.提示类.显示信息("更换成功!");
					}
					else
					{
						全局变量.提示类.显示信息("国王不能换国!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("已在本国!");
			}
		}
	}
}
