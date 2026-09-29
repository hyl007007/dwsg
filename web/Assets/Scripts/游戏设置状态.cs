using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 原设置页继续使用原有按钮、Grid 和说明栏，只补齐当前状态与静音的作用范围。
public sealed class 游戏设置状态 : MonoBehaviour
{
    private const string 音乐偏好 = "DWSG.MusicMuted";
    private 主界面UI脚本 主界面;
    private Text 说明;
    private Button 音乐开, 音乐关, 普通难度, 挑战难度;
    private RectTransform 音乐开选中, 音乐关选中, 普通选中, 挑战选中;

    public void 初始化(主界面UI脚本 来源)
    {
        if (主界面 != null) return;
        主界面 = 来源;
        var 说明对象 = transform.Find("说明文本");
        if (说明对象 != null) 说明 = 说明对象.GetComponent<Text>();
        音乐开 = 按钮("音乐开"); 音乐关 = 按钮("音乐关");
        普通难度 = 按钮("难度低"); 挑战难度 = 按钮("难度高");
        var 选中模板 = 原选中框();
        音乐开选中 = 配置选中框(音乐开, 选中模板); 音乐关选中 = 配置选中框(音乐关, 选中模板);
        普通选中 = 配置选中框(普通难度, 选中模板); 挑战选中 = 配置选中框(挑战难度, 选中模板);
        if (音乐开 != null) 音乐开.onClick.AddListener(开启音乐);
        if (音乐关 != null) 音乐关.onClick.AddListener(关闭音乐);
        if (普通难度 != null) 普通难度.onClick.AddListener(刷新);
        if (挑战难度 != null) 挑战难度.onClick.AddListener(刷新);
        应用静音(PlayerPrefs.GetInt(音乐偏好, 主界面.背景音乐对象.mute ? 1 : 0) != 0);
        刷新();
    }

    private static RectTransform 原选中框()
    {
        foreach (var 根 in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var 招募 = 根.GetComponentInChildren<招募将领>(true);
            if (招募 == null || 招募.将领列表对象 == null || 招募.将领列表对象.transform.childCount == 0) continue;
            return 招募.将领列表对象.transform.GetChild(0).Find("将领选中背景") as RectTransform;
        }
        Debug.LogWarning("游戏设置缺少原招募将领选中框模板");
        return null;
    }

    private static RectTransform 配置选中框(Button 按钮, RectTransform 模板)
    {
        if (按钮 == null || 模板 == null) return null;
        // 原八段边框随卡片锚点伸展，保留原颜色、材质和角片，不覆盖设置插图或接管点击。
        var 边框 = Instantiate(模板, 按钮.transform.parent, false);
        边框.name = "当前选中边框";
        边框.anchorMin = Vector2.zero; 边框.anchorMax = Vector2.one;
        边框.offsetMin = 边框.offsetMax = Vector2.zero; 边框.localScale = Vector3.one;
        foreach (var 图 in 边框.GetComponentsInChildren<Graphic>(true)) 图.raycastTarget = false;
        边框.SetAsLastSibling(); 边框.gameObject.SetActive(false);
        return 边框;
    }

    private static void 设置选中(RectTransform 边框, bool 当前)
    { if (边框 != null) 边框.gameObject.SetActive(当前); }

    private Button 按钮(string 名字)
    {
        var 节点 = transform.Find("设置列表布局/" + 名字 + "/设置按钮");
        return 节点 == null ? null : 节点.GetComponent<Button>();
    }

    private void 开启音乐() { 保存静音(false); }
    private void 关闭音乐() { 保存静音(true); }

    private void 保存静音(bool 静音)
    {
        应用静音(静音);
        PlayerPrefs.SetInt(音乐偏好, 静音 ? 1 : 0);
        PlayerPrefs.Save();
        刷新();
    }

    private void 应用静音(bool 静音)
    {
        // 原按钮引用的两路音乐仍按原接线处理，避免把其他提示音当作背景音乐。
        if (音乐开 != null)
            for (int i = 0; i < 音乐开.onClick.GetPersistentEventCount(); i++)
            {
                var 原音乐 = 音乐开.onClick.GetPersistentTarget(i) as AudioSource;
                if (原音乐 != null && 音乐开.onClick.GetPersistentMethodName(i) == "set_mute") 原音乐.mute = 静音;
            }
        if (主界面 != null && 主界面.背景音乐对象 != null) 主界面.背景音乐对象.mute = 静音;
        if (全局变量.战斗界面UI对象 != null)
        {
            var 战场音乐 = 全局变量.战斗界面UI对象.GetComponent<AudioSource>();
            if (战场音乐 != null) 战场音乐.mute = 静音;
        }
    }

    private void OnEnable() { 刷新(); }

    private void 刷新()
    {
        if (主界面 == null || 主界面.背景音乐对象 == null) return;
        bool 静音 = 主界面.背景音乐对象.mute;
        bool 挑战 = 全局变量.难度 == 2;
        if (说明 != null)
        {
            说明.text = "音乐：" + (静音 ? "关闭" : "开启") + "　　难度：" + (挑战 ? "挑战" : "普通");
            说明.raycastTarget = false;
            var 原标签 = 音乐开 == null ? null : 音乐开.transform.parent.Find("设置名字");
            var 原文字 = 原标签 == null ? null : 原标签.GetComponent<Text>();
            if (原文字 != null) { 说明.color = 原文字.color; 说明.font = 原文字.font; }
        }
        设置选中(音乐开选中, !静音); 设置选中(音乐关选中, 静音);
        设置选中(普通选中, !挑战); 设置选中(挑战选中, 挑战);
        if (音乐开 != null) 音乐开.interactable = 静音;
        if (音乐关 != null) 音乐关.interactable = !静音;
        if (普通难度 != null) 普通难度.interactable = 挑战;
        if (挑战难度 != null) 挑战难度.interactable = !挑战;
    }
}
