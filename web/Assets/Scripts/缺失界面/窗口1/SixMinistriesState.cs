using System;
using System.Collections.Generic;
using System.Linq;

namespace Dwsg.Window1
{
    public enum MinistryJobKind { Recruit, Plant, Relief, MilitaryAttack, MilitaryDefense, Study, Persuasion }
    public sealed class MinistryOfficer
    {
        public long Id;
        public string Name, Owner, Source;
        public double Experience;
        public int Persuasion;
        public MinistryOfficer Copy() { return (MinistryOfficer)MemberwiseClone(); }
    }
    public sealed class MinistryJob
    {
        public long Id, StartedAt, ReadyAt, OfficerId, TargetId;
        public string Owner, TargetOwner;
        public MinistryJobKind Kind;
        public int Slot = -1, Candidate = -1, ExpectedProgress;
        public MinistryJob Copy() { return (MinistryJob)MemberwiseClone(); }
    }
    public sealed class MinistryBuff
    {
        public string Owner;
        public MinistryJobKind Kind;
        public long StartedAt, ExpiresAt;
        public double Bonus;
        public MinistryBuff Copy() { return (MinistryBuff)MemberwiseClone(); }
    }
    public sealed class SixMinistriesState
    {
        public int Version = 1;
        public string WorldId;
        public long NextId = 1, LastTimestamp;
        public bool NpcsSeeded;
        public List<MinistryOfficer> Officers = new List<MinistryOfficer>();
        public List<MinistryJob> Jobs = new List<MinistryJob>();
        public List<MinistryBuff> Buffs = new List<MinistryBuff>();
        public SixMinistriesState Copy()
        {
            return new SixMinistriesState { Version = Version, WorldId = WorldId, NextId = NextId, LastTimestamp = LastTimestamp, NpcsSeeded = NpcsSeeded,
                Officers = Officers == null ? null : Officers.Select(o => o == null ? null : o.Copy()).ToList(),
                Jobs = Jobs == null ? null : Jobs.Select(j => j == null ? null : j.Copy()).ToList(),
                Buffs = Buffs == null ? null : Buffs.Select(b => b == null ? null : b.Copy()).ToList() };
        }
        public static SixMinistriesState Empty(string worldId, SixMinistriesConfig rules)
        {
            var state = new SixMinistriesState { WorldId = worldId, NpcsSeeded = true };
            foreach (var npc in rules.Npcs)
                state.Officers.Add(new MinistryOfficer { Id = state.NextId++, Name = npc.Name, Owner = npc.Owner, Source = npc.Key });
            return state;
        }
        public bool Validate(string expectedWorld, SixMinistriesConfig rules, long now, ISet<string> playerKeys, out string error)
        {
            error = "六部存档版本、世界、对象或计时无效";
            if (rules == null || !rules.IsValid() || Version != 1 || WorldId != expectedWorld || !SixMinistriesConfig.Text(WorldId, 160) ||
                !NpcsSeeded || now < 1 || now > rules.MaxTimestamp || LastTimestamp < 0 || LastTimestamp > now ||
                NextId < 1 || NextId >= rules.MaxIdentity || playerKeys == null || Officers == null || Jobs == null || Buffs == null ||
                Officers.Count > rules.MaxOfficers || Jobs.Count > rules.MaxJobs || Buffs.Count > rules.MaxBuffs) return false;
            var ids = new HashSet<long>(); var npcSources = new HashSet<string>();
            foreach (var officer in Officers)
            {
                if (officer == null || officer.Id <= 0 || officer.Id >= NextId || !ids.Add(officer.Id) ||
                    !SixMinistriesConfig.Text(officer.Name, 24) || !SixMinistriesConfig.Number(officer.Experience) ||
                    officer.Experience > rules.OfficerExperienceLimit || officer.Experience != Math.Floor(officer.Experience) ||
                    officer.Persuasion < 0 || officer.Persuasion > rules.PersuasionGoal || officer.Persuasion % rules.PersuasionStep != 0 ||
                    (!playerKeys.Contains(officer.Owner ?? "") && !rules.NpcOwner(officer.Owner))) return false;
                var npc = rules.Npcs.FirstOrDefault(n => n.Key == officer.Source);
                if (npc != null)
                {
                    if (officer.Name != npc.Name || !npcSources.Add(npc.Key) ||
                        (rules.NpcOwner(officer.Owner) && (officer.Owner != npc.Owner || officer.Persuasion == rules.PersuasionGoal)) ||
                        (playerKeys.Contains(officer.Owner) && officer.Persuasion != rules.PersuasionGoal)) return false;
                }
                else
                {
                    int candidate;
                    if (officer.Source == null || !officer.Source.StartsWith("candidate:", StringComparison.Ordinal) ||
                        !int.TryParse(officer.Source.Substring(10), out candidate) || candidate < 0 || candidate >= rules.Candidates.Length ||
                        officer.Name != rules.Candidates[candidate] || !playerKeys.Contains(officer.Owner) || officer.Persuasion != 0) return false;
                }
            }
            if (Officers.Where(o => playerKeys.Contains(o.Owner)).GroupBy(o => o.Owner).Any(g => g.Count() > rules.RosterLimit)) return false;
            var slots = new HashSet<string>(); var busy = new HashSet<long>(); var targets = new HashSet<long>();
            foreach (var job in Jobs)
            {
                int duration = job == null ? 0 : rules.Duration(job.Kind);
                if (job == null || job.Id <= 0 || job.Id >= NextId || !ids.Add(job.Id) || !playerKeys.Contains(job.Owner ?? "") || duration <= 0 ||
                    job.StartedAt < 1 || job.StartedAt > LastTimestamp || job.ReadyAt > rules.MaxTimestamp || job.ReadyAt - job.StartedAt != duration) return false;
                if (job.Kind == MinistryJobKind.Recruit)
                { if (job.Candidate < 0 || job.Candidate >= rules.Candidates.Length || job.Slot != -1) return false; }
                else if (job.Kind == MinistryJobKind.Plant)
                { if (job.Slot < 0 || job.Slot >= rules.PlotCount || job.Candidate != -1) return false; }
                else if (job.Slot != -1 || job.Candidate != -1) return false;
                bool civil = job.Kind == MinistryJobKind.Relief || job.Kind == MinistryJobKind.Study;
                if (civil ? !Officers.Any(o => o.Id == job.OfficerId) || !busy.Add(job.OfficerId) : job.OfficerId != 0) return false;
                if (job.Kind == MinistryJobKind.Persuasion)
                {
                    if (!Officers.Any(o => o.Id == job.TargetId && rules.Npcs.Any(n => n.Key == o.Source)) || !targets.Add(job.TargetId) ||
                        !rules.NpcOwner(job.TargetOwner) || job.ExpectedProgress < 0 || job.ExpectedProgress >= rules.PersuasionGoal ||
                        job.ExpectedProgress % rules.PersuasionStep != 0) return false;
                }
                else if (job.TargetId != 0 || job.TargetOwner != null || job.ExpectedProgress != 0) return false;
                string key = job.Owner + "/" + job.Kind + "/" + (job.Kind == MinistryJobKind.Recruit ? job.Candidate : job.Kind == MinistryJobKind.Plant ? job.Slot : civil ? job.OfficerId : 0);
                if (!slots.Add(key)) return false;
            }
            if (busy.Overlaps(targets)) return false;
            foreach (string owner in playerKeys)
            {
                int reserved = Jobs.Count(j => j.Owner == owner && (j.Kind == MinistryJobKind.Recruit ||
                    j.Kind == MinistryJobKind.Persuasion && j.ExpectedProgress + rules.PersuasionStep >= rules.PersuasionGoal &&
                    Officers.Any(o => o.Id == j.TargetId && o.Owner == j.TargetOwner && o.Persuasion == j.ExpectedProgress)));
                if (Officers.Count(o => o.Owner == owner) + reserved > rules.RosterLimit) return false;
            }
            var buffKeys = new HashSet<string>();
            foreach (var buff in Buffs)
            {
                if (buff == null || !playerKeys.Contains(buff.Owner ?? "") ||
                    (buff.Kind != MinistryJobKind.MilitaryAttack && buff.Kind != MinistryJobKind.MilitaryDefense) ||
                    !buffKeys.Add(buff.Owner + "/" + buff.Kind) || buff.StartedAt < 1 || buff.StartedAt > LastTimestamp ||
                    buff.ExpiresAt > rules.MaxTimestamp || buff.ExpiresAt - buff.StartedAt != rules.BuffSeconds || buff.Bonus != rules.MilitaryBonus) return false;
            }
            error = null; return true;
        }
    }
}
