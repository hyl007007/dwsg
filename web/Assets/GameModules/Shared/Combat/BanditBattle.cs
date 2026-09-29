using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Combat
{
    public sealed class CombatRandom
    {
        public ulong State { get; private set; }
        public CombatRandom(ulong state)
        {
            if (state == 0) throw new ArgumentOutOfRangeException(nameof(state));
            State = state;
        }
        public int Next(int minimum, int maximum)
        {
            if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
            ulong range = (ulong)((long)maximum - minimum);
            ulong limit = ulong.MaxValue - ulong.MaxValue % range;
            ulong value;
            do
            {
                ulong state = State;
                state ^= state >> 12; state ^= state << 25; state ^= state >> 27;
                State = state;
                value = unchecked(state * 2685821657736338717UL);
            } while (value >= limit);
            return (int)(minimum + (long)(value % range));
        }
    }

    public sealed class CombatUnit
    {
        public string GeneralId;
        public JObject General;
        public JObject Troop;
        public int Side;
        public int Slot = -1;
        public string LastTargetId;
        public double OriginalQuantity;
        public double Wounded;
        public bool Retired;
        public float Progress;
        public bool WaitForTarget;
        public int WaitFrames;
        public double Remaining => General["详细信息"].Value<double>("剩余兵力");
        public int TroopClass => Troop.Value<int>("兵种");
        public int Career => General["将领属性"]["初始属性"].Value<int>("职业");
    }

    public sealed class CombatProfile
    {
        public double AttackTechnology, CavalryTechnology, DefenseTechnology, InfantryTechnology, ArcherTechnology;
        public double LifeTechnology, MachineTechnology, BlockTechnology, PierceTechnology, DodgeTechnology;
        public double NationAttack, NationDefense, AttackState, DefenseState, SpeedState, ExperienceState;
        public double AttackTitle, DefenseTitle, LifeTitle, SpeedTitle;
        public bool Emperor;
        public double AttackBonus(int career) => CombatModifiers.AttackTechnology(AttackTechnology, CavalryTechnology, ArcherTechnology, career);
        public double DefenseBonus(int career) => CombatModifiers.DefenseTechnology(DefenseTechnology, CavalryTechnology, InfantryTechnology, career);
    }

    public sealed class CombatHit
    {
        public long Frame;
        public string AttackerId, DefenderId;
        public double Damage;
        public bool Pierce, Block, Dodge;
    }

    public sealed class BanditBattle
    {
        public string BattleId, ArmyId, PlayerId, NpcPlayerId;
        public int X, Y;
        public long ArrivalUtcMs, NextTickUtcMs, StartedUtcMs, Frame;
        public string Phase = "marching";
        public bool SettlementApplied;
        public long SettledUtcMs;
        public ulong RandomState;
        public float AttackFormationX = -24.25f, DefenseFormationX = 24.25f;
        public int AttackEntered, DefenseEntered;
        public List<CombatUnit> Attackers = new List<CombatUnit>();
        public List<CombatUnit> Defenders = new List<CombatUnit>();
        public List<CombatHit> LastHits = new List<CombatHit>();
        public 战斗奖励 Reward;
        public bool IsTerminal => Phase == "won" || Phase == "lost" || Phase == "withdrawn";
    }

    public static class BanditBattleRules
    {
        // 原 Update 协程的进度/移动步长以固定60Hz推进；墙钟到达时间和播放倍速分开。
        public const int FramesPerTick = 6;
        public const long TickMilliseconds = 100;
        private static readonly int[] Front = { 2, 1, 3, 0, 4, 7, 6, 8, 5, 9, 12, 11, 13, 10, 14 };
        private static readonly int[] Middle = { 7, 6, 8, 5, 9, 12, 11, 13, 10, 14, 2, 1, 3, 0, 4 };
        private static readonly int[] Rear = { 12, 11, 13, 10, 14, 7, 6, 8, 5, 9, 2, 1, 3, 0, 4 };

        public static CombatUnit CreateUnit(string generalId, JObject general, JObject troop, int side)
        {
            var copy = (JObject)general.DeepClone();
            double quantity = copy["将领配兵"].Value<double>("数量");
            if (double.IsNaN(quantity) || double.IsInfinity(quantity) || quantity < 0 || quantity != Math.Floor(quantity))
                throw new InvalidOperationException("原配兵数量无效");
            if (troop.Value<double>("生命值") <= 0 || troop.Value<float>("攻击速度") <= 0 || troop.Value<float>("移动速度") <= 0)
                throw new InvalidOperationException("原兵种配置无效");
            copy["详细信息"]["剩余兵力"] = quantity;
            copy["详细信息"]["状态"] = 1.0;
            copy["详细信息"]["坑位颜色"] = (double)side;
            return new CombatUnit { GeneralId = generalId, General = copy, Troop = (JObject)troop.DeepClone(), Side = side, OriginalQuantity = quantity };
        }

        public static void Advance(BanditBattle battle, Func<CombatUnit, long, CombatProfile> profile,
            Action<CombatUnit, long> recalculateOwner, Action<JObject, double> grantExperience)
        {
            if (battle.Phase != "fighting") throw new InvalidOperationException("战场尚未到达或已结束");
            var random = new CombatRandom(battle.RandomState);
            battle.LastHits.Clear();
            for (int step = 0; step < FramesPerTick && !battle.IsTerminal; step++)
            {
                battle.Frame++;
                long utc = battle.StartedUtcMs + battle.Frame * 1000 / 60;
                Enter(battle.Attackers, ref battle.AttackFormationX, ref battle.AttackEntered, true);
                Enter(battle.Defenders, ref battle.DefenseFormationX, ref battle.DefenseEntered, false);
                foreach (CombatUnit actor in battle.Attackers.Concat(battle.Defenders))
                {
                    if (actor.Retired || actor.Slot < 0 || actor.Remaining <= 0) continue;
                    CombatProfile attackerProfile = profile(actor, utc);
                    actor.Progress = Math.Min(3f, actor.Progress + 战斗规则.攻击进度步长(actor.Troop.Value<float>("攻击速度"),
                        (float)CombatModifiers.SpeedTechnology(attackerProfile.MachineTechnology, actor.TroopClass) / 100f,
                        (float)attackerProfile.SpeedTitle / 100f, (float)attackerProfile.SpeedState / 100f, 1f / 60f));
                    if (actor.Progress < 3f) continue;
                    CombatUnit target = FindTarget(battle, actor, profile, utc);
                    if (target == null) { actor.WaitForTarget = true; continue; }
                    if (actor.WaitForTarget)
                    {
                        actor.WaitFrames++;
                        if (actor.WaitFrames > 15) { actor.WaitFrames = 0; actor.WaitForTarget = false; }
                        continue;
                    }
                    Hit(battle, actor, target, profile, utc, random, recalculateOwner, grantExperience);
                    actor.Progress = 0f;
                }
                if (battle.Defenders.Sum(unit => unit.Remaining) <= 0) battle.Phase = "won";
                else if (battle.Attackers.Where(unit => !unit.Retired).Sum(unit => unit.Remaining) <= 0)
                    battle.Phase = battle.Attackers.All(unit => unit.Retired) ? "withdrawn" : "lost";
            }
            battle.RandomState = random.State;
        }

        private static void Enter(List<CombatUnit> units, ref float position, ref int entered, bool attacking)
        {
            float destination = attacking ? 24.25f : -24.25f;
            float speed = units.Min(member => member.Troop.Value<float>("移动速度")) * 0.5f;
            position = attacking ? Math.Min(destination, position + speed / 60f) : Math.Max(destination, position - speed / 60f);
            if (position != destination || entered >= units.Count) return;
            CombatUnit unit = units[entered];
            if (unit.Retired) { entered++; return; }
            int[] priority = unit.TroopClass == 3 ? Middle : unit.TroopClass == 4 ? Rear : Front;
            // 一次原出征至多五将，前两列都不会占满；保留原对应兵种的坑位顺序。
            unit.Slot = priority.First(slot => units.All(other => other.Slot != slot));
            entered++;
        }

        public static double Attack(CombatUnit actor, CombatUnit target, CombatProfile profile)
        {
            double career = target == null ? 0 : 战斗规则.职业攻击加成(actor.Career, actor.TroopClass, target.TroopClass) / 100.0;
            double counter = target == null ? 0 : 战斗规则.兵种攻击加成(actor.Career, actor.TroopClass, target.TroopClass) / 100.0;
            return 战斗规则.最终攻击力(actor.General["将领属性"]["最终属性"].Value<double>("攻击"), actor.Troop.Value<double>("攻击力"),
                profile.AttackBonus(actor.Career) / 100.0, profile.NationAttack / 100.0, career, counter,
                profile.AttackState / 100.0, profile.AttackTitle / 100.0, 0);
        }

        public static double Defense(CombatUnit actor, CombatUnit target, CombatProfile actorProfile, CombatProfile targetProfile)
        {
            // 原方法在攻将上读取守将；职业参数、国家和兵种参数方向保持原实际调用。
            double career = 战斗规则.职业防御加成(target.Career, actor.TroopClass, target.TroopClass) / 100.0;
            double counter = 战斗规则.兵种防御加成(target.Career, actor.TroopClass, target.TroopClass) / 100.0;
            return 战斗规则.最终防御力(target.General["将领属性"]["最终属性"].Value<double>("防御"), target.Troop.Value<double>("防御力"),
                targetProfile.DefenseBonus(actor.Career) / 100.0, actorProfile.NationDefense / 100.0, career, counter,
                targetProfile.DefenseState / 100.0, targetProfile.DefenseTitle / 100.0, 0);
        }

        public static double Life(CombatUnit target, CombatProfile profile)
        {
            return 战斗规则.守方血量(target.General["将领属性"]["最终属性"].Value<double>("生命值"), target.Troop.Value<double>("生命值"),
                CombatModifiers.LifeTechnology(profile.LifeTechnology) / 100.0, 战斗规则.职业生命加成(target.Career, target.TroopClass) / 100.0, profile.LifeTitle / 100.0);
        }

        private sealed class TargetScore { public CombatUnit Unit; public double Damage, Attack, Loss; }
        private static CombatUnit FindTarget(BanditBattle battle, CombatUnit actor, Func<CombatUnit, long, CombatProfile> profile, long utc)
        {
            if (actor.Troop.Value<int>("ID") == 403) return null;
            var opponents = actor.Side == 0 ? battle.Defenders : battle.Attackers;
            var scores = new List<TargetScore>();
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    var target = opponents.FirstOrDefault(unit => !unit.Retired && unit.Slot == column * 5 + row && unit.Remaining > 0);
                    if (target == null) continue;
                    actor.LastTargetId = target.GeneralId;
                    var previousTarget = battle.Attackers.Concat(battle.Defenders).FirstOrDefault(unit => unit.GeneralId == target.LastTargetId);
                    var attackerProfile = profile(actor, utc);
                    var defenderProfile = profile(target, utc);
                    scores.Add(new TargetScore { Unit = target,
                        Damage = 战斗规则.最终伤害(Attack(actor, target, attackerProfile), Defense(actor, target, attackerProfile, defenderProfile), actor.Remaining, Life(target, defenderProfile)),
                        Attack = Attack(target, previousTarget, defenderProfile), Loss = target.OriginalQuantity - target.Remaining });
                    break;
                }
            }
            if (scores.Count == 0) return null;
            int mode = actor.General["详细信息"].Value<int>("攻击模式");
            if (mode == 0) scores.Sort((left, right) => left.Damage < right.Damage ? 1 : -1);
            else if (mode == 1) scores.Sort((left, right) => left.Attack < right.Attack ? 1 : -1);
            else if (mode == 2) scores.Sort((left, right) => left.Loss < right.Loss ? 1 : -1);
            actor.LastTargetId = scores[0].Unit.GeneralId;
            return scores[0].Unit;
        }

        private static void Hit(BanditBattle battle, CombatUnit actor, CombatUnit target, Func<CombatUnit, long, CombatProfile> profile,
            long utc, CombatRandom random, Action<CombatUnit, long> recalculateOwner, Action<JObject, double> grantExperience)
        {
            var attackerProfile = profile(actor, utc);
            var defenderProfile = profile(target, utc);
            double attack = Attack(actor, target, attackerProfile);
            double defense = Defense(actor, target, attackerProfile, defenderProfile);
            // 保留原短路：不符合兵种条件时不消费概率随机数，穿透→格挡→闪避顺序不变。
            bool pierce = actor.TroopClass == 3 && 战斗规则.是否穿透(actor.TroopClass, attackerProfile.PierceTechnology, attackerProfile.Emperor, random.Next(1, 101));
            if (pierce) defense = 0;
            double damage = 战斗规则.最终伤害(attack, defense, actor.Remaining, Life(target, defenderProfile));
            recalculateOwner(actor, utc);
            bool block = target.TroopClass == 2 && (actor.TroopClass == 1 || actor.TroopClass == 2) && 战斗规则.是否格挡(target.TroopClass, actor.TroopClass, defenderProfile.BlockTechnology, defenderProfile.Emperor, random.Next(1, 101));
            bool dodge = target.TroopClass == 1 && (actor.TroopClass == 3 || actor.TroopClass == 4) && 战斗规则.是否闪避(target.TroopClass, actor.TroopClass, defenderProfile.DodgeTechnology, defenderProfile.Emperor, random.Next(1, 101));
            if (block || dodge) damage = 0;
            damage = Math.Min(damage, target.Remaining);
            target.General["详细信息"]["剩余兵力"] = target.Remaining - damage;
            target.General["将领配兵"]["数量"] = target.Remaining;
            if (target.Side == 0) target.Wounded += 战斗规则.伤兵数量(damage, 0);
            grantExperience(actor.General, 战斗规则.将领经验(damage, target.Troop.Value<double>("攻击力"), defenderProfile.ExperienceState));
            battle.LastHits.Add(new CombatHit { Frame = battle.Frame, AttackerId = actor.GeneralId, DefenderId = target.GeneralId,
                Damage = damage, Pierce = pierce, Block = block, Dodge = dodge });
        }

        public static 战斗奖励 CalculateRewards(BanditBattle battle, double resourceBonus)
        {
            return 战斗规则.计算奖励(battle.Defenders.Select(unit => new 击杀兵种信息 {
                兵种ID = unit.Troop.Value<int>("ID"), 兵种攻击 = unit.Troop.Value<double>("攻击力"), 数量 = unit.OriginalQuantity - unit.Remaining }), resourceBonus);
        }
    }
}
