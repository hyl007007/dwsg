using UnityEngine;

public class 文字滚动 : MonoBehaviour
{
	public int 类型;

	public int 方向;

	public float Speed = 50f;

	public float OverPos;

	public float StartPos;

	public RectTransform Information;

	private Vector2 pos;

	private void FixedUpdate()
	{
		ScrollResult();
	}

	private void ScrollResult()
	{
		if (类型 == 0)
		{
			pos = new Vector2(Speed * Time.fixedDeltaTime, 0f);
			if (方向 == 0)
			{
				if (Information.anchoredPosition.x < OverPos)
				{
					Information.anchoredPosition = new Vector2(StartPos, Information.anchoredPosition.y);
				}
				else
				{
					Information.anchoredPosition += -pos;
				}
			}
			else if (Information.anchoredPosition.x > StartPos)
			{
				Information.anchoredPosition = new Vector2(OverPos, Information.anchoredPosition.y);
			}
			else
			{
				Information.anchoredPosition += pos;
			}
			return;
		}
		pos = new Vector2(0f, Speed * Time.fixedDeltaTime);
		if (方向 == 0)
		{
			if (Information.anchoredPosition.y < OverPos)
			{
				Information.anchoredPosition = new Vector2(Information.anchoredPosition.x, StartPos);
			}
			else
			{
				Information.anchoredPosition += -pos;
			}
		}
		else if (Information.anchoredPosition.y > StartPos)
		{
			Information.anchoredPosition = new Vector2(Information.anchoredPosition.x, OverPos);
		}
		else
		{
			Information.anchoredPosition += pos;
		}
	}
}
