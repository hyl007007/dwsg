using System;
using System.Globalization;
using Newtonsoft.Json;
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
    public string 六部政务;
    public string 资源点;
}

// 候选只在本次同步读档中使用；资源目录先临时载入，其他模块等全部军情通过后才绑定。
public sealed class 界面扩展恢复候选
{
    internal 界面扩展存档数据 数据;
    internal object 玩家列表, 角色;
    internal CityModuleDto 城池;
    internal LocalSocialAdapter 社交;
    internal SixMinistriesImport 六部;
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
        var 原资源 = 资源点规则.本地.捕获回滚状态();
        界面扩展恢复候选 候选;
        string 错误;
        if (!尝试准备(null, out 候选, out 错误) || !尝试绑定(候选, out 错误))
        {
            资源点规则.本地.恢复回滚状态(原资源);
            throw new InvalidOperationException(错误);
        }
    }

    public static void 确保世界()
    {
        if (string.IsNullOrEmpty(世界标识) || !ReferenceEquals(玩家列表锚点, 全局变量.所有玩家数据表) ||
            !ReferenceEquals(角色锚点, 当前角色)) 新建世界();
    }

    public static 界面扩展存档数据 捕获()
    {
        确保世界();
        // 行军已由保存入口验证。先结算资源点收益、修筑，再捕获同一份财产与扩展。
        string resource;
        var resourceResult = 资源点规则.本地.导出(TIME.getTime(), out resource);
        if (!resourceResult.Success) throw new InvalidOperationException(resourceResult.Message);
        string city = CityLocalAdapter.Local.ExportJson(世界标识);
        return new 界面扩展存档数据
        {
            世界标识 = 世界标识,
            任务邮件 = Window1Module.导出JSON(),
            城池内政 = city,
            社交关系 = 社交界面入口.ExportJson(),
            六部政务 = SixMinistriesModule.Export(世界标识),
            资源点 = resource
        };
    }

    // 兼容直接恢复扩展的调用。统一读档入口使用准备/绑定，夹在二者之间恢复并校验军情。
    public static bool 尝试恢复(界面扩展存档数据 data, out string error)
    {
        var 原资源 = 资源点规则.本地.捕获回滚状态();
        界面扩展恢复候选 候选;
        if (尝试准备(data, out 候选, out error) && 尝试绑定(候选, out error)) return true;
        资源点规则.本地.恢复回滚状态(原资源);
        return false;
    }

    public static bool 尝试准备(界面扩展存档数据 data, out 界面扩展恢复候选 候选, out string error)
    {
        候选 = null;
        try { return 准备(data, out 候选, out error); }
        catch (JsonException ex) { error = ex.Message; return false; }
        catch (InvalidOperationException ex) { error = ex.Message; return false; }
        catch (ArgumentException ex) { error = ex.Message; return false; }
        catch (OverflowException ex) { error = ex.Message; return false; }
    }

    private static bool 准备(界面扩展存档数据 data, out 界面扩展恢复候选 候选, out string error)
    {
        候选 = null;
        error = null;
        // 旧档没有扩展时只生成一个新标识，所有缺失模块共用它。
        if (data == null) data = new 界面扩展存档数据 { 世界标识 = Guid.NewGuid().ToString("N") };
        Guid id;
        if (data.版本 != 1 || string.IsNullOrEmpty(data.世界标识) || !Guid.TryParseExact(data.世界标识, "N", out id))
        {
            error = "扩展存档版本或世界标识无效";
            return false;
        }
        if (当前角色 == null) { error = "当前角色尚未初始化"; return false; }
        if (!string.IsNullOrEmpty(data.任务邮件))
        {
            if (data.任务邮件.Length > 16000000) { error = "任务邮件存档过大"; return false; }
            var mail = JsonConvert.DeserializeObject<Window1WorldState>(data.任务邮件,
                new JsonSerializerSettings { MaxDepth = 32, TypeNameHandling = TypeNameHandling.None });
            if (mail == null || !mail.Validate(data.世界标识, out error)) return false;
        }
        var self = 社交身份();
        if (!LocalSocialAdapter.ValidId(self.Id) || !LocalSocialAdapter.ValidName(self.Name, 20))
        { error = "社交世界或角色身份无效"; return false; }
        var socialAdapter = new LocalSocialAdapter(self, data.世界标识);
        if (!string.IsNullOrEmpty(data.社交关系))
        {
            var social = socialAdapter.ImportJson(data.社交关系);
            if (!social.Succeeded) { error = social.Message; return false; }
        }
        var profile = socialAdapter.UpdateLocalPlayer(self);
        if (!profile.Succeeded) { error = profile.Message; return false; }
        SixMinistriesImport ministries;
        if (!SixMinistriesModule.TryDecode(data.世界标识, data.六部政务, out ministries, out error)) return false;
        CityModuleDto city;
        var cityResult = CityLocalAdapter.Local.TryDecode(data.城池内政, data.世界标识, out city);
        if (!cityResult.Success) { error = cityResult.Message; return false; }
        // 解码资源军情需要本次目录。此处不结算、不绑定其他模块；调用方持有原资源运行时快照。
        var resource = 资源点规则.本地.恢复(data.资源点, data.世界标识, TIME.getTime());
        if (!resource.Success) { error = resource.Message; return false; }
        候选 = new 界面扩展恢复候选
        {
            数据 = data, 玩家列表 = 全局变量.所有玩家数据表, 角色 = 当前角色,
            城池 = city, 社交 = socialAdapter, 六部 = ministries
        };
        error = null;
        return true;
    }

    public static bool 尝试绑定(界面扩展恢复候选 候选, out string error)
    {
        error = "扩展恢复候选或目标世界已变更";
        if (候选 == null || !ReferenceEquals(候选.玩家列表, 全局变量.所有玩家数据表) ||
            !ReferenceEquals(候选.角色, 当前角色)) return false;
        // 唯一仍可能拒绝的绑定先执行，尚未替换任何其他扩展。
        if (!SixMinistriesModule.AttachValidated(候选.六部, out error)) return false;
        // JSON 已在准备阶段校验，目标玩家表也已确认。后续绑定不再解析资源或重校旧时钟。
        if (!Window1Module.导入JSON(候选.数据.世界标识, 候选.数据.任务邮件, out error))
            throw new InvalidOperationException("已校验的任务邮件无法绑定：" + error);
        CityLocalAdapter.Local.AttachValidated(候选.城池);
        社交界面入口.SetAdapter(候选.社交);
        绑定世界(候选.数据.世界标识);
        error = null;
        return true;
    }
}
