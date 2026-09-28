using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================================================================
// 战斗倍速设置：战斗场景里控制时间流速（1倍 / 2倍 / 3倍 / 4倍）
//
// 挂在 "战斗界面UI" 根物体上就行，场景里不用手动搭任何东西。
//
// 面板样式照参考图做的竖排菜单：顶部一行金色标题，中间一列青绿金边按钮，
// 最下面一个"返 回"。按钮底图、面板底色、四角边框全部借用工程里现有的美术资源
// （通用绿色背景 7966 / 通用背景边框 7967-7969 / 青绿按钮 7992），没有新增任何图片。
//
// 它自己会做完这几件事：
//   1. 进战斗（战斗界面UI 被激活）时，找到右下角的"设置"齿轮按钮，把点击事件接上去；
//   2. 第一次进战斗时把面板搭出来，搭完默认隐藏；
//   3. 点齿轮开/关面板；面板里点 1倍/2倍/3倍/4倍 立刻换速度并高亮当前档位；
//      点"返 回"、点面板外、按 Esc 都能关面板；
//   4. 战斗中按数字键 1 / 2 / 3 / 4 也能换速度；
//   5. 退出战斗（战斗界面UI 被隐藏或销毁）时把 Time.timeScale 还原成 1。
//
// 想在编辑器里手动微调面板：菜单【工具 / 战斗倍速面板 / 生成到场景】。
// ================================================================
public class 战斗速度设置 : MonoBehaviour
{
	// 存档键：进战斗时自动套用上次选的倍速
	private const string 倍速存档键 = "战斗倍速";

	// ---- 面板引用：留空会自动生成；手动搭好面板后也可以在 Inspector 里拖进来 ----
	public GameObject 设置面板对象;
	public Button 一倍按钮;
	public Button 二倍按钮;
	public Button 三倍按钮;
	public Button 四倍按钮;
	public Button 返回按钮;
	public Text 倍速角标文本;

	// ---- 运行期状态 ----
	private float 当前倍速 = 1f;

	private bool 已挂钩设置按钮;

	private Font 界面字体;

	private Dictionary<string, Sprite> 素材表;

	// ---- 面板尺寸（画布单位。战斗界面UI 根节点整体缩放 2 倍，所以屏幕上看起来是两倍大）----
	private const float 面板宽 = 140f;

	private const float 面板高 = 320f;

	private const float 边框厚度 = 8f;

	private const float 按钮宽 = 96f;

	private const float 按钮高 = 40f;

	private const float 按钮间距 = 57f;

	private const float 第一个按钮的纵坐标 = 96f;

	private const float 标题的纵坐标 = 136f;

	// ---- 配色：金色文字直接照参考图取的色 ----
	private static readonly Vector2 居中锚点 = new Vector2(0.5f, 0.5f);

	private static readonly Color 金色字 = new Color(0.847f, 0.835f, 0.584f, 1f);

	private static readonly Color 标题金 = new Color(0.884f, 0.894f, 0.66f, 1f);

	private static readonly Color 选中字色 = new Color(0.98f, 0.97f, 0.84f, 1f);

	private static readonly Color 未选中字色 = new Color(0.847f, 0.835f, 0.584f, 1f);

	private static readonly Color 选中底色 = new Color(1f, 1f, 1f, 1f);

	private static readonly Color 未选中底色 = new Color(0.76f, 0.78f, 0.78f, 1f);

	private static readonly Color 描边色 = new Color(0.06f, 0.07f, 0.04f, 0.95f);

	private void OnEnable()
	{
		生成面板();
		关联设置按钮();
		应用倍速(读取存档倍速(), false);
	}

	//退战斗：收面板并把速度还原，免得影响主界面
	private void OnDisable()
	{
		if (设置面板对象 != null)
		{
			设置面板对象.SetActive(value: false);
		}
		Time.timeScale = 1f;
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
	}

	//键盘备用：战斗内按 1 / 2 / 3 / 4 切换倍速，Esc 关面板
	private void Update()
	{
		//聊天输入框正在打字时，1/2/3/4 和 Esc 都留给输入框，别顺手把倍速改掉
		if (聊天系统.正在输入)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
		{
			设置1倍速度();
		}
		if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
		{
			设置2倍速度();
		}
		if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
		{
			设置3倍速度();
		}
		if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
		{
			设置4倍速度();
		}
		if (Input.GetKeyDown(KeyCode.Escape) && 设置面板对象 != null && 设置面板对象.activeSelf)
		{
			关闭设置面板();
		}
	}

	// ==================== 面板生成 ====================

	//生成倍速面板（只会生成一次）。
	//已经手动在场景里搭好面板、并把引用拖进 Inspector 的话，这里会直接跳过。
	public void 生成面板()
	{
		if (设置面板对象 != null)
		{
			return;
		}
		界面字体 = 取字体();
		Sprite 按钮底图 = 取贴图("7992.dat_d1");
		if (按钮底图 == null)
		{
			按钮底图 = 取贴图("7992.dat");
		}

		// ---- 最外层容器：尺寸为 0，只用来定位屏幕中心 ----
		GameObject 面板 = 新建节点("倍速设置面板", base.transform, Vector2.zero, Vector2.zero, 居中锚点);
		设置面板对象 = 面板;

		// ---- 隐形遮罩：点面板外面的地方就把面板关掉（参考图里没有变暗效果，所以这里全透明）----
		GameObject 遮罩 = 新建节点("遮罩", 面板.transform, Vector2.zero, Vector2.zero, 居中锚点);
		RectTransform 遮罩框 = 遮罩.GetComponent<RectTransform>();
		遮罩框.anchorMin = Vector2.zero;
		遮罩框.anchorMax = Vector2.one;
		遮罩框.offsetMin = new Vector2(-600f, -600f);
		遮罩框.offsetMax = new Vector2(600f, 600f);
		Image 遮罩图 = 遮罩.AddComponent<Image>();
		遮罩图.color = new Color(1f, 1f, 1f, 0f);
		遮罩图.raycastTarget = true;
		Button 遮罩按钮 = 遮罩.AddComponent<Button>();
		遮罩按钮.transition = Selectable.Transition.None;
		遮罩按钮.targetGraphic = 遮罩图;
		遮罩按钮.onClick.AddListener(关闭设置面板);

		// ---- 面板底：通用绿色背景 + 通用背景边框 ----
		GameObject 底板 = 新建节点("面板底", 面板.transform, new Vector2(面板宽, 面板高), Vector2.zero, 居中锚点);
		Image 底板图 = 底板.AddComponent<Image>();
		底板图.sprite = 取贴图("7966.dat");
		底板图.type = Image.Type.Simple;
		底板图.color = ((底板图.sprite == null) ? new Color(0.09f, 0.14f, 0.11f, 0.97f) : Color.white);
		底板图.raycastTarget = true;
		建边框(底板.transform);
		Transform 内容 = 底板.transform;

		// ---- 标题：战斗倍速 ----
		Text 标题 = 建文本(内容, "标题", "战斗倍速", 26, 标题金, new Vector2(面板宽 - 边框厚度 * 2f, 34f), new Vector2(0f, 标题的纵坐标), TextAnchor.MiddleCenter, true, 居中锚点);
		加描边(标题, 描边色, 1.6f);

		// ---- 一列按钮：1倍速 / 2倍速 / 3倍速 / 4倍速 / 返 回 ----
		string[] 文案列表 = new string[5] { "1倍速", "2倍速", "3倍速", "4倍速", "返 回" };
		Button[] 按钮列表 = new Button[5];
		for (int i = 0; i < 文案列表.Length; i++)
		{
			按钮列表[i] = 建按钮(内容, "倍速按钮" + (i + 1), 文案列表[i], 按钮底图, new Vector2(按钮宽, 按钮高), new Vector2(0f, 第一个按钮的纵坐标 - 按钮间距 * (float)i), 20);
		}
		一倍按钮 = 按钮列表[0];
		二倍按钮 = 按钮列表[1];
		三倍按钮 = 按钮列表[2];
		四倍按钮 = 按钮列表[3];
		返回按钮 = 按钮列表[4];
		一倍按钮.onClick.AddListener(设置1倍速度);
		二倍按钮.onClick.AddListener(设置2倍速度);
		三倍按钮.onClick.AddListener(设置3倍速度);
		四倍按钮.onClick.AddListener(设置4倍速度);
		返回按钮.onClick.AddListener(关闭设置面板);

		设置面板对象.SetActive(value: false);
		刷新按钮高亮();
	}

	//通用背景边框：四角用 7967、上下边用 7968、左右边用 7969
	//（和场景里"增援界面UI / 通用背景边框"用同一套图、同一套摆法，右上/左下/右下靠负缩放镜像）
	private void 建边框(Transform 父物体)
	{
		Sprite 角图 = 取贴图("7967.dat");
		Sprite 横图 = 取贴图("7968.dat");
		Sprite 竖图 = 取贴图("7969.dat");
		float 半 = 边框厚度 * 0.5f;
		Vector2 角尺寸 = new Vector2(边框厚度, 边框厚度);
		if (角图 != null)
		{
			建图(父物体, "左上", 角图, 角尺寸, new Vector2(半, -半), new Vector2(0f, 1f), new Vector3(1f, 1f, 1f));
			建图(父物体, "右上", 角图, 角尺寸, new Vector2(-半, -半), new Vector2(1f, 1f), new Vector3(-1f, 1f, 1f));
			建图(父物体, "左下", 角图, 角尺寸, new Vector2(半, 半), new Vector2(0f, 0f), new Vector3(1f, -1f, 1f));
			建图(父物体, "右下", 角图, 角尺寸, new Vector2(-半, 半), new Vector2(1f, 0f), new Vector3(-1f, -1f, 1f));
		}
		Vector2 横尺寸 = new Vector2(边框厚度 * -2f, 边框厚度);
		Vector2 竖尺寸 = new Vector2(边框厚度, 边框厚度 * -2f);
		if (横图 != null)
		{
			建拉伸图(父物体, "上", 横图, 横尺寸, new Vector2(0f, -半), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector3(1f, 1f, 1f));
			建拉伸图(父物体, "下", 横图, 横尺寸, new Vector2(0f, 半), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector3(1f, -1f, 1f));
		}
		if (竖图 != null)
		{
			建拉伸图(父物体, "左", 竖图, 竖尺寸, new Vector2(半, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector3(1f, 1f, 1f));
			建拉伸图(父物体, "右", 竖图, 竖尺寸, new Vector2(-半, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector3(-1f, 1f, 1f));
		}
	}

	//在"设置"齿轮按钮上方挂一个角标，随时显示当前倍速；顺便把它的点击事件接上
	private void 关联设置按钮()
	{
		if (已挂钩设置按钮)
		{
			return;
		}
		Transform 设置物体 = 找子物体("设置");
		if (设置物体 == null)
		{
			return;
		}
		Button 齿轮按钮 = 设置物体.GetComponent<Button>();
		if (齿轮按钮 != null)
		{
			齿轮按钮.onClick.RemoveListener(切换设置面板);
			齿轮按钮.onClick.AddListener(切换设置面板);
			已挂钩设置按钮 = true;
		}
		if (倍速角标文本 == null)
		{
			倍速角标文本 = 建文本(设置物体, "倍速角标", "1倍", 12, 标题金, new Vector2(60f, 20f), new Vector2(0f, 18f), TextAnchor.MiddleCenter, true, new Vector2(0.5f, 1f));
			加描边(倍速角标文本, 描边色, 1.2f);
		}
		刷新按钮高亮();
	}

	// ==================== 对外操作 ====================

	public void 切换设置面板()
	{
		if (设置面板对象 == null)
		{
			return;
		}
		bool 要打开 = !设置面板对象.activeSelf;
		设置面板对象.SetActive(value: 要打开);
		if (要打开)
		{
			刷新按钮高亮();
		}
	}

	public void 关闭设置面板()
	{
		if (设置面板对象 != null)
		{
			设置面板对象.SetActive(value: false);
		}
	}

	public void 设置1倍速度()
	{
		应用倍速(1f, false);
	}

	public void 设置2倍速度()
	{
		应用倍速(2f, false);
	}

	public void 设置3倍速度()
	{
		应用倍速(3f, false);
	}

	public void 设置4倍速度()
	{
		应用倍速(4f, false);
	}

	private void 应用倍速(float 速度, bool 收起面板)
	{
		if (速度 <= 0f)
		{
			速度 = 1f;
		}
		当前倍速 = 速度;
		Time.timeScale = 速度;
		PlayerPrefs.SetFloat(倍速存档键, 速度);
		刷新按钮高亮();
		if (收起面板)
		{
			关闭设置面板();
		}
	}

	private float 读取存档倍速()
	{
		float 速度 = PlayerPrefs.GetFloat(倍速存档键, 1f);
		if (速度 != 1f && 速度 != 2f && 速度 != 3f && 速度 != 4f)
		{
			速度 = 1f;
		}
		return 速度;
	}

	//选中的那一档亮起来，其它三档压暗
	private void 刷新按钮高亮()
	{
		设选中(一倍按钮, 当前倍速 == 1f);
		设选中(二倍按钮, 当前倍速 == 2f);
		设选中(三倍按钮, 当前倍速 == 3f);
		设选中(四倍按钮, 当前倍速 == 4f);
		if (倍速角标文本 != null)
		{
			倍速角标文本.text = 倍速文字(当前倍速) + "倍";
		}
	}

	private void 设选中(Button 按钮, bool 选中)
	{
		if (按钮 == null)
		{
			return;
		}
		Image 图 = 按钮.image;
		if (图 != null)
		{
			图.color = (选中 ? 选中底色 : 未选中底色);
		}
		Text 文字 = 按钮.GetComponentInChildren<Text>(includeInactive: true);
		if (文字 != null)
		{
			文字.color = (选中 ? 选中字色 : 未选中字色);
		}
	}

	private static string 倍速文字(float 速度)
	{
		if (速度 == 2f)
		{
			return "2";
		}
		if (速度 == 3f)
		{
			return "3";
		}
		if (速度 == 4f)
		{
			return "4";
		}
		return "1";
	}

	// ==================== 搭 UI 用的小工具 ====================

	private Button 建按钮(Transform 父物体, string 名字, string 文字, Sprite 底图, Vector2 尺寸, Vector2 位置, int 字号)
	{
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, 居中锚点);
		Image 图 = 物体.AddComponent<Image>();
		图.sprite = 底图;
		图.type = Image.Type.Simple;
		图.color = Color.white;
		图.raycastTarget = true;
		Button 按钮 = 物体.AddComponent<Button>();
		按钮.targetGraphic = 图;
		按钮.transition = Selectable.Transition.ColorTint;
		if (!string.IsNullOrEmpty(文字))
		{
			Text 文字组件 = 建文本(物体.transform, "文字", 文字, 字号, 金色字, 尺寸, Vector2.zero, TextAnchor.MiddleCenter, true, 居中锚点);
			加描边(文字组件, 描边色, 1.4f);
		}
		return 按钮;
	}

	private Image 建图(Transform 父物体, string 名字, Sprite 图, Vector2 尺寸, Vector2 位置, Vector2 锚点, Vector3 缩放)
	{
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, 锚点);
		物体.transform.localScale = 缩放;
		Image 图片 = 物体.AddComponent<Image>();
		图片.sprite = 图;
		图片.type = Image.Type.Simple;
		图片.raycastTarget = false;
		return 图片;
	}

	private Image 建拉伸图(Transform 父物体, string 名字, Sprite 图, Vector2 尺寸, Vector2 位置, Vector2 锚点最小, Vector2 锚点最大, Vector3 缩放)
	{
		GameObject 物体 = 新建拉伸节点(名字, 父物体, 尺寸, 位置, 锚点最小, 锚点最大);
		物体.transform.localScale = 缩放;
		Image 图片 = 物体.AddComponent<Image>();
		图片.sprite = 图;
		图片.type = Image.Type.Simple;
		图片.raycastTarget = false;
		return 图片;
	}

	private Text 建文本(Transform 父物体, string 名字, string 内容, int 字号, Color 颜色, Vector2 尺寸, Vector2 位置, TextAnchor 对齐, bool 粗体, Vector2 锚点)
	{
		if (界面字体 == null)
		{
			界面字体 = 取字体();
		}
		GameObject 物体 = 新建节点(名字, 父物体, 尺寸, 位置, 锚点);
		Text 文本 = 物体.AddComponent<Text>();
		文本.text = 内容;
		文本.font = 界面字体;
		文本.fontSize = 字号;
		文本.fontStyle = (粗体 ? FontStyle.Bold : FontStyle.Normal);
		文本.alignment = 对齐;
		文本.color = 颜色;
		文本.raycastTarget = false;
		文本.supportRichText = false;
		文本.horizontalOverflow = HorizontalWrapMode.Overflow;
		文本.verticalOverflow = VerticalWrapMode.Overflow;
		return 文本;
	}

	private static void 加描边(Graphic 图形, Color 颜色, float 距离)
	{
		if (图形 == null)
		{
			return;
		}
		Outline 描边 = 图形.gameObject.AddComponent<Outline>();
		描边.effectColor = 颜色;
		描边.effectDistance = new Vector2(距离, -距离);
		描边.useGraphicAlpha = true;
	}

	private static GameObject 新建节点(string 名字, Transform 父物体, Vector2 尺寸, Vector2 位置, Vector2 锚点)
	{
		GameObject 物体 = new GameObject(名字, typeof(RectTransform));
		RectTransform 框 = 物体.GetComponent<RectTransform>();
		框.SetParent(父物体, worldPositionStays: false);
		框.localRotation = Quaternion.identity;
		框.localScale = Vector3.one;
		框.anchorMin = 锚点;
		框.anchorMax = 锚点;
		框.pivot = new Vector2(0.5f, 0.5f);
		框.anchoredPosition = 位置;
		框.sizeDelta = 尺寸;
		return 物体;
	}

	private static GameObject 新建拉伸节点(string 名字, Transform 父物体, Vector2 尺寸, Vector2 位置, Vector2 锚点最小, Vector2 锚点最大)
	{
		GameObject 物体 = new GameObject(名字, typeof(RectTransform));
		RectTransform 框 = 物体.GetComponent<RectTransform>();
		框.SetParent(父物体, worldPositionStays: false);
		框.localRotation = Quaternion.identity;
		框.localScale = Vector3.one;
		框.anchorMin = 锚点最小;
		框.anchorMax = 锚点最大;
		框.pivot = new Vector2(0.5f, 0.5f);
		框.anchoredPosition = 位置;
		框.sizeDelta = 尺寸;
		return 物体;
	}

	// ==================== 借用工程里现有的美术资源 ====================

	//按贴图名找 Sprite（工程里的图都是单图精灵，名字就是文件名去掉 .png，
	//比如 7966.dat.png -> "7966.dat"）。整张表只扫一次，之后走缓存。
	private Sprite 取贴图(string 名字)
	{
		if (素材表 == null)
		{
			素材表 = new Dictionary<string, Sprite>();
			foreach (Sprite 图 in Resources.FindObjectsOfTypeAll<Sprite>())
			{
				if (图 == null || string.IsNullOrEmpty(图.name) || 素材表.ContainsKey(图.name))
				{
					continue;
				}
				素材表.Add(图.name, 图);
			}
		}
		Sprite 结果;
		if (素材表.TryGetValue(名字, out 结果))
		{
			return 结果;
		}
		//找不到就退一步：找名字以它开头的（比如 7992.dat_d1 的精灵名可能是 7992.dat）
		foreach (KeyValuePair<string, Sprite> 条目 in 素材表)
		{
			if (条目.Key.StartsWith(名字))
			{
				return 条目.Value;
			}
		}
		return null;
	}

	private Transform 找子物体(string 名字)
	{
		foreach (Transform 子物体 in GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (子物体 != base.transform && 子物体.name == 名字)
			{
				return 子物体;
			}
		}
		return null;
	}

	//中文字体：直接借战斗界面里任意一个 Text 用的字体（工程用的是自带的中文字体资源）
	private Font 取字体()
	{
		foreach (Text 文本 in GetComponentsInChildren<Text>(includeInactive: true))
		{
			if (文本.font != null)
			{
				return 文本.font;
			}
		}
		try
		{
			return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		}
		catch (System.Exception)
		{
			try
			{
				return Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
			catch (System.Exception)
			{
				return null;
			}
		}
	}
}
