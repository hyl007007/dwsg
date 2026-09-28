using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 显示建筑信息脚本 : MonoBehaviour
{
	public Image 头像对象;

	public Text 名字等级对象;

	public Text 建筑效果对象;

	public Text 封地产钱对象;

	public Text 总产钱对象;

	public 建筑信息 建筑信息对象;

	public void 显示建筑信息()
	{
		int num = 建筑信息对象.获取建筑头像索引();
		头像对象.sprite = 全局变量.大厅头像资源表[num];
		名字等级对象.text = 建筑信息对象.获取建筑等级文本();
	}
}
