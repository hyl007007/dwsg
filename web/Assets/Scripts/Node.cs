public class Node
{
	public int x;

	public int y;

	public int type;

	public Node parent;

	public int G;

	public int H;

	public int F;

	public Node(int x, int y, int type)
	{
		this.x = x;
		this.y = y;
		this.type = type;
	}
}
