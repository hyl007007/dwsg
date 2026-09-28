using Newtonsoft.Json;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class 存档脚本 : MonoBehaviour
{
    public GameObject 存档列表对象;

    private string 存档文件夹;

    private bool 正在写入;

    public void 刷新存档列表()
    {
        if (全局变量.当局赌场下注列表.Count > 0)
        {
            全局变量.提示类.显示信息("请等待赌场结束");
            return;
        }
        读取所有存档();
        显示所有存档();
    }

    public void 读取所有存档()
    {
        if (全局变量.当局赌场下注列表.Count > 0)
        {
            全局变量.提示类.显示信息("请等待赌场结束");
            return;
        }
        存档文件夹 = UnityEngine.Application.persistentDataPath;
        全局变量.所有存档列表.Clear();
        string[] files = Directory.GetFiles(存档文件夹, "存档*.txt");
        for (int i = 0; i < files.Length; i++)
        {
            UnityEngine.Debug.Log("读取存档" + files[i]);
            string text = File.ReadAllText(files[i]);
            text = 加密.DecryptString(text);
            if (text != null)
            {
                存档信息库类 text2 = new 存档信息库类();
                text2 = (存档信息库类)JsonConvert.DeserializeObject(text, typeof(存档信息库类));
                全局变量.所有存档列表.Add((存档信息库类)text2);
            }
        }
    }

    public void 显示所有存档()
    {
        if (全局变量.当局赌场下注列表.Count > 0)
        {
            全局变量.提示类.显示信息("请等待赌场结束");
            return;
        }
        int count = 全局变量.所有存档列表.Count;
        for (int i = 0; i < 6; i++)
        {
            Text component = 存档列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>();
            if (i < count)
                component.text = "存档" + (i + 1).ToString() + "：" + 全局变量.所有存档列表[i].玩家列表[0].基础信息.名字 + "<" + 全局变量.所有存档列表[i].玩家列表[0].基础信息.等级.ToString() + "级>   " + 全局变量.所有存档列表[i].玩家列表[0].基础信息.国家 + "   " + TIME.转时间格式(全局变量.所有存档列表[i].存档时间);
            else
                component.text = "存档" + (i + 1).ToString() + "：<空记录>";
        }
    }

    public void 读取指定存档(int 第几个存档)
    {
        if (全局变量.当局赌场下注列表.Count>0)
        {
            全局变量.提示类.显示信息("请等待赌场结束");
            return;
        }
        if (第几个存档 <= 全局变量.所有存档列表.Count)
        {
            UnityEngine.Debug.Log("读取存档" + 第几个存档.ToString() + "成功!");
            全局变量.所有国家列表.Clear();
            全局变量.所有玩家数据表.Clear();
            全局变量.所有城池列表.Clear();
            全局变量.所有国家列表 = null;
            全局变量.所有玩家数据表 = null;
            全局变量.所有城池列表 = null;
            全局变量.所有国家列表 = 全局变量.所有存档列表[第几个存档 - 1].国家列表;
            全局变量.所有玩家数据表 = 全局变量.所有存档列表[第几个存档 - 1].玩家列表;
            全局变量.所有城池列表 = 全局变量.所有存档列表[第几个存档 - 1].城池列表;
            int count = 全局变量.所有玩家数据表.Count;
            for (int i = 0; i < count; i++)
                全局变量.所有玩家数据表[i].重置将领状态();
            所有城池界面脚本.重置城池状态();
            SceneManager.LoadScene(1);
        }
    }

    public void 写入指定存档(int 第几个存档)
    {
        if (全局变量.当局赌场下注列表.Count > 0)
        {
            全局变量.提示类.显示信息("请等待赌场结束");
            return;
        }

        UnityEngine.Debug.Log("写入存档" + 第几个存档.ToString());
        string contents = JsonConvert.SerializeObject(new 存档信息库类
        {
            ID = 第几个存档,
            存档时间 = TIME.getTime(),
            国家列表 = 全局变量.所有国家列表,
            城池列表 = 全局变量.所有城池列表,
            玩家列表 = 全局变量.所有玩家数据表
        });
        string text = 存档文件夹 + "/存档" + 第几个存档.ToString() + ".txt";
        contents = 加密.EncryptString(contents);
        File.WriteAllText(text, contents);
        UnityEngine.Debug.Log(text);
        刷新存档列表();
        全局变量.提示类.显示信息("保存存档" + 第几个存档.ToString() + "成功!");
    }
}
