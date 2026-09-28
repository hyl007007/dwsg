#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// ================================================================
// 军情通知角标的小工具
//
// 角标平时是运行时装配的（全局任务脚本 在 Start 里调 军情通知角标.装配）。
// 这个菜单只是额外给一条「烤进场景」的路：跑完角标就是场景里的真实物体，
// 底图、尺寸、位置、字号都能在 Inspector / Scene 视图里直接调。
// （跑完记得 Ctrl+S 保存场景。）
// ================================================================
public static class 军情角标工具
{
	[MenuItem("工具/军情通知角标/按选中的按钮生成", false, 200)]
	private static void 按选中的按钮生成()
	{
		GameObject 选中物体 = Selection.activeGameObject;
		if (选中物体 == null)
		{
			EditorUtility.DisplayDialog("军情通知角标", "先在 Hierarchy 里选中世界地图左下角的「主界面_军情」按钮。", "知道了");
			return;
		}
		Transform 红点 = 找红点(选中物体.transform);
		if (红点 == null)
		{
			EditorUtility.DisplayDialog("军情通知角标", "这个物体下面没找到红点（原来叫 Image 的那个子物体）。", "知道了");
			return;
		}
		Undo.RegisterFullObjectHierarchyUndo(选中物体, "生成军情通知角标");
		军情通知角标.装配(红点.gameObject);
		预览一下(红点.gameObject);
		EditorSceneManager.MarkSceneDirty(选中物体.scene);
		Debug.Log("军情通知角标已生成，记得保存场景（Ctrl+S）。", 选中物体);
	}

	[MenuItem("工具/军情通知角标/选中角标看细节", false, 201)]
	private static void 选中角标看细节()
	{
		GameObject 选中物体 = Selection.activeGameObject;
		Transform 红点 = ((选中物体 == null) ? null : 找红点(选中物体.transform));
		if (红点 == null)
		{
			EditorUtility.DisplayDialog("军情通知角标", "先选中「主界面_军情」按钮。", "知道了");
			return;
		}
		Selection.activeGameObject = 红点.gameObject;
		EditorGUIUtility.PingObject(红点.gameObject);
	}

	private static Transform 找红点(Transform 根)
	{
		foreach (Transform 子物体 in 根)
		{
			if (子物体.name == "Image" || 子物体.name == "军情红点" || 子物体.name == "通知角标")
			{
				return 子物体;
			}
		}
		return null;
	}

	//编辑模式下军情列表是空的、角标会被自动隐藏，这里手动摆成「有 9 条通知」的样子，方便调尺寸位置
	private static void 预览一下(GameObject 红点)
	{
		红点.SetActive(value: true);
		Text 数字 = 红点.GetComponentInChildren<Text>(includeInactive: true);
		if (数字 != null)
		{
			数字.text = "9";
		}
	}
}
#endif