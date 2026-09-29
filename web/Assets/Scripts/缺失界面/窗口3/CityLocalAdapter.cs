using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using 玩家数据结构;

namespace Dwsg.Window3
{
    public sealed class CityLocalAdapter : ICityAdapter
    {
        public const long TaxInterval = 86400;
        public Func<long> UtcNow = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private CityModuleDto state = new CityModuleDto();
        private object firstCity, firstPlayer;
        public static readonly CityLocalAdapter Local = new CityLocalAdapter();
        internal bool HasPendingWork { get { EnsureWorld(); return state.Repairs.Count > 0; } }

        public static 城池信息库类 City(int x, int y)
        {
            return 全局变量.所有城池列表.FirstOrDefault(c => c != null && c.坐标x == x && c.坐标y == y);
        }
        public static 玩家数据 Player(int index)
        {
            return index >= 0 && index < 全局变量.所有玩家数据表.Count ? 全局变量.所有玩家数据表[index] : null;
        }
        private static 玩家数据 PlayerId(int id) { return 全局变量.所有玩家数据表.FirstOrDefault(p => p != null && p.基础信息.ID == id); }
        public static 玩家数据 Me { get { return Player(全局变量.本机身份); } }
        public static bool Friendly(城池信息库类 c)
        {
            var p = Me;
            return c != null && p != null && (c.城主 == 全局变量.本机身份 ||
                (!string.IsNullOrEmpty(c.国家) && c.国家 == p.基础信息.国家 && 全局方法类.获取指定名字的国家(c.国家) != null));
        }
        public static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0; }

        private void EnsureWorld()
        {
            object city = 全局变量.所有城池列表.FirstOrDefault();
            object player = 全局变量.所有玩家数据表.FirstOrDefault();
            bool sameCities = firstCity != null && 全局变量.所有城池列表.Any(c => ReferenceEquals(c, firstCity));
            bool samePlayers = firstPlayer != null && 全局变量.所有玩家数据表.Any(p => ReferenceEquals(p, firstPlayer));
            if (!(sameCities || ReferenceEquals(city, firstCity)) || !(samePlayers || ReferenceEquals(player, firstPlayer)))
            {
                state = new CityModuleDto(); firstCity = city; firstPlayer = player;
            }
        }

        // Call after restoring the world. Reset for a new world or an old slot without this DTO.
        public void Reset() { state = new CityModuleDto(); firstCity = 全局变量.所有城池列表.FirstOrDefault(); firstPlayer = 全局变量.所有玩家数据表.FirstOrDefault(); }
        public void Reset(string worldKey) { Reset(); state.WorldKey = worldKey; }
        // Export BEFORE capturing world resources: this settles due repairs and refunds only once.
        public string ExportJson() { EnsureWorld(); Settle(); return JsonConvert.SerializeObject(state); }
        public string ExportJson(string worldKey)
        {
            EnsureWorld();
            if (string.IsNullOrEmpty(worldKey) || worldKey.Length > 128) throw new ArgumentException("世界标识必须为1–128字符。", "worldKey");
            if (state.WorldKey != null && state.WorldKey != worldKey) throw new InvalidOperationException("切换世界后须先Reset或ImportJson。");
            state.WorldKey = worldKey; return ExportJson();
        }
        public CityResult ImportJson(string json)
        {
            return ImportValidated(json, null);
        }
        public CityResult ImportJson(string json, string worldKey)
        {
            if (string.IsNullOrEmpty(worldKey) || worldKey.Length > 128) return CityResult.Fail("世界标识无效。");
            return ImportValidated(json, worldKey);
        }
        private CityResult ImportValidated(string json, string worldKey)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 1000000) return CityResult.Fail("城池附加存档为空或过大。");
            try
            {
                var dto = JsonConvert.DeserializeObject<CityModuleDto>(json, new JsonSerializerSettings { MaxDepth = 16, TypeNameHandling = TypeNameHandling.None });
                if (dto == null || dto.Version != 1 || dto.Bookmarks == null || dto.Repairs == null || dto.Taxes == null || dto.Candidates == null || dto.RecentRequests == null)
                    return CityResult.Fail("不支持的城池附加存档。");
                if (dto.WorldKey != null && dto.WorldKey.Length > 128 || worldKey != null && dto.WorldKey != worldKey)
                    return CityResult.Fail("城池附加存档不属于当前世界。");
                if (dto.Bookmarks.Count > 1024 || dto.Repairs.Count > 512 || dto.Taxes.Count > 4096 || dto.Candidates.Count > 4096 || dto.RecentRequests.Count > 512)
                    return CityResult.Fail("城池附加存档超出容量。");
                if (dto.Bookmarks.Any(b => b == null || PlayerId(b.PlayerId) == null || City(b.X, b.Y) == null) ||
                    dto.Bookmarks.GroupBy(b => b.PlayerId + ":" + b.X + ":" + b.Y).Any(g => g.Count() > 1) ||
                    dto.Repairs.Any(r => r == null || City(r.X, r.Y) == null || PlayerId(r.PlayerId) == null ||
                        !Enum.IsDefined(typeof(CityRepairKind), r.Kind) || !Finite(r.Amount) || r.Amount <= 0 || r.Amount > 10000 ||
                        r.Copper != Math.Ceiling(r.Amount / 10) || r.Food != Math.Ceiling(r.Amount / 20) ||
                        r.StartedUtc < 0 || r.EndsUtc != r.StartedUtc + 30 || r.StartedUtc > UtcNow() + 30) ||
                    dto.Repairs.GroupBy(r => r.X + ":" + r.Y).Any(g => g.Count() > 1) ||
                    dto.Taxes.Any(t => t == null || City(t.X, t.Y) == null || !Enum.IsDefined(typeof(CityTaxKind), t.Kind) || t.LastUtc < 0 || t.LastUtc > UtcNow() + 30) ||
                    dto.Taxes.GroupBy(t => t.X + ":" + t.Y + ":" + t.Kind).Any(g => g.Count() > 1) ||
                    dto.Candidates.Any(c => c == null || PlayerId(c.PlayerId) == null || City(c.X, c.Y) == null || string.IsNullOrEmpty(c.Nation)) ||
                    dto.Candidates.GroupBy(c => c.PlayerId + ":" + c.X + ":" + c.Y).Any(g => g.Count() > 1) ||
                    dto.RecentRequests.Any(r => string.IsNullOrEmpty(r) || r.Length > 80)) return CityResult.Fail("城池附加存档校验失败，未改动当前状态。");
                Reset(); state = dto; CityRepairClock.Watch(this); return CityResult.Ok("城池附加状态已恢复。");
            }
            catch (JsonException) { return CityResult.Fail("城池附加存档格式错误。"); }
        }

        private string CheckRequest(string request)
        {
            if (string.IsNullOrEmpty(request) || request.Length > 80) return "操作凭据无效。";
            return state.RecentRequests.Contains(request) ? "该操作已经处理，请刷新后再操作。" : null;
        }
        private void Remember(string request)
        {
            state.RecentRequests.Add(request);
            if (state.RecentRequests.Count > 512) state.RecentRequests.RemoveAt(0);
        }
        public CityRepairOrder Pending(int x, int y) { EnsureWorld(); Settle(); return state.Repairs.FirstOrDefault(r => r.X == x && r.Y == y); }
        public void Settle()
        {
            EnsureWorld(); long now = UtcNow();
            for (int i = state.Repairs.Count - 1; i >= 0; i--)
            {
                var r = state.Repairs[i]; var c = City(r.X, r.Y); var p = PlayerId(r.PlayerId);
                bool invalid = c == null || p == null || c.城主 != r.Owner || c.国家 != r.Nation || c.正在交战 ||
                    !Finite(r.Kind == CityRepairKind.Wall ? c.城墙 : c.道路) ||
                    !(c.城主 >= 0 && Player(c.城主) == p || !string.IsNullOrEmpty(c.国家) && c.国家 == p.基础信息.国家);
                if (!invalid && now < r.EndsUtc) continue;
                if (invalid)
                {
                    // Refund the original payer, never the current viewer. The world owns this balance.
                    if (p != null && Finite(p.财产信息.铜钱 + r.Copper) && Finite(p.财产信息.粮食 + r.Food))
                    { p.财产信息.铜钱 += r.Copper; p.财产信息.粮食 += r.Food; }
                }
                else if (r.Kind == CityRepairKind.Wall) c.城墙 = Math.Min(c.获取城墙上限(), c.城墙 + r.Amount);
                else c.道路 = Math.Min(c.获取道路上限(), c.道路 + r.Amount);
                state.Repairs.RemoveAt(i);
            }
        }
        public CityRepairQuote Quote(int x, int y, CityRepairKind kind)
        {
            EnsureWorld(); Settle(); var c = City(x, y);
            var q = new CityRepairQuote { X = x, Y = y, Kind = kind };
            if (c == null || Me == null) { q.Error = "城池或本地角色不存在。"; return q; }
            q.Owner = c.城主; q.Nation = c.国家; q.PlayerId = Me.基础信息.ID;
            if (!Enum.IsDefined(typeof(CityRepairKind), kind)) q.Error = "未知修筑类型。";
            else if (!Friendly(c)) q.Error = "只可修筑自己或本国城池。";
            else if (c.正在交战) q.Error = "交战中不可修筑。";
            else if (state.Repairs.Any(r => r.X == x && r.Y == y)) q.Error = "本城已有修筑任务，请等待完成。";
            q.Before = kind == CityRepairKind.Wall ? c.城墙 : c.道路;
            double limit = kind == CityRepairKind.Wall ? c.获取城墙上限() : c.获取道路上限();
            if (!Finite(q.Before) || !Finite(limit) || q.Before > limit) q.Error = "设施数据异常，请恢复有效存档。";
            else if (q.Before == limit) q.Error = "已达上限，无需修筑。";
            q.Amount = Math.Max(0, Math.Min(10000, limit - q.Before)); q.Copper = Math.Ceiling(q.Amount / 10); q.Food = Math.Ceiling(q.Amount / 20);
            if (q.Allowed && (!Finite(Me.财产信息.铜钱) || !Finite(Me.财产信息.粮食) || Me.财产信息.铜钱 < q.Copper || Me.财产信息.粮食 < q.Food)) q.Error = "铜钱或粮食不足，未扣费。";
            return q;
        }
        public CityResult Repair(CityRepairQuote quote, string request)
        {
            EnsureWorld(); string error = CheckRequest(request);
            if (error != null) return CityResult.Fail(error);
            if (quote == null) return CityResult.Fail("请先选择修筑项目。");
            var fresh = Quote(quote.X, quote.Y, quote.Kind);
            if (!fresh.Allowed) return CityResult.Fail(fresh.Error);
            if (fresh.PlayerId != quote.PlayerId || fresh.Owner != quote.Owner || fresh.Nation != quote.Nation || fresh.Before != quote.Before || fresh.Amount != quote.Amount || fresh.Copper != quote.Copper || fresh.Food != quote.Food)
                return CityResult.Fail("城池状态或费用已变化，请重新确认。");
            long now = UtcNow();
            var order = new CityRepairOrder { X = fresh.X, Y = fresh.Y, Owner = fresh.Owner, Nation = fresh.Nation,
                PlayerId = Me.基础信息.ID, Kind = fresh.Kind, Amount = fresh.Amount, Copper = fresh.Copper, Food = fresh.Food, StartedUtc = now, EndsUtc = now + fresh.Seconds };
            Me.财产信息.铜钱 -= fresh.Copper; Me.财产信息.粮食 -= fresh.Food;
            state.Repairs.Add(order); Remember(request);
            CityRepairClock.Watch(this);
            return CityResult.Ok("本地修筑已开始，30秒后完成；归属变化或交战会取消并退费。");
        }
        public string TaxPermission(int x, int y, CityTaxKind kind)
        {
            EnsureWorld(); var c = City(x, y); var p = Me;
            if (c == null || p == null) return "城池或本地角色不存在。";
            if (c.正在交战) return "交战中不可征收。";
            if (kind == CityTaxKind.Lord) { if (c.城主 != 全局变量.本机身份) return "只有本城城主可征收。"; }
            else if (kind == CityTaxKind.Nation)
            {
                var n = 全局方法类.获取指定名字的国家(c.国家);
                if (n == null || string.IsNullOrEmpty(c.国家)) return "无主城没有国家征收。";
                if (c.国家 != p.基础信息.国家 || n.国王 != p.基础信息.ID) return "只有所属国家国王可征收，所得进入国库。";
            }
            else return "未知征收类型。";
            long left = TaxRemaining(x, y, kind);
            if (left > 0) return "本城该项已征收，" + Math.Ceiling(left / 3600.0) + "小时内不可重复。";
            return null;
        }
        public long TaxRemaining(int x, int y, CityTaxKind kind)
        {
            EnsureWorld(); var t = state.Taxes.FirstOrDefault(r => r.X == x && r.Y == y && r.Kind == kind);
            return t == null ? 0 : Math.Max(0, t.LastUtc + TaxInterval - UtcNow());
        }
        public CityResult Collect(int x, int y, CityTaxKind kind, string request)
        {
            EnsureWorld(); string error = CheckRequest(request) ?? TaxPermission(x, y, kind);
            if (error != null) return CityResult.Fail(error);
            var c = City(x, y); var n = 全局方法类.获取指定名字的国家(c.国家);
            double copper = kind == CityTaxKind.Lord ? c.城主征收_铜 : c.国家征收_铜;
            double food = kind == CityTaxKind.Lord ? c.城主征收_粮 : c.国家征收_粮;
            if (!Finite(copper) || !Finite(food) || copper + food <= 0) return CityResult.Fail("本城没有可征收额度。");
            double oldCopper = kind == CityTaxKind.Lord ? Me.财产信息.铜钱 : n.铜钱;
            double oldFood = kind == CityTaxKind.Lord ? Me.财产信息.粮食 : n.粮食;
            if (!Finite(oldCopper) || !Finite(oldFood) || !Finite(oldCopper + copper) || !Finite(oldFood + food)) return CityResult.Fail("资源数值异常，未进行征收。");
            if (kind == CityTaxKind.Lord) { Me.财产信息.铜钱 += copper; Me.财产信息.粮食 += food; }
            else { n.铜钱 += copper; n.粮食 += food; }
            var tax = state.Taxes.FirstOrDefault(t => t.X == x && t.Y == y && t.Kind == kind);
            if (tax == null) { tax = new CityTaxRecord { X = x, Y = y, Kind = kind }; state.Taxes.Add(tax); }
            tax.LastUtc = UtcNow(); Remember(request);
            return CityResult.Ok("已在本地" + (kind == CityTaxKind.Lord ? "个人财产" : "国家国库") + "记账：铜" + copper.ToString("N0") + " / 粮" + food.ToString("N0") + "。");
        }
        private static bool HasFief(城池信息库类 c, 玩家数据 p)
        {
            return p != null && p.封地信息表.Any(f => f.所在城池 != null && f.所在城池.x == c.坐标x && f.所在城池.y == c.坐标y);
        }
        public string CandidatePermission(int x, int y)
        {
            EnsureWorld(); var c = City(x, y); var p = Me;
            if (c == null || p == null) return "城池或本地角色不存在。";
            if (c.规模 == 4) return "都城由国王治理，不参与城主竞选。";
            if (c.正在交战) return "交战中不可竞选。";
            if (string.IsNullOrEmpty(c.国家) || 全局方法类.获取指定名字的国家(c.国家) == null) return "无主城没有城主竞选，请通过攻占取得归属。";
            if (c.国家 != p.基础信息.国家 || !HasFief(c, p)) return "竞选须为本国成员，并在本城拥有封地。";
            if (c.城主 == 全局变量.本机身份) return "你已是本城城主。";
            if (state.Candidates.Any(a => a.X == x && a.Y == y && a.PlayerId == p.基础信息.ID && a.Nation == c.国家)) return "已登记本地候选，等待国王任命。";
            return null;
        }
        public CityResult Apply(int x, int y, string request)
        {
            EnsureWorld(); var error = CheckRequest(request) ?? CandidatePermission(x, y);
            if (error != null) return CityResult.Fail(error);
            state.Candidates.RemoveAll(a => a.X == x && a.Y == y && a.PlayerId == Me.基础信息.ID);
            state.Candidates.Add(new CityCandidate { X = x, Y = y, PlayerId = Me.基础信息.ID, Nation = City(x, y).国家 }); Remember(request);
            return CityResult.Ok("已登记本地候选，不收费用；由本国国王在政务页任命。未连接投票服务器。");
        }
        public List<CityCandidate> Candidates(int x, int y) { EnsureWorld(); return state.Candidates.Where(c => c.X == x && c.Y == y).ToList(); }
        public CityResult Appoint(int x, int y, int playerId, string request)
        {
            EnsureWorld(); var c = City(x, y); var p = PlayerId(playerId);
            var n = c == null ? null : 全局方法类.获取指定名字的国家(c.国家);
            string error = CheckRequest(request);
            if (error != null) return CityResult.Fail(error);
            if (c == null || Me == null || n == null || n.国王 != Me.基础信息.ID || Me.基础信息.国家 != c.国家) return CityResult.Fail("只有所属国家国王可任命。");
            if (c.正在交战 || c.规模 == 4) return CityResult.Fail("交战城池或都城不可任命。");
            if (p == null || p.基础信息.国家 != c.国家 || !HasFief(c, p) || !state.Candidates.Any(a => a.X == x && a.Y == y && a.PlayerId == playerId && a.Nation == c.国家)) return CityResult.Fail("候选人已失去资格，请重新登记。");
            c.城主 = 全局变量.所有玩家数据表.IndexOf(p); state.Candidates.RemoveAll(a => a.X == x && a.Y == y); Remember(request);
            return CityResult.Ok("本地城主已任命为" + p.基础信息.名字 + "。");
        }
        public List<CityBookmark> Bookmarks()
        {
            EnsureWorld(); return Me == null ? new List<CityBookmark>() : state.Bookmarks.Where(b => b.PlayerId == Me.基础信息.ID).ToList();
        }
        public bool IsBookmarked(int x, int y) { return Bookmarks().Any(b => b.X == x && b.Y == y); }
        public CityResult Bookmark(int x, int y, bool add)
        {
            EnsureWorld(); if (Me == null || add && City(x, y) == null) return CityResult.Fail("无法收藏不存在的城池。");
            bool exists = IsBookmarked(x, y);
            if (add && !exists)
            {
                if (Bookmarks().Count >= 128) return CityResult.Fail("每位君主最多收藏128座城池。");
                state.Bookmarks.Add(new CityBookmark { PlayerId = Me.基础信息.ID, X = x, Y = y });
            }
            else if (!add) state.Bookmarks.RemoveAll(b => b.PlayerId == Me.基础信息.ID && b.X == x && b.Y == y);
            return CityResult.Ok(add ? exists ? "本城已在收藏册中。" : "城池已加入本地收藏册。" : "已取消收藏。");
        }
        public CityScoutReport Scout(int x, int y)
        {
            Settle(); var c = City(x, y); if (c == null) return null;
            var r = new CityScoutReport { City = c.名称, X = x, Y = y, Nation = c.获取国家名字(), Lord = c.获取城主名字(),
                Wall = c.城墙, WallLimit = c.获取城墙上限(), Road = c.道路, RoadLimit = c.获取道路上限(), Fiefs = c.城池封地列表.Count,
                Notice = c.公告, Fighting = c.正在交战, CanReadDefenders = Friendly(c), ObservedUtc = UtcNow() };
            if (r.CanReadDefenders) r.Defenders.AddRange(DefenderRows(c));
            return r;
        }
        public static List<string> DefenderRows(城池信息库类 c)
        {
            var rows = new List<string>();
            foreach (var idx in c.城池驻防列表)
            {
                if (idx == null) continue;
                var p = Player(idx.第几个玩家); var found = p == null ? null : p.获取指定ID标识的将领索引(idx.将领ID标识);
                var general = found != null && found.第几个封地 >= 0 && found.第几个将领 >= 0 ? p.封地信息表[found.第几个封地].将领信息表[found.第几个将领] : null;
                if (general != null) rows.Add(GeneralRow(general) + "  · " + p.基础信息.名字);
                else rows.Add("驻防记录 #" + idx.将领ID标识 + " · 将领已离开或记录失效");
            }
            foreach (var general in c.城池玩家驻防列表) if (general != null) rows.Add(GeneralRow(general) + "  · 本地参战记录");
            return rows;
        }
        private static string GeneralRow(将领信息 g)
        {
            string name = g.将领属性 == null || g.将领属性.初始属性 == null ? "将领 #" + g.ID : g.将领属性.初始属性.名字;
            return name + "  兵力 " + (g.将领配兵 == null ? "未配兵" : g.将领配兵.数量.ToString("N0"));
        }
    }
}
