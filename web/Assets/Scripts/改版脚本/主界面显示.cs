using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 主界面显示 : MonoBehaviour
{
    public Text 版本;
    public Text 难度;
    void Start()
    {
        if (true)
        {
            版本.text = "版本: 叮当三国";
            
            switch (全局变量.难度)
            {
                case 1:
                    难度.text = "难度: 普通难度";
                    break;
                case 4:
                    难度.text = "难度: 挑战难度";
                    break;
            }

        }
    }
}
