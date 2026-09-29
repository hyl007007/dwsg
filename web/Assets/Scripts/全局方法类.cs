using System;
using System.Security.Cryptography;
using System.Text;
using 玩家数据结构;

public class 全局方法类
{
	public static bool 删除指定国家(string 国家名字)
	{
		int count = 全局变量.所有国家列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有国家列表[i].国号 == 国家名字)
			{
				全局变量.所有国家列表.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public static int 获取指定国家的索引(string 国家名字)
	{
		int count = 全局变量.所有国家列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有国家列表[i].国号 == 国家名字)
			{
				return i;
			}
		}
		return -1;
	}

	public static 国家信息库类 获取指定ID的国家(int 指定ID)
	{
		int count = 全局变量.所有国家列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有国家列表[i].ID == 指定ID)
			{
				return 全局变量.所有国家列表[i];
			}
		}
		return null;
	}

	public static string 获取指定ID玩家名字(int id)
	{
		for (int i = 0; i < 全局变量.所有玩家数据表.Count; i++)
		{
			if (全局变量.所有玩家数据表[i].基础信息.ID == id)
			{
				return 全局变量.所有玩家数据表[i].基础信息.名字;
            }
		}

		return null;
	}

	public static string 获取指定ID状态文本(int id)
	{
		switch (id)
		{
            case 0:
                return "空闲";
            case 1:
                return "战斗";
            case 2:
                return "驻防";
            case 3:
                return "俘虏";
            default:
				return "未知";
		}
	}
    public static int 根据突围获取材料数量(int 突围)
    {
        switch (突围)
        {
            case 90:
                return 5;
            case 92:
                return 5;
            case 94:
                return 10;
            case 96:
                return 15;
            case 99:
                return 20;
            default:
                return 999;
        }
    }
    //public static int 根据突围获取材料数量(int 突围)
    //{
    //	switch (突围)
    //{
    //	case 90:
    //		return 5;
    //     case 94:
    //         return 10;
    //    case 96:
    //       return 15;
    //  case 99:
    //      return 20;
    //   default:
    //	return 999;
    //	}
    //}


    public static 国家信息库类 获取指定名字的国家(string 名字)
	{
		int count = 全局变量.所有国家列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有国家列表[i].国号 == 名字)
			{
				return 全局变量.所有国家列表[i];
			}
		}
		return null;
	}

	public static 玩家数据 获取指定名字的玩家(string 名字)
	{
		int count = 全局变量.所有玩家数据表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有玩家数据表[i].基础信息.名字 == 名字)
			{
				return 全局变量.所有玩家数据表[i];
			}
		}
		return null;
	}

	public static string GetStrMd5(string ConvertString)
	{
		return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(ConvertString))).Replace("-", "");
	}

    public static 将领信息 获取指定名字将领信息(string 将领名字)
    {
        将领信息 将领 = new 将领信息();
        for (int i = 0; i < 全局变量.所有玩家数据表.Count; i++)
        {
            for (int j = 0; j < 全局变量.所有玩家数据表[i].封地信息表[0].将领信息表.Count; j++)
            {
                if (全局变量.所有玩家数据表[i].封地信息表[0].将领信息表[j].将领属性.初始属性.名字 == 将领名字)
                {
                    return 全局变量.所有玩家数据表[i].封地信息表[0].将领信息表[j];
                }
            }
        }

        return 将领;

    }
}
