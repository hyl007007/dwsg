using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class 界面窗口管理器 : MonoBehaviour
{
    private static 界面窗口管理器 当前场景管理器;

    public static bool 有活动功能窗口
    {
        get
        {
            var 管理器 = 当前场景管理器;
            return 管理器 != null && 管理器.gameObject.scene == SceneManager.GetActiveScene() &&
                管理器.当前窗口 != null && 管理器.当前窗口.gameObject.activeInHierarchy;
        }
    }
    private readonly List<界面互斥窗口> 窗口列表 = new List<界面互斥窗口>();
    private readonly List<界面互斥窗口> 返回路径 = new List<界面互斥窗口>();
    private 界面互斥窗口 当前窗口;
    private 界面互斥窗口 点击来源;
    private 界面互斥窗口 刚关闭的窗口;
    private 主界面UI脚本 主界面;
    private bool 有点击来源;
    private bool 指针按下;
    private bool 正在切换;
    private int 释放帧;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void 安装()
    {
        SceneManager.sceneLoaded -= 安装场景;
        SceneManager.sceneLoaded += 安装场景;
        安装场景(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void 安装场景(Scene 场景, LoadSceneMode 模式)
    {
        主界面UI脚本 主界面对象 = null;
        foreach (GameObject 根对象 in 场景.GetRootGameObjects())
        {
            if (根对象.GetComponent<界面窗口管理器>() != null) return;
            var 脚本 = 根对象.GetComponent<主界面UI脚本>();
            if (脚本 != null) 主界面对象 = 脚本;
        }
        if (主界面对象 == null) return;
        var 对象 = new GameObject("界面窗口管理器");
        SceneManager.MoveGameObjectToScene(对象, 场景);
        对象.AddComponent<界面窗口管理器>().初始化(场景, 主界面对象);
    }

    private void 初始化(Scene 场景, 主界面UI脚本 主界面对象)
    {
        当前场景管理器 = this;
        主界面 = 主界面对象;
        foreach (GameObject 根对象 in 场景.GetRootGameObjects())
        {
            Canvas 画布 = 根对象.GetComponent<Canvas>();
            // 主场景的 2–9 层是功能窗口；地图、战场和 10 层提示独立运行。
            if (画布 == null || 画布.renderMode != RenderMode.ScreenSpaceOverlay ||
                画布.sortingOrder < 2 || 画布.sortingOrder >= 10 || 根对象.name == "战斗结束结算界面") continue;
            var 窗口 = 根对象.AddComponent<界面互斥窗口>();
            窗口.管理器 = this;
            窗口列表.Add(窗口);
        }
        foreach (GameObject 根对象 in 场景.GetRootGameObjects())
        {
            foreach (Button 按钮 in 根对象.GetComponentsInChildren<Button>(true))
            {
                绑定点击来源(按钮.gameObject);
                // 在原有按钮动作结束后恢复父窗口，保留原有数据刷新和返回动作。
                按钮.onClick.AddListener(尝试返回上级);
            }
            foreach (EventTrigger 点击事件 in 根对象.GetComponentsInChildren<EventTrigger>(true))
                绑定点击来源(点击事件.gameObject);
        }
        foreach (var 窗口 in 窗口列表)
            if (窗口.gameObject.activeInHierarchy) 打开窗口(窗口);
    }

    private void 绑定点击来源(GameObject 对象)
    {
        var 来源 = 对象.GetComponent<界面点击来源>();
        if (来源 == null) 来源 = 对象.AddComponent<界面点击来源>();
        来源.管理器 = this;
        来源.所属窗口 = 对象.GetComponentInParent<界面互斥窗口>(true);
    }

    // 运行时搭建的界面不会参与场景初始扫描，完成搭建后通过此入口接入互斥与返回路径。
    public static bool 注册运行时窗口(GameObject 对象)
    {
        if (对象 == null || 对象.transform.parent != null ||
            对象.scene != SceneManager.GetActiveScene()) return false;
        Canvas 画布 = 对象.GetComponent<Canvas>();
        if (画布 == null || 画布.renderMode != RenderMode.ScreenSpaceOverlay ||
            画布.sortingOrder < 2 || 画布.sortingOrder >= 10) return false;
        var 管理器 = 获取当前场景管理器();
        if (管理器 == null) return false;
        var 窗口 = 对象.GetComponent<界面互斥窗口>();
        if (窗口 == null) 窗口 = 对象.AddComponent<界面互斥窗口>();
        if (窗口.管理器 != null && 窗口.管理器 != 管理器) return false;
        窗口.管理器 = 管理器;
        if (!管理器.窗口列表.Contains(窗口)) 管理器.窗口列表.Add(窗口);
        foreach (Button 按钮 in 对象.GetComponentsInChildren<Button>(true))
            管理器.接入按钮(按钮);
        foreach (EventTrigger 点击事件 in 对象.GetComponentsInChildren<EventTrigger>(true))
            管理器.绑定点击来源(点击事件.gameObject);
        if (对象.activeInHierarchy) 管理器.打开窗口(窗口);
        return true;
    }

    // 列表后续生成的行按钮同样记录点击来源；重复注册不会累积返回监听。
    public static bool 注册运行时按钮(Button 按钮)
    {
        var 管理器 = 获取当前场景管理器();
        if (管理器 == null || 按钮 == null ||
            按钮.gameObject.scene != SceneManager.GetActiveScene()) return false;
        管理器.接入按钮(按钮);
        return true;
    }

    private void 接入按钮(Button 按钮)
    {
        绑定点击来源(按钮.gameObject);
        按钮.onClick.RemoveListener(尝试返回上级);
        按钮.onClick.AddListener(尝试返回上级);
    }

    private static 界面窗口管理器 获取当前场景管理器()
    {
        if (当前场景管理器 != null && 当前场景管理器.gameObject.scene == SceneManager.GetActiveScene())
            return 当前场景管理器;
        foreach (GameObject 根对象 in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var 管理器 = 根对象.GetComponent<界面窗口管理器>();
            if (管理器 != null) { 当前场景管理器 = 管理器; return 管理器; }
        }
        return null;
    }

    internal void 记录点击来源(界面互斥窗口 来源)
    {
        点击来源 = 来源;
        刚关闭的窗口 = null;
        有点击来源 = true;
        指针按下 = true;
    }

    internal void 释放指针()
    {
        指针按下 = false;
        释放帧 = Time.frameCount;
    }

    internal void 打开窗口(界面互斥窗口 窗口)
    {
        if (正在切换 || 当前窗口 == 窗口) return;
        var 来源 = 点击来源;
        if (!有点击来源 && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            来源 = EventSystem.current.currentSelectedGameObject.GetComponentInParent<界面互斥窗口>(true);

        int 返回位置 = 返回路径.IndexOf(窗口);
        if (来源 != null && 返回位置 >= 0)
            返回路径.RemoveRange(返回位置, 返回路径.Count - 返回位置);
        else if (当前窗口 != null && 来源 == 当前窗口)
        {
            返回路径.Add(当前窗口);
        }
        else if (当前窗口 == null && 来源 != null && 来源 == 刚关闭的窗口) 返回路径.Add(来源);
        else 返回路径.Clear();

        当前窗口 = 窗口;
        正在切换 = true;
        try
        {
            foreach (var 旧窗口 in 窗口列表)
                if (旧窗口 != 窗口 && 旧窗口 != null && 旧窗口.gameObject.activeSelf)
                    旧窗口.gameObject.SetActive(false);
        }
        finally { 正在切换 = false; }
    }

    internal void 关闭窗口(界面互斥窗口 窗口)
    {
        if (!正在切换 && 当前窗口 == 窗口)
        {
            刚关闭的窗口 = 窗口;
            当前窗口 = null;
        }
    }

    private void 尝试返回上级()
    {
        if (this == null || 正在切换 || 主界面 == null || !主界面.gameObject.activeInHierarchy) return;
        // 同一个原窗口切换查看对象不会再次触发 OnEnable；主导航点击仍应结束旧返回路径。
        if (当前窗口 != null)
        {
            if (有点击来源 && 点击来源 == null) 返回路径.Clear();
            return;
        }
        while (返回路径.Count > 0)
        {
            int 最后 = 返回路径.Count - 1;
            var 父窗口 = 返回路径[最后];
            返回路径.RemoveAt(最后);
            if (父窗口 == null) continue;
            当前窗口 = 父窗口;
            正在切换 = true;
            try { 父窗口.gameObject.SetActive(true); }
            finally { 正在切换 = false; }
            break;
        }
    }

    public static void 关闭当前场景窗口()
    {
        foreach (var 根对象 in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var 管理器 = 根对象.GetComponent<界面窗口管理器>();
            if (管理器 != null) 管理器.关闭所有窗口();
        }
    }

    private void 关闭所有窗口()
    {
        返回路径.Clear();
        当前窗口 = null;
        刚关闭的窗口 = null;
        正在切换 = true;
        try
        {
            foreach (var 窗口 in 窗口列表)
                if (窗口 != null && 窗口.gameObject.activeSelf) 窗口.gameObject.SetActive(false);
        }
        finally { 正在切换 = false; }
    }

    private void LateUpdate()
    {
        if (主界面 != null && !主界面.gameObject.activeInHierarchy)
        {
            if (当前窗口 != null || 返回路径.Count > 0) 关闭所有窗口();
        }
        else 尝试返回上级();
        刚关闭的窗口 = null;
        if (有点击来源 && !指针按下 && 释放帧 <= Time.frameCount)
        {
            有点击来源 = false;
            点击来源 = null;
        }
    }

    private void OnDestroy()
    {
        if (当前场景管理器 == this) 当前场景管理器 = null;
    }
}
