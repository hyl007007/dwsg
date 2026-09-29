using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class 存档脚本 : MonoBehaviour
{
    public GameObject 存档列表对象;
    private const int 存档槽位数 = 6;
    private bool 正在写入;
    private bool 已设置读取说明;

    private void OnEnable()
    {
        if (已设置读取说明) return;
        var 原按钮 = transform.Find("进入游戏") as RectTransform;
        if (原按钮 == null) return;
        var 按钮 = 原按钮.GetComponent<Button>();
        if (按钮 != null)
        {
            按钮.interactable = false;
            按钮.enabled = false;
        }
        原按钮.gameObject.SetActive(false);
        已设置读取说明 = true;

        var 模板 = 存档列表对象 == null ? null : 存档列表对象.GetComponentInChildren<Text>(true);
        if (模板 == null)
        {
            Debug.LogWarning("读取存档页缺少原列表文字，无法显示读取说明。");
            return;
        }
        var 说明 = Instantiate(模板, transform);
        说明.name = "读取存档说明";
        说明.text = "点击存档槽位直接读取";
        说明.alignment = TextAnchor.MiddleCenter;
        说明.fontSize = Mathf.Min(模板.fontSize, 18);
        说明.raycastTarget = false;
        var 框 = 说明.rectTransform;
        框.anchorMin = 原按钮.anchorMin;
        框.anchorMax = 原按钮.anchorMax;
        框.pivot = 原按钮.pivot;
        框.anchoredPosition = 原按钮.anchoredPosition;
        框.sizeDelta = 原按钮.sizeDelta;
        框.localScale = 原按钮.localScale;
        框.localRotation = 原按钮.localRotation;
        说明.gameObject.SetActive(true);
    }

    public static void 显示操作提示(string 内容)
    {
        var 场景 = SceneManager.GetActiveScene();
        var 提示 = 全局变量.提示类;
        if (提示 == null || 提示.gameObject.scene != 场景 || !提示.isActiveAndEnabled)
        {
            提示 = null;
            foreach (var 根 in 场景.GetRootGameObjects())
            {
                foreach (var 当前提示 in 根.GetComponentsInChildren<提示移动>(true))
                {
                    if (!当前提示.isActiveAndEnabled) continue;
                    提示 = 当前提示;
                    break;
                }
                if (提示 != null) break;
            }
            if (提示 != null) 全局变量.提示类 = 提示;
        }
        if (提示 != null) 提示.显示信息(内容);
        else Debug.LogWarning(内容);
    }

    private bool 可以操作()
    {
        if (全局变量.当局赌场下注列表.Count == 0) return true;
        显示操作提示("请等待赌场结束");
        return false;
    }

    private static string 文件路径(int 槽位)
    {
        return Path.Combine(Application.persistentDataPath, "存档" + 槽位 + ".txt");
    }

    private static bool 尝试读取(string 路径, int 槽位, out 存档信息库类 存档)
    {
        存档 = null;
        if (!File.Exists(路径)) return false;
        try
        {
            var 数据 = JsonConvert.DeserializeObject<存档信息库类>(加密.DecryptString(File.ReadAllText(路径)));
            if (数据 == null || 数据.ID != 槽位 || 数据.存档版本 != 1 ||
                数据.国家列表 == null || 数据.城池列表 == null || 数据.玩家列表 == null ||
                全局变量.本机身份 < 0 || 数据.玩家列表.Count <= 全局变量.本机身份 ||
                数据.玩家列表[全局变量.本机身份] == null || 数据.玩家列表[全局变量.本机身份].基础信息 == null)
                return false;
            存档 = 数据;
            return true;
        }
        catch (Exception 异常) when (异常 is IOException || 异常 is UnauthorizedAccessException ||
            异常 is FormatException || 异常 is CryptographicException || 异常 is JsonException || 异常 is ArgumentException)
        {
            Debug.LogWarning("存档" + 槽位 + "无法读取：" + 异常.GetType().Name);
            return false;
        }
    }

    public void 刷新存档列表()
    {
        if (!可以操作()) return;
        读取所有存档();
        显示所有存档();
    }

    public void 读取所有存档()
    {
        if (!可以操作()) return;
        全局变量.所有存档列表.Clear();
        for (int 槽位 = 1; 槽位 <= 存档槽位数; 槽位++)
        {
            存档信息库类 存档;
            if (尝试读取(文件路径(槽位), 槽位, out 存档))
                全局变量.所有存档列表.Add(存档);
            else if (尝试读取(文件路径(槽位) + ".bak", 槽位, out 存档))
            {
                全局变量.所有存档列表.Add(存档);
                Debug.LogWarning("存档" + 槽位 + "已从备份读取，请重新保存。");
            }
        }
    }

    public void 显示所有存档()
    {
        if (!可以操作() || 存档列表对象 == null) return;
        for (int i = 0; i < 存档槽位数; i++)
        {
            var 标签 = 存档列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>();
            var 存档 = 全局变量.所有存档列表.Find(记录 => 记录.ID == i + 1);
            标签.text = "存档" + (i + 1) + "：<空记录>";
            if (存档 != null)
            {
                var 玩家 = 存档.玩家列表[全局变量.本机身份].基础信息;
                标签.text = "存档" + (i + 1) + "：" + 玩家.名字 + "<" + 玩家.等级 + "级>   " +
                    玩家.国家 + "   " + TIME.转时间格式(存档.存档时间);
            }
        }
    }

    public void 读取指定存档(int 第几个存档)
    {
        if (!可以操作() || 第几个存档 < 1 || 第几个存档 > 存档槽位数) return;
        读取所有存档();
        var 存档 = 全局变量.所有存档列表.Find(记录 => 记录.ID == 第几个存档);
        if (存档 == null)
        {
            显示操作提示("该槽位没有可读取的存档");
            return;
        }
        var 原国家 = 全局变量.所有国家列表;
        var 原玩家 = 全局变量.所有玩家数据表;
        var 原城池 = 全局变量.所有城池列表;
        // 开始菜单也能读档，此时可能还没有建立当前角色。
        bool 有原角色 = 原玩家 != null && 全局变量.本机身份 >= 0 && 原玩家.Count > 全局变量.本机身份 &&
            原玩家[全局变量.本机身份] != null && 原玩家[全局变量.本机身份].基础信息 != null;
        // 回滚快照不结算收益或修筑。未建立角色时也能还原尚未绑定的资源模块。
        if (有原角色)
        {
            try { 界面扩展存档.确保世界(); }
            catch (Exception 异常) when (异常 is InvalidOperationException || 异常 is ArgumentException)
            {
                显示操作提示("当前世界的扩展数据尚未就绪，未读取存档。");
                Debug.LogWarning("准备读档失败：" + 异常.Message);
                return;
            }
        }
        var 原资源 = Dwsg.Window3.资源点规则.本地.捕获回滚状态();
        // 不清空旧列表：它们可能仍被当前世界或缓存的存档引用。
        全局变量.所有国家列表 = 存档.国家列表;
        全局变量.所有玩家数据表 = 存档.玩家列表;
        全局变量.所有城池列表 = 存档.城池列表;
        List<军情信息> 恢复军情 = null;
        List<Ai军情信息> 恢复Ai军情 = null;
        string 读取错误 = null;
        bool 已通过 = false;
        try
        {
            界面扩展恢复候选 候选;
            // 资源目录先临时恢复，行军 codec 才能绑定资源目标；其他扩展保持原对象直到全部通过。
            if (界面扩展存档.尝试准备(存档.界面扩展, out 候选, out 读取错误) &&
                行军存档.尝试恢复(存档.行军, out 恢复军情, out 恢复Ai军情, out 读取错误) &&
                缺失界面.窗口4.和平驻防规则.尝试重建城池关联(恢复军情, out 读取错误))
            {
                var 资源检查 = Dwsg.Window3.资源点规则.本地.校验军情集合(恢复军情);
                if (!资源检查.Success) 读取错误 = 资源检查.Message;
                else 已通过 = 界面扩展存档.尝试绑定(候选, out 读取错误);
            }
        }
        finally
        {
            if (!已通过)
            {
                全局变量.所有国家列表 = 原国家;
                全局变量.所有玩家数据表 = 原玩家;
                全局变量.所有城池列表 = 原城池;
                // 还原原运行时目录与锚点，不重校旧时间、不入账，也不丢失未绑定状态。
                Dwsg.Window3.资源点规则.本地.恢复回滚状态(原资源);
            }
        }
        if (!已通过)
        {
            显示操作提示("该存档的数据无法读取，当前世界已保留。");
            Debug.LogWarning("存档读取失败：" + 读取错误);
            return;
        }
        foreach (var 玩家 in 全局变量.所有玩家数据表) 玩家.重置将领状态();
        所有城池界面脚本.重置城池状态();
        // 验证全部通过后一次性切换队列，队员均引用本次载入的真实玩家表。
        全局变量.军情列表 = 恢复军情;
        全局变量.Ai军情列表 = 恢复Ai军情;
        foreach (var 军情 in 恢复军情)
            foreach (var 将领 in 军情.队列将领列表)
                将领.详细信息.状态 = 缺失界面.窗口4.和平驻防规则.是驻防军情(军情) ? 2 : 1;
        foreach (var 军情 in 恢复Ai军情)
            foreach (var 将领 in 军情.队列将领列表) 将领.详细信息.状态 = 1;
        全局变量.战场列表 = new List<战场信息>();
        全局变量.战斗界面UI对象 = null;
        缺失界面.窗口4.和平驻防规则.推进(TIME.getTime());
        SceneManager.LoadScene(1);
    }

    public void 写入指定存档(int 第几个存档)
    {
        if (!可以操作() || 正在写入 || 第几个存档 < 1 || 第几个存档 > 存档槽位数) return;
        try { 界面扩展存档.确保世界(); }
        catch (Exception 异常) when (异常 is InvalidOperationException || 异常 is ArgumentException)
        {
            显示操作提示("当前世界的扩展数据尚未就绪，未保存存档。");
            Debug.LogWarning("准备保存失败：" + 异常.Message);
            return;
        }
        行军存档数据 行军;
        string 行军错误;
        if (!行军存档.尝试捕获(out 行军, out 行军错误))
        {
            显示操作提示(行军错误);
            return;
        }
        正在写入 = true;
        string 路径 = 文件路径(第几个存档);
        string 临时文件 = 路径 + ".tmp";
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            var 扩展 = 界面扩展存档.捕获();
            string 内容 = 加密.EncryptString(JsonConvert.SerializeObject(new 存档信息库类
            {
                ID = 第几个存档,
                存档时间 = TIME.getTime(),
                国家列表 = 全局变量.所有国家列表,
                城池列表 = 全局变量.所有城池列表,
                玩家列表 = 全局变量.所有玩家数据表,
                界面扩展 = 扩展,
                行军 = 行军
            }));
            File.WriteAllText(临时文件, 内容);
            存档信息库类 检查记录;
            if (!尝试读取(临时文件, 第几个存档, out 检查记录)) throw new IOException("存档校验失败");
            if (File.Exists(路径))
            {
                // 坏档恢复后再次保存时，保留仍有效的上一份备份。
                string 备份路径 = 尝试读取(路径, 第几个存档, out 检查记录) ? 路径 + ".bak" : null;
                File.Replace(临时文件, 路径, 备份路径);
            }
            else File.Move(临时文件, 路径);
            刷新存档列表();
            显示操作提示("保存存档" + 第几个存档 + "成功!");
        }
        catch (Exception 异常) when (异常 is IOException || 异常 is UnauthorizedAccessException ||
            异常 is JsonException || 异常 is CryptographicException || 异常 is NotSupportedException ||
            异常 is InvalidOperationException || 异常 is ArgumentException)
        {
            Debug.LogError("保存存档失败：" + 异常.GetType().Name);
            显示操作提示("保存失败，原存档与备份已保留");
        }
        finally
        {
            正在写入 = false;
        }
    }
}
