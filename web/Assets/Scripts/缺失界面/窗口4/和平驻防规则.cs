using System;
using System.Collections.Generic;
using System.Linq;
using 玩家数据结构;

namespace 缺失界面.窗口4
{
    public enum 军事任务用途 { 出征 = 0, 守方援军 = 1, 和平驻防 = 2, 驻防撤回 = 3, 占领资源点 = 4 }
    public enum 驻防任务阶段 { 前往 = 0, 驻守 = 1, 撤回 = 2, 参战 = 3 }

    // 存档应按用途还原此类型，并把队列将领重新绑定到所属玩家的原将领对象。
    public sealed class 驻防军情信息 : 军情信息
    {
        public 驻防军情信息()
        {
            任务用途 = 军事任务用途.和平驻防;
            身份 = 和平驻防规则.旧调度隔离身份;
            战场类型 = 1;
        }
        public 驻防任务阶段 阶段;
        public int 所属玩家;
        public int 出发封地ID;
        public double 出发坐标x;
        public double 出发坐标y;
        public int 驻防坐标x;
        public int 驻防坐标y;
        public long 出发时间;
        public string 目标国家;
        public int 目标城主;
        public string 变更说明 = "";
        [Newtonsoft.Json.JsonIgnore] public string 服务器任务ID;
        [Newtonsoft.Json.JsonIgnore] public string 服务器战场ID;
    }

    public static class 和平驻防规则
    {
        public const int 驻防占用状态 = 2;
        // 旧调度只识别 0、78、666。独立用途不得进入旧战斗分支。
        public const int 旧调度隔离身份 = 79;
        public const int 每格秒数 = 10;
        public static bool 是驻防军情(军情信息 情)
        {
            return 情 is 驻防军情信息 || (情 != null && (情.任务用途 == 军事任务用途.和平驻防 || 情.任务用途 == 军事任务用途.驻防撤回));
        }
        public static 军事任务用途 获取用途(军情信息 情)
        {
            return 情 != null && 情.任务用途 != 军事任务用途.出征 ? 情.任务用途 : 情 != null && 情.身份 == 78 ? 军事任务用途.守方援军 : 军事任务用途.出征;
        }
        public static List<驻防军情信息> 获取任务()
        {
            return 全局变量.军情列表 == null ? new List<驻防军情信息>() : 全局变量.军情列表.OfType<驻防军情信息>().ToList();
        }
        private static 玩家数据 玩家(int 号)
        {
            return 全局变量.所有玩家数据表 != null && 号 >= 0 && 号 < 全局变量.所有玩家数据表.Count ? 全局变量.所有玩家数据表[号] : null;
        }
        private static 封地信息 所属封地(玩家数据 主, 将领信息 将)
        {
            if (主 == null || 主.封地信息表 == null || 将 == null) return null;
            return 主.封地信息表.FirstOrDefault(x => x != null && x.将领信息表 != null && x.将领信息表.Contains(将));
        }
        private static 城池信息库类 城池(int x, int y)
        {
            return 全局变量.所有城池列表 == null ? null : 全局变量.所有城池列表.FirstOrDefault(c => c != null && c.坐标x == x && c.坐标y == y);
        }
        public static bool 友方城池(玩家数据 主, 城池信息库类 城)
        {
            int 号 = 全局变量.所有玩家数据表 != null ? 全局变量.所有玩家数据表.IndexOf(主) : -1;
            return 主 != null && 主.基础信息 != null && 城 != null && 号 >= 0 &&
                (城.城主 == 号 || (!string.IsNullOrEmpty(主.基础信息.国家) && 城.国家 == 主.基础信息.国家));
        }
        public static 军事结果 检查目标(玩家数据 主, 城池信息库类 城)
        {
            if (主 == null) return 军事结果.拒绝(军事错误.无角色, "角色尚未载入。");
            if (城 == null || 全局变量.所有城池列表 == null || !全局变量.所有城池列表.Contains(城))
                return 军事结果.拒绝(军事错误.状态冲突, "城池已变更，请重新选择。");
            if (!友方城池(主, 城)) return 军事结果.拒绝(军事错误.状态冲突, "只能驻防本人或本国城池。");
            if (城.正在交战) return 军事结果.拒绝(军事错误.状态冲突, "城池正在交战，请使用守方援军。");
            if (!军事本地规则.有限(城.获取驻防上限()) || 城.获取驻防上限() <= 0)
                return 军事结果.拒绝(军事错误.达到上限, "本城没有可用驻防容量。");
            return 军事结果.通过("可派遣驻防");
        }
        public static long 计算行军秒数(double x, double y, int 目标x, int 目标y)
        {
            return Dwsg.Shared.Combat.PeaceGarrisonRules.TravelSeconds(x, y, 目标x, 目标y);
        }
        public static double 已占兵力(城池信息库类 城, 驻防军情信息 忽略 = null)
        {
            if (Dwsg.Network.GameNetwork.Enabled && 城 != null)
                return Dwsg.Administration.AdministrationClient.PublicCity(城.坐标x, 城.坐标y)?.Value<double>("和平驻防兵力") ?? 0;
            var 已计 = new HashSet<将领信息>();
            double 数量 = 0;
            if (城 == null) return 0;
            if (城.城池玩家驻防列表 != null)
                foreach (var 将 in 城.城池玩家驻防列表)
                    if (将 != null && 将.将领配兵 != null && 已计.Add(将)) 数量 += Math.Max(0, 将.将领配兵.数量);
            foreach (var 情 in 获取任务())
                if (!ReferenceEquals(情, 忽略) && 情.阶段 == 驻防任务阶段.前往 && 情.驻防坐标x == 城.坐标x && 情.驻防坐标y == 城.坐标y && 情.队列将领列表 != null)
                    foreach (var 将 in 情.队列将领列表)
                        if (将 != null && 将.将领配兵 != null && 已计.Add(将)) 数量 += Math.Max(0, 将.将领配兵.数量);
            return 数量;
        }
        public static 军事结果 派遣(玩家数据 主, 封地信息 封地, 城池信息库类 城, List<将领信息> 将领表, long 现在)
        {
            if (Dwsg.Network.GameNetwork.Enabled) return 军事结果.拒绝(军事错误.状态冲突, "联机驻防须等待服务器确认。");
            var 检查 = 检查目标(主, 城);
            if (!检查.成功) return 检查;
            检查 = 军事本地规则.检查出征(主, 将领表);
            if (!检查.成功) return 检查;
            if (封地 == null || !主.封地信息表.Contains(封地) || 封地.所在城池 == null ||
                主.封地信息表.Count(x => x != null && x.ID == 封地.ID) != 1 || 将领表.Any(x => !封地.将领信息表.Contains(x)))
                return 军事结果.拒绝(军事错误.无封地, "请从同一有效封地选择驻防将领。");
            if (封地.所在城池.x < 0 || 封地.所在城池.y < 0 || 城.坐标x < 0 || 城.坐标y < 0 ||
                将领表.Any(x => !军事本地规则.有限(x.将领配兵.ID) || x.将领配兵.ID != Math.Floor(x.将领配兵.ID) || x.将领配兵.数量 != Math.Floor(x.将领配兵.数量)))
                return 军事结果.拒绝(军事错误.数量无效, "驻防坐标或部队数据无效。");
            if (全局变量.军情列表 == null || 将领表.Any(将 => 全局变量.军情列表.Any(q => q != null && q.队列将领列表 != null && q.队列将领列表.Contains(将)) ||
                全局变量.所有城池列表.Any(c => c != null && c.城池玩家驻防列表 != null && c.城池玩家驻防列表.Contains(将))))
                return 军事结果.拒绝(军事错误.状态冲突, "将领已有行军或驻防任务，不能重复派遣。");
            double 兵力 = 将领表.Sum(x => x.将领配兵.数量), 已占 = 已占兵力(城);
            if (!军事本地规则.有限(兵力) || !军事本地规则.有限(已占) || 已占 + 兵力 > 城.获取驻防上限())
                return 军事结果.拒绝(军事错误.达到上限, "驻防兵力超过本城容量（含在途部队）。");
            long 秒 = 计算行军秒数(封地.所在城池.x, 封地.所在城池.y, 城.坐标x, 城.坐标y);
            if (现在 < 0 || 秒 < 0 || 现在 > 253402300799L - 秒) return 军事结果.拒绝(军事错误.数量无效, "行军时间或坐标无效。");
            var 情 = new 驻防军情信息 {
                身份 = 旧调度隔离身份, 战场类型 = 1, 所属玩家 = 全局变量.所有玩家数据表.IndexOf(主), 出发封地ID = 封地.ID,
                出发坐标x = 封地.所在城池.x, 出发坐标y = 封地.所在城池.y, 驻防坐标x = 城.坐标x, 驻防坐标y = 城.坐标y,
                坐标x = 城.坐标x, 坐标y = 城.坐标y, 出发时间 = 现在, 到达时间 = 现在 + 秒, 目标国家 = 城.国家,
                目标城主 = 城.城主, 队列将领列表 = new List<将领信息>(将领表)
            };
            foreach (var 将 in 将领表) 将.详细信息.状态 = 驻防占用状态;
            全局变量.军情列表.Add(情);
            return 军事结果.通过("驻防部队已出发，预计" + 秒 + "秒后到达。");
        }
        private static void 移除城池关联(将领信息 将)
        {
            if (全局变量.所有城池列表 == null) return;
            foreach (var 城 in 全局变量.所有城池列表)
                if (城 != null && 城.城池玩家驻防列表 != null) 城.城池玩家驻防列表.RemoveAll(x => ReferenceEquals(x, 将));
        }
        private static 封地信息 返回封地(驻防军情信息 情, 玩家数据 主)
        {
            if (主 == null || 主.封地信息表 == null) return null;
            var 表 = 主.封地信息表.Where(x => x != null && x.ID == 情.出发封地ID && x.所在城池 != null).ToList();
            if (表.Count == 1) return 表[0];
            var 实际 = 情.队列将领列表.Select(x => 所属封地(主, x)).Distinct().ToList();
            return 实际.Count == 1 && 实际[0] != null && 实际[0].所在城池 != null ? 实际[0] : null;
        }
        private static void 当前坐标(驻防军情信息 情, long 现在, out double x, out double y)
        {
            double 进度 = 情.到达时间 <= 情.出发时间 ? 1 : Math.Min(1, Math.Max(0, ((double)现在 - 情.出发时间) / ((double)情.到达时间 - 情.出发时间)));
            x = 情.出发坐标x + (情.坐标x - 情.出发坐标x) * 进度;
            y = 情.出发坐标y + (情.坐标y - 情.出发坐标y) * 进度;
        }
        private static bool 开始返回(驻防军情信息 情, long 现在, string 说明)
        {
            var 封地 = 返回封地(情, 玩家(情.所属玩家));
            if (封地 == null) { 情.变更说明 = "返回封地已失效，请先恢复所属封地。"; return false; }
            double x, y;
            当前坐标(情, 现在, out x, out y);
            long 秒 = 计算行军秒数(x, y, 封地.所在城池.x, 封地.所在城池.y);
            if (现在 < 0 || 秒 < 0 || 现在 > 253402300799L - 秒) return false;
            foreach (var 将 in 情.队列将领列表) 移除城池关联(将);
            情.任务用途 = 军事任务用途.驻防撤回;
            情.阶段 = 驻防任务阶段.撤回;
            情.出发封地ID = 封地.ID;
            情.出发坐标x = x; 情.出发坐标y = y;
            情.坐标x = 封地.所在城池.x; 情.坐标y = 封地.所在城池.y;
            情.出发时间 = 现在; 情.到达时间 = 现在 + 秒; 情.已进入战场 = false;
            情.变更说明 = 说明;
            return true;
        }
        public static 军事结果 撤回(玩家数据 主, 驻防军情信息 情, long 现在)
        {
            if (Dwsg.Network.GameNetwork.Enabled) return 军事结果.拒绝(军事错误.状态冲突, "联机撤回须等待服务器确认。");
            if (情 == null || 全局变量.军情列表 == null || !全局变量.军情列表.Contains(情) || !ReferenceEquals(主, 玩家(情.所属玩家)))
                return 军事结果.拒绝(军事错误.状态冲突, "驻防任务已变更，请重新选择。");
            if (情.阶段 == 驻防任务阶段.撤回) return 军事结果.拒绝(军事错误.状态冲突, "部队已在返回途中。");
            var 城 = 城池(情.驻防坐标x, 情.驻防坐标y);
            if (情.阶段 == 驻防任务阶段.参战 || (情.阶段 == 驻防任务阶段.驻守 && 城 != null && 城.正在交战))
                return 军事结果.拒绝(军事错误.状态冲突, "驻防部队正在参战，请先完成战斗或在战场撤退。");
            if (情.队列将领列表 == null || 情.队列将领列表.Count == 0 || 情.队列将领列表.Any(x => 所属封地(主, x) == null || x.详细信息 == null || x.详细信息.状态 != 驻防占用状态))
                return 军事结果.拒绝(军事错误.状态冲突, "将领状态已变更，不能撤回该任务。");
            return 开始返回(情, 现在, "主动撤回") ? 军事结果.通过("驻防部队已撤回，到达封地后恢复空闲；保留现有配兵。") : 军事结果.拒绝(军事错误.无封地, "没有有效返回封地，未释放将领。");
        }
        public static void 推进(long 现在)
        {
            if (Dwsg.Network.GameNetwork.Enabled) return;
            if (现在 < 0 || 全局变量.军情列表 == null) return;
            foreach (var 情 in 获取任务())
            {
                var 主 = 玩家(情.所属玩家);
                if (情.队列将领列表 == null) 情.队列将领列表 = new List<将领信息>();
                for (int i = 情.队列将领列表.Count - 1; i >= 0; i--)
                {
                    var 将 = 情.队列将领列表[i];
                    bool 有效 = 所属封地(主, 将) != null && 将.详细信息 != null && 将.将领配兵 != null &&
                        (将.详细信息.状态 == 驻防占用状态 || (情.阶段 == 驻防任务阶段.参战 && 将.详细信息.状态 == 1));
                    if (!有效) { 移除城池关联(将); 情.队列将领列表.RemoveAt(i); }
                }
                if (情.队列将领列表.Count == 0) { 全局变量.军情列表.Remove(情); continue; }
                if (情.阶段 == 驻防任务阶段.参战)
                {
                    if (情.队列将领列表.Any(x => x.详细信息.状态 == 1)) continue;
                    情.阶段 = 驻防任务阶段.驻守;
                    情.已进入战场 = false;
                }
                if (情.阶段 == 驻防任务阶段.撤回)
                {
                    var 地 = 返回封地(情, 主);
                    if (地 == null) { 情.变更说明 = "返回封地已失效，部队等待有效封地。"; continue; }
                    if (情.坐标x != 地.所在城池.x || 情.坐标y != 地.所在城池.y) { 开始返回(情, 现在, "返回封地已迁移，重新计算路程"); continue; }
                    if (现在 < 情.到达时间) continue;
                    foreach (var 将 in 情.队列将领列表) { 移除城池关联(将); 将.详细信息.状态 = 0; }
                    全局变量.军情列表.Remove(情);
                    continue;
                }
                var 城 = 城池(情.驻防坐标x, 情.驻防坐标y);
                if (!友方城池(主, 城) || 城.国家 != 情.目标国家 || 城.城主 != 情.目标城主)
                { 开始返回(情, 现在, "城池归属已变更，自动撤回"); continue; }
                if (情.阶段 == 驻防任务阶段.驻守) continue;
                if (现在 < 情.到达时间) continue;
                double 数量 = 情.队列将领列表.Sum(x => x.将领配兵.数量);
                double 已占 = 已占兵力(城, 情);
                if (城.正在交战 || !军事本地规则.有限(数量) || !军事本地规则.有限(已占) || 数量 <= 0 || 已占 + 数量 > 城.获取驻防上限())
                { 开始返回(情, 现在, 城.正在交战 ? "到达时城池正在交战，自动撤回" : "到达时驻防容量不足，自动撤回"); continue; }
                if (城.城池玩家驻防列表 == null) 城.城池玩家驻防列表 = new List<将领信息>();
                foreach (var 将 in 情.队列将领列表) if (!城.城池玩家驻防列表.Contains(将)) 城.城池玩家驻防列表.Add(将);
                情.阶段 = 驻防任务阶段.驻守;
                情.变更说明 = "";
            }
        }
        // 两种攻城入口都取这个快照，并在真正加入守方前调用标记参战。
        public static List<将领信息> 获取守军(城池信息库类 城, long 现在)
        {
            推进(现在);
            var 结果 = new List<将领信息>();
            if (城 == null || 城.城池玩家驻防列表 == null) return 结果;
            foreach (var 情 in 获取任务())
                if (情.阶段 == 驻防任务阶段.驻守 && 情.驻防坐标x == 城.坐标x && 情.驻防坐标y == 城.坐标y && 友方城池(玩家(情.所属玩家), 城))
                    foreach (var 将 in 情.队列将领列表)
                        if (将.详细信息.状态 == 驻防占用状态 && 将.将领配兵.数量 > 0 && 城.城池玩家驻防列表.Contains(将) && !结果.Contains(将)) 结果.Add(将);
            return 结果;
        }
        public static void 标记参战(城池信息库类 城, IList<将领信息> 守军)
        {
            if (城 == null || 守军 == null) return;
            foreach (var 情 in 获取任务())
                if (情.阶段 == 驻防任务阶段.驻守 && 情.驻防坐标x == 城.坐标x && 情.驻防坐标y == 城.坐标y && 情.队列将领列表.Any(将 => 守军.Contains(将)))
                {
                    情.阶段 = 驻防任务阶段.参战; 情.已进入战场 = true;
                    foreach (var 将 in 情.队列将领列表)
                        if (守军.Contains(将)) { 将.详细信息.状态 = 1; 将.详细信息.坑位颜色 = 1; }
                }
        }
        // 引擎必须在卸兵/设置空闲之前调用。true 表示本模块接管，不能再执行旧的卸兵返回。
        public static bool 接管战后返回(将领信息 将, long 现在, bool 主动撤退 = false)
        {
            var 情 = 获取任务().FirstOrDefault(x => x.队列将领列表 != null && x.队列将领列表.Contains(将));
            if (情 == null) return false;
            if (所属封地(玩家(情.所属玩家), 将) == null || 将.详细信息 == null || 将.详细信息.状态 == 3)
            {
                移除城池关联(将); 情.队列将领列表.Remove(将);
                if (情.队列将领列表.Count == 0) 全局变量.军情列表.Remove(情);
                return true;
            }
            if (情.阶段 != 驻防任务阶段.参战) return true;
            // 沿用战斗实际剩余兵力，保留幸存士兵在将领身上，不转入封地闲兵。
            if (将.将领配兵 != null && 军事本地规则.有限(将.详细信息.剩余兵力) && 将.详细信息.剩余兵力 >= 0)
                将.将领配兵.数量 = Math.Min(将.将领配兵.数量, 将.详细信息.剩余兵力);
            if (主动撤退)
            {
                var 返 = new 驻防军情信息 {
                    所属玩家 = 情.所属玩家, 出发封地ID = 情.出发封地ID, 出发坐标x = 情.驻防坐标x, 出发坐标y = 情.驻防坐标y,
                    驻防坐标x = 情.驻防坐标x, 驻防坐标y = 情.驻防坐标y, 坐标x = 情.驻防坐标x, 坐标y = 情.驻防坐标y,
                    出发时间 = 现在, 到达时间 = 现在, 目标国家 = 情.目标国家, 目标城主 = 情.目标城主,
                    阶段 = 驻防任务阶段.驻守, 队列将领列表 = new List<将领信息> { 将 }
                };
                if (开始返回(返, 现在, "驻防部队从战场撤退"))
                {
                    将.详细信息.状态 = 驻防占用状态;
                    情.队列将领列表.Remove(将);
                    if (情.队列将领列表.Count == 0) 全局变量.军情列表.Remove(情);
                    全局变量.军情列表.Add(返);
                    推进(现在);
                }
                else 情.变更说明 = "返回封地失效，未释放驻防将领。";
                return true;
            }
            将.详细信息.状态 = 驻防占用状态;
            if (情.队列将领列表.All(x => x.详细信息.状态 == 驻防占用状态))
            {
                情.阶段 = 驻防任务阶段.驻守; 情.已进入战场 = false;
                推进(现在);
            }
            return true;
        }
        public static bool 尝试检查驻防关联(List<军情信息> 军情, out string 错误)
        {
            Dictionary<城池信息库类, List<将领信息>> 计划;
            return 尝试建立重建计划(军情, out 计划, out 错误);
        }
        // 仅供冷读档：先检查全部记录，再统一替换城池玩家驻防表；不修改 NPC 名将驻防表。
        public static bool 尝试重建城池关联(List<军情信息> 军情, out string 错误)
        {
            Dictionary<城池信息库类, List<将领信息>> 计划;
            if (!尝试建立重建计划(军情, out 计划, out 错误)) return false;
            foreach (var 项 in 计划) 项.Key.城池玩家驻防列表 = 项.Value;
            return true;
        }
        private static bool 重建失败(string 原因, out string 错误) { 错误 = 原因; return false; }
        private static bool 尝试建立重建计划(List<军情信息> 军情, out Dictionary<城池信息库类, List<将领信息>> 计划, out string 错误)
        {
            计划 = null;
            错误 = null;
            if (军情 == null || 全局变量.所有城池列表 == null || 全局变量.所有玩家数据表 == null)
                return 重建失败("军情或真实世界表缺失，不能重建驻防。", out 错误);
            var 候选 = new Dictionary<城池信息库类, List<将领信息>>();
            foreach (var 城 in 全局变量.所有城池列表)
                if (城 != null) 候选[城] = new List<将领信息>();
            var 全部将领 = new HashSet<将领信息>();
            var 驻防记录 = new List<驻防军情信息>();
            foreach (var 原情 in 军情)
            {
                if (原情 == null || 原情.队列将领列表 == null) return 重建失败("军情记录或将领引用缺失。", out 错误);
                foreach (var 将 in 原情.队列将领列表)
                    if (将 == null || !全部将领.Add(将)) return 重建失败("将领被重复编入军情，不能重建驻防。", out 错误);
                bool 驻防用途 = 原情.任务用途 == 军事任务用途.和平驻防 || 原情.任务用途 == 军事任务用途.驻防撤回;
                var 情 = 原情 as 驻防军情信息;
                if ((情 != null) != 驻防用途) return 重建失败("驻防用途与任务记录类型不符。", out 错误);
                if (情 == null) continue;
                if (情.身份 != 旧调度隔离身份 || 情.战场类型 != 1 || 情.队列将领列表.Count == 0 || 情.队列将领列表.Count > 5 ||
                    情.阶段 < 驻防任务阶段.前往 || 情.阶段 > 驻防任务阶段.撤回 ||
                    (情.任务用途 == 军事任务用途.驻防撤回) != (情.阶段 == 驻防任务阶段.撤回) ||
                    情.出发时间 < 0 || 情.到达时间 <= 情.出发时间 || 情.到达时间 > 253402300799L ||
                    !军事本地规则.有限(情.出发坐标x) || !军事本地规则.有限(情.出发坐标y) || 情.出发坐标x < 0 || 情.出发坐标y < 0 ||
                    情.驻防坐标x < 0 || 情.驻防坐标y < 0 || 情.已进入战场 ||
                    (情.阶段 != 驻防任务阶段.撤回 && (情.坐标x != 情.驻防坐标x || 情.坐标y != 情.驻防坐标y)))
                    return 重建失败("驻防阶段、用途或行军时间无效。", out 错误);
                var 主 = 玩家(情.所属玩家);
                var 地 = 主 != null && 主.封地信息表 != null ? 主.封地信息表.Where(x => x != null && x.ID == 情.出发封地ID).ToList() : new List<封地信息>();
                if (主 == null || 主.基础信息 == null || 地.Count != 1 || 地[0].所在城池 == null)
                    return 重建失败("驻防玩家或出发封地不存在，不能重建引用。", out 错误);
                foreach (var 将 in 情.队列将领列表)
                    if (!ReferenceEquals(所属封地(主, 将), 地[0]) || 将.详细信息 == null || 将.详细信息.身份 != 主.基础信息.ID ||
                        将.详细信息.状态 != 驻防占用状态 || 将.将领配兵 == null || !军事本地规则.有限(将.将领配兵.数量) || 将.将领配兵.数量 < 0)
                        return 重建失败("驻防将领引用、归属或占用状态无效，未重建任何关联。", out 错误);
                var 城 = 城池(情.驻防坐标x, 情.驻防坐标y);
                if (城 != null && 全局变量.所有城池列表.Count(x => x != null && x.坐标x == 城.坐标x && x.坐标y == 城.坐标y) != 1)
                    return 重建失败("驻防目标城池坐标重复。", out 错误);
                if (情.阶段 == 驻防任务阶段.驻守 && 城 != null && 友方城池(主, 城) && 城.国家 == 情.目标国家 && 城.城主 == 情.目标城主)
                    候选[城].AddRange(情.队列将领列表);
                驻防记录.Add(情);
            }
            // 世界 JSON 中的城池驻防对象可能是副本，只接受能对应完整驻防任务的引用。
            foreach (var 城 in 候选.Keys)
                if (城.城池玩家驻防列表 != null)
                {
                    var 已见 = new HashSet<string>();
                    foreach (var 旧 in 城.城池玩家驻防列表)
                    {
                        if (旧 == null || 旧.详细信息 == null || !已见.Add(旧.详细信息.身份 + ":" + 旧.ID) || !驻防记录.Exists(q => q.阶段 == 驻防任务阶段.驻守 &&
                            q.驻防坐标x == 城.坐标x && q.驻防坐标y == 城.坐标y && q.队列将领列表.Exists(x => x.ID == 旧.ID && x.详细信息.身份 == 旧.详细信息.身份)))
                            return 重建失败("城池驻防包含无法对应行军存档的引用，未修改任何驻防表。", out 错误);
                    }
                }
            计划 = 候选;
            return true;
        }
        public static string 状态说明(驻防军情信息 情)
        {
            return 情.阶段 == 驻防任务阶段.前往 ? "驻防途中" : 情.阶段 == 驻防任务阶段.撤回 ? "返回途中" : 情.阶段 == 驻防任务阶段.参战 ? "驻防参战" : "驻守中";
        }
    }
}
