#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ================================================================
// 战斗倍速面板的小工具
//
// 倍速面板平时是运行时用代码生成的，这个菜单只是额外给一条"烤进场景"的路：
// 生成之后面板就是场景里的真实物体，可以在 Scene 视图里随便挪位置、换图、调色。
// （生成出来的面板默认是隐藏的，要在 Hierarchy 里选中它、或者在 Scene 视图里
//   打开右上角的"显示隐藏物体"才看得见。）
// ================================================================
public static class 战斗倍速面板工具
{
	[MenuItem("工具/战斗倍速面板/生成到场景", false, 100)]
	private static void 生成到场景()
	{
		if (EditorApplication.isPlaying)
		{
			EditorUtility.DisplayDialog("战斗倍速面板", "请先退出播放模式再生成。", "知道了");
			return;
		}
		GameObject 选中物体 = Selection.activeGameObject;
		战斗速度设置 脚本 = ((选中物体 == null) ? null : 选中物体.GetComponent<战斗速度设置>());
		if (脚本 == null)
		{
			EditorUtility.DisplayDialog("战斗倍速面板", "请先在 Hierarchy 里选中挂着【战斗速度设置】脚本的物体（一般叫 战斗界面UI）。", "知道了");
			return;
		}
		if (脚本.设置面板对象 != null)
		{
			EditorUtility.DisplayDialog("战斗倍速面板", "这个物体上已经有倍速面板了，不用重复生成。", "知道了");
			return;
		}
		Undo.RegisterFullObjectHierarchyUndo(选中物体, "生成战斗倍速面板");
		脚本.生成面板();
		EditorUtility.SetDirty(脚本);
		EditorSceneManager.MarkSceneDirty(选中物体.scene);
		Debug.Log("战斗倍速面板已生成，记得保存场景（Ctrl+S）。", 选中物体);
	}

	[MenuItem("工具/战斗倍速面板/选中已有的面板", false, 101)]
	private static void 选中已有的面板()
	{
		GameObject 选中物体 = Selection.activeGameObject;
		战斗速度设置 脚本 = ((选中物体 == null) ? null : 选中物体.GetComponent<战斗速度设置>());
		if (脚本 == null || 脚本.设置面板对象 == null)
		{
			EditorUtility.DisplayDialog("战斗倍速面板", "先在 Hierarchy 里选中挂着【战斗速度设置】脚本的物体。", "知道了");
			return;
		}
		Selection.activeGameObject = 脚本.设置面板对象;
		EditorGUIUtility.PingObject(脚本.设置面板对象);
	}
}
#endif