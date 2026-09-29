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

    private Button 定位按钮;
    private Button.ButtonClickedEvent 定位事件;

    private void OnEnable()
    {
        var 按钮对象 = transform.Find("定位");
        if (按钮对象 == null) return;
        定位按钮 = 按钮对象.GetComponent<Button>();
        if (定位按钮 == null) return;
        if (定位事件 == null || 定位按钮.onClick != 定位事件)
        {
            // 原按钮还有无条件关闭面板的持久回调；由定位成功后统一关闭。
            定位事件 = new Button.ButtonClickedEvent();
            定位事件.AddListener(根据名字定位指定城池位置);
            定位按钮.onClick = 定位事件;
        }
        界面窗口管理器.注册运行时按钮(定位按钮);
    }

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
        var 查询对象 = transform.Find("查询");
        var 输入框 = 查询对象 == null ? null : 查询对象.GetComponent<InputField>();
        if (输入框 == null && 定位名称 != null)
            输入框 = 定位名称.GetComponentInParent<InputField>(true);
        string 名称 = (输入框 != null ? 输入框.text : 定位名称 != null ? 定位名称.text : "") ?? "";
        名称 = 名称.Trim();
        if (名称.Length == 0)
        {
            显示定位提示("请输入城池名称。");
            return;
        }
        if (全局变量.所有城池列表 == null || 全局变量.所有城池列表.Count == 0)
        {
            显示定位提示("城池数据尚未加载，请稍后重试。");
            return;
        }
        if (滑动对象 == null)
        {
            显示定位提示("地图尚未准备好，请返回世界地图后重试。");
            return;
        }
        for (int i = 0; i < 全局变量.所有城池列表.Count; i++)
        {
            var 城池 = 全局变量.所有城池列表[i];
            if (城池 == null || 城池.名称 != 名称) continue;
            定位地图到指定位置(城池.坐标x, 城池.坐标y);
            gameObject.SetActive(false);
            return;
        }
        显示定位提示("未找到该城池，请核对城池名称。");
    }

    private void 显示定位提示(string 内容)
    {
        var 提示 = 全局变量.提示类;
        if (提示 == null)
        {
            foreach (var 根对象 in gameObject.scene.GetRootGameObjects())
            {
                提示 = 根对象.GetComponentInChildren<提示移动>(true);
                if (提示 != null) break;
            }
        }
        if (提示 != null) 提示.显示信息(内容);
        else Debug.LogWarning(内容);
    }

    public void 定位地图到指定位置(int x, int y)
    {
        float num = 0.0054f;
        float num2 = 0.0222f;
        滑动对象.normalizedPosition = new Vector2((float)x * num, 1f - (float)y * num2);
    }
}
