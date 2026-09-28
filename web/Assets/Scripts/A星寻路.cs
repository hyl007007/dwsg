using System.Collections.Generic;
using UnityEngine;

public class A星寻路
{
	private static int mapW = 全局大地图库.大地图表.GetLength(1);

	private static int mapH = 全局大地图库.大地图表.GetLength(0);

	private Node[,] nodeMap = new Node[mapH, mapW];

	private List<Node> open = new List<Node>();

	private List<Node> close = new List<Node>();

	public string 指定身份 = "";

	public int 开始寻路(int x0, int y0, int x1, int y1)
	{
		if (x0 < 0 || x0 >= mapW || y0 < 0 || y0 >= mapH)
		{
			UnityEngine.Debug.Log("坐标越界");
			return -1;
		}
		Node node = 获取格子数据(x0, y0);
		Node node2 = 获取格子数据(x1, y1);
		open.Clear();
		close.Clear();
		node.parent = null;
		node.F = 0;
		node.G = 0;
		node.H = 0;
		close.Add(node);
		for (int i = 0; i < 3000; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				OpenAdd(node.x, node.y, j, node, node2);
			}
			if (open.Count == 0)
			{
				return -1;
			}
			open.Sort(SortOpenList);
			close.Add(open[0]);
			node = open[0];
			open.RemoveAt(0);
			if (node == node2)
			{
				List<Node> list = new List<Node>();
				list.Add(node2);
				int g = node2.G;
				while (node2.parent != null)
				{
					list.Add(node2.parent);
					node2 = node2.parent;
				}
				list.Reverse();
				for (int k = 0; k < list.Count; k++)
				{
				}
				return g;
			}
		}
		UnityEngine.Debug.Log("距离过远");
		return -1;
	}

	public void OpenAdd(int x, int y, int fx, Node parent, Node end)
	{
		int num = 10;
		int num2 = -1;
		int y2 = -1;
		switch (fx)
		{
		case 1:
			if (y - 2 >= 0 && 获取格子数据(x, y - 1).type == 1 && 获取格子数据(x, y - 2).type > 1)
			{
				num = 10;
				num2 = x;
				y2 = y - 2;
			}
			break;
		case 5:
			if (y + 2 < mapH && 获取格子数据(x, y + 1).type == 1 && 获取格子数据(x, y + 2).type > 1)
			{
				num = 10;
				num2 = x;
				y2 = y + 2;
			}
			break;
		case 7:
			for (int j = 1; j < 6; j++)
			{
				if (x - j >= 0)
				{
					Node node2 = 获取格子数据(x - j, y);
					if (node2.type == 0)
					{
						break;
					}
					if (node2.type > 1)
					{
						num = 10 * (j - 1);
						num2 = x - j;
						y2 = y;
					}
				}
			}
			break;
		case 3:
			for (int i = 1; i < 6; i++)
			{
				if (x + i < mapW)
				{
					Node node = 获取格子数据(x + i, y);
					if (node.type == 0)
					{
						break;
					}
					if (node.type > 1)
					{
						num = 10 * (i - 1);
						num2 = x + i;
						y2 = y;
					}
				}
			}
			break;
		case 0:
			if (x - 1 >= 0 && y - 2 >= 0 && 获取格子数据(x, y - 1).type == 1 && 获取格子数据(x - 1, y - 2).type > 1)
			{
				num = 14;
				num2 = x - 1;
				y2 = y - 2;
			}
			if (x - 1 >= 0 && y - 2 >= 0 && 获取格子数据(x - 1, y - 1).type == 1 && 获取格子数据(x - 1, y - 2).type > 1)
			{
				num = 14;
				num2 = x - 1;
				y2 = y - 2;
			}
			if (x - 2 >= 0 && y - 2 >= 0 && 获取格子数据(x - 1, y - 1).type == 1 && 获取格子数据(x - 2, y - 2).type > 1)
			{
				num = 28;
				num2 = x - 2;
				y2 = y - 2;
			}
			if (x - 3 >= 0 && y - 2 >= 0 && 获取格子数据(x - 1, y - 1).type == 1 && 获取格子数据(x - 2, y - 1).type == 1 && 获取格子数据(x - 3, y - 2).type > 1)
			{
				num = 42;
				num2 = x - 3;
				y2 = y - 2;
			}
			if (x - 4 >= 0 && y - 2 >= 0 && 获取格子数据(x - 1, y - 1).type == 1 && 获取格子数据(x - 2, y - 1).type == 1 && 获取格子数据(x - 3, y - 2).type == 1 && 获取格子数据(x - 4, y - 2).type > 1)
			{
				num = 56;
				num2 = x - 4;
				y2 = y - 2;
			}
			break;
		case 2:
			if (x + 1 < mapW && y - 2 >= 0 && 获取格子数据(x, y - 1).type == 1 && 获取格子数据(x + 1, y - 2).type > 1)
			{
				num = 14;
				num2 = x + 1;
				y2 = y - 2;
			}
			if (x + 1 < mapW && y - 2 >= 0 && 获取格子数据(x + 1, y - 1).type == 1 && 获取格子数据(x + 1, y - 2).type > 1)
			{
				num = 14;
				num2 = x + 1;
				y2 = y - 2;
			}
			if (x + 2 < mapW && y - 2 >= 0 && 获取格子数据(x + 1, y - 1).type == 1 && 获取格子数据(x + 2, y - 2).type > 1)
			{
				num = 28;
				num2 = x + 2;
				y2 = y - 2;
			}
			if (x + 3 < mapW && y - 2 >= 0 && 获取格子数据(x + 1, y - 1).type == 1 && 获取格子数据(x + 2, y - 1).type == 1 && 获取格子数据(x + 3, y - 2).type > 1)
			{
				num = 42;
				num2 = x + 3;
				y2 = y - 2;
			}
			if (x + 4 < mapW && y - 2 >= 0 && 获取格子数据(x + 1, y - 1).type == 1 && 获取格子数据(x + 2, y - 1).type == 1 && 获取格子数据(x + 3, y - 2).type == 1 && 获取格子数据(x + 4, y - 2).type > 1)
			{
				num = 56;
				num2 = x + 4;
				y2 = y - 2;
			}
			break;
		case 4:
			if (x + 1 < mapW && y + 2 < mapH && 获取格子数据(x, y + 1).type == 1 && 获取格子数据(x + 1, y + 2).type > 1)
			{
				num = 14;
				num2 = x + 1;
				y2 = y + 2;
			}
			if (x + 1 < mapW && y + 2 < mapH && 获取格子数据(x + 1, y + 1).type == 1 && 获取格子数据(x + 1, y + 2).type > 1)
			{
				num = 14;
				num2 = x + 1;
				y2 = y + 2;
			}
			if (x + 2 < mapW && y + 2 < mapH && 获取格子数据(x + 1, y + 1).type == 1 && 获取格子数据(x + 2, y + 2).type > 1)
			{
				num = 28;
				num2 = x + 2;
				y2 = y + 2;
			}
			if (x + 3 < mapW && y + 2 < mapH && 获取格子数据(x + 1, y + 1).type == 1 && 获取格子数据(x + 2, y + 1).type == 1 && 获取格子数据(x + 3, y + 2).type > 1)
			{
				num = 42;
				num2 = x + 3;
				y2 = y + 2;
			}
			if (x + 4 < mapW && y + 2 < mapH && 获取格子数据(x + 1, y + 1).type == 1 && 获取格子数据(x + 2, y + 1).type == 1 && 获取格子数据(x + 3, y + 2).type == 1 && 获取格子数据(x + 4, y + 2).type > 1)
			{
				num = 56;
				num2 = x + 4;
				y2 = y + 2;
			}
			break;
		case 6:
			if (x - 1 >= 0 && y + 2 < mapH && 获取格子数据(x, y + 1).type == 1 && 获取格子数据(x - 1, y + 2).type > 1)
			{
				num = 14;
				num2 = x - 1;
				y2 = y + 2;
			}
			if (x - 1 >= 0 && y + 2 < mapH && 获取格子数据(x - 1, y + 1).type == 1 && 获取格子数据(x - 1, y + 2).type > 1)
			{
				num = 14;
				num2 = x - 1;
				y2 = y + 2;
			}
			if (x - 2 >= 0 && y + 2 < mapH && 获取格子数据(x - 1, y + 1).type == 1 && 获取格子数据(x - 2, y + 2).type > 1)
			{
				num = 28;
				num2 = x - 2;
				y2 = y + 2;
			}
			if (x - 3 >= 0 && y + 2 < mapH && 获取格子数据(x - 1, y + 1).type == 1 && 获取格子数据(x - 2, y + 1).type == 1 && 获取格子数据(x - 3, y + 2).type > 1)
			{
				num = 42;
				num2 = x - 3;
				y2 = y + 2;
			}
			if (x - 4 >= 0 && y + 2 < mapH && 获取格子数据(x - 1, y + 1).type == 1 && 获取格子数据(x - 2, y + 1).type == 1 && 获取格子数据(x - 3, y + 2).type == 1 && 获取格子数据(x - 4, y + 2).type > 1)
			{
				num = 56;
				num2 = x - 4;
				y2 = y + 2;
			}
			break;
		}
		if (num2 > -1)
		{
			Node node3 = 获取格子数据(num2, y2);
			if (!open.Contains(node3) && !close.Contains(node3) && (node3.type != 3 || node3 == end))
			{
				node3.parent = parent;
				node3.G = parent.G + num;
				node3.H = Mathf.Abs(end.x - node3.x) + Mathf.Abs(end.y - node3.y);
				node3.F = node3.G + node3.H;
				open.Add(node3);
			}
		}
	}

	public void 加入指定坐标到开启列表(int x, int y, int G, Node parent, Node end)
	{
		if (x >= 0 && x < mapW && y >= 0 && y < mapH)
		{
			Node node = 获取格子数据(x, y);
			if (!open.Contains(node) && !close.Contains(node) && node.type != 0 && (node.type != 3 || node == end))
			{
				node.parent = parent;
				node.G = parent.G + G;
				node.H = Mathf.Abs(end.x - node.x) + Mathf.Abs(end.y - node.y);
				node.F = node.G + node.H;
				open.Add(node);
			}
		}
	}

	public Node 获取格子数据(int x, int y)
	{
		if (nodeMap[y, x] == null)
		{
			if (全局大地图库.大地图表[y, x] == 0)
			{
				nodeMap[y, x] = new Node(x, y, 0);
			}
			else if (全局大地图库.大地图表[y, x] >= 2)
			{
				城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(x + 1, y + 1);
				if (指定身份 != "")
				{
					if (城池信息库类.国家 != 指定身份)
					{
						nodeMap[y, x] = new Node(x, y, 3);
					}
					else
					{
						nodeMap[y, x] = new Node(x, y, 2);
					}
				}
				else if (城池信息库类.获取城池身份() != 0)
				{
					nodeMap[y, x] = new Node(x, y, 3);
				}
				else
				{
					nodeMap[y, x] = new Node(x, y, 2);
				}
			}
			else
			{
				nodeMap[y, x] = new Node(x, y, 1);
			}
		}
		return nodeMap[y, x];
	}

	private int SortOpenList(Node a, Node b)
	{
		if (a.F > b.F)
		{
			return 1;
		}
		if (a.F == b.F)
		{
			return 1;
		}
		return -1;
	}
}
