using System;
using System.Collections.Generic;

namespace Dwsg.Shared.Combat
{
    public static class 战斗规则
    {
        public static double 兵种攻击加成(double 职业, double 兵种, double 对方兵种)
        {
            double 加成 = 0.0;
            if (兵种 == 1.0 && 对方兵种 == 2.0)
            {
                加成 += 50.0;
            }
            if (兵种 == 3.0 && 对方兵种 == 1.0)
            {
                加成 += 50.0;
            }
            return 加成;
        }

        public static double 兵种防御加成(double 职业, double 兵种, double 对方兵种)
        {
            return 兵种 == 2.0 && 对方兵种 == 3.0 ? 50.0 : 0.0;
        }

        public static double 职业攻击加成(double 职业, double 兵种, double 对方兵种)
        {
            double 加成 = 0.0;
            if (兵种 == 1.0 && 职业 == 1.0)
            {
                加成 += 15.0;
            }
            if (兵种 == 3.0 && 职业 == 4.0)
            {
                加成 += 30.0;
            }
            return 加成;
        }

        public static double 职业防御加成(double 职业, double 兵种, double 对方兵种)
        {
            double 加成 = 0.0;
            if (兵种 == 1.0 && 职业 == 1.0)
            {
                加成 += 15.0;
            }
            if (兵种 == 2.0)
            {
                if (职业 == 2.0)
                {
                    加成 += 20.0;
                }
                if (对方兵种 == 3.0)
                {
                    加成 += 50.0;
                }
            }
            return 加成;
        }

        public static double 职业生命加成(double 职业, double 兵种)
        {
            if (职业 == 3.0)
            {
                return 35.0;
            }
            return 兵种 == 2.0 && 职业 == 2.0 ? 15.0 : 0.0;
        }

        // 比例由原科技、国家、称号和状态入口提供，保持原乘法分组及国家科技上限处理。
        public static double 最终攻击力(double 将领攻击, double 兵种攻击, double 科技比例, double 国家科技比例, double 职业比例, double 克制比例, double 状态比例, double 称号比例, double 城墙比例)
        {
            if (国家科技比例 > 2.0)
            {
                国家科技比例 = 0.0;
            }
            return (将领攻击 * 0.05 + 兵种攻击) * (1.0 + 科技比例 + 国家科技比例 + 职业比例) * (1.0 + 克制比例 + 城墙比例) * (1.0 + 状态比例 + 称号比例);
        }

        public static double 最终防御力(double 将领防御, double 兵种防御, double 科技比例, double 国家科技比例, double 职业比例, double 克制比例, double 状态比例, double 称号比例, double 城墙比例)
        {
            if (国家科技比例 > 2.0)
            {
                国家科技比例 = 0.0;
            }
            return (将领防御 * 0.05 + 兵种防御) * (1.0 + 科技比例 + 国家科技比例 + 职业比例) * (1.0 + 克制比例 + 城墙比例) * (1.0 + 状态比例 + 称号比例);
        }

        public static double 守方血量(double 将领生命, double 兵种生命, double 科技比例, double 职业比例, double 称号比例)
        {
            return (兵种生命 + 将领生命) * (1.0 + 科技比例 + 职业比例) * (1.0 + 称号比例);
        }

        public static double 最终伤害(double 攻击, double 防御, double 兵力, double 血量)
        {
            double 伤害 = Math.Round((float)((攻击 - 防御) * 兵力 / 血量), MidpointRounding.ToEven);
            return 伤害 < 0.0 ? 0.0 : 伤害;
        }

        public static double 伤兵数量(double 损失, int 战场类型)
        {
            double 死亡 = 战场类型 == 0 ? 0.0 : 损失 * 0.3;
            return Math.Floor((float)(损失 - 死亡));
        }

        public static double 将领经验(double 击杀, double 敌军兵种攻击, double 敌方经验状态加成)
        {
            double 经验 = 击杀 * 敌军兵种攻击 * 0.5;
            return 经验 * (1.0 + 敌方经验状态加成 / 100.0);
        }

        public static float 攻击进度步长(float 兵种攻速, float 科技比例, float 称号比例, float 状态比例, float 经过秒数)
        {
            float 攻速 = 兵种攻速 * (1f + 科技比例 + 称号比例 + 状态比例);
            float 步长 = 4f / (60f / 攻速);
            return 步长 * 经过秒数;
        }

        public static bool 是否格挡(double 兵种, double 对方兵种, double 格斗科技, bool 帝王称号, int 随机值)
        {
            double 概率 = 格斗科技 * 3.0;
            概率 *= 1.0 + (帝王称号 ? 0.1 : 0.0);
            return 兵种 == 2.0 && (对方兵种 == 2.0 || 对方兵种 == 1.0) && 随机值 <= 概率;
        }

        public static bool 是否穿透(double 兵种, double 精准科技, bool 帝王称号, int 随机值)
        {
            double 概率 = 精准科技 * 6.0;
            概率 *= 1.0 + (帝王称号 ? 0.1 : 0.0);
            return 兵种 == 3.0 && 随机值 <= 概率;
        }

        public static bool 是否闪避(double 兵种, double 对方兵种, double 驯马科技, bool 帝王称号, int 随机值)
        {
            double 概率 = 驯马科技 * 7.0;
            概率 *= 1.0 + (帝王称号 ? 0.1 : 0.0);
            return 兵种 == 1.0 && (对方兵种 == 3.0 || 对方兵种 == 4.0) && 随机值 <= 概率;
        }

        public static 战斗奖励 计算奖励(IEnumerable<击杀兵种信息> 击杀列表, double 资源声望加成)
        {
            double 声望 = 0.0;
            double 铜钱 = 0.0;
            double 显示粮食 = 0.0;
            double 显示黄金 = 0.0;
            foreach (击杀兵种信息 击杀 in 击杀列表)
            {
                声望 += 击杀.兵种攻击 * 0.05 * 击杀.数量;
                铜钱 += 击杀.兵种攻击 * 0.1 * 击杀.数量;
                显示粮食 += 击杀.兵种攻击 * 0.3 * 击杀.数量;
                显示黄金 += 击杀.兵种攻击 * 0.002 * 击杀.数量;
            }
            double 加成比例 = 资源声望加成 / 100.0;
            声望 *= 1.0 + 加成比例;
            铜钱 *= 1.0 + 加成比例;
            显示粮食 *= 1.0 + 加成比例;
            return new 战斗奖励
            {
                声望 = Math.Floor((float)Math.Min(声望, 10000000.0)),
                国库铜钱 = Math.Floor((float)Math.Min(铜钱, 10000000.0)),
                国库粮食 = Math.Floor((float)Math.Min(铜钱, 10000000.0)),
                原提示粮食 = Math.Floor((float)Math.Min(显示粮食, 30000000.0)),
                原提示黄金 = Math.Floor((float)显示黄金)
            };
        }
    }

    public sealed class 击杀兵种信息
    {
        public int 兵种ID;
        public double 兵种攻击;
        public double 数量;
    }

    public sealed class 战斗奖励
    {
        public double 声望;
        public double 国库铜钱;
        public double 国库粮食;
        public double 原提示粮食;
        public double 原提示黄金;
    }
}
