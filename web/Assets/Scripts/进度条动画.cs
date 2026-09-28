using UnityEngine;
using UnityEngine.UI;

public class 进度条动画 : MonoBehaviour
{
	public Text 提示文本;

	public Transform 进度条;

	private Vector2 进度条初始位置;

	private void Start()
	{
		进度条初始位置 = 进度条.localPosition;
	}

	private void FixedUpdate()
	{
		float num = 28f;
		if (进度条.localPosition.x < num)
		{
			float maxDistanceDelta = 40f * Time.deltaTime;
			进度条.localPosition = Vector2.MoveTowards(进度条.localPosition, new Vector2(num, 进度条.localPosition.y), maxDistanceDelta);
		}
		else
		{
			进度条.localPosition = 进度条初始位置;
		}
	}
}
