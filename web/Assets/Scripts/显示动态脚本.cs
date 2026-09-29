using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口4;

public class 显示动态脚本 : MonoBehaviour
{
	public Text 将领显示;

	public Text 俘虏显示;

	public Text 兵力显示;

	public Text 伤兵显示;

    private void Awake()
    {
        foreach (string 类型 in new[] { "建筑", "征兵", "研究" })
        {
            var 队列 = transform.Find(类型 + "队列信息/显示");
            if (队列 == null) 队列 = transform.Find("动态布局/" + 类型 + "队列信息/显示");
            if (队列 != null && 队列.GetComponent<Text>() != null) 队列.GetComponent<Text>().text = "即时";
        }
    }

    private void OnEnable() { 刷新显示(); InvokeRepeating("刷新显示", 1f, 1f); }
    private void OnDisable() { CancelInvoke("刷新显示"); }

	public void 刷新显示()
	{
		int 本机身份 = 全局变量.本机身份;
        if (军事缺口入口.当前玩家() == null)
        {
            if (将领显示 != null) 将领显示.text = "0/0";
            if (俘虏显示 != null) 俘虏显示.text = "0/0";
            if (兵力显示 != null) 兵力显示.text = "0";
            if (伤兵显示 != null) 伤兵显示.text = "0";
            return;
        }
		将领显示.text = 全局变量.所有玩家数据表[本机身份].获取将领总数().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限.ToString();
		俘虏显示.text = 全局变量.所有玩家数据表[本机身份].获取俘虏总数().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限.ToString();
		兵力显示.text = 全局变量.所有玩家数据表[本机身份].获取兵力总数().ToString();
		伤兵显示.text = 全局变量.所有玩家数据表[本机身份].获取伤兵总数().ToString();
	}
}
