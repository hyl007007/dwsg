using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using 玩家数据结构;

namespace Dwsg.Window1
{
    public sealed class ExistingWorldAdapter : ILocalWorld
    {
        internal static 玩家数据 CurrentPlayer
        {
            get
            {
                var players = 全局变量.所有玩家数据表;
                int index = 全局变量.本机身份;
                return players == null || index < 0 || index >= players.Count ? null : players[index];
            }
        }
        public string PlayerKey
        { get { var p = CurrentPlayer; return p == null || p.基础信息 == null ? null : "player:" + p.基础信息.ID.ToString(CultureInfo.InvariantCulture); } }

        public bool TryRead(out WorldProgress progress, out string error)
        {
            progress = null; error = "当前世界或君主尚未就绪";
            var p = CurrentPlayer;
            if (p == null || p.基础信息 == null || p.封地信息表 == null || p.科技信息 == null) return false;
            var result = new WorldProgress { LordLevel = p.基础信息.等级, Merit = p.基础信息.战功 };
            var seen = new HashSet<将领信息>();
            foreach (var land in p.封地信息表)
            {
                if (land == null) continue;
                if (land.建筑信息表 != null)
                    foreach (var b in land.建筑信息表)
                    {
                        if (b == null || b.类型 < 0 || b.等级 <= 0) continue;
                        result.Buildings++; result.BuildingLevels += b.等级;
                        if (b.类型 == 0) result.HallLevel = Math.Max(result.HallLevel, b.等级);
                    }
                if (land.将领信息表 != null)
                    foreach (var g in land.将领信息表)
                    {
                        if (g == null || !seen.Add(g)) continue;
                        result.Generals++;
                        if (g.将领属性 != null && g.将领属性.成长点数 != null)
                        {
                            double level = g.将领属性.成长点数.等级;
                            if (!WorldProgress.Number(level)) { error = "将领等级数值异常"; return false; }
                            result.GeneralLevels += level; result.GeneralLevel = Math.Max(result.GeneralLevel, level);
                        }
                        if (g.将领配兵 != null)
                        { if (!WorldProgress.Number(g.将领配兵.数量)) { error = "将领配兵数值异常"; return false; } result.Troops += g.将领配兵.数量; }
                    }
                if (land.闲兵信息表 != null) foreach (var t in land.闲兵信息表) if (t != null)
                { if (!WorldProgress.Number(t.数量)) { error = "闲兵数值异常"; return false; } result.Troops += t.数量; }
            }
            var tech = p.科技信息;
            double[] levels = { tech.工程设计, tech.征召技巧, tech.种植技术, tech.行军技巧, tech.市场贸易, tech.建筑学,
                tech.铸铁技术, tech.甲胄制造, tech.药草研究, tech.阵法技巧, tech.抛射技巧, tech.驾驭技巧, tech.战车设计,
                tech.统帅能力, tech.信仰, tech.仓储, tech.安置, tech.格斗, tech.精准, tech.驯马, tech.精工 };
            if (levels.Any(l => !WorldProgress.Number(l))) { error = "科技等级数值异常"; return false; }
            result.Technology = levels.Sum();
            if (!result.IsValid()) { error = "世界进度数值异常，暂不可领取"; return false; }
            progress = result; error = null; return true;
        }

        public bool TryGrant(Reward reward, out string error)
        {
            error = "奖励数据无效";
            if (reward == null || !reward.IsValid()) return false;
            var p = CurrentPlayer;
            if (p == null || p.基础信息 == null || p.财产信息 == null || p.背包道具列表 == null || p.背包装备列表 == null)
            { error = "当前君主数据未就绪"; return false; }
            var money = p.财产信息;
            double[] current = { money.铜钱, money.粮食, money.白银, money.黄金 };
            double[] add = { reward.Copper, reward.Grain, reward.Silver, reward.Gold };
            string[] labels = { "铜钱", "粮食", "白银", "黄金" };
            for (int i = 0; i < current.Length; i++)
            {
                if (!WorldProgress.Number(current[i]) || current[i] + add[i] > Reward.ResourceLimit)
                { error = labels[i] + "领取后将超过20亿上限，请先使用部分资源"; return false; }
            }

            var bag = p.背包道具列表;
            var lists = new[] { bag.宝物道具列表, bag.加速道具列表, bag.生产道具列表, bag.宝箱道具列表, bag.强化道具列表, bag.任务道具列表 };
            var equipment = p.背包装备列表;
            if (lists.Any(l => l == null) || equipment.武器装备列表 == null || equipment.头盔装备列表 == null || equipment.铠甲装备列表 == null || equipment.坐骑装备列表 == null ||
                !WorldProgress.Number(p.基础信息.背包容量上限) || p.基础信息.背包容量上限 != Math.Floor(p.基础信息.背包容量上限))
            { error = "背包结构或容量异常，本次未发放"; return false; }
            var changes = new Dictionary<List<道具信息>, List<道具信息>>();
            foreach (var group in reward.Items.GroupBy(i => i.Name, StringComparer.Ordinal))
            {
                if (全局道具库.获取指定名字的道具(group.Key) == null)
                { error = "找不到道具：" + group.Key + "，本次未发放"; return false; }
                var original = bag.获取道具分类列表(group.Key);
                if (original == null) { error = "道具分类不可用：" + group.Key; return false; }
                List<道具信息> candidate;
                if (!changes.TryGetValue(original, out candidate))
                {
                    if (original.Any(i => i == null || string.IsNullOrEmpty(i.名字) || !WorldProgress.Number(i.数量) || i.数量 < 1 || i.数量 > 999 || i.数量 != Math.Floor(i.数量)))
                    { error = "现有道具堆叠异常，本次未发放"; return false; }
                    candidate = original.Select(i => new 道具信息(i.名字, i.数量) { ID = i.ID }).ToList();
                    changes.Add(original, candidate);
                }
                int remaining = group.Sum(i => i.Count);
                foreach (var stack in candidate.Where(i => i.名字 == group.Key))
                {
                    int put = Math.Min(remaining, 999 - (int)stack.数量);
                    stack.数量 += put; remaining -= put;
                }
                while (remaining > 0) { int put = Math.Min(remaining, 999); candidate.Add(new 道具信息(group.Key, put)); remaining -= put; }
            }
            int oldSlots = lists.Sum(l => l.Count) + equipment.武器装备列表.Count + equipment.头盔装备列表.Count + equipment.铠甲装备列表.Count + equipment.坐骑装备列表.Count;
            int newSlots = oldSlots + changes.Sum(c => c.Value.Count - c.Key.Count);
            if (newSlots > p.基础信息.背包容量上限 && changes.Count > 0)
            { error = "背包空间不足，请先整理背包（需要 " + Math.Max(0, newSlots - oldSlots) + " 个新格）"; return false; }
            // 所有校验和分堆已经完成。同步入账，不调用可能触发旧随机玩法的道具使用逻辑。
            money.铜钱 += reward.Copper; money.粮食 += reward.Grain; money.白银 += reward.Silver; money.黄金 += reward.Gold;
            foreach (var change in changes) { change.Key.Clear(); change.Key.AddRange(change.Value); }
            error = null; return true;
        }
    }

    // 主窗口将此 DTO/JSON 与同一世界的玩家和资源一起写入同一存档事务。
    // 加载世界：先恢复全局玩家列表，再调用 导入JSON(worldId, json, out error)。
    // 新世界/旧档无扩展：在玩家初始化完成后调用 重置(worldId)。不另写世界数据库或旁路文件。
    public static class Window1Module
    {
        private static Window1WorldState state;
        private static object listAnchor, firstPlayerAnchor;
        private static JournalService service;
        private static readonly ExistingWorldAdapter adapter = new ExistingWorldAdapter();
        public static event Action Changed;
        public static bool HasPersistentWorldId { get; private set; }
        public static string CurrentWorldId { get { return Dwsg.Network.GameNetwork.Enabled ? Dwsg.Progress.ProgressClient.WorldId : Service == null ? null : state.WorldId; } }
        private static bool Ready { get { return ExistingWorldAdapter.CurrentPlayer != null && adapter.PlayerKey != null; } }
        private static object FirstPlayer { get { return 全局变量.所有玩家数据表 != null && 全局变量.所有玩家数据表.Count > 0 ? 全局变量.所有玩家数据表[0] : null; } }

        public static JournalService Service
        {
            get
            {
                if (Dwsg.Network.GameNetwork.Enabled) return Dwsg.Progress.ProgressClient.Journal;
                if (!Ready) return null;
                if (state == null || !ReferenceEquals(listAnchor, 全局变量.所有玩家数据表) || !ReferenceEquals(firstPlayerAnchor, FirstPlayer))
                    Reset("local-session-" + Guid.NewGuid().ToString("N"), false);
                return service;
            }
        }
        private static void Reset(string worldId, bool persistent)
        {
            if (!LocalMail.ValidText(worldId, 160)) throw new ArgumentException("世界标识无效", "worldId");
            state = new Window1WorldState { WorldId = worldId };
            Attach(persistent);
        }
        private static void Attach(bool persistent)
        {
            listAnchor = 全局变量.所有玩家数据表; firstPlayerAnchor = FirstPlayer;
            service = new JournalService(state, adapter, () => DateTime.Now);
            HasPersistentWorldId = persistent;
        }
        public static void 重置(string worldId)
        { Reset(worldId, true); if (Ready) { string ignored; service.Refresh(out ignored); } Signal(); }
        // 英文稳定别名便于统一槽位扩展适配；中文接口保持兼容。
        public static void Reset(string worldId) { 重置(worldId); }
        public static string ExportJson() { return 导出JSON(); }
        public static bool ImportJson(string worldId, string json, out string error) { return 导入JSON(worldId, json, out error); }
        public static string 导出JSON()
        {
            if (Dwsg.Network.GameNetwork.Enabled) return null;
            var s = Service;
            if (s == null) return null;
            string ignored; s.Refresh(out ignored);
            return JsonConvert.SerializeObject(state);
        }
        public static Window1WorldState 导出状态()
        { var json = 导出JSON(); return json == null ? null : JsonConvert.DeserializeObject<Window1WorldState>(json); }
        public static bool 导入JSON(string worldId, string json, out string error)
        {
            error = "任务邮件存档无效";
            if (!Ready || !LocalMail.ValidText(worldId, 160)) return false;
            if (string.IsNullOrEmpty(json)) { 重置(worldId); error = null; return true; }
            if (json.Length > 16000000) { error = "任务邮件存档过大"; return false; }
            try
            {
                var loaded = JsonConvert.DeserializeObject<Window1WorldState>(json, new JsonSerializerSettings { MaxDepth = 32, TypeNameHandling = TypeNameHandling.None });
                if (loaded == null || !loaded.Validate(worldId, out error)) return false;
                state = loaded; Attach(true); Signal(); error = null; return true;
            }
            catch (JsonException) { return false; }
        }
        public static void 通知世界数据变更()
        { var s = Service; if (s != null) { string ignored; s.Refresh(out ignored); } Signal(); }
        public static bool 接收本地邮件(LocalMail mail, out string error)
        {
            var s = Service; if (s == null) { error = "当前世界尚未就绪"; return false; }
            bool result = s.ReceiveLocalMail(mail, out error); if (result) Signal(); return result;
        }
        public static bool 发布本地告示(LocalNotice notice, out string error)
        {
            error = "本地告示无效";
            if (Dwsg.Network.GameNetwork.Enabled) { error = "联机告示由服务器发布"; return false; }
            if (Service == null || notice == null || !notice.IsValid() || notice.Id.StartsWith("builtin.", StringComparison.Ordinal)) return false;
            if (state.Notices.Any(n => n.Id == notice.Id)) { error = "告示标识已存在"; return false; }
            if (state.Notices.Count >= 100) { error = "告示列表已满"; return false; }
            state.Notices.Add(notice.Copy()); error = null; Signal(); return true;
        }
        public static List<LocalNotice> 告示列表()
        {
            if (Service == null) return new List<LocalNotice>();
            var p = ExistingWorldAdapter.CurrentPlayer;
            int landIndex = 全局变量.第几个封地;
            var land = p.封地信息表 != null && landIndex >= 0 && landIndex < p.封地信息表.Count ? p.封地信息表[landIndex] : null;
            WorldProgress progress; string error;
            adapter.TryRead(out progress, out error);
            var entries = new List<LocalNotice>
            {
                new LocalNotice { Id = "builtin.offline", Title = "通信状态", Pinned = true,
                    Body = Dwsg.Network.GameNetwork.Enabled ? "当前连接游戏服务器。任务、邮件与六部政务由服务器保存，操作结果以服务器回执为准。" : "当前处于离线状态，跨设备来信暂不可用。" },
                new LocalNotice { Id = "builtin.progress", Title = "封地政务 · " + (land == null ? "未选择封地" : land.封地名字),
                    Body = progress == null ? error : "君主：" + p.基础信息.名字 + "\n所属国家：" + p.基础信息.国家 + "\n当前封地：" + (land == null ? "未选择" : land.封地名字) +
                    "\n\n麾下建筑 " + progress.Buildings.ToString("0") + " 座，建筑等级合计 " + progress.BuildingLevels.ToString("0") +
                    "\n将领 " + progress.Generals.ToString("0") + " 名，最高等级 " + progress.GeneralLevel.ToString("0") +
                    "\n可用军队 " + progress.Troops.ToString("0") + "，累计战功 " + progress.Merit.ToString("0") },
                new LocalNotice { Id = "builtin.rules", Title = "任务与奖励须知", Body = "完成成长任务与成就后，达成记录会保留，每项奖励可领取一次。\n\n日常任务每日刷新。从当天首次载入游戏时的进度开始计算，之后建筑、科技、将领总等级和战功的净增加计入今日任务。已经达成的进度会保留，调整日期不会让已领取的奖励重复发放。\n\n领取奖励后，各项资源余额不能超过20亿；道具每堆最多999个。空间或余额上限不足时，整份奖励暂不发放。先整理背包或使用资源，再来领取，领取机会会保留。\n\n" + (Dwsg.Network.GameNetwork.Enabled ? "任务与邮件由服务器持续保存。重新登录同一角色后恢复，领取结果以服务器确认记录为准。" : HasPersistentWorldId ? "保存游戏时，任务、告示与邮件进度一并保存。读档后恢复到保存时的状态。" : "任务、告示与邮件记录仅在本次游戏中保留，离开游戏后不会保留。") }
            };
            if (!Dwsg.Network.GameNetwork.Enabled && state != null) entries.AddRange(state.Notices.Select(n => n.Copy()));
            return entries.OrderByDescending(n => n.Pinned).ThenByDescending(n => n.PublishedUtcTicks).ToList();
        }
        internal static void Signal() { if (Changed != null) Changed(); }
    }
}
