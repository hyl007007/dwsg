using UnityEngine;

public class 伤害显示 : MonoBehaviour
{
	private float 要移动的距离;

	private float 移动距离 = 3f;

	private int 重新获取初始位置 = 1;

	private Vector2 初始位置;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		if (base.gameObject.activeSelf)
		{
			if (重新获取初始位置 == 1)
			{
				初始位置 = base.transform.localPosition;
				重新获取初始位置 = 0;
				要移动的距离 = 初始位置.y + 移动距离;
			}
			else if (base.transform.localPosition.y < 要移动的距离)
			{
				float maxDistanceDelta = 2f * Time.deltaTime;
				base.transform.localPosition = Vector2.MoveTowards(base.transform.localPosition, new Vector2(base.transform.localPosition.x, 要移动的距离), maxDistanceDelta);
			}
			else
			{
				base.gameObject.SetActive(value: false);
				重新获取初始位置 = 1;
			}
		}
	}
}
