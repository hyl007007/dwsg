using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class 创建武将脚本 : MonoBehaviour
{
    public Image 将领头像;
    private List<Texture2D> texure = new List<Texture2D>();
    private List<Sprite> sprites = new List<Sprite>();
    private List<Sprite> 自制精灵 = new List<Sprite>();
    private Sprite 原头像;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void 限制自制工具入口()
    {
#if !UNITY_EDITOR
        SceneManager.sceneLoaded -= 隐藏自制工具入口;
        SceneManager.sceneLoaded += 隐藏自制工具入口;
        隐藏自制工具入口(SceneManager.GetActiveScene(), LoadSceneMode.Single);
#endif
    }

#if !UNITY_EDITOR
    private static void 隐藏自制工具入口(Scene 场景, LoadSceneMode 模式)
    {
        foreach (var 根 in 场景.GetRootGameObjects())
        {
            if (根.name != "开始游戏方式") continue;
            var 入口 = 根.transform.Find("创建武将");
            if (入口 == null) continue;
            var 按钮 = 入口.GetComponent<Button>();
            if (按钮 != null) 按钮.interactable = false;
            入口.gameObject.SetActive(false);
        }
    }
#endif

    void Start()
    {
        原头像 = 将领头像 == null ? null : 将领头像.sprite;
        var 保存节点 = transform.Find("保存按钮");
        var 保存按钮 = 保存节点 == null ? null : 保存节点.GetComponent<Button>();
        if (保存按钮 != null)
        {
            保存按钮.onClick = new Button.ButtonClickedEvent();
            保存按钮.interactable = false;
        }
        LoadTexture();
#if UNITY_EDITOR
        存档脚本.显示操作提示("自制武将仅供编辑器预览，保存尚未接通。");
#endif
    }

    void Update()
    {
        
    }

    public void 切换头像()
    {
        if (将领头像 != null && sprites.Count > 0)
        {
            将领头像.sprite = sprites[0];
            print(sprites.Count);
        }
    }

    private static byte[] GetImageByte(string imagePath)
    {
        return File.ReadAllBytes(imagePath);
    }

    private List<string> getImagePath()
    {
        List<string> filePath = new List<string>();
        string 目录 = Path.Combine(Application.temporaryCachePath, "头像资源");
        if (!Directory.Exists(目录)) return filePath;
        var 已找到 = new HashSet<string>();
        string imgType = "*.JPG|*.PNG|*.jpg|*.png";
        string[] imageType = imgType.Split('|');
        try
        {
            for (int i = 0; i < imageType.Length; i++)
            {
                string[] dirs = Directory.GetFiles(目录, imageType[i]);
                for (int j = 0; j < dirs.Length; j++)
                    if (已找到.Add(dirs[j])) filePath.Add(dirs[j]);
            }
        }
        catch (System.Exception 异常) when (异常 is IOException || 异常 is System.UnauthorizedAccessException)
        {
            Debug.LogWarning("自制头像目录无法读取，沿用工程头像：" + 异常.GetType().Name);
        }

        return filePath;
    }

    private void LoadTexture()
    {
        释放自制资源();
        List<string> filePaths = getImagePath();
        for (int i = 0; i < filePaths.Count; i++)
        {
            Texture2D texture2 = null;
            try
            {
                byte[] bytes = GetImageByte(filePaths[i]);
                texture2 = new Texture2D(48, 48);
                if (!texture2.LoadImage(bytes))
                {
                    Destroy(texture2);
                    continue;
                }
                Sprite sprite = Sprite.Create(texture2,
                    new Rect(0, 0, Mathf.Min(48, texture2.width), Mathf.Min(48, texture2.height)), Vector2.zero);
                texure.Add(texture2);
                自制精灵.Add(sprite);
                sprites.Add(sprite);
            }
            catch (System.Exception 异常) when (异常 is IOException || 异常 is System.UnauthorizedAccessException ||
                异常 is System.ArgumentException || 异常 is UnityException)
            {
                if (texture2 != null) Destroy(texture2);
                Debug.LogWarning("自制头像无法读取，已跳过：" + 异常.GetType().Name);
            }
        }
        if (sprites.Count == 0)
        {
            if (原头像 != null) sprites.Add(原头像);
            else if (全局变量.所有头像资源表 != null)
                foreach (var 头像 in 全局变量.所有头像资源表)
                    if (头像 != null) { sprites.Add(头像); break; }
            if (将领头像 != null && sprites.Count > 0) 将领头像.sprite = sprites[0];
        }
    }

    private void 释放自制资源()
    {
        if (将领头像 != null && 自制精灵.Contains(将领头像.sprite)) 将领头像.sprite = 原头像;
        foreach (var 精灵 in 自制精灵) if (精灵 != null) Destroy(精灵);
        foreach (var 贴图 in texure) if (贴图 != null) Destroy(贴图);
        自制精灵.Clear();
        texure.Clear();
        sprites.Clear();
    }

    private void OnDestroy()
    {
        释放自制资源();
    }
}
