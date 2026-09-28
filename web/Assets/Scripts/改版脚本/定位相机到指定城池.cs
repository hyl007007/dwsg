using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 定位相机到指定城池 : MonoBehaviour
{
    public ScrollRect 滑动对象;

    public Text 定位名称;

    public Text 定位x;

    public Text 定位y;

    public void 根据坐标定位指定城池位置()
    {
        if (定位x.text == "")
            return;
        if (定位y.text == "")
            return;
        定位地图到指定位置(Convert.ToInt32(定位x.text), Convert.ToInt32(定位y.text));
    }

    public void 根据名字定位指定城池位置()
    {
        print(定位名称.text);
        for (int i = 0; i < 全局变量.所有城池列表.Count; i++)
        {
            if (全局变量.所有城池列表[i].名称 == 定位名称.text.Trim())
                定位地图到指定位置(全局变量.所有城池列表[i].坐标x, 全局变量.所有城池列表[i].坐标y);
        }
    }

    public void 定位地图到指定位置(int x, int y)
    {
        float num = 0.0054f;
        float num2 = 0.0222f;
        滑动对象.normalizedPosition = new Vector2((float)x * num, 1f - (float)y * num2);
    }
}
