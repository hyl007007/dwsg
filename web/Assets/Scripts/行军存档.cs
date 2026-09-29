using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using 玩家数据结构;
using 缺失界面.窗口4;
using Dwsg.Window3;
using Newtonsoft.Json;

public sealed class 行军存档数据
{
    public int 版本 = 1;
    public List<行军任务存档> 军情列表 = new List<行军任务存档>();
    public List<行军任务存档> Ai军情列表 = new List<行军任务存档>();
    // 旧档缺少此扩展时为空。只保存仍在行军的 AI 本体，不登记到玩家封地。
    public List<临时AI将领存档> 临时AI将领列表 = new List<临时AI将领存档>();
}

public sealed class 临时AI将领存档
{
    public int 临时ID;
    public int 玩家ID;
    public 将领信息 将领;
}

public sealed class 驻防任务存档
{
    public 驻防任务阶段 阶段;
    public int 所属玩家ID;
    public int 出发封地ID;
    public double 出发坐标x;
    public double 出发坐标y;
    public int 驻防坐标x;
    public int 驻防坐标y;
    public long 出发时间;
    public string 目标国家;
    public int 目标城主;
    public string 变更说明;
}

public sealed class 资源任务存档
{
    [JsonProperty(Required = Required.Always)] public string 世界标识;
    [JsonProperty(Required = Required.Always)] public string 资源点标识;
    [JsonProperty(Required = Required.Always)] public int 所属玩家ID;
    [JsonProperty(Required = Required.Always)] public int 出发封地ID;
    [JsonProperty(Required = Required.Always)] public int 目标批次;
    [JsonProperty(Required = Required.Always)] public int 出发坐标x;
    [JsonProperty(Required = Required.Always)] public int 出发坐标y;
    [JsonProperty(Required = Required.Always)] public long 出发时间;
    [JsonProperty(Required = Required.Always)] public 资源出征阶段 阶段;
}

public sealed class 行军任务存档
{
    public bool 临时AI部队;
    public 军事任务用途 任务用途;
    public 驻防任务存档 驻防;
    public 资源任务存档 资源;
    public int 战场类型;
    public int 坐标x;
    public int 坐标y;
    public long 到达时间;
    // 军情列表中 0 为本机进攻、78 为本机防守、666 为 AI 进攻。
    public int 身份;
    public List<行军将领存档> 部队 = new List<行军将领存档>();
}

public sealed class 行军将领存档
{
    // 0 使用真实将领引用；正数引用本份存档的临时池，绝不改变将领信息.ID。
    public int 临时AI将领ID;
    public int 玩家ID;
    public int 封地ID;
    // 玩家名下的实例 ID，不是将领属性.初始属性.ID。
    public int 将领ID;
}

public static class 行军存档
{
    private const long 最大到达秒 = 253402300799L;

    private sealed class 登记将领
    {
        public 行军将领存档 引用;
        public 将领信息 将领;
    }

    private sealed class 将领引用比较器 : IEqualityComparer<将领信息>
    {
        public bool Equals(将领信息 左, 将领信息 右) { return ReferenceEquals(左, 右); }
        public int GetHashCode(将领信息 将领) { return RuntimeHelpers.GetHashCode(将领); }
    }

    private sealed class 世界索引
    {
        public readonly Dictionary<int, Dictionary<int, 登记将领>> 玩家 = new Dictionary<int, Dictionary<int, 登记将领>>();
        public readonly Dictionary<将领信息, 登记将领> 对象 = new Dictionary<将领信息, 登记将领>(new 将领引用比较器());
    }

    // 只捕获尚未进入战场的任务。失败不提供部分数据，不修改任务或将领。
    public static bool 尝试捕获(out 行军存档数据 数据, out string 错误)
    {
        数据 = null;
        错误 = null;
        var 军情 = 全局变量.军情列表;
        var Ai军情 = 全局变量.Ai军情列表;
        if (军情 == null || Ai军情 == null) return 失败("军情列表缺失，无法保存。", out 错误);
        foreach (var 任务 in 军情)
        {
            if (任务 == null) return 失败("军情记录缺失，无法保存。", out 错误);
            if (任务.已进入战场) return 失败("存在已进入战场的部队，请在战斗结束后保存。", out 错误);
        }
        foreach (var 任务 in Ai军情)
        {
            if (任务 == null) return 失败("AI 军情记录缺失，无法保存。", out 错误);
            if (任务.已进入战场) return 失败("存在已进入战场的 AI 部队，请在战斗结束后保存。", out 错误);
        }
        if (全局变量.所有城池列表 != null)
            foreach (var 城池 in 全局变量.所有城池列表)
                if (城池 != null && 城池.正在交战) return 失败("存在正在交战的城池，请在战斗结束后保存。", out 错误);
        if (全局变量.战场列表 != null)
            foreach (var 战场 in 全局变量.战场列表)
                if (战场 != null && !战场.战斗结束) return 失败("存在尚未结束的战场，请在战斗结束后保存。", out 错误);

        if (!和平驻防规则.尝试检查驻防关联(军情, out 错误)) return false;
        var 候选 = new 行军存档数据 { 版本 = 2 };
        if (军情.Exists(资源点战斗适配.是资源军情))
        {
            候选.版本 = 3;
            var 检查 = 资源点规则.本地.校验军情集合(军情);
            if (!检查.Success) return 失败(检查.Message, out 错误);
        }
        if (军情.Count == 0 && Ai军情.Count == 0) { 数据 = 候选; return true; }
        世界索引 世界;
        if (!尝试建立世界索引(out 世界, out 错误)) return false;
        var 已使用 = new HashSet<将领信息>(new 将领引用比较器());
        foreach (var 任务 in 军情)
        {
            行军任务存档 记录;
            if (!尝试捕获任务(任务.战场类型, 任务.坐标x, 任务.坐标y, 任务.到达时间, 任务.身份,
                任务.队列将领列表, true, 任务.临时AI部队, 和平驻防规则.获取用途(任务), 捕获驻防(任务 as 驻防军情信息), 捕获资源(任务 as 资源点军情信息), 世界, 已使用, 候选, out 记录, out 错误)) return false;
            候选.军情列表.Add(记录);
        }
        foreach (var 任务 in Ai军情)
        {
            行军任务存档 记录;
            if (!尝试捕获任务(任务.战场类型, 任务.坐标x, 任务.坐标y, 任务.到达时间, 任务.身份,
                任务.队列将领列表, false, false, 军事任务用途.出征, null, null, 世界, 已使用, 候选, out 记录, out 错误)) return false;
            候选.Ai军情列表.Add(记录);
        }
        数据 = 候选;
        return true;
    }

    // 调用者须先切换真实世界表；成功后才统一替换两份军情列表及更新出征状态。
    // 旧档的 null 返回两份新的空列表，绝不沿用当前 static 军情。
    public static bool 尝试恢复(行军存档数据 数据, out List<军情信息> 军情,
        out List<Ai军情信息> Ai军情, out string 错误)
    {
        军情 = null;
        Ai军情 = null;
        错误 = null;
        if (数据 != null && ((数据.版本 != 1 && 数据.版本 != 2 && 数据.版本 != 3) || 数据.军情列表 == null || 数据.Ai军情列表 == null || 数据.临时AI将领列表 == null))
            return 失败("行军存档版本不支持或任务列表缺失。", out 错误);
        if (数据 != null && 数据.军情列表.Count == 0 && 数据.Ai军情列表.Count == 0 && 数据.临时AI将领列表.Count > 0)
            return 失败("临时 AI 将领池没有对应的行军任务，不能丢弃部队。", out 错误);
        var 本地候选 = new List<军情信息>();
        var Ai候选 = new List<Ai军情信息>();
        if (数据 == null || (数据.军情列表.Count == 0 && 数据.Ai军情列表.Count == 0))
        {
            if (!和平驻防规则.尝试检查驻防关联(本地候选, out 错误)) return false;
            军情 = 本地候选; Ai军情 = Ai候选; return true;
        }
        世界索引 世界;
        if (!尝试建立世界索引(out 世界, out 错误)) return false;
        Dictionary<int, 临时AI将领存档> 临时池;
        if (!尝试建立临时池(数据, 世界, out 临时池, out 错误)) return false;
        var 已使用 = new HashSet<将领信息>(new 将领引用比较器());
        foreach (var 任务 in 数据.军情列表)
        {
            if (数据.版本 < 3 && 任务 != null && (任务.资源 != null || 任务.战场类型 == 资源点军情信息.资源战场类型 || 任务.任务用途 == 军事任务用途.占领资源点))
                return 失败("旧版行军存档不能包含资源任务。", out 错误);
            List<将领信息> 部队;
            var 兼容任务 = 兼容旧用途(任务, 数据.版本);
            if (!尝试解析任务(兼容任务, true, 世界, 已使用, 临时池, out 部队, out 错误)) return false;
            var 驻 = 兼容任务.驻防;
            军情信息 情 = 兼容任务.资源 != null ? 恢复资源(兼容任务.资源) : 驻 == null ? new 军情信息() : new 驻防军情信息 {
                阶段 = 驻.阶段, 所属玩家 = 全局变量.所有玩家数据表.FindIndex(x => x != null && x.基础信息.ID == 驻.所属玩家ID),
                出发封地ID = 驻.出发封地ID, 出发坐标x = 驻.出发坐标x, 出发坐标y = 驻.出发坐标y,
                驻防坐标x = 驻.驻防坐标x, 驻防坐标y = 驻.驻防坐标y, 出发时间 = 驻.出发时间,
                目标国家 = 驻.目标国家, 目标城主 = 驻.目标城主, 变更说明 = 驻.变更说明 ?? ""
            };
            情.临时AI部队 = 兼容任务.临时AI部队;
            情.任务用途 = 兼容任务.任务用途; 情.战场类型 = 任务.战场类型; 情.坐标x = 任务.坐标x; 情.坐标y = 任务.坐标y;
            情.到达时间 = 任务.到达时间; 情.身份 = 任务.身份; 情.队列将领列表 = 部队;
            本地候选.Add(情);
        }
        foreach (var 任务 in 数据.Ai军情列表)
        {
            List<将领信息> 部队;
            if (!尝试解析任务(任务, false, 世界, 已使用, 临时池, out 部队, out 错误)) return false;
            Ai候选.Add(new Ai军情信息 { 战场类型 = 任务.战场类型, 坐标x = 任务.坐标x, 坐标y = 任务.坐标y,
                到达时间 = 任务.到达时间, 身份 = 任务.身份, 队列将领列表 = 部队, 已进入战场 = false });
        }
        foreach (var 项 in 临时池.Values)
            if (!已使用.Contains(项.将领)) return 失败("存档含未被行军任务引用的临时 AI 将领，不能静默丢弃。", out 错误);
        if (!和平驻防规则.尝试检查驻防关联(本地候选, out 错误)) return false;
        if (本地候选.Exists(资源点战斗适配.是资源军情))
        {
            var 检查 = 资源点规则.本地.校验军情集合(本地候选);
            if (!检查.Success) return 失败(检查.Message, out 错误);
        }
        军情 = 本地候选;
        Ai军情 = Ai候选;
        return true;
    }

    private static bool 尝试建立世界索引(out 世界索引 世界, out string 错误)
    {
        世界 = new 世界索引();
        错误 = null;
        if (全局变量.所有玩家数据表 == null) return 失败("真实玩家表缺失，无法解析行军部队。", out 错误);
        foreach (var 玩家 in 全局变量.所有玩家数据表)
        {
            if (玩家 == null || 玩家.基础信息 == null || 玩家.封地信息表 == null || 玩家.基础信息.ID < 0)
                return 失败("玩家或封地记录无效，无法解析行军部队。", out 错误);
            int 玩家ID = 玩家.基础信息.ID;
            if (世界.玩家.ContainsKey(玩家ID)) return 失败("玩家 ID 重复，无法确定部队归属。", out 错误);
            var 将领表 = new Dictionary<int, 登记将领>();
            世界.玩家.Add(玩家ID, 将领表);
            var 封地ID表 = new HashSet<int>();
            foreach (var 封地 in 玩家.封地信息表)
            {
                // 初始化脚本为 NPC 建立的唯一封地 ID 为 0，正常玩家封地从 1 开始。
                if (封地 == null || 封地.ID < 0 || 封地.将领信息表 == null || !封地ID表.Add(封地.ID))
                    return 失败("封地记录无效或 ID 重复，无法确定部队归属。", out 错误);
                foreach (var 将领 in 封地.将领信息表)
                {
                    if (将领 == null || 将领.ID <= 0 || 将领.详细信息 == null || 将领.详细信息.身份 != 玩家ID)
                        return 失败("将领实例 ID 或所属玩家无效，无法解析行军部队。", out 错误);
                    if (将领表.ContainsKey(将领.ID) || 世界.对象.ContainsKey(将领))
                        return 失败("将领实例重复登记，无法确定行军部队。", out 错误);
                    var 登记 = new 登记将领 { 将领 = 将领,
                        引用 = new 行军将领存档 { 玩家ID = 玩家ID, 封地ID = 封地.ID, 将领ID = 将领.ID } };
                    将领表.Add(将领.ID, 登记);
                    世界.对象.Add(将领, 登记);
                }
            }
        }
        return true;
    }

    private static bool 尝试捕获任务(int 类型, int x, int y, long 到达, int 身份,
        List<将领信息> 部队, bool 为军情列表, bool 临时AI, 军事任务用途 用途, 驻防任务存档 驻防, 资源任务存档 资源, 世界索引 世界, HashSet<将领信息> 已使用, 行军存档数据 候选数据,
        out 行军任务存档 记录, out string 错误)
    {
        记录 = null;
        错误 = null;
        if (部队 == null || 部队.Count == 0 || 部队.Count > 5) return 失败("行军部队必须包含 1 至 5 名将领。", out 错误);
        var 候选 = new 行军任务存档 { 临时AI部队 = 临时AI, 战场类型 = 类型, 坐标x = x, 坐标y = y, 到达时间 = 到达, 身份 = 身份, 任务用途 = 用途, 驻防 = 驻防, 资源 = 资源 };
        if (临时AI)
        {
            if (!检查任务信息(候选, 为军情列表, out 错误) ||
                !尝试捕获临时部队(候选, 部队, 世界, 已使用, 候选数据, out 错误)) return false;
            记录 = 候选;
            return true;
        }
        foreach (var 将领 in 部队)
        {
            登记将领 登记;
            if (将领 == null || !世界.对象.TryGetValue(将领, out 登记))
                return 失败("行军部队未登记于真实玩家将领表（可能为临时 AI 部队），请等待该军情结束后保存。", out 错误);
            候选.部队.Add(new 行军将领存档 { 玩家ID = 登记.引用.玩家ID, 封地ID = 登记.引用.封地ID, 将领ID = 登记.引用.将领ID });
        }
        List<将领信息> 已解析部队;
        if (!尝试解析任务(候选, 为军情列表, 世界, 已使用, null, out 已解析部队, out 错误)) return false;
        记录 = 候选;
        return true;
    }

    private static bool 尝试解析任务(行军任务存档 任务, bool 为军情列表, 世界索引 世界,
        HashSet<将领信息> 已使用, Dictionary<int, 临时AI将领存档> 临时池, out List<将领信息> 部队, out string 错误)
    {
        部队 = null;
        错误 = null;
        if (任务 == null || 任务.部队 == null || 任务.部队.Count == 0 || 任务.部队.Count > 5)
            return 失败("行军任务缺失或部队人数无效。", out 错误);
        if (!检查任务信息(任务, 为军情列表, out 错误)) return false;
        if (任务.临时AI部队) return 尝试解析临时部队(任务, 临时池, 已使用, out 部队, out 错误);
        var 候选 = new List<将领信息>();
        int 所属玩家ID = -1;
        foreach (var 引用 in 任务.部队)
        {
            Dictionary<int, 登记将领> 将领表;
            登记将领 登记;
            if (引用 == null || 引用.临时AI将领ID != 0 || 引用.玩家ID < 0 || 引用.封地ID < 0 || 引用.将领ID <= 0 ||
                !世界.玩家.TryGetValue(引用.玩家ID, out 将领表) || !将领表.TryGetValue(引用.将领ID, out 登记) ||
                登记.引用.封地ID != 引用.封地ID)
                return 失败("行军部队引用的玩家、封地或将领不存在，或所属封地不符。", out 错误);
            if (所属玩家ID >= 0 && 所属玩家ID != 引用.玩家ID)
                return 失败("同一行军部队中的将领所属玩家不一致。", out 错误);
            所属玩家ID = 引用.玩家ID;
            if (!已使用.Add(登记.将领)) return 失败("同一将领被重复编入行军任务。", out 错误);
            候选.Add(登记.将领);
        }
        if (为军情列表 && (任务.身份 == 0 || 任务.身份 == 78))
        {
            var 玩家表 = 全局变量.所有玩家数据表;
            int 本机 = 全局变量.本机身份;
            if (本机 < 0 || 本机 >= 玩家表.Count || 玩家表[本机].基础信息.ID != 所属玩家ID)
                return 失败("本机行军任务的将领所属玩家不符。", out 错误);
        }
        if (任务.驻防 != null)
        {
            if (任务.驻防.所属玩家ID != 所属玩家ID || 候选.Exists(x => x.详细信息.状态 != 和平驻防规则.驻防占用状态))
                return 失败("驻防任务的玩家或将领占用状态不符。", out 错误);
            foreach (var 引用 in 任务.部队)
                if (引用.封地ID != 任务.驻防.出发封地ID) return 失败("驻防任务的出发封地与将领引用不符。", out 错误);
        }
        if (任务.资源 != null)
        {
            var 情 = 恢复资源(任务.资源);
            情.战场类型 = 任务.战场类型; 情.身份 = 任务.身份; 情.任务用途 = 任务.任务用途;
            情.坐标x = 任务.坐标x; 情.坐标y = 任务.坐标y; 情.到达时间 = 任务.到达时间;
            情.队列将领列表 = 候选;
            var 检查 = 资源点规则.本地.校验军情(情);
            if (!检查.Success) return 失败(检查.Message, out 错误);
        }
        部队 = 候选;
        return true;
    }

    private static bool 检查任务信息(行军任务存档 任务, bool 为军情列表, out string 错误)
    {
        错误 = null;
        if (任务.临时AI部队 && (!为军情列表 || 任务.身份 != 666 || 任务.战场类型 != 1 ||
            任务.任务用途 != 军事任务用途.出征 || 任务.驻防 != null || 任务.资源 != null))
            return 失败("临时 AI 来源仅适用于 AI推城产生的攻城行军，不能混用驻防或真实将领。", out 错误);
        if (任务.战场类型 == 资源点军情信息.资源战场类型 || 任务.资源 != null || 任务.任务用途 == 军事任务用途.占领资源点)
        {
            var 资源 = 任务.资源;
            Guid 世界;
            if (!为军情列表 || 任务.临时AI部队 || 任务.身份 != 0 || 任务.战场类型 != 资源点军情信息.资源战场类型 ||
                任务.任务用途 != 军事任务用途.占领资源点 || 任务.驻防 != null || 资源 == null ||
                !Guid.TryParseExact(资源.世界标识, "N", out 世界) || string.IsNullOrEmpty(资源.资源点标识) || 资源.资源点标识.Length > 80 ||
                资源.所属玩家ID < 0 || 资源.出发封地ID < 0 || 资源.目标批次 < 0 || 资源.目标批次 == int.MaxValue ||
                资源.出发坐标x < 1 || 资源.出发坐标y < 1 || 资源.出发时间 < 0 || 资源.出发时间 >= 任务.到达时间 ||
                任务.到达时间 > 最大到达秒 || 资源.阶段 != 资源出征阶段.前往)
                return 失败("资源任务的用途、世界、出发时间或阶段无效。", out 错误);
            return true; // 目录、批次、出发封地及真实将领在解析后统一校验。
        }
        if ((任务.战场类型 != 0 && 任务.战场类型 != 1) || 任务.到达时间 <= 0 || 任务.到达时间 > 最大到达秒 || 任务.身份 < 0)
            return 失败("行军任务的战场类型、身份或到达秒无效。", out 错误);
        bool 为驻防 = 任务.任务用途 == 军事任务用途.和平驻防 || 任务.任务用途 == 军事任务用途.驻防撤回;
        if (为驻防)
        {
            var 驻 = 任务.驻防;
            if (!为军情列表 || 任务.身份 != 和平驻防规则.旧调度隔离身份 || 任务.战场类型 != 1 || 驻 == null ||
                驻.所属玩家ID < 0 || 驻.出发封地ID < 0 || 驻.出发时间 < 0 || 驻.出发时间 >= 任务.到达时间 ||
                !军事本地规则.有限(驻.出发坐标x) || !军事本地规则.有限(驻.出发坐标y) || 驻.出发坐标x < 0 || 驻.出发坐标y < 0 ||
                驻.驻防坐标x < 0 || 驻.驻防坐标y < 0 || 驻.阶段 < 驻防任务阶段.前往 || 驻.阶段 > 驻防任务阶段.撤回 ||
                (任务.任务用途 == 军事任务用途.驻防撤回) != (驻.阶段 == 驻防任务阶段.撤回) ||
                (驻.阶段 != 驻防任务阶段.撤回 && (任务.坐标x != 驻.驻防坐标x || 任务.坐标y != 驻.驻防坐标y)))
                return 失败("驻防用途、阶段、出发时间或归属记录无效。", out 错误);
        }
        else if (任务.驻防 != null || (任务.任务用途 != 军事任务用途.出征 && 任务.任务用途 != 军事任务用途.守方援军) ||
            (任务.任务用途 == 军事任务用途.守方援军 && (!为军情列表 || 任务.身份 != 78 || 任务.战场类型 != 1)) ||
            (任务.身份 == 78 && 任务.任务用途 != 军事任务用途.守方援军))
            return 失败("行军任务用途与身份不符。", out 错误);
        if (为军情列表 && !为驻防 && ((任务.身份 != 0 && 任务.身份 != 78 && 任务.身份 != 666) ||
            (任务.身份 != 0 && 任务.战场类型 != 1)))
            return 失败("行军任务的进攻或防守身份无效。", out 错误);
        if (任务.战场类型 == 0)
        {
            if (任务.坐标x < 0 || 任务.坐标x >= 全局变量.横向山贼数量 || 任务.坐标y < 0 || 任务.坐标y >= 全局变量.竖向山贼数量)
                return 失败("行军任务的山贼坐标超出地图。", out 错误);
        }
        else
        {
            int 目标数量 = 0;
            if (全局变量.所有城池列表 != null)
                foreach (var 城池 in 全局变量.所有城池列表)
                    if (城池 != null && 城池.坐标x == 任务.坐标x && 城池.坐标y == 任务.坐标y) 目标数量++;
            if (目标数量 > 1 || (!为驻防 && 目标数量 != 1)) return 失败("行军任务的目标城池不存在或坐标重复。", out 错误);
        }
        return true;
    }

    private static bool 尝试捕获临时部队(行军任务存档 任务, List<将领信息> 部队, 世界索引 世界,
        HashSet<将领信息> 已使用, 行军存档数据 数据, out string 错误)
    {
        错误 = null;
        int 所属玩家ID = -1;
        foreach (var 将领 in 部队)
        {
            if (将领 == null || 将领.详细信息 == null || !整数范围(将领.详细信息.身份, 0, int.MaxValue))
                return 失败("临时 AI 将领所属玩家无效。", out 错误);
            int 玩家ID = (int)将领.详细信息.身份;
            if (所属玩家ID >= 0 && 所属玩家ID != 玩家ID) return 失败("临时 AI 部队所属玩家不一致。", out 错误);
            所属玩家ID = 玩家ID;
            if (!检查临时将领(将领, 玩家ID, 世界, out 错误)) return false;
            if (!已使用.Add(将领)) return 失败("同一临时 AI 将领被重复编入行军任务。", out 错误);
            if (数据.临时AI将领列表.Count == int.MaxValue) return 失败("临时 AI 将领池过大。", out 错误);
            int 临时ID = 数据.临时AI将领列表.Count + 1;
            数据.临时AI将领列表.Add(new 临时AI将领存档 { 临时ID = 临时ID, 玩家ID = 玩家ID, 将领 = 将领 });
            任务.部队.Add(new 行军将领存档 { 玩家ID = 玩家ID, 封地ID = -1, 将领ID = 0, 临时AI将领ID = 临时ID });
        }
        return true;
    }

    private static bool 尝试建立临时池(行军存档数据 数据, 世界索引 世界,
        out Dictionary<int, 临时AI将领存档> 临时池, out string 错误)
    {
        临时池 = null;
        错误 = null;
        var 候选 = new Dictionary<int, 临时AI将领存档>();
        var 对象 = new HashSet<将领信息>(new 将领引用比较器());
        foreach (var 项 in 数据.临时AI将领列表)
        {
            if (项 == null || 项.临时ID <= 0 || 候选.ContainsKey(项.临时ID) || 项.将领 == null || !对象.Add(项.将领))
                return 失败("临时 AI 将领池存在缺失或重复实例。", out 错误);
            if (!检查临时将领(项.将领, 项.玩家ID, 世界, out 错误)) return false;
            候选.Add(项.临时ID, 项);
        }
        临时池 = 候选;
        return true;
    }

    private static bool 尝试解析临时部队(行军任务存档 任务, Dictionary<int, 临时AI将领存档> 临时池,
        HashSet<将领信息> 已使用, out List<将领信息> 部队, out string 错误)
    {
        部队 = null;
        错误 = null;
        if (临时池 == null) return 失败("临时 AI 将领池缺失。", out 错误);
        var 候选 = new List<将领信息>();
        int 所属玩家ID = -1;
        foreach (var 引用 in 任务.部队)
        {
            临时AI将领存档 项;
            if (引用 == null || 引用.临时AI将领ID <= 0 || 引用.将领ID != 0 || 引用.封地ID != -1 ||
                !临时池.TryGetValue(引用.临时AI将领ID, out 项) || 引用.玩家ID != 项.玩家ID ||
                (所属玩家ID >= 0 && 所属玩家ID != 项.玩家ID))
                return 失败("临时 AI 部队池引用或归属不符，不能与真实将领引用混用。", out 错误);
            所属玩家ID = 项.玩家ID;
            if (!已使用.Add(项.将领)) return 失败("同一临时 AI 将领被重复编入行军任务。", out 错误);
            // 使用本份 JSON 反序列化得到的唯一对象，不克隆玩家将领，不增长永久名册。
            候选.Add(项.将领);
        }
        部队 = 候选;
        return true;
    }

    private static bool 检查临时将领(将领信息 将领, int 玩家ID, 世界索引 世界, out string 错误)
    {
        错误 = null;
        if (玩家ID < 0 || !世界.玩家.ContainsKey(玩家ID) || 将领 == null || 将领.ID != 0 || 世界.对象.ContainsKey(将领))
            return 失败("临时 AI 将领来源或所属玩家无效。", out 错误);
        if (将领.详细信息 == null || 将领.将领属性 == null || 将领.将领属性.初始属性 == null ||
            将领.将领属性.成长点数 == null || 将领.将领属性.最终属性 == null || 将领.将领配兵 == null ||
            将领.将领装备表 == null || 将领.将领培养 == null)
            return 失败("临时 AI 将领完整状态缺失。", out 错误);
        var 详情 = 将领.详细信息;
        var 初始 = 将领.将领属性.初始属性;
        var 成长 = 将领.将领属性.成长点数;
        var 最终 = 将领.将领属性.最终属性;
        var 培养 = 将领.将领培养;
        var 配兵 = 将领.将领配兵;
        if (详情.身份 != 玩家ID || (详情.状态 != 0 && 详情.状态 != 1) || 详情.坑位颜色 != 0 ||
            !整数范围(初始.ID, 1, int.MaxValue) || 初始.职业 < 1 || 初始.职业 > 4 ||
            !整数范围(初始.类型, 1, 4) || !整数范围(成长.等级, 1, 99) ||
            !整数范围(配兵.ID, 1, int.MaxValue) || !整数范围(配兵.数量, 1, int.MaxValue))
            return 失败("临时 AI 将领身份、状态、兵种或兵数无效。", out 错误);
        int 兵种数 = 0;
        if (全局兵种库.属性表 != null)
            foreach (var 兵种 in 全局兵种库.属性表)
                if (兵种 != null && 兵种.ID == 配兵.ID) 兵种数++;
        if (兵种数 != 1) return 失败("临时 AI 将领的兵种不存在或重复。", out 错误);
        if (!全部有限(详情.忠诚, 详情.所属封地, 详情.经验, 详情.升级需要经验, 详情.俸禄, 详情.剩余体力,
            详情.剩余兵力, 详情.攻击模式, 详情.俘虏玩家, 详情.编队, 详情.将领叛逃计时器,
            初始.头像特效, 初始.成长, 初始.突围, 初始.武力, 初始.智力, 初始.统帅, 初始.体力上限,
            成长.爆点数, 成长.副属性爆点, 成长.总分配点数, 成长.武力分配点, 成长.智力分配点, 成长.统帅分配点,
            最终.武力, 最终.攻击, 最终.智力, 最终.防御, 最终.统帅, 最终.统兵, 最终.生命值, 最终.体力上限, 最终.统帅加成,
            培养.保底次数, 培养.保底上限, 培养.培养次数))
            return 失败("临时 AI 将领包含非有限数值。", out 错误);
        foreach (var 装备 in 将领.将领装备表)
        {
            if (装备 == null || 装备.装备信息 == null || 装备.炼魂属性 == null ||
                !全部有限(装备.强化等级, 装备.品质, 装备.强化值, 装备.保底次数, 装备.已强化次数, 装备.装备信息.等级, 装备.装备信息.基础值))
                return 失败("临时 AI 将领装备状态无效。", out 错误);
            foreach (var 炼魂 in 装备.炼魂属性)
                if (炼魂 == null || !整数范围(炼魂.类型, 1, 5) || !全部有限(炼魂.炼魂值))
                    return 失败("临时 AI 将领炼魂状态无效。", out 错误);
        }
        return true;
    }

    private static bool 整数范围(double 值, int 下限, int 上限)
    {
        return !double.IsNaN(值) && !double.IsInfinity(值) && 值 >= 下限 && 值 <= 上限 && 值 == Math.Truncate(值);
    }

    private static bool 全部有限(params double[] 数值)
    {
        foreach (double 值 in 数值) if (double.IsNaN(值) || double.IsInfinity(值)) return false;
        return true;
    }

    private static 驻防任务存档 捕获驻防(驻防军情信息 情)
    {
        if (情 == null) return null;
        int 号 = 情.所属玩家;
        int ID = 全局变量.所有玩家数据表 != null && 号 >= 0 && 号 < 全局变量.所有玩家数据表.Count && 全局变量.所有玩家数据表[号] != null
            ? 全局变量.所有玩家数据表[号].基础信息.ID : -1;
        return new 驻防任务存档 { 阶段 = 情.阶段, 所属玩家ID = ID, 出发封地ID = 情.出发封地ID,
            出发坐标x = 情.出发坐标x, 出发坐标y = 情.出发坐标y, 驻防坐标x = 情.驻防坐标x, 驻防坐标y = 情.驻防坐标y,
            出发时间 = 情.出发时间, 目标国家 = 情.目标国家, 目标城主 = 情.目标城主, 变更说明 = 情.变更说明 };
    }
    private static 资源任务存档 捕获资源(资源点军情信息 情)
    {
        return 情 == null ? null : new 资源任务存档 { 世界标识 = 情.世界标识, 资源点标识 = 情.资源点标识,
            所属玩家ID = 情.所属玩家ID, 出发封地ID = 情.出发封地ID, 目标批次 = 情.目标批次,
            出发坐标x = 情.出发坐标x, 出发坐标y = 情.出发坐标y, 出发时间 = 情.出发时间, 阶段 = 情.阶段 };
    }

    private static 资源点军情信息 恢复资源(资源任务存档 资源)
    {
        return new 资源点军情信息 { 世界标识 = 资源.世界标识, 资源点标识 = 资源.资源点标识,
            所属玩家ID = 资源.所属玩家ID, 出发封地ID = 资源.出发封地ID, 目标批次 = 资源.目标批次,
            出发坐标x = 资源.出发坐标x, 出发坐标y = 资源.出发坐标y, 出发时间 = 资源.出发时间, 阶段 = 资源.阶段 };
    }

    private static 行军任务存档 兼容旧用途(行军任务存档 任务, int 版本)
    {
        if (任务 == null || 版本 != 1 || 任务.身份 != 78 || 任务.任务用途 != 军事任务用途.出征 || 任务.驻防 != null || 任务.资源 != null) return 任务;
        return new 行军任务存档 { 任务用途 = 军事任务用途.守方援军, 战场类型 = 任务.战场类型, 坐标x = 任务.坐标x,
            坐标y = 任务.坐标y, 到达时间 = 任务.到达时间, 身份 = 任务.身份, 部队 = 任务.部队, 临时AI部队 = 任务.临时AI部队 };
    }

    private static bool 失败(string 原因, out string 错误) { 错误 = 原因; return false; }
}
