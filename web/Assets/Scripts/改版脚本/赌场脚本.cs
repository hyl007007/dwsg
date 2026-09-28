using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 赌场脚本 : MonoBehaviour
{
    public Text 小金额;
    public Text 豹子金额;
    public Text 大金额;
    public void 开始下注(int id)
    {
        添加下注到赌场(id);
    }
    
    private void 添加下注到赌场(int type)
    {
        if (!全局变量.赌场是否开始)
        {
            int 下注金额 = 0;
            if (type == 0)
                下注金额 = Convert.ToInt32(小金额.text);
            if (type == 1)
                下注金额 = Convert.ToInt32(大金额.text);
            if (type == 2)
                下注金额 = Convert.ToInt32(豹子金额.text);
            for (int i = 0; i < 全局变量.当局赌场下注列表.Count; i++)
            {
                if (全局变量.当局赌场下注列表[i].下注类型 == type)
                {
                    return;
                }
            }
            
            if (全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 >= 下注金额)
            {
                全局变量.当局赌场下注列表.Add(new 赌场信息(type, 下注金额));
                全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 -= 下注金额;
                全局变量.提示类.显示信息($"下注成功{下注金额}");
            }
            else
            {
                全局变量.提示类.显示信息("余额不足下注失败");
            }
        }
        else
        {
            全局变量.提示类.显示信息("已经开始无法下注");
        }
        
    }
}

public class 赌场信息
{
    public int 下注类型;
    public int 下注金额;

    public 赌场信息(int type,int money)
    {
        下注类型 = type;
        下注金额 = money;
    }
}
