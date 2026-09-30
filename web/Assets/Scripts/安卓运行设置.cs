using UnityEngine;

// 安卓端随首个场景安装，跨登录和游戏场景保留；不接管网络连接或游戏内音乐开关。
public sealed class 安卓运行设置 : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static 安卓运行设置 实例;
    private bool 已暂停音频;
    private bool 暂停前音频状态;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void 安装()
    {
        if (实例 == null) new GameObject("安卓运行设置").AddComponent<安卓运行设置>();
    }

    private void Awake()
    {
        if (实例 != null && 实例 != this) { Destroy(gameObject); return; }
        实例 = this;
        DontDestroyOnLoad(gameObject);
        // 原启动脚本已经使用 60 帧；首场景加载前也采用同一上限。
        Application.targetFrameRate = 60;
    }

    private void OnApplicationPause(bool 暂停)
    {
        if (!暂停) { 恢复音频(); return; }
        if (已暂停音频) return;
        暂停前音频状态 = AudioListener.pause;
        AudioListener.pause = true;
        已暂停音频 = true;
    }

    private void OnDestroy()
    {
        if (实例 != this) return;
        恢复音频();
        实例 = null;
    }

    private void 恢复音频()
    {
        if (!已暂停音频) return;
        AudioListener.pause = 暂停前音频状态;
        已暂停音频 = false;
    }
#endif
}
