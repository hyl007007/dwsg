using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class 开始界面背景动画 : MonoBehaviour
{
	public Transform 背景;

	public Transform 前景;

	private Vector2 背景初始位置;

	private Vector2 前景初始位置;

	public 提示移动 提示对象;

	private bool 正在验证;

	private void Start()
	{
		背景初始位置 = 背景.localPosition;
		前景初始位置 = 前景.localPosition;
	}

	private IEnumerator 验证状态()
	{
		//本地固定验证通过：不再依赖失效的外部网盘核对，直接放行进入游戏
		全局变量.验证变量 = 1;
		yield break;
	}

	public void 启动验证()
	{
		if (全局变量.验证变量 == 0)
		{
			UnityEngine.Debug.Log("启动验证");
			StartCoroutine(验证状态());
		}
	}

	private void FixedUpdate()
	{
		float num = 511f;
		if (背景.localPosition.x != num)
		{
			float maxDistanceDelta = 30f * Time.deltaTime;
			背景.localPosition = Vector2.MoveTowards(背景.localPosition, new Vector2(num, 背景.localPosition.y), maxDistanceDelta);
		}
		else
		{
			背景.localPosition = 背景初始位置;
		}
		if (前景.localPosition.x != num)
		{
			float maxDistanceDelta2 = 60f * Time.deltaTime;
			前景.localPosition = Vector2.MoveTowards(前景.localPosition, new Vector2(num, 前景.localPosition.y), maxDistanceDelta2);
		}
		else
		{
			前景.localPosition = 前景初始位置;
		}
	}
}
