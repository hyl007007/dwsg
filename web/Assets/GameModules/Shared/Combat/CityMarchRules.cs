using System;
using System.Collections.Generic;

namespace Dwsg.Shared.Combat
{
    // 原A星寻路的城市跳点、方向顺序、费用及敌城不可穿越规则。
    public sealed class CityMarchRules
    {
        private sealed class Node
        {
            public int X, Y, Type, G, F;
        }
        private readonly int[,] map;
        private readonly Node[,] nodes;
        private readonly Func<int, int, bool> friendlyCity;
        private readonly List<Node> open = new List<Node>(), closed = new List<Node>();

        public CityMarchRules(int[,] map, Func<int, int, bool> friendlyCity)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.friendlyCity = friendlyCity ?? throw new ArgumentNullException(nameof(friendlyCity));
            nodes = new Node[map.GetLength(0), map.GetLength(1)];
        }

        public int Find(int x0, int y0, int x1, int y1)
        {
            if (!Inside(x0, y0) || !Inside(x1, y1)) return -1;
            Node current = Get(x0, y0), end = Get(x1, y1);
            open.Clear(); closed.Clear();
            current.G = current.F = 0; closed.Add(current);
            for (int iteration = 0; iteration < 3000; iteration++)
            {
                for (int direction = 0; direction < 8; direction++) Add(current, end, direction);
                if (open.Count == 0) return -1;
                // 原比较器在相等时也返回1；保留原实际路径费用。
                open.Sort((a, b) => a.F >= b.F ? 1 : -1);
                current = open[0]; closed.Add(current); open.RemoveAt(0);
                if (current == end) return current.G;
            }
            return -1;
        }

        private void Add(Node parent, Node end, int direction)
        {
            int x = parent.X, y = parent.Y, targetX = -1, targetY = -1, cost = 10;
            if (direction == 1 || direction == 5)
            {
                int dy = direction == 1 ? -1 : 1;
                if (Type(x, y + dy) == 1 && Type(x, y + dy * 2) > 1)
                { targetX = x; targetY = y + dy * 2; }
            }
            else if (direction == 3 || direction == 7)
            {
                int dx = direction == 3 ? 1 : -1;
                for (int distance = 1; distance < 6; distance++)
                {
                    if (!Inside(x + dx * distance, y)) continue;
                    int type = Type(x + dx * distance, y);
                    if (type == 0) break;
                    if (type > 1) { targetX = x + dx * distance; targetY = y; cost = 10 * (distance - 1); }
                }
            }
            else
            {
                int dx = direction == 0 || direction == 6 ? -1 : 1;
                int dy = direction == 0 || direction == 2 ? -1 : 1;
                int endY = y + dy * 2;
                if ((Type(x, y + dy) == 1 || Type(x + dx, y + dy) == 1) && Type(x + dx, endY) > 1)
                { targetX = x + dx; targetY = endY; cost = 14; }
                if (Type(x + dx, y + dy) == 1 && Type(x + dx * 2, endY) > 1)
                { targetX = x + dx * 2; targetY = endY; cost = 28; }
                if (Type(x + dx, y + dy) == 1 && Type(x + dx * 2, y + dy) == 1 && Type(x + dx * 3, endY) > 1)
                { targetX = x + dx * 3; targetY = endY; cost = 42; }
                // 原第四跳的第三个路格位于终点行，不是中间行。
                if (Type(x + dx, y + dy) == 1 && Type(x + dx * 2, y + dy) == 1 && Type(x + dx * 3, endY) == 1 && Type(x + dx * 4, endY) > 1)
                { targetX = x + dx * 4; targetY = endY; cost = 56; }
            }
            if (targetX < 0) return;
            Node node = Get(targetX, targetY);
            if (open.Contains(node) || closed.Contains(node) || (node.Type == 3 && node != end)) return;
            node.G = parent.G + cost;
            node.F = node.G + Math.Abs(end.X - node.X) + Math.Abs(end.Y - node.Y);
            open.Add(node);
        }

        private bool Inside(int x, int y) => x >= 0 && y >= 0 && x < map.GetLength(1) && y < map.GetLength(0);
        private int Type(int x, int y) => Inside(x, y) ? Get(x, y).Type : 0;
        private Node Get(int x, int y)
        {
            return nodes[y, x] ?? (nodes[y, x] = new Node { X = x, Y = y,
                Type = map[y, x] == 0 ? 0 : map[y, x] >= 2 ? (friendlyCity(x + 1, y + 1) ? 2 : 3) : 1 });
        }
    }
}
