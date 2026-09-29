using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class 建国脚本 : MonoBehaviour
{
	public Text 国名对象;

	public Text 国号对象;

	public Text 国都对象;

	public Text 国家宣言对象;

	public Text 国名输入对象;

	public Text 国号输入对象;

	public Text 宣言输入对象;

	public 城池信息库类 国都城池信息;

	public GameObject 国家列表布局;

	public void 输入国家名字()
	{
		if (国名输入对象.text != null && !(国名输入对象.text == ""))
		{
			if (Encoding.Default.GetBytes(国名输入对象.text).Length < 17)
			{
				string text = 国名输入对象.text;
				UnityEngine.Debug.Log(text);
				国名对象.text = text;
			}
			else
			{
				全局变量.提示类.显示信息("国名错误,重新输入!");
			}
		}
	}

	public void 输入国家国号()
	{
		if (国号输入对象.text != null && !(国号输入对象.text == ""))
		{
			if (Encoding.Default.GetBytes(国号输入对象.text).Length > 3)
			{
				全局变量.提示类.显示信息("国号错误,重新输入!");
				return;
			}
			string text = 国号输入对象.text;
			UnityEngine.Debug.Log(text);
			国号对象.text = text;
		}
	}

	public void 输入国家宣言()
	{
		if (宣言输入对象.text != null && !(宣言输入对象.text == ""))
		{
			string text = 宣言输入对象.text;
			UnityEngine.Debug.Log(text);
			国家宣言对象.text = text;
		}
	}

	public void 确定建国()
	{
		if (!(国名对象.text == "") && 国名对象.text != null && !(国号对象.text == "") && 国号对象.text != null && !(国都对象.text == "") && 国都对象.text != null && !(国家宣言对象.text == "") && 国家宣言对象.text != null && 国都城池信息 != null)
		{
			int 本机身份 = 全局变量.本机身份;
			NationClient.Create(国名对象.text, 国号对象.text, 国家宣言对象.text, 国都城池信息, 结果 =>
			{
				if (this == null) return;
				全局变量.提示类.显示信息(结果?.Message ?? "服务器未确认建国，请重试");
				if (结果 == null || 结果.Code != Dwsg.Shared.GameCodes.Ok || 全局变量.本机身份 != 本机身份) return;
				base.gameObject.SetActive(value: false);
				国家列表布局.SetActive(value: false);
			});
		}
	}
}
