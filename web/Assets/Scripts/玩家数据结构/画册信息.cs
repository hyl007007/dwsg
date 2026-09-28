using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 画册信息 
{
    public long 到期时间;
    public string 名字;
    public int 将领id;
    public 画册信息()
    {

    }

    public 画册信息(long time,string name,int id)
    {
        到期时间 = time;
        名字 = name;
        将领id = id;
    }
}
