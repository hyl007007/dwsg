using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Android 的 Back 留在游戏内，并让触摸继续由原 EventSystem 和按钮回调处理。
[DefaultExecutionOrder(-10000)]
public sealed class 安卓输入适配 : MonoBehaviour
{
    private static 安卓输入适配 实例;
    private static bool 上帧键盘可见;
    private static int 键盘判定帧 = -1, 弹窗判定帧 = -1;
    private static bool 本帧键盘消耗返回;
    private static 原界面小弹窗 本帧顶层弹窗;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void 安装()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        Input.backButtonLeavesApp = false;
        if (实例 != null) return;
        var 对象 = new GameObject("安卓输入适配");
        DontDestroyOnLoad(对象);
        实例 = 对象.AddComponent<安卓输入适配>();
    }

    private void Update()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        bool 键盘可见 = TouchScreenKeyboard.visible;
        if (Input.GetKeyDown(KeyCode.Escape) && !返回键由软键盘消费() &&
            顶层小弹窗() == null && !界面窗口管理器.处理安卓返回())
            聊天系统.尝试关闭当前聊天();
        上帧键盘可见 = 键盘可见;
    }

    // IME 收起与 Escape 可能发生在同一帧；结果缓存供小弹窗的 Update 共用。
    public static bool 返回键由软键盘消费()
    {
        if (Application.platform != RuntimePlatform.Android) return false;
        if (键盘判定帧 == Time.frameCount) return 本帧键盘消耗返回;
        键盘判定帧 = Time.frameCount;
        var 选中 = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
        var 输入 = 选中 == null ? null : 选中.GetComponentInParent<InputField>();
        本帧键盘消耗返回 = TouchScreenKeyboard.visible || 上帧键盘可见 ||
            (输入 != null && 输入.isFocused);
        if (本帧键盘消耗返回 && 输入 != null) 输入.DeactivateInputField();
        return 本帧键盘消耗返回;
    }

    // 同帧缓存顶层弹窗，避免关闭它后下层弹窗再次响应同一次 Back。
    public static bool 小弹窗处理返回(原界面小弹窗 弹窗)
    {
        if (Application.platform != RuntimePlatform.Android) return true;
        return !返回键由软键盘消费() && 顶层小弹窗() == 弹窗;
    }

    private static 原界面小弹窗 顶层小弹窗()
    {
        if (弹窗判定帧 == Time.frameCount) return 本帧顶层弹窗;
        弹窗判定帧 = Time.frameCount;
        本帧顶层弹窗 = null;
        foreach (var 候选 in FindObjectsOfType<原界面小弹窗>())
        {
            if (!候选.gameObject.activeInHierarchy) continue;
            if (本帧顶层弹窗 == null || 候选.transform.IsChildOf(本帧顶层弹窗.transform))
                本帧顶层弹窗 = 候选;
            else if (!本帧顶层弹窗.transform.IsChildOf(候选.transform))
            {
                var 候选画布 = 候选.GetComponent<Canvas>();
                var 顶层画布 = 本帧顶层弹窗.GetComponent<Canvas>();
                int 候选顺序 = 候选画布 == null ? 0 : 候选画布.sortingOrder;
                int 顶层顺序 = 顶层画布 == null ? 0 : 顶层画布.sortingOrder;
                if (候选顺序 > 顶层顺序 ||
                    (候选顺序 == 顶层顺序 && 候选.transform.GetSiblingIndex() > 本帧顶层弹窗.transform.GetSiblingIndex()))
                    本帧顶层弹窗 = 候选;
            }
        }
        return 本帧顶层弹窗;
    }

    private void OnDestroy()
    {
        if (实例 == this) 实例 = null;
    }
}
