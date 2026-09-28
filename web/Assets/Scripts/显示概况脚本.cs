using UnityEngine;
using UnityEngine.UI;

public class 显示概况脚本 : MonoBehaviour
{
	public Text 国家名字;

	public Text 国王名字;

	public Text 国都名字;

	public Text 城池数量;

	public Text 成员数量;

	public Text 科技等级;

	public Text 排名;

	public Text 国家名字效率显示;

    private void Start()
    {
		刷新显示();
    }

    public void 刷新显示()
	{
		int 本机身份 = 全局变量.本机身份;
		国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[本机身份].基础信息.国家);
		if (国家信息库类 != null)
		{
			国家名字.text = 国家信息库类.国名 + "(" + 国家信息库类.国号 + ")";
			int 国王 = 国家信息库类.国王;
			国王名字.text = 全局变量.所有玩家数据表[国王].基础信息.名字;
			城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(国家信息库类.国都x, 国家信息库类.国都y);
			国都名字.text = 城池信息库类.名称;
			int count = 国家信息库类.城池列表.Count;
			城池数量.text = count.ToString() + "/" + 全局变量.所有城池列表.Count.ToString();
			成员数量.text = 国家信息库类.成员列表.Count.ToString();
			科技等级.text = 国家信息库类.科技等级.ToString();
			全局变量.所有国家列表.Sort((国家信息库类 x, 国家信息库类 y) => (x.城池列表.Count < y.城池列表.Count) ? 1 : (-1));
			排名.text = (全局方法类.获取指定国家的索引(国家信息库类.国号) + 1).ToString();
			国家名字效率显示.text = 国家信息库类.国名 + "(" + 国家信息库类.国号 + "," + 国家信息库类.获取国家规模名称() + ")    效率:" + 国家信息库类.效率.ToString() + "%";
        }
	}
}
