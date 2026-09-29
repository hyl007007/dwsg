using UnityEngine;
using UnityEngine.SceneManagement;

namespace 缺失界面.窗口4
{
    [DefaultExecutionOrder(-100)]
    public sealed class 驻防调度组件 : MonoBehaviour
    {
        private long 上次推进 = -1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void 安装()
        {
            SceneManager.sceneLoaded -= 场景载入;
            SceneManager.sceneLoaded += 场景载入;
            场景载入(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void 场景载入(Scene 场景, LoadSceneMode 模式)
        {
            bool 有主界面 = false;
            foreach (var 根 in 场景.GetRootGameObjects())
            {
                if (根.GetComponent<驻防调度组件>() != null) return;
                if (根.GetComponent<主界面UI脚本>() != null) 有主界面 = true;
            }
            if (!有主界面) return;
            var 调度 = new GameObject("驻防行军调度");
            SceneManager.MoveGameObjectToScene(调度, 场景);
            调度.AddComponent<驻防调度组件>();
        }
        private void Update()
        {
            if (Dwsg.Network.GameNetwork.Enabled) return;
            long 现在 = TIME.getTime();
            if (现在 == 上次推进) return;
            上次推进 = 现在;
            和平驻防规则.推进(现在);
        }
    }
}
