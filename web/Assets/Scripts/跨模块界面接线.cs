using System;
using System.Collections;
using Dwsg.Window3;
using UnityEngine;
using UnityEngine.SceneManagement;
using 缺失界面.窗口2;

// 城池页复用国家/玩家详情，保留模块自身的无数据回退与共同返回路径。
public sealed class 跨模块界面接线 : MonoBehaviour
{
    private Func<string, bool> 国家入口;
    private Func<int, bool> 城主入口;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void 安装()
    {
        SceneManager.sceneLoaded -= 安装场景;
        SceneManager.sceneLoaded += 安装场景;
        安装场景(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void 安装场景(Scene scene, LoadSceneMode mode)
    {
        bool main = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<跨模块界面接线>() != null) return;
            if (root.GetComponent<主界面UI脚本>() != null) main = true;
        }
        if (!main) return;
        var host = new GameObject("跨模块界面接线");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<跨模块界面接线>();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        var installer = CityNavigation.Find<NationUiInstaller>();
        if (installer == null || installer.Window == null) yield break;
        var window = installer.Window;
        国家入口 = code =>
        {
            if (window == null || string.IsNullOrEmpty(code)) return false;
            return window.OpenExistingNation(code);
        };
        城主入口 = id =>
        {
            // 本机城主继续使用原君主页；其他角色才打开缺失的公开名片。
            var me = CityLocalAdapter.Me;
            if (me != null && me.基础信息 != null && me.基础信息.ID == id) return false;
            return window != null && window.OpenPlayer(id);
        };
        CityNavigation.ShowNation = 国家入口;
        CityNavigation.ShowLord = 城主入口;
    }

    private void OnDestroy()
    {
        if (CityNavigation.ShowNation == 国家入口) CityNavigation.ShowNation = null;
        if (CityNavigation.ShowLord == 城主入口) CityNavigation.ShowLord = null;
    }
}
