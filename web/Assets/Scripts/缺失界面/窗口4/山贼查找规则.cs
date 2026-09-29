using System;
using System.Collections.Generic;
using System.Linq;

namespace 缺失界面.窗口4
{
    public static class 山贼查找规则
    {
        public static List<山贼属性信息> 查询(List<山贼属性信息> 数据, int 等级, int x, int y)
        {
            if (数据 == null || 等级 < 0 || 等级 > 10) return new List<山贼属性信息>();
            return 数据.Where(贼 => 贼 != null && (等级 == 0 || 贼.等级 == 等级))
                .OrderBy(贼 => Math.Abs((long)贼.坐标x - x) + Math.Abs((long)贼.坐标y - y))
                .ThenBy(贼 => 贼.坐标x).ThenBy(贼 => 贼.坐标y).ToList();
        }
    }
}
