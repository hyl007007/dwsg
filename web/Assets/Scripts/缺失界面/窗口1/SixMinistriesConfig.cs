using System;
using System.Linq;

namespace Dwsg.Window1
{
    // 本地可替换配置，不是官方手游数值。公开资料仅确认六部方向，
    // 本工程以铜粮、短计时和已有将领修炼规则形成可保存的离线闭环。
    // 六部专有费用/时长/奖励均集中于此；将领修炼复用窗口4原规则。
    public sealed class SixMinistriesConfig
    {
        public int RosterLimit = 12, PlotCount = 2, RecruitSeconds = 60, PlantSeconds = 60;
        public int ReliefSeconds = 120, MilitarySeconds = 60, StudySeconds = 120, PersuasionSeconds = 180;
        public int BuffSeconds = 1800, PersuasionStep = 25, PersuasionGoal = 100, ExperiencePerLevel = 100;
        public double RecruitCopper = 1000, PlantCopper = 500, PlantGrain = 1000;
        public double ReliefCopper = 500, ReliefGrain = 500, ReliefRewardCopper = 1500, ReliefExperience = 50;
        public double MilitaryCopper = 2000, MilitaryGrain = 1000, MilitaryBonus = .05;
        public double StudyCopper = 1000, StudyExperience = 50, PersuasionCopper = 3000, OfficerExperienceLimit = 9900;
        public long MaxIdentity = 1000000000000, MaxTimestamp = 4102444800;
        public int MaxOfficers = 6147, MaxJobs = 8192, MaxBuffs = 1024;
        public string[] Candidates = { "沈文", "顾农", "周谋" };
        // NPC为本地独立官署实体，不借用玩家账号，也不模拟真人/联网招募。
        // 仅新世界或缺少此可选存档字段时生成一次；转移/解雇后不会重刷。
        public MinistryNpcDefinition[] Npcs = {
            new MinistryNpcDefinition { Key = "npc.prefecture.wen", Owner = "npc:prefecture", Name = "地方文书" },
            new MinistryNpcDefinition { Key = "npc.prefecture.nong", Owner = "npc:prefecture", Name = "地方农官" },
            new MinistryNpcDefinition { Key = "npc.prefecture.zheng", Owner = "npc:prefecture", Name = "地方政官" }
        };
        public SixMinistriesConfig Copy()
        {
            var copy = (SixMinistriesConfig)MemberwiseClone();
            copy.Candidates = Candidates == null ? null : (string[])Candidates.Clone();
            copy.Npcs = Npcs == null ? null : Npcs.Select(n => n == null ? null : n.Copy()).ToArray();
            return copy;
        }
        public bool IsValid()
        {
            double[] resources = { RecruitCopper, PlantCopper, PlantGrain, ReliefCopper, ReliefGrain,
                ReliefRewardCopper, ReliefExperience, MilitaryCopper, MilitaryGrain, StudyCopper,
                StudyExperience, PersuasionCopper, OfficerExperienceLimit };
            int[] times = { RecruitSeconds, PlantSeconds, ReliefSeconds, MilitarySeconds, StudySeconds, PersuasionSeconds, BuffSeconds };
            return RosterLimit > 0 && RosterLimit <= 100 && PlotCount > 0 && PlotCount <= 10 &&
                times.All(t => t > 0 && t <= 86400) && resources.All(n => Number(n) && n > 0 && n == Math.Floor(n)) &&
                Number(MilitaryBonus) && MilitaryBonus > 0 && MilitaryBonus <= 1 && PersuasionStep > 0 &&
                PersuasionGoal > 0 && PersuasionGoal % PersuasionStep == 0 && ExperiencePerLevel > 0 &&
                MaxIdentity > 1000 && MaxTimestamp > 1000000000 && MaxOfficers > RosterLimit && MaxJobs > 0 && MaxBuffs > 0 &&
                Candidates != null && Candidates.Length > 0 && Candidates.Length <= 12 && Candidates.All(n => Text(n, 24)) &&
                Candidates.Distinct(StringComparer.Ordinal).Count() == Candidates.Length && Npcs != null && Npcs.Length <= 100 &&
                Npcs.All(n => n != null && Text(n.Key, 80) && Text(n.Name, 24) && Text(n.Owner, 80) && n.Owner.StartsWith("npc:", StringComparison.Ordinal)) &&
                Npcs.Select(n => n.Key).Distinct(StringComparer.Ordinal).Count() == Npcs.Length;
        }
        public int Duration(MinistryJobKind kind)
        {
            switch (kind)
            {
                case MinistryJobKind.Recruit: return RecruitSeconds;
                case MinistryJobKind.Plant: return PlantSeconds;
                case MinistryJobKind.Relief: return ReliefSeconds;
                case MinistryJobKind.MilitaryAttack: case MinistryJobKind.MilitaryDefense: return MilitarySeconds;
                case MinistryJobKind.Study: return StudySeconds;
                case MinistryJobKind.Persuasion: return PersuasionSeconds;
                default: return 0;
            }
        }
        public bool NpcOwner(string owner) { return Npcs.Any(n => n.Owner == owner); }
        public static bool Number(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= Reward.ResourceLimit; }
        public static bool Text(string value, int max) { return !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl); }
    }
    public sealed class MinistryNpcDefinition
    {
        public string Key, Owner, Name;
        public MinistryNpcDefinition Copy() { return (MinistryNpcDefinition)MemberwiseClone(); }
    }
}
