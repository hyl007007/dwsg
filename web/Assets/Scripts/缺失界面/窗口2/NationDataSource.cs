using Dwsg.Administration;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using 玩家数据结构;

namespace 缺失界面.窗口2
{
    public enum NationNoticeKind { 公告, 宣言 }
    public enum NationOffice { 大都督, 丞相, 奋武将军, 征东将军, 都尉, 侍郎 }
    public enum NationRanking { 战功, 贡献 }
    public enum NationError { None, NoPlayer, NoNation, Forbidden, InvalidInput, MissingMember, Cooldown, InvalidBalance }

    public sealed class NationActionResult
    {
        public NationError Error;
        public string Message;
        public bool Success { get { return Error == NationError.None; } }
        public static NationActionResult Ok(string message) { return new NationActionResult { Message = message }; }
        public static NationActionResult Fail(NationError error, string message) { return new NationActionResult { Error = error, Message = message }; }
    }

    // These are detached display snapshots. Sorting or editing one never changes the world lists.
    public sealed class NationPlayerSnapshot
    {
        public int Id, Avatar, Fiefs;
        public double Generals;
        public string Name, NationCode, Title, Office, Appointment;
        public float Level;
        public double Merit, Contribution, Prestige;
    }

    public sealed class NationCitySnapshot
    {
        public int X, Y, OwnerId, Fiefs, Garrison;
        public string Name, NationCode, Scale, Owner, Talent, Totem, Notice;
        public bool Capital, InBattle;
        public double Tax, Wall;
    }

    public sealed class NationSnapshot
    {
        public int Id, KingId, Rank;
        public string Name, Code, Scale, Notice, Declaration;
        public double Welfare, Efficiency, Technology, Copper, Grain;
        public long LastElection, ElectionInterval, ElectionRemaining;
        public bool CanPublishNotice, CanPublishDeclaration, CanManage;
        public NationPlayerSnapshot King;
        public NationCitySnapshot Capital;
        public List<NationPlayerSnapshot> Members = new List<NationPlayerSnapshot>();
        public List<NationCitySnapshot> Cities = new List<NationCitySnapshot>();
        public Dictionary<NationOffice, NationPlayerSnapshot> Appointments = new Dictionary<NationOffice, NationPlayerSnapshot>();
    }

    public interface INationDataSource
    {
        string ConnectionLabel { get; }
        string OwnNationCode { get; }
        int ActorId { get; }
        int SalaryRemaining { get; }
        NationSnapshot ReadNation(string code);
        List<NationSnapshot> ReadNationRanking();
        List<NationPlayerSnapshot> ReadPlayerRanking(string code, NationRanking ranking);
        NationPlayerSnapshot ReadPlayer(int id);
        NationCitySnapshot ReadCity(string code, int x, int y);
        NationActionResult Publish(string code, NationNoticeKind kind, string text);
        NationActionResult Appoint(string code, NationOffice office, int memberId);
        NationActionResult ClaimSalary(string expectedNationCode = null);
    }

    public static class NationDataSource
    {
        // Delegates follow world replacement after loading a save. No copied database or network success is implied.
        public static INationDataSource Current = new LocalNationDataSource(
            () => 全局变量.所有国家列表, () => 全局变量.所有玩家数据表,
            () => 全局变量.所有城池列表, () => 全局变量.本机身份, TIME.getTime,
            () => 全局变量.领取倒计时, value => 全局变量.领取倒计时 = value);

        public static string Number(double value) { return double.IsNaN(value) || double.IsInfinity(value) ? "数据异常" : Math.Abs(value) >= 1e12 ? value.ToString("0.##E+0") : value.ToString("0.##"); }
        public static string Scale(int count) { return count >= 300 ? "帝国" : count >= 200 ? "王国" : count >= 100 ? "公国" : count >= 15 ? "侯国" : "小国"; }
    }

    public static class NationSalaryRules
    {
        public static 官职信息 Office(double merit, bool king)
        {
            if (king) return 官职信息.国王;
            if (merit >= 15000) return 官职信息.大都督;
            if (merit >= 8000) return 官职信息.大将军;
            if (merit >= 7000) return 官职信息.卫将军;
            if (merit >= 5000) return 官职信息.中郎将;
            if (merit >= 3000) return 官职信息.监军;
            if (merit >= 2000) return 官职信息.校尉;
            return 官职信息.平民;
        }

        public static int MeritCost(官职信息 office)
        {
            switch (office)
            {
                case 官职信息.校尉: return 2000;
                case 官职信息.监军: return 3000;
                case 官职信息.中郎将: return 5000;
                case 官职信息.卫将军: return 7000;
                case 官职信息.大将军: return 8000;
                case 官职信息.大都督: return 15000;
                default: return 0;
            }
        }

        public static int Gold(官职信息 office)
        {
            switch (office)
            {
                case 官职信息.校尉: return 100000;
                case 官职信息.监军: return 200000;
                case 官职信息.中郎将: return 300000;
                case 官职信息.卫将军: return 400000;
                case 官职信息.大将军: return 450000;
                case 官职信息.大都督: return 500000;
                case 官职信息.国王: return 50000;
                default: return 0;
            }
        }

        public static double Share(官职信息 office)
        {
            switch (office)
            {
                case 官职信息.校尉: return .5;
                case 官职信息.监军: return .55;
                case 官职信息.中郎将: return .6;
                case 官职信息.卫将军: return .7;
                case 官职信息.大将军: return .75;
                case 官职信息.大都督: return .9;
                case 官职信息.国王: return .95;
                default: return 0;
            }
        }

        public static string Description()
        {
            return "官职 / 所需战功 / 黄金 / 国库铜粮比例\n" +
                "校尉：2000 / 10万 / 50%\n监军：3000 / 20万 / 55%\n中郎将：5000 / 30万 / 60%\n" +
                "卫将军：7000 / 40万 / 70%\n大将军：8000 / 45万 / 75%\n大都督：15000 / 50万 / 90%\n" +
                "国王：不扣战功 / 5万 / 95%\n按当前战功确定官职，领取扣除所需战功。\n" +
                "每5分钟可领取一次；铜粮按国库余额取整。\n国家任命不影响俸禄档位；国王身份保留。";
        }
    }

    public sealed class LocalNationDataSource : INationDataSource
    {
        private readonly Func<IList<国家信息库类>> nations;
        private readonly Func<IList<玩家数据>> players;
        private readonly Func<IList<城池信息库类>> cities;
        private readonly Func<int> actorIndex, salaryRemaining;
        private readonly Func<long> now;
        private readonly Action<int> setSalaryRemaining;

        public LocalNationDataSource(Func<IList<国家信息库类>> nations, Func<IList<玩家数据>> players,
            Func<IList<城池信息库类>> cities, Func<int> actorIndex, Func<long> now,
            Func<int> salaryRemaining, Action<int> setSalaryRemaining)
        {
            this.nations = nations; this.players = players; this.cities = cities;
            this.actorIndex = actorIndex; this.now = now; this.salaryRemaining = salaryRemaining; this.setSalaryRemaining = setSalaryRemaining;
        }

        public string ConnectionLabel { get { return GameNetwork.Enabled ? "多人世界 · 服务器同步" : "本地世界 · 未连接多人服务器"; } }
        private 玩家数据 Actor { get { var all = players(); int index = actorIndex(); return index >= 0 && index < all.Count ? all[index] : null; } }
        public string OwnNationCode { get { return Actor != null && Actor.基础信息 != null ? Actor.基础信息.国家 ?? "" : ""; } }
        public int ActorId { get { return Actor != null && Actor.基础信息 != null ? Actor.基础信息.ID : -1; } }
        public int SalaryRemaining { get { return Math.Max(0, salaryRemaining()); } }

        private 国家信息库类 FindNation(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var nation in nations()) if (nation != null && nation.国号 == code) return nation;
            return null;
        }

        private 玩家数据 FindPlayer(int id)
        {
            if (id < 0) return null;
            foreach (var player in players()) if (player != null && player.基础信息 != null && player.基础信息.ID == id) return player;
            return null;
        }

        private bool IsMember(国家信息库类 nation, int id)
        {
            var player = FindPlayer(id);
            return player != null && player.基础信息.国家 == nation.国号 &&
                (nation.国王 == id || (nation.成员列表 != null && nation.成员列表.Contains(id)));
        }

        public NationPlayerSnapshot ReadPlayer(int id)
        {
            var player = FindPlayer(id);
            if (player == null) return null;
            var info = player.基础信息; var nation = FindNation(info.国家);
            string appointment = "无国家任命";
            if (nation != null) foreach (NationOffice office in Enum.GetValues(typeof(NationOffice)))
                if (OfficeId(nation, office) == id && IsMember(nation, id)) appointment = office.ToString();
            return new NationPlayerSnapshot { Id = id, Name = info.名字 ?? "未命名角色", NationCode = info.国家 ?? "", Avatar = info.头像,
                Level = info.等级, Title = info.称号名 ?? "无", Merit = info.战功, Contribution = info.贡献, Prestige = info.声望,
                Office = NationSalaryRules.Office(info.战功, nation != null && nation.国王 == id).ToString(), Appointment = appointment,
                Fiefs = PublicCount(id, "公开封地数", player.封地信息表 == null ? 0 : player.封地信息表.Count), Generals = PublicCount(id, "公开将领数", CountGenerals(player)) };
        }

        private static int PublicCount(int id, string field, int fallback)
        {
            var rows = GameNetwork.CurrentSnapshot?.PublicWorld["玩家列表"] as JArray;
            return GameNetwork.Enabled && rows != null && id >= 0 && id < rows.Count ? rows[id].Value<int?>(field) ?? fallback : fallback;
        }

        private static int CountGenerals(玩家数据 player)
        {
            // The old total helper logs capacity warnings for initialized kings. A read-only profile must not trigger gameplay/log side effects.
            int count = 0;
            if (player.封地信息表 != null) foreach (var fief in player.封地信息表)
                if (fief != null && fief.将领信息表 != null) count += fief.将领信息表.Count;
            return count;
        }

        public NationCitySnapshot ReadCity(string code, int x, int y)
        {
            var nation = FindNation(code);
            foreach (var city in cities())
            {
                if (city == null || city.国家 != code || city.坐标x != x || city.坐标y != y) continue;
                var owner = FindPlayer(city.城主);
                string[] scales = { "小城", "县城", "郡城", "州城", "国都" };
                string[] talents = { "产粮", "产钱", "征兵" }, totems = { "无", "神农", "蚩尤", "风后" };
                return new NationCitySnapshot { X = x, Y = y, OwnerId = city.城主, Name = city.名称, NationCode = code,
                    Scale = city.规模 >= 0 && city.规模 < scales.Length ? scales[city.规模] : "未知规模",
                    Owner = owner == null ? "无城主" : owner.基础信息.名字, InBattle = city.正在交战,
                    Capital = nation != null && nation.国都x == x && nation.国都y == y, Tax = city.税率, Wall = city.城墙,
                    Talent = (city.天赋类型 >= 0 && city.天赋类型 < talents.Length ? talents[city.天赋类型] : "未知") + NationDataSource.Number(city.天赋加成) + "%",
                    Totem = city.图腾类型 >= 0 && city.图腾类型 < totems.Length ? totems[city.图腾类型] : "未知",
                    Notice = city.公告 ?? "", Fiefs = GameNetwork.Enabled ? AdministrationClient.PublicCity(x, y)?.Value<int>("封地数量") ?? 0 : city.城池封地列表 == null ? 0 : city.城池封地列表.Count,
                    Garrison = (city.城池驻防列表 == null ? 0 : city.城池驻防列表.Count) + (city.城池玩家驻防列表 == null ? 0 : city.城池玩家驻防列表.Count) };
            }
            return null;
        }

        public NationSnapshot ReadNation(string code)
        {
            var nation = FindNation(code); if (nation == null) return null;
            var result = Summary(nation);
            result.King = IsMember(nation, nation.国王) ? ReadPlayer(nation.国王) : null;
            result.Capital = ReadCity(code, nation.国都x, nation.国都y);
            var ids = new HashSet<int>();
            if (nation.成员列表 != null) foreach (int id in nation.成员列表)
                if (ids.Add(id) && IsMember(nation, id)) result.Members.Add(ReadPlayer(id));
            result.Members.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (var city in cities()) if (city != null && city.国家 == code) result.Cities.Add(ReadCity(code, city.坐标x, city.坐标y));
            result.Scale = NationDataSource.Scale(result.Cities.Count);
            result.Cities.Sort((a, b) => { int first = b.Capital.CompareTo(a.Capital); if (first != 0) return first; first = a.X.CompareTo(b.X); return first != 0 ? first : a.Y.CompareTo(b.Y); });
            foreach (NationOffice office in Enum.GetValues(typeof(NationOffice)))
                result.Appointments[office] = IsMember(nation, OfficeId(nation, office)) ? ReadPlayer(OfficeId(nation, office)) : null;
            var ranking = ReadNationRanking(); result.Rank = ranking.FindIndex(item => item.Id == nation.ID && item.Code == code) + 1;
            return result;
        }

        private NationSnapshot Summary(国家信息库类 nation)
        {
            bool member = IsMember(nation, ActorId), king = member && nation.国王 == ActorId;
            long interval = Math.Max(0, nation.轮选时间间隔), elapsed = Math.Max(0, (GameNetwork.Enabled && GameNetwork.CurrentSnapshot != null ? GameNetwork.CurrentSnapshot.ServerUtcMs / 1000 : now()) - nation.上次轮选时间);
            return new NationSnapshot { Id = nation.ID, Code = nation.国号, Name = nation.国名, KingId = nation.国王,
                Notice = nation.公告 ?? "", Declaration = nation.宣言 ?? "", Welfare = nation.民生值, Efficiency = nation.效率,
                Technology = nation.科技等级, Copper = nation.铜钱, Grain = nation.粮食, LastElection = nation.上次轮选时间,
                ElectionInterval = interval, ElectionRemaining = Math.Max(0, interval - elapsed),
                CanManage = king, CanPublishNotice = king || (member && nation.丞相 == ActorId), CanPublishDeclaration = king };
        }

        public List<NationSnapshot> ReadNationRanking()
        {
            var result = new List<NationSnapshot>();
            foreach (var nation in nations())
            {
                if (nation == null) continue;
                var item = Summary(nation);
                foreach (var city in cities()) if (city != null && city.国家 == nation.国号)
                    item.Cities.Add(new NationCitySnapshot { X = city.坐标x, Y = city.坐标y });
                item.Scale = NationDataSource.Scale(item.Cities.Count); result.Add(item);
            }
            result.Sort((a, b) => { int count = b.Cities.Count.CompareTo(a.Cities.Count); if (count != 0) return count; count = a.Id.CompareTo(b.Id); return count != 0 ? count : string.CompareOrdinal(a.Code, b.Code); });
            for (int i = 0; i < result.Count; i++) result[i].Rank = i + 1;
            return result;
        }

        public List<NationPlayerSnapshot> ReadPlayerRanking(string code, NationRanking ranking)
        {
            var nation = ReadNation(code); var result = nation == null ? new List<NationPlayerSnapshot>() : new List<NationPlayerSnapshot>(nation.Members);
            result.Sort((a, b) => { int value = (ranking == NationRanking.战功 ? b.Merit : b.Contribution).CompareTo(ranking == NationRanking.战功 ? a.Merit : a.Contribution); return value != 0 ? value : a.Id.CompareTo(b.Id); });
            return result;
        }

        private WorldState CommandWorld()
        {
            var mapping = new JObject();
            for (int i = 0; i < players().Count; i++) mapping["local-" + i] = i;
            var rows = new JArray();
            foreach (var player in players()) rows.Add(new JObject { ["基础信息"] = JObject.FromObject(player.基础信息) });
            return new WorldState { Data = new JObject { ["玩家列表"] = rows, ["国家列表"] = JArray.FromObject(nations()) }, EntityMappings = new JObject { ["players"] = mapping } };
        }
        private NationActionResult ApplyNationCommand(WorldState world, string code, GameResult result)
        {
            if (result.Code != GameCodes.Ok) return NationActionResult.Fail(result.Code == GameCodes.Forbidden ? NationError.Forbidden : NationError.InvalidInput, result.Message);
            var original = FindNation(code); var changed = Dwsg.Shared.Economy.TerritoryRules.Nation(world, code);
            original.公告 = changed.Value<string>("公告"); original.宣言 = changed.Value<string>("宣言");
            foreach (NationOffice office in Enum.GetValues(typeof(NationOffice))) SetOffice(original, office, changed.Value<int>(office.ToString()));
            return NationActionResult.Ok(result.Message);
        }
        public NationActionResult Publish(string code, NationNoticeKind kind, string text)
        {
            if (GameNetwork.Enabled) return NationActionResult.Fail(NationError.Forbidden, "联机编辑须等待服务器确认。");
            var world = CommandWorld(); var nation = FindNation(code);
            return ApplyNationCommand(world, code, AdministrationRules.Publish(world, "local-" + actorIndex(), code, kind.ToString(), text,
                nation == null ? "" : kind == NationNoticeKind.公告 ? nation.公告 ?? "" : nation.宣言 ?? ""));
        }

        private static int OfficeId(国家信息库类 nation, NationOffice office)
        {
            switch (office)
            {
                case NationOffice.大都督: return nation.大都督;
                case NationOffice.丞相: return nation.丞相;
                case NationOffice.奋武将军: return nation.奋武将军;
                case NationOffice.征东将军: return nation.征东将军;
                case NationOffice.都尉: return nation.都尉;
                default: return nation.侍郎;
            }
        }

        private static void SetOffice(国家信息库类 nation, NationOffice office, int id)
        {
            switch (office)
            {
                case NationOffice.大都督: nation.大都督 = id; break;
                case NationOffice.丞相: nation.丞相 = id; break;
                case NationOffice.奋武将军: nation.奋武将军 = id; break;
                case NationOffice.征东将军: nation.征东将军 = id; break;
                case NationOffice.都尉: nation.都尉 = id; break;
                case NationOffice.侍郎: nation.侍郎 = id; break;
            }
        }

        public NationActionResult Appoint(string code, NationOffice office, int memberId)
        {
            if (GameNetwork.Enabled) return NationActionResult.Fail(NationError.Forbidden, "联机任免须等待服务器确认。");
            if (!Enum.IsDefined(typeof(NationOffice), office) || memberId < -1) return NationActionResult.Fail(NationError.InvalidInput, "任命参数无效。");
            var world = CommandWorld(); var nation = FindNation(code);
            return ApplyNationCommand(world, code, AdministrationRules.AppointNation(world, "local-" + actorIndex(), code, office.ToString(),
                memberId < 0 ? null : "local-" + memberId, nation == null ? null : AdministrationRules.StablePlayer(world, OfficeId(nation, office))));
        }

        private static bool Valid(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0; }

        public NationActionResult ClaimSalary(string expectedNationCode = null)
        {
            var actor = Actor; if (actor == null || actor.基础信息 == null || actor.财产信息 == null) return NationActionResult.Fail(NationError.NoPlayer, "当前角色数据未就绪。");
            if (expectedNationCode != null && expectedNationCode != OwnNationCode) return NationActionResult.Fail(NationError.Forbidden, "当前角色已换国，请返回所属国家重新查看俸禄。");
            var nation = FindNation(OwnNationCode); if (nation == null || !IsMember(nation, ActorId)) return NationActionResult.Fail(NationError.NoNation, "请先加入有效国家再领取俸禄。");
            if (SalaryRemaining > 0) return NationActionResult.Fail(NationError.Cooldown, "俸禄还需等待" + TIME.ToTimeFormat(SalaryRemaining) + "。");
            var office = NationSalaryRules.Office(actor.基础信息.战功, nation.国王 == ActorId);
            if (office == 官职信息.平民) return NationActionResult.Fail(NationError.Forbidden, "平民没有俸禄；达到2000战功后成为校尉。");
            if (!Valid(nation.铜钱) || !Valid(nation.粮食) || !Valid(actor.基础信息.战功) ||
                !Valid(actor.财产信息.黄金) || !Valid(actor.财产信息.铜钱) || !Valid(actor.财产信息.粮食))
                return NationActionResult.Fail(NationError.InvalidBalance, "资源数据异常，未领取或扣除资源。");
            double copper = Math.Floor(nation.铜钱 * NationSalaryRules.Share(office)), grain = Math.Floor(nation.粮食 * NationSalaryRules.Share(office));
            int gold = NationSalaryRules.Gold(office), cost = NationSalaryRules.MeritCost(office);
            if (!Valid(actor.财产信息.黄金 + gold) || !Valid(actor.财产信息.铜钱 + copper) || !Valid(actor.财产信息.粮食 + grain))
                return NationActionResult.Fail(NationError.InvalidBalance, "领取后资源超出范围，未扣除任何资源。");
            nation.铜钱 -= copper; nation.粮食 -= grain;
            actor.财产信息.黄金 += gold; actor.财产信息.铜钱 += copper; actor.财产信息.粮食 += grain;
            actor.基础信息.战功 -= cost; actor.基础信息.官职 = NationSalaryRules.Office(actor.基础信息.战功, nation.国王 == ActorId);
            setSalaryRemaining(300);
            return NationActionResult.Ok("俸禄已领取：黄金" + gold + "，铜钱" + NationDataSource.Number(copper) + "，粮食" + NationDataSource.Number(grain) + "；扣除战功" + cost + "。");
        }
    }
}
