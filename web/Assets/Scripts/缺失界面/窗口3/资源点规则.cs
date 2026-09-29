using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using 玩家数据结构;
using 缺失界面.窗口4;

namespace Dwsg.Window3
{
    public enum 资源出征阶段 { 前往 = 0, 参战 = 1, 已结束 = 2 }

    // 外部只能持有并传回；不暴露可修改的目录或世界引用。
    public sealed class 资源点回滚状态
    {
        internal 资源点规则 所属;
        internal 资源点存档 目录;
        internal object 玩家, 城池;
        internal long 载入;
        internal bool 接入;
    }

    // root 的行军 DTO 保存这些附加字段，队列仍由行军存档绑定真实将领引用。
    public sealed class 资源点军情信息 : 军情信息
    {
        public const int 资源战场类型 = 2;
        public string 世界标识, 资源点标识, 服务器战场ID;
        public int 所属玩家ID, 出发封地ID, 目标批次;
        public int 出发坐标x, 出发坐标y;
        public long 出发时间;
        public 资源出征阶段 阶段;
        public 资源点军情信息()
        {
            身份 = 0;
            战场类型 = 资源战场类型;
            // 追加用途 4；不占用和平驻防的 2/3 或身份 79。
            任务用途 = 军事任务用途.占领资源点;
        }
    }

    // 本工程本地配置：最多两处、库存为基准一小时产量、采尽后十分钟恢复。
    // 接入次序：恢复目录 -> 恢复/校验军情 -> 开启战斗 hook -> 推进；导出在财产快照前。
    public sealed class 资源点规则
    {
        public static readonly 资源点规则 本地 = new 资源点规则();
        public const int 占领上限 = 2, 结算间隔 = 10, 恢复间隔 = 600;
        private const long 最大秒 = 253402300799L;
        private const double 最大财产 = 9007199254740991d;
        private 资源点存档 状态;
        private object 玩家锚点, 城池锚点;
        // 默认关闭；root 完成军情调度、战果分支和存读 hook 后才开启。
        private bool 离线战斗接入;
        public bool 战斗接入完成 { get { return Dwsg.Network.GameNetwork.Enabled ? Dwsg.Combat.ResourceClient.Ready : 离线战斗接入; } set { 离线战斗接入 = value; } }
        public long 载入号 { get; private set; }

        // 不透明运行时快照仅用于同步加载事务，允许尚未绑定和正在交战的世界。
        private static 资源点存档 复制目录(资源点存档 原)
        {
            return 原 == null ? null : new 资源点存档 { 版本 = 原.版本, 配置版本 = 原.配置版本,
                世界标识 = 原.世界标识, 待生成 = 原.待生成, 点位 = 原.点位.Select(x => x.副本()).ToList() };
        }
        public 资源点回滚状态 捕获回滚状态()
        {
            return new 资源点回滚状态 { 所属 = this, 目录 = 复制目录(状态), 玩家 = 玩家锚点,
                城池 = 城池锚点, 载入 = 载入号, 接入 = 战斗接入完成 };
        }
        public void 恢复回滚状态(资源点回滚状态 原)
        {
            if (原 == null || !ReferenceEquals(原.所属, this)) throw new ArgumentException("资源事务快照来源无效。");
            状态 = 复制目录(原.目录); 玩家锚点 = 原.玩家; 城池锚点 = 原.城池;
            载入号 = 原.载入; 战斗接入完成 = 原.接入;
        }

        public static int 基础时产(资源点类型 类型)
        {
            var 基准 = new 玩家数据();
            var 地 = new 封地信息();
            地.建筑信息表.Add(new 建筑信息 { 类型 = 0, 等级 = 1 });
            地.建筑信息表.Add(new 建筑信息 { 类型 = 2, 等级 = 1 });
            地.建筑信息表.Add(new 建筑信息 { 类型 = 3, 等级 = 1 });
            基准.封地信息表.Add(地);
            // 房屋只加人口；铜钱的实际 getter 使用大厅，农场 getter 使用类型 3。
            double 数 = 类型 == 资源点类型.铜矿 ? 基准.获取铜钱产量() :
                类型 == 资源点类型.牧场 ? 基准.获取粮食产量() : -1;
            if (!FiefActions.Finite(数) || 数 < 1 || 数 > 1000000 || 数 != Math.Floor(数))
                throw new InvalidOperationException("资源基础时产无效。");
            return (int)数;
        }
        private bool 同世界()
        {
            return 状态 != null && ReferenceEquals(玩家锚点, 全局变量.所有玩家数据表) &&
                ReferenceEquals(城池锚点, 全局变量.所有城池列表);
        }
        private static bool 秒合法(long 秒) { return 秒 >= 0 && 秒 <= 最大秒; }
        private static 玩家数据 玩家(int ID)
        {
            if (全局变量.所有玩家数据表 == null) return null;
            var 表 = 全局变量.所有玩家数据表.Where(x => x != null && x.基础信息 != null && x.基础信息.ID == ID).ToList();
            return 表.Count == 1 ? 表[0] : null;
        }
        private static 玩家数据 当前玩家()
        {
            var 表 = 全局变量.所有玩家数据表;
            return 表 != null && 全局变量.本机身份 >= 0 && 全局变量.本机身份 < 表.Count ? 表[全局变量.本机身份] : null;
        }
        private static bool 整坐标(double 数) { return FiefActions.Finite(数) && 数 <= int.MaxValue && 数 == Math.Floor(数); }
        private static bool 可放点(int x, int y)
        {
            var 图 = 全局大地图库.大地图表;
            if (图 == null || x < 1 || y < 1 || x > 图.GetLength(1) || y > 图.GetLength(0) || 图[y - 1, x - 1] != 1) return false;
            if (全局变量.所有城池列表 != null && 全局变量.所有城池列表.Any(c => c != null && c.坐标x == x && c.坐标y == y)) return false;
            // 山贼目录在场景加载后才建立，仍须提前保留它的整个坐标域。
            if (x < 全局变量.横向山贼数量 && y < 全局变量.竖向山贼数量) return false;
            return 全局变量.所有山贼数据列表 == null || !全局变量.所有山贼数据列表.Any(c => c != null && c.坐标x == x && c.坐标y == y);
        }
        private static List<资源点配置> 生成目录()
        {
            var 主 = 当前玩家(); var 图 = 全局大地图库.大地图表;
            var 地 = 主 == null || 主.封地信息表 == null ? null : 主.封地信息表
                .Where(x => x != null && x.所在城池 != null && 整坐标(x.所在城池.x) && 整坐标(x.所在城池.y))
                .OrderBy(x => x.ID).FirstOrDefault();
            var 结果 = new List<资源点配置>();
            if (地 == null || 图 == null) return 结果;
            return Dwsg.Shared.Combat.ResourcePointRules.Generate(图, (int)地.所在城池.x, (int)地.所在城池.y, 可放点);
        }

        private static 资源点存档 新状态(string 世界, IEnumerable<资源点配置> 目录)
        {
            var 新 = new 资源点存档 { 世界标识 = 世界 };
            foreach (var 点 in 目录)
            {
                if (点 == null) throw new ArgumentException("资源点配置缺失。");
                int 产 = 基础时产(点.类型);
                新.点位.Add(new 资源点状态 { 标识 = 点.标识, 类型 = 点.类型, 坐标x = 点.坐标x, 坐标y = 点.坐标y, 时产 = 产, 剩余库存 = 产 });
            }
            新.待生成 = 新.点位.Count == 0;
            return 新;
        }
        private static string 检查状态(资源点存档 候选, string 世界)
        {
            Guid 标识;
            if (!Guid.TryParseExact(世界, "N", out 标识) || 候选 == null || 候选.世界标识 != 世界 || 候选.版本 != 1 || 候选.配置版本 != 1)
                return "资源点版本或世界标识无效。";
            if (候选.点位 == null || 候选.点位.Count > 128 || 候选.待生成 != (候选.点位.Count == 0)) return "资源点目录无效。";
            var ID = new HashSet<string>(StringComparer.Ordinal); var 坐标 = new HashSet<string>(); var 占领数 = new Dictionary<int, int>();
            foreach (var 点 in 候选.点位)
            {
                if (点 == null || string.IsNullOrEmpty(点.标识) || 点.标识.Length > 80 || !ID.Add(点.标识) ||
                    !坐标.Add(点.坐标x + "," + 点.坐标y) || !可放点(点.坐标x, 点.坐标y)) return "资源点标识、位置重复或覆盖既有目标。";
                if ((点.类型 != 资源点类型.铜矿 && 点.类型 != 资源点类型.牧场) || 点.时产 != 基础时产(点.类型) ||
                    点.剩余库存 < 0 || 点.剩余库存 > 点.时产 || 点.产出余数 < 0 || 点.产出余数 >= 3600 ||
                    点.节奏余秒 < 0 || 点.节奏余秒 >= 结算间隔 || 点.批次 < 0 || 点.批次 == int.MaxValue ||
                    !秒合法(点.结算时间) || !秒合法(点.恢复时间) || 点.占领玩家ID < -1) return "资源点库存、产量、时间或结算余数无效。";
                if (点.剩余库存 == 0)
                {
                    if (点.占领玩家ID != -1 || 点.恢复时间 <= 点.结算时间 || 点.恢复时间 - 点.结算时间 > 恢复间隔 || 点.节奏余秒 != 0 || 点.产出余数 != 0) return "采尽点位状态无效。";
                }
                else if (点.恢复时间 != 0 || (点.占领玩家ID == -1 && 点.节奏余秒 != 0)) return "资源点恢复阶段无效。";
                if (点.占领玩家ID >= 0)
                {
                    var 主 = 玩家(点.占领玩家ID);
                    if (主 == null || 主.财产信息 == null) return "资源点占领者不存在。";
                    int 数; 占领数.TryGetValue(点.占领玩家ID, out 数); 占领数[点.占领玩家ID] = 数 + 1;
                    if (数 >= 占领上限) return "资源点占领数量超限。";
                }
            }
            return null;
        }
        private static 资源点存档 解析(string json)
        {
            if (json.Length > 262144) throw new JsonSerializationException("资源点快照过大。");
            JObject 对象;
            using (var 读 = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, MaxDepth = 16 })
            {
                对象 = JObject.Load(读, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (读.Read()) throw new JsonSerializationException("资源点快照尾部存在额外内容。");
            }
            foreach (var 项 in 对象.Descendants().OfType<JProperty>())
            {
                JTokenType 期望 = 项.Name == "世界标识" || 项.Name == "标识" ? JTokenType.String :
                    项.Name == "待生成" ? JTokenType.Boolean : 项.Name == "点位" ? JTokenType.Array : JTokenType.Integer;
                if (项.Value.Type != 期望) throw new JsonSerializationException("资源点字段类型无效：" + 项.Name);
            }
            return 对象.ToObject<资源点存档>(JsonSerializer.Create(new JsonSerializerSettings
            { MissingMemberHandling = MissingMemberHandling.Error, TypeNameHandling = TypeNameHandling.None, MetadataPropertyHandling = MetadataPropertyHandling.Ignore }));
        }
        // 缺失字段的旧档传 null；无封地时保留待生成，首次建立封地后由推进补齐。
        public CityResult 恢复(string json, string 世界, long 现在, IEnumerable<资源点配置> 预置 = null)
        {
            if (!秒合法(现在)) return CityResult.Fail("资源点时间无效。");
            try
            {
                var 候选 = json == null ? 新状态(世界, 预置 ?? 生成目录()) : 解析(json);
                string 错 = 检查状态(候选, 世界);
                if (错 != null) return CityResult.Fail(错);
                状态 = 候选; 载入号++; 玩家锚点 = 全局变量.所有玩家数据表; 城池锚点 = 全局变量.所有城池列表;
                return CityResult.Ok("资源点状态已载入。");
            }
            catch (Exception e) when (e is JsonException || e is ArgumentException || e is OverflowException || e is InvalidOperationException)
            { return CityResult.Fail("资源点快照无法读取。"); }
        }
        public List<资源点状态> 查询(string 搜索 = "")
        {
            if (Dwsg.Network.GameNetwork.Enabled) return Dwsg.Combat.ResourceClient.Read(搜索);
            if (!同世界()) return new List<资源点状态>();
            return 状态.点位.Where(x => string.IsNullOrEmpty(搜索) ||
                (x.类型 + " " + x.坐标x + "," + x.坐标y).IndexOf(搜索, StringComparison.OrdinalIgnoreCase) >= 0).Select(x => x.副本()).ToList();
        }
        private 资源点状态 找点(string ID) { return 同世界() ? 状态.点位.Find(x => x.标识 == ID) : null; }
        public int 已占数量(int 玩家ID) { return 查询().Count(x => x.占领玩家ID == 玩家ID); }
        public List<资源点军情信息> 军情()
        {
            if (Dwsg.Network.GameNetwork.Enabled) return Dwsg.Combat.ResourceClient.Armies();
            return !同世界() || 全局变量.军情列表 == null ? new List<资源点军情信息>() : 全局变量.军情列表
                .OfType<资源点军情信息>().Where(x => x.世界标识 == 状态.世界标识 && x.阶段 != 资源出征阶段.已结束).ToList();
        }
        public CityResult 校验军情(资源点军情信息 情)
        {
            var 点 = 情 == null ? null : 找点(情.资源点标识);
            var 主 = 情 == null ? null : 玩家(情.所属玩家ID);
            var 地表 = 主 == null || 主.封地信息表 == null ? new List<封地信息>() : 主.封地信息表.Where(x => x != null && x.ID == 情.出发封地ID).ToList();
            var 地 = 地表.Count == 1 ? 地表[0] : null;
            if (点 == null || 主 == null || 地 == null || 地.所在城池 == null || 情.世界标识 != 状态.世界标识 ||
                情.战场类型 != 资源点军情信息.资源战场类型 || 情.身份 != 0 || 情.任务用途 != 军事任务用途.占领资源点 || 情.临时AI部队 ||
                情.阶段 < 资源出征阶段.前往 || 情.阶段 > 资源出征阶段.参战 ||
                情.已进入战场 != (情.阶段 == 资源出征阶段.参战) || !秒合法(情.出发时间) || !秒合法(情.到达时间) ||
                情.到达时间 <= 情.出发时间 || 情.坐标x != 点.坐标x || 情.坐标y != 点.坐标y || 情.目标批次 != 点.批次 ||
                情.出发坐标x != 地.所在城池.x || 情.出发坐标y != 地.所在城池.y ||
                情.队列将领列表 == null || 情.队列将领列表.Count < 1 || 情.队列将领列表.Count > 5 ||
                情.队列将领列表.Any(x => x == null || 地.将领信息表 == null || !地.将领信息表.Contains(x) || x.详细信息 == null ||
                    x.将领配兵 == null || !FiefActions.Finite(x.将领配兵.数量) || x.详细信息.身份 != 情.所属玩家ID) ||
                情.队列将领列表.Distinct().Count() != 情.队列将领列表.Count)
                return CityResult.Fail("资源出征目标、阶段或将领引用无效。");
            return CityResult.Ok("资源军情校验通过。");
        }
        public CityResult 检查目标(string ID)
        {
            if (Dwsg.Network.GameNetwork.Enabled) return Dwsg.Combat.ResourceClient.CheckTarget(ID);
            if (!战斗接入完成) return CityResult.Fail("资源点战斗尚未接入。");
            var 点 = 找点(ID); var 主 = 当前玩家();
            if (点 == null || 主 == null || 主.基础信息 == null || 主.基础信息.ID < 0 ||
                !ReferenceEquals(玩家(主.基础信息.ID), 主) || 主.财产信息 == null) return CityResult.Fail("请重新选择资源点。");
            if (主.封地信息表 == null || 主.封地信息表.Count == 0) return CityResult.Fail("请先建立封地。");
            if (点.占领玩家ID != -1 || 点.剩余库存 <= 0 || 点.恢复时间 != 0 || 军情().Any(x => x.资源点标识 == ID))
                return CityResult.Fail("资源点已占领、恢复中或已有部队出征。");
            if (已占数量(主.基础信息.ID) + 军情().Count(x => x.所属玩家ID == 主.基础信息.ID) >= 占领上限)
                return CityResult.Fail("已达到两处资源点上限，出征中的目标计入名额。");
            return CityResult.Ok("可以选择将领占领资源点。");
        }
        // root 在绑定全部真实军情后调用；对同点预占、同将多用及超限一次检查。
        public CityResult 校验军情集合(IList<军情信息> 列表)
        {
            if (!同世界() || 列表 == null) return CityResult.Fail("资源军情列表缺失。");
            var 目标 = new HashSet<string>(); var 预占 = new Dictionary<int, int>();
            foreach (var 情 in 列表)
            {
                if (情 == null) return CityResult.Fail("军情记录缺失。");
                var 资源 = 情 as 资源点军情信息;
                if (情 != null && 情.战场类型 == 资源点军情信息.资源战场类型 && 资源 == null)
                    return CityResult.Fail("资源军情附加数据缺失。");
                if (资源 == null) continue;
                var 检查 = 校验军情(资源); if (!检查.Success) return 检查;
                var 点 = 找点(资源.资源点标识);
                if (点.占领玩家ID != -1 || 点.剩余库存 <= 0 || !目标.Add(点.标识)) return CityResult.Fail("资源出征目标已占领、恢复中或重复。");
                int 数; 预占.TryGetValue(资源.所属玩家ID, out 数); 预占[资源.所属玩家ID] = ++数;
                if (数 + 已占数量(资源.所属玩家ID) > 占领上限) return CityResult.Fail("资源点预占名额超限。");
                foreach (var 将 in 资源.队列将领列表)
                    if (列表.Count(x => x != null && x.队列将领列表 != null && x.队列将领列表.Contains(将)) != 1)
                        return CityResult.Fail("资源将领重复用于其他军情。");
            }
            return CityResult.Ok("资源军情集合校验通过。");
        }
        public CityResult 派遣(string ID, int 封地ID, IList<将领信息> 将领, long 现在, long 请求载入号, out 资源点军情信息 情)
        {
            情 = null;
            if (请求载入号 != 载入号) return CityResult.Fail("世界已切换，请重新选择资源将领。");
            var 检查 = 检查目标(ID); if (!检查.Success) return 检查;
            if (!秒合法(现在)) return CityResult.Fail("资源出征时间无效。");
            var 点 = 找点(ID); var 主 = 当前玩家();
            var 地表 = 主.封地信息表 == null ? new List<封地信息>() : 主.封地信息表.Where(x => x != null && x.ID == 封地ID).ToList();
            var 地 = 地表.Count == 1 ? 地表[0] : null;
            if (地 == null || 地.所在城池 == null || !整坐标(地.所在城池.x) || !整坐标(地.所在城池.y) ||
                将领 == null || 将领.Count < 1 || 将领.Count > 5 || 将领.Distinct().Count() != 将领.Count || 全局变量.军情列表 == null)
                return CityResult.Fail("请选择本封地的一至五名空闲将领。");
            foreach (var 将 in 将领)
            {
                if (将 == null || 地.将领信息表 == null || !地.将领信息表.Contains(将) || 将.详细信息 == null || 将.将领配兵 == null ||
                    将.将领属性 == null || 将.将领属性.初始属性 == null || 将.将领属性.成长点数 == null || 将.将领属性.最终属性 == null ||
                    将.详细信息.身份 != 主.基础信息.ID || 将.详细信息.状态 != 0 || !FiefActions.Finite(将.详细信息.剩余体力) || 将.详细信息.剩余体力 < 5 ||
                    将.将领配兵.ID != Math.Floor(将.将领配兵.ID) || 全局兵种库.查询指定ID的数据(将.将领配兵.ID) == null ||
                    !FiefActions.Finite(将.将领配兵.数量) || 将.将领配兵.数量 < 1 || 将.将领配兵.数量 != Math.Floor(将.将领配兵.数量) ||
                    全局变量.军情列表.Any(x => x != null && x.队列将领列表 != null && x.队列将领列表.Contains(将)) ||
                    (全局变量.Ai军情列表 != null && 全局变量.Ai军情列表.Any(x => x != null && x.队列将领列表 != null && x.队列将领列表.Contains(将))) ||
                    (全局变量.所有城池列表 != null && 全局变量.所有城池列表.Any(x => x != null && x.城池玩家驻防列表 != null && x.城池玩家驻防列表.Contains(将))))
                    return CityResult.Fail("将领、配兵或体力已变化，或将领已在其他军情中。");
            }
            long 秒 = Math.Max(10, ((long)Math.Abs(地.所在城池.x - 点.坐标x) + (long)Math.Abs(地.所在城池.y - 点.坐标y)) * 10);
            if (秒 > 最大秒 - 现在) return CityResult.Fail("资源出征到达时间无效。");
            情 = new 资源点军情信息 { 世界标识 = 状态.世界标识, 资源点标识 = ID, 所属玩家ID = 主.基础信息.ID,
                出发封地ID = 地.ID, 出发坐标x = (int)地.所在城池.x, 出发坐标y = (int)地.所在城池.y,
                坐标x = 点.坐标x, 坐标y = 点.坐标y, 目标批次 = 点.批次, 出发时间 = 现在, 到达时间 = 现在 + 秒,
                队列将领列表 = new List<将领信息>(将领) };
            foreach (var 将 in 将领) { 将.详细信息.状态 = 1; 将.详细信息.剩余体力 -= 5; }
            全局变量.军情列表.Add(情);
            return CityResult.Ok("资源部队已出发，预计" + 秒 + "秒后到达。");
        }
        public CityResult 撤回(资源点军情信息 情)
        {
            var 主 = 当前玩家();
            if (!同世界() || 情 == null || 主 == null || 主.基础信息 == null || 情.所属玩家ID != 主.基础信息.ID ||
                情.世界标识 != 状态.世界标识 || 情.阶段 != 资源出征阶段.前往 || 情.已进入战场 ||
                全局变量.军情列表 == null || !全局变量.军情列表.Contains(情)) return CityResult.Fail("只能撤回自己尚未进入战场的资源部队。");
            foreach (var 将 in 情.队列将领列表) if (将 != null && 将.详细信息 != null && 将.详细信息.状态 == 1) 将.详细信息.状态 = 0;
            情.阶段 = 资源出征阶段.已结束; 全局变量.军情列表.Remove(情);
            return CityResult.Ok("部队已返回待命，保留配兵；已用体力不退还。");
        }
        internal CityResult 结算战果(资源点军情信息 情, bool 胜利, long 现在)
        {
            var 检查 = 校验军情(情); if (!检查.Success) return 检查;
            if (!秒合法(现在) || 现在 < 情.到达时间 || 情.阶段 != 资源出征阶段.参战 ||
                !全局变量.军情列表.Contains(情)) return CityResult.Fail("资源战果不能结算。");
            var 点 = 找点(情.资源点标识);
            if (胜利 && (点.占领玩家ID != -1 || 点.恢复时间 != 0 || 点.剩余库存 <= 0 || 已占数量(情.所属玩家ID) >= 占领上限))
                return CityResult.Fail("资源点归属或名额已变化。");
            if (胜利) { 点.占领玩家ID = 情.所属玩家ID; 点.结算时间 = 现在; 点.节奏余秒 = 0; }
            情.阶段 = 资源出征阶段.已结束;
            // 不清军情、不复制将领、不改战损；root 的实际参战集合与统一退场负责清理。
            return CityResult.Ok(胜利 ? "已占领" + 点.类型 + "，开始采集。" : "资源点未占领。");
        }
        private static void 刷新库存(资源点状态 点)
        {
            点.剩余库存 = 点.时产; 点.恢复时间 = 0; 点.结算时间 = 0;
            点.产出余数 = 点.节奏余秒 = 0; 点.批次++;
        }
        private CityResult 结算(long 现在, string 放弃ID)
        {
            if (!同世界() || !秒合法(现在)) return CityResult.Fail("请先载入当前世界的资源点。");
            string 错 = 检查状态(状态, 状态.世界标识); if (错 != null) return CityResult.Fail(错);
            var 副本 = 状态.点位.Select(x => x.副本()).ToList();
            var 收入 = new Dictionary<int, long[]>();
            foreach (var 点 in 副本)
            {
                int 所属 = 点.占领玩家ID;
                long 实得;
                try { 实得 = Dwsg.Shared.Combat.ResourcePointRules.Advance(点, 现在, 点.标识 == 放弃ID); }
                catch (InvalidOperationException e) { return CityResult.Fail(e.Message); }
                if (所属 >= 0)
                {
                    long[] 账; if (!收入.TryGetValue(所属, out 账)) 收入[所属] = 账 = new long[2];
                    账[(int)点.类型] += 实得;
                }
            }
            // 先检查全部财产，再一次应用；任何负值/NaN/溢出都不消耗库存或时间。
            foreach (var 项 in 收入)
            {
                var 主 = 玩家(项.Key); var 钱 = 主 == null ? null : 主.财产信息;
                if (钱 == null || !FiefActions.Finite(钱.铜钱) || !FiefActions.Finite(钱.粮食) ||
                    钱.铜钱 > 最大财产 - 项.Value[0] || 钱.粮食 > 最大财产 - 项.Value[1]) return CityResult.Fail("资源收益无法入账，财产数据无效。");
            }
            foreach (var 项 in 收入) { var 钱 = 玩家(项.Key).财产信息; 钱.铜钱 += 项.Value[0]; 钱.粮食 += 项.Value[1]; }
            状态.点位 = 副本;
            return CityResult.Ok("资源点已结算。");
        }
        public CityResult 推进(long 现在, out List<资源点军情信息> 到达)
        {
            到达 = new List<资源点军情信息>();
            if (!同世界() || !秒合法(现在)) return CityResult.Fail("资源点世界或时间无效。");
            if (同世界() && 状态.待生成)
            {
                var 目录 = 生成目录();
                if (目录.Count > 0) { var 新 = 新状态(状态.世界标识, 目录); 状态 = 新; }
            }
            var 结果 = 结算(现在, null); if (!结果.Success) return 结果;
            if (战斗接入完成) 到达 = 军情().Where(x => x.阶段 == 资源出征阶段.前往 && !x.已进入战场 && x.到达时间 <= 现在).ToList();
            return 结果;
        }
        public CityResult 放弃(string ID, long 现在)
        {
            var 点 = 找点(ID); var 主 = 当前玩家();
            if (点 == null || 主 == null || 主.基础信息 == null || 点.占领玩家ID != 主.基础信息.ID || 现在 < 点.结算时间)
                return CityResult.Fail("只能放弃自己占领的资源点。");
            var 结果 = 结算(现在, ID);
            return 结果.Success ? CityResult.Ok("已放弃资源点，剩余库存和不足一单位的进度保留。") : 结果;
        }
        // 只用于读档失败回滚，含战斗中点位状态；不入账，也不代替正常保存的导出。
        public CityResult 备份(out string json)
        {
            json = null;
            if (!同世界()) return CityResult.Fail("请先载入当前世界的资源点。");
            string 错 = 检查状态(状态, 状态.世界标识); if (错 != null) return CityResult.Fail(错);
            json = JsonConvert.SerializeObject(状态);
            return CityResult.Ok("资源回滚快照已捕获。");
        }
        // root 保存前调用；不推进战斗。资源状态与入账后的玩家财产写入同一个槽位。
        public CityResult 导出(long 现在, out string json)
        {
            json = null;
            if (!同世界()) return CityResult.Fail("请先载入当前世界的资源点。");
            if (全局变量.军情列表 != null && 全局变量.军情列表.OfType<资源点军情信息>().Any(x => x.世界标识 == 状态.世界标识 && x.已进入战场))
                return CityResult.Fail("请在资源战斗结束后保存。");
            var 检查 = 校验军情集合(全局变量.军情列表); if (!检查.Success) return 检查;
            var 结果 = 结算(现在, null); if (!结果.Success) return 结果;
            json = JsonConvert.SerializeObject(状态);
            return CityResult.Ok("资源点快照已捕获。");
        }
    }
}
