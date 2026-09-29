using System.Collections;
using Dwsg.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dwsg.Network
{
    public sealed class GameNetworkRunner : MonoBehaviour
    {
        private static GameNetworkRunner instance;
        private bool returningToLogin;
        private void OnEnable() { GameNetwork.StatusChanged += OnStatusChanged; }
        private void OnDisable() { GameNetwork.StatusChanged -= OnStatusChanged; }
        private void OnStatusChanged(GameResult result)
        {
            if (result == null || (result.Code != GameCodes.Unauthenticated && result.Code != GameCodes.SessionReplaced)) return;
            全局变量.是否为登录 = false;
            if (!returningToLogin && SceneManager.GetActiveScene().buildIndex != 0)
                StartCoroutine(ReturnToLogin(result.Message));
        }
        private IEnumerator ReturnToLogin(string message)
        {
            returningToLogin = true;
            // 使用原登录布局重新认证，不让已经失效的角色继续停在可操作界面。
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var login in root.GetComponentsInChildren<登录脚本>(true))
                {
                    login.准备登录入口();
                    if (login.log != null) login.log.text = string.IsNullOrEmpty(message) ? "会话已失效，请重新登录。" : message;
                }
            returningToLogin = false;
        }
        internal static void StartWork(IEnumerator routine)
        {
            if (instance == null)
            {
                var runner = new GameObject("Game network") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(runner);
                instance = runner.AddComponent<GameNetworkRunner>();
            }
            instance.StartCoroutine(routine);
        }
    }
}
