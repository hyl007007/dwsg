using UnityEngine;
using UnityEngine.UI;

public class 将领改名脚本 : MonoBehaviour
{
	public Text 原将领名字;

	public Text 现将领名字;

	public 将领列表显示 将领列表显示脚本;

	public Text 输入名字对象;

	public void 更新输入结果()
	{
		if (输入名字对象.text != null && !(输入名字对象.text == ""))
		{
			string text = 输入名字对象.text;
			UnityEngine.Debug.Log(text);
			现将领名字.text = text;
		}
	}

	public void 确定改名()
	{
		if (现将领名字.text != "")
		{
			将领列表显示脚本.将领修改名字(现将领名字.text);
			全局变量.提示类.显示信息("改名成功!");
			base.gameObject.SetActive(value: false);
		}
	}
}
