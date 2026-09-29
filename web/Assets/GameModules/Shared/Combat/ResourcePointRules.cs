using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Window3;

namespace Dwsg.Shared.Combat
{
    // 原资源点目录与采集算法，离线和服务器共用；不访问玩家、时钟或 Unity 对象。
    public static class ResourcePointRules
    {
        public const int Limit = 2, Interval = 10, Recovery = 600;
        public const long MaximumSecond = 253402300799L;

        public static List<资源点配置> Generate(int[,] map, int sourceX, int sourceY, Func<int, int, bool> allowed)
        {
            int width = map.GetLength(1), height = map.GetLength(0);
            var output = new List<资源点配置>();
            if (sourceX < 1 || sourceY < 1 || sourceX > width || sourceY > height || map[sourceY - 1, sourceX - 1] < 2) return output;
            var distance = new int[height, width];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) distance[y, x] = -1;
            var queue = new Queue<int>(); queue.Enqueue((sourceY - 1) * width + sourceX - 1); distance[sourceY - 1, sourceX - 1] = 0;
            int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue(), x = cell % width, y = cell / width;
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i], ny = y + dy[i];
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || map[ny, nx] < 1 || distance[ny, nx] >= 0) continue;
                    distance[ny, nx] = distance[y, x] + 1; queue.Enqueue(ny * width + nx);
                }
            }
            var cells = new List<int>();
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                if (distance[y, x] >= 0 && allowed(x + 1, y + 1)) cells.Add(y * width + x);
            cells = cells.OrderBy(i => distance[i / width, i % width]).ThenBy(i => i).Take(2).ToList();
            if (cells.Count != 2) return output;
            for (int i = 0; i < cells.Count; i++)
            {
                int x = cells[i] % width + 1, y = cells[i] / width + 1;
                output.Add(new 资源点配置 { 标识 = "local.v1." + x + "." + y + "." + i, 坐标x = x, 坐标y = y, 类型 = (资源点类型)i });
            }
            return output;
        }

        // 操作候选副本；调用方全部验证入账后才提交。保留十秒节奏和不足一单位的产出余数。
        public static long Advance(资源点状态 point, long now, bool abandon)
        {
            if (now < 0 || now > MaximumSecond || point.时产 < 1 || point.时产 > 1000000 ||
                point.剩余库存 < 0 || point.剩余库存 > point.时产 || point.产出余数 < 0 || point.产出余数 >= 3600 ||
                point.节奏余秒 < 0 || point.节奏余秒 >= Interval || point.结算时间 < 0 || point.结算时间 > MaximumSecond)
                throw new InvalidOperationException("资源点数据无效。");
            long income = 0;
            if (point.占领玩家ID >= 0)
            {
                if (now < point.结算时间) return 0;
                long seconds = now - point.结算时间 + point.节奏余秒;
                long used = abandon ? seconds : seconds / Interval * Interval;
                long numerator = used * point.时产 + point.产出余数;
                income = Math.Min(point.剩余库存, numerator / 3600);
                if (income == point.剩余库存)
                {
                    long needed = (long)point.剩余库存 * 3600 - point.产出余数;
                    long duration = abandon ? (needed + point.时产 - 1) / point.时产 :
                        ((needed + point.时产 * Interval - 1) / (point.时产 * Interval)) * Interval;
                    long depleted = point.结算时间 + Math.Max(0, duration - point.节奏余秒);
                    if (depleted > MaximumSecond - Recovery) throw new InvalidOperationException("资源恢复时间溢出。");
                    point.剩余库存 = 0; point.占领玩家ID = -1; point.恢复时间 = depleted + Recovery;
                    point.产出余数 = point.节奏余秒 = 0; point.结算时间 = now;
                }
                else
                {
                    point.剩余库存 -= (int)income; point.产出余数 = (int)(numerator % 3600);
                    point.节奏余秒 = (int)(seconds - used); point.结算时间 = now;
                }
            }
            if (point.恢复时间 > 0 && now >= point.恢复时间)
            {
                if (point.批次 >= int.MaxValue - 1) throw new InvalidOperationException("资源点批次已达上限。");
                point.剩余库存 = point.时产; point.恢复时间 = point.结算时间 = 0;
                point.产出余数 = point.节奏余秒 = 0; point.批次++;
            }
            if (abandon) { point.占领玩家ID = -1; point.节奏余秒 = 0; }
            return income;
        }
    }
}
