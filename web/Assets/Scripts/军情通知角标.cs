using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================================================================
// 军情通知角标：世界地图界面左下角「军情」按钮右上角的红色数字角标
//
// 这个按钮上原本只有一个纯红色方块（场景里叫 Image，默认隐藏），全局任务脚本
// 在「有需要提示的军情」时会把它点亮。这里把它换成参考图那种红底白字的圆角标：
// 有通知就显示条数，没有通知就整个隐藏。
//
// 接入只加了一行（全局任务脚本.Start 里）：
//       军情通知角标.装配(提示);
//
// 之后角标自己每秒刷新一次，不用再管别的地方。
//
// 通知条数 = 军情列表的全部条数（自己出征的行军、AI 来袭、增援，全都算）。
// 军情面板里有多少条，角标上就显示多少条；列表空了角标就自己隐藏。
// ================================================================
public class 军情通知角标 : MonoBehaviour
{
	// 超过这个数就显示成 99+
	private const int 显示上限 = 99;

	// ---- 角标外观：尺寸和位置都是按参考图量出来的（画布单位，按钮本身是 50x69）----
	private const float 角标直径 = 25f;

	private const float 角标横坐标 = 23f;

	private const float 角标纵坐标 = 18f;

	private const int 数字字号 = 15;

	private const int 数字小字号 = 11;

	private const string 角标贴图路径 = "通用界面/军情红点";

	private static readonly Color 数字颜色 = Color.white;

	private static readonly Color 数字描边色 = new Color(0.42f, 0.05f, 0.04f, 1f);

	// ---- 运行期状态 ----
	private GameObject 角标对象;

	private Image 角标底图;

	private Text 数字文本;

	private Font 界面字体;

	private float 下次刷新时间;

	//把角标装到「军情」按钮上。组件挂在按钮（父物体）上，这样角标被隐藏时刷新也不会停。
	public static 军情通知角标 装配(GameObject 角标对象)
	{
		if (角标对象 == null)
		{
			return null;
		}
		Transform 父物体 = 角标对象.transform.parent;
		if (父物体 == null)
		{
			return null;
		}
		军情通知角标 组件 = 父物体.GetComponent<军情通知角标>();
		if (组件 == null)
		{
			组件 = 父物体.gameObject.AddComponent<军情通知角标>();
		}
		组件.绑定角标(角标对象);
		return 组件;
	}

	private void 绑定角标(GameObject 对象)
	{
		角标对象 = 对象;
		对象.name = "军情红点";
		角标底图 = 对象.GetComponent<Image>();
		if (角标底图 == null)
		{
			角标底图 = 对象.AddComponent<Image>();
		}
		生成外观();
		刷新();
	}

	//把原来那个红方块改造成参考图的圆角标：换底图、改尺寸位置、加数字
	private void 生成外观()
	{
		RectTransform 框 = 角标对象.GetComponent<RectTransform>();
		if (框 == null)
		{
			框 = 角标对象.AddComponent<RectTransform>();
		}
		框.anchorMin = new Vector2(0.5f, 0.5f);
		框.anchorMax = new Vector2(0.5f, 0.5f);
		框.pivot = new Vector2(0.5f, 0.5f);
		框.anchoredPosition = new Vector2(角标横坐标, 角标纵坐标);
		框.sizeDelta = new Vector2(角标直径, 角标直径);
		框.localScale = Vector3.one;
		框.localRotation = Quaternion.identity;
		角标底图.sprite = 取角标贴图();
		角标底图.type = Image.Type.Simple;
		角标底图.preserveAspect = false;
		角标底图.color = Color.white;
		角标底图.raycastTarget = false;
		数字文本 = 建数字文本();
	}

	private Text 建数字文本()
	{
		GameObject 物体;
		Transform 已有 = 角标对象.transform.Find("通知数字");
		if (已有 != null)
		{
			物体 = 已有.gameObject;
		}
		else
		{
			物体 = new GameObject("通知数字", typeof(RectTransform));
			物体.GetComponent<RectTransform>().SetParent(角标对象.transform, worldPositionStays: false);
		}
		RectTransform 框 = 物体.GetComponent<RectTransform>();
		框.anchorMin = new Vector2(0.5f, 0.5f);
		框.anchorMax = new Vector2(0.5f, 0.5f);
		框.pivot = new Vector2(0.5f, 0.5f);
		框.anchoredPosition = Vector2.zero;
		框.sizeDelta = new Vector2(角标直径, 角标直径);
		框.localScale = Vector3.one;
		框.localRotation = Quaternion.identity;
		Text 文本 = 物体.GetComponent<Text>();
		if (文本 == null)
		{
			文本 = 物体.AddComponent<Text>();
		}
		文本.font = 取字体();
		文本.fontSize = 数字字号;
		文本.fontStyle = FontStyle.Bold;
		文本.alignment = TextAnchor.MiddleCenter;
		文本.color = 数字颜色;
		文本.raycastTarget = false;
		文本.supportRichText = false;
		文本.horizontalOverflow = HorizontalWrapMode.Overflow;
		文本.verticalOverflow = VerticalWrapMode.Overflow;
		if (物体.GetComponent<Outline>() == null)
		{
			Outline 描边 = 物体.AddComponent<Outline>();
			描边.effectColor = 数字描边色;
			描边.effectDistance = new Vector2(1f, -1f);
			描边.useGraphicAlpha = true;
		}
		return 文本;
	}

	//通知条数：军情列表里所有条目都算（不是只算身份!=0 的那种）
	public static int 取通知条数()
	{
		List<军情信息> 列表 = 全局变量.军情列表;
		if (列表 == null)
		{
			return 0;
		}
		int 条数 = 0;
		for (int i = 0; i < 列表.Count; i++)
		{
			if (列表[i] != null)
			{
				条数++;
			}
		}
		return 条数;
	}

	//每秒刷一次（用真实时间，战斗倍速不会影响它）
	private void Update()
	{
		if (Time.unscaledTime < 下次刷新时间)
		{
			return;
		}
		下次刷新时间 = Time.unscaledTime + 1f;
		刷新();
	}

	public void 刷新()
	{
		if (角标对象 == null)
		{
			return;
		}
		int 条数 = 取通知条数();
		bool 要显示 = 条数 > 0;
		if (角标对象.activeSelf != 要显示)
		{
			角标对象.SetActive(要显示);
		}
		if (!要显示 || 数字文本 == null)
		{
			return;
		}
		if (条数 > 显示上限)
		{
			数字文本.text = 显示上限.ToString() + "+";
			数字文本.fontSize = 数字小字号;
		}
		else
		{
			数字文本.text = 条数.ToString();
			数字文本.fontSize = 数字字号;
		}
	}

	// ==================== 找图 / 兜底 ====================

	private Sprite 取角标贴图()
	{
		Sprite 贴图 = Resources.Load<Sprite>(角标贴图路径);
		if (贴图 != null)
		{
			return 贴图;
		}
		return 生成圆形贴图();
	}

	//兜底：万一 通用界面/军情红点 没导入成功，就现场画一个红圆，免得角标变成方块
	private Sprite 生成圆形贴图()
	{
		int 尺寸 = 50;
		Texture2D 贴图 = new Texture2D(尺寸, 尺寸, TextureFormat.RGBA32, mipChain: false);
		float 半径 = 尺寸 * 0.5f - 1f;
		float 中心 = (尺寸 - 1) * 0.5f;
		for (int y = 0; y < 尺寸; y++)
		{
			for (int x = 0; x < 尺寸; x++)
			{
				float 距离 = Mathf.Sqrt((x - 中心) * (x - 中心) + (y - 中心) * (y - 中心));
				Color 颜色;
				if (距离 > 半径)
				{
					颜色 = new Color(0f, 0f, 0f, 0f);
				}
				else if (距离 > 半径 - 2f)
				{
					颜色 = new Color(0.17f, 0.035f, 0.027f, 1f);
				}
				else
				{
					float 比例 = y / (float)尺寸;
					颜色 = new Color(Mathf.Lerp(243f, 188f, 比例) / 255f, Mathf.Lerp(44f, 13f, 比例) / 255f, Mathf.Lerp(30f, 16f, 比例) / 255f, 1f);
				}
				贴图.SetPixel(x, y, 颜色);
			}
		}
		贴图.Apply();
		return Sprite.Create(贴图, new Rect(0f, 0f, 尺寸, 尺寸), new Vector2(0.5f, 0.5f), 100f);
	}

	//中文字体：直接借界面上任意一个 Text 正在用的字体，拿不到再退到 Unity 内置字体
	private Font 取字体()
	{
		if (界面字体 != null)
		{
			return 界面字体;
		}
		foreach (Text 文本 in Resources.FindObjectsOfTypeAll<Text>())
		{
			if (文本 != null && 文本.font != null)
			{
				界面字体 = 文本.font;
				return 界面字体;
			}
		}
		try
		{
			界面字体 = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		}
		catch (System.Exception)
		{
			界面字体 = null;
		}
		if (界面字体 == null)
		{
			try
			{
				界面字体 = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
			catch (System.Exception)
			{
				界面字体 = null;
			}
		}
		return 界面字体;
	}
}
