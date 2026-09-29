using System;
using System.Globalization;
using Dwsg.Window1;
using Dwsg.Window3;
using Dwsg.Social;

// 与世界列表同槽位保存，不使用测试数据、PlayerPrefs 或旁路文件。
public sealed class 界面扩展存档数据
{
    public int 版本 = 1;
    public string 世界标识;
    public string 任务邮件;
    public string 城池内政;
    public string 社交关系;
}

public static class 界面扩展存档
{
    private static string 世界标识;
    private static object 玩家列表锚点;
    private static object 角色锚点;

    private static object 当前角色
    {
        get
        {
            int index = 全局变量.本机身份;
            return 全局变量.所有玩家数据表 != null && index >= 0 && index < 全局变量.所有玩家数据表.Count
                ? 全局变量.所有玩家数据表[index] : null;
        }
    }

    private static SocialPlayerDto 社交身份()
    {
        int index = 全局变量.本机身份;
        if (当前角色 == null) throw new InvalidOperationException("当前角色尚未初始化");
        var info = 全局变量.所有玩家数据表[index].基础信息;
        return new SocialPlayerDto
        {
            Id = "local-" + Convert.ToString(info.ID, CultureInfo.InvariantCulture),
            Name = info.名字,
            Level = Math.Max(1, Math.Min(999, (int)info.等级)),
            Country = string.IsNullOrEmpty(info.国家) ? "无" : info.国家
        };
    }

    private static void 绑定世界(string id)
    {
        世界标识 = id;
        玩家列表锚点 = 全局变量.所有玩家数据表;
        角色锚点 = 当前角色;
    }

    public static void 新建世界()
    {
        string id = Guid.NewGuid().ToString("N");
        Window1Module.重置(id);
        CityLocalAdapter.Local.Reset(id);
        var result = 社交界面入口.RestoreForWorld(id, 社交身份(), null);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        绑定世界(id);
    }

    public static 界面扩展存档数据 捕获()
    {
        if (string.IsNullOrEmpty(世界标识) || !ReferenceEquals(玩家列表锚点, 全局变量.所有玩家数据表) ||
            !ReferenceEquals(角色锚点, 当前角色)) 新建世界();
        // 修筑结算可能修改资源，先结算，再捕获同一份世界与奖励状态。
        string city = CityLocalAdapter.Local.ExportJson(世界标识);
        return new 界面扩展存档数据
        {
            世界标识 = 世界标识,
            任务邮件 = Window1Module.导出JSON(),
            城池内政 = city,
            社交关系 = 社交界面入口.ExportJson()
        };
    }

    // 调用方先切换世界列表。失败时必须恢复旧列表与旧扩展快照，再保留当前场景。
    public static bool 尝试恢复(界面扩展存档数据 data, out string error)
    {
        try { return 恢复(data, out error); }
        catch (InvalidOperationException ex) { error = ex.Message; return false; }
        catch (ArgumentException ex) { error = ex.Message; return false; }
    }

    private static bool 恢复(界面扩展存档数据 data, out string error)
    {
        error = null;
        if (data == null)
        {
            新建世界();
            return true;
        }
        Guid id;
        if (data.版本 != 1 || string.IsNullOrEmpty(data.世界标识) || !Guid.TryParseExact(data.世界标识, "N", out id))
        {
            error = "扩展存档版本或世界标识无效";
            return false;
        }
        if (!Window1Module.导入JSON(data.世界标识, data.任务邮件, out error)) return false;
        if (string.IsNullOrEmpty(data.城池内政)) CityLocalAdapter.Local.Reset(data.世界标识);
        else
        {
            var city = CityLocalAdapter.Local.ImportJson(data.城池内政, data.世界标识);
            if (!city.Success) { error = city.Message; return false; }
        }
        var social = 社交界面入口.RestoreForWorld(data.世界标识, 社交身份(), data.社交关系);
        if (!social.Succeeded) { error = social.Message; return false; }
        绑定世界(data.世界标识);
        return true;
    }
}
