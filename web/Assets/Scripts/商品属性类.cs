public class 商品属性类
{
	public string 道具名;

	public double 黄金售价 = 1.0;

	public double 白银售价;

	public int 限购数量;

	public 商品属性类(string a, double b, double c,int num = -1)
	{
		道具名 = a;
		黄金售价 = b;
		白银售价 = c;
        限购数量 = num;
    }
}
