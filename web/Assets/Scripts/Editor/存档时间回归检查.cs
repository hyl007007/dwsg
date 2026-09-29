#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using 玩家数据结构;

public static class 存档时间回归检查
{
    private static readonly MethodInfo 读取方法 = typeof(存档脚本).GetMethod(
        "尝试读取", BindingFlags.NonPublic | BindingFlags.Static);

    // 可由 Unity 批处理的 -executeMethod 存档时间回归检查.执行 调用。
    public static void 执行()
    {
        if (读取方法 == null) throw new MissingMethodException("存档脚本", "尝试读取");
        string 临时目录 = Path.Combine(Path.GetTempPath(), "dwsg-save-time-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(临时目录);
        int 原身份 = 全局变量.本机身份;
        try
        {
            全局变量.本机身份 = 0;
            long[] 正常时间 = { 1700000000L, 0L, -1L, 2147483648L, -62135596800L, 253402300799L };
            for (int i = 0; i < 正常时间.Length; i++)
            {
                string 路径 = Path.Combine(临时目录, "正常" + i + ".txt");
                写入(路径, 1, 正常时间[i]);
                检查读取(路径, 1, true, 正常时间[i]);
            }

            long[] 异常时间 = { long.MaxValue, long.MinValue, -62135596801L, 253402300800L };
            for (int i = 0; i < 异常时间.Length; i++)
            {
                string 路径 = Path.Combine(临时目录, "异常" + i + ".txt");
                写入(路径, 1, 异常时间[i]);
                检查读取(路径, 1, false, 异常时间[i]);
            }

            string 旧档路径 = Path.Combine(临时目录, "旧档.txt");
            写入(旧档路径, 1, 0L, true);
            检查读取(旧档路径, 1, true, 0L);

            string 主档路径 = Path.Combine(临时目录, "存档1.txt");
            写入(主档路径, 1, long.MaxValue);
            写入(主档路径 + ".bak", 1, 1700000000L);
            检查读取(主档路径, 1, false, long.MaxValue);
            检查读取(主档路径 + ".bak", 1, true, 1700000000L);
            for (int 槽位 = 2; 槽位 <= 6; 槽位++)
            {
                string 路径 = Path.Combine(临时目录, "存档" + 槽位 + ".txt");
                写入(路径, 槽位, 0L);
                检查读取(路径, 槽位, true, 0L);
            }
            Debug.Log("存档时间回归检查通过：日期与边界、旧档、坏主档及有效备份、后续槽位共18项。");
        }
        finally
        {
            全局变量.本机身份 = 原身份;
            foreach (string 文件 in Directory.GetFiles(临时目录)) File.Delete(文件);
            Directory.Delete(临时目录);
        }
    }

    private static void 写入(string 路径, int 槽位, long 时间, bool 省略版本 = false)
    {
        var 数据 = new 存档信息库类 { ID = 槽位, 存档时间 = 时间 };
        数据.玩家列表.Add(new 玩家数据());
        var 内容 = JObject.Parse(JsonConvert.SerializeObject(数据));
        if (省略版本) 内容.Remove("存档版本");
        File.WriteAllText(路径, 加密.EncryptString(内容.ToString(Formatting.None)));
    }

    private static void 检查读取(string 路径, int 槽位, bool 应可读取, long 时间)
    {
        object[] 参数 = { 路径, 槽位, null };
        bool 可读取 = (bool)读取方法.Invoke(null, 参数);
        var 数据 = (存档信息库类)参数[2];
        if (可读取 != 应可读取 || (!可读取 && 数据 != null) ||
            (可读取 && (数据 == null || 数据.ID != 槽位 || 数据.存档版本 != 1 || 数据.存档时间 != 时间)))
            throw new InvalidOperationException("存档时间回归失败：" + Path.GetFileName(路径));
    }
}
#endif
