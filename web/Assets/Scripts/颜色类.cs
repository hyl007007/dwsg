using System.Globalization;
using UnityEngine;

public class 颜色类
{
	public static Color GetColor(string color)
	{
		if (color.Length == 0)
		{
			return new Color(0f, 0f, 0f);
		}
		color = color.Substring(1);
		int num = int.Parse(color, NumberStyles.HexNumber);
		return new Color((float)((num >> 16) & 0xFF) / 255f, (float)((num >> 8) & 0xFF) / 255f, (float)(num & 0xFF) / 255f);
	}
}
