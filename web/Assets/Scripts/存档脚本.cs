using Newtonsoft.Json;
using System;
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

    private bool 可以操作()
    {
        if (全局变量.当局赌场下注列表.Count == 0) return true;
        全局变量.提示类.显示信息("请等待赌场结束");
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
            全局变量.提示类.显示信息("该槽位没有可读取的存档");
            return;
        }
        var 原国家 = 全局变量.所有国家列表;
        var 原玩家 = 全局变量.所有玩家数据表;
        var 原城池 = 全局变量.所有城池列表;
        // 开始菜单也能读档，此时可能还没有建立当前角色。
        bool 有原角色 = 原玩家 != null && 全局变量.本机身份 >= 0 && 原玩家.Count > 全局变量.本机身份 &&
            原玩家[全局变量.本机身份] != null && 原玩家[全局变量.本机身份].基础信息 != null;
        var 原扩展 = 有原角色 ? 界面扩展存档.捕获() : null;
        // 不清空旧列表：它们可能仍被当前世界或缓存的存档引用。
        全局变量.所有国家列表 = 存档.国家列表;
        全局变量.所有玩家数据表 = 存档.玩家列表;
        全局变量.所有城池列表 = 存档.城池列表;
        string 扩展错误;
        if (!界面扩展存档.尝试恢复(存档.界面扩展, out 扩展错误))
        {
            全局变量.所有国家列表 = 原国家;
            全局变量.所有玩家数据表 = 原玩家;
            全局变量.所有城池列表 = 原城池;
            string 恢复错误;
            if (原扩展 != null) 界面扩展存档.尝试恢复(原扩展, out 恢复错误);
            全局变量.提示类.显示信息("该存档的界面数据无法读取，当前世界已保留");
            Debug.LogWarning("扩展存档读取失败：" + 扩展错误);
            return;
        }
        foreach (var 玩家 in 全局变量.所有玩家数据表) 玩家.重置将领状态();
        所有城池界面脚本.重置城池状态();
        SceneManager.LoadScene(1);
    }

    public void 写入指定存档(int 第几个存档)
    {
        if (!可以操作() || 正在写入 || 第几个存档 < 1 || 第几个存档 > 存档槽位数) return;
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
                界面扩展 = 扩展
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
            全局变量.提示类.显示信息("保存存档" + 第几个存档 + "成功!");
        }
        catch (Exception 异常) when (异常 is IOException || 异常 is UnauthorizedAccessException ||
            异常 is JsonException || 异常 is CryptographicException || 异常 is NotSupportedException)
        {
            Debug.LogError("保存存档失败：" + 异常.GetType().Name);
            全局变量.提示类.显示信息("保存失败，原存档与备份已保留");
        }
        finally
        {
            正在写入 = false;
        }
    }
}
