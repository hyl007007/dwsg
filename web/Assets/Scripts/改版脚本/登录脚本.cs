using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class 登录脚本 : MonoBehaviour
{
    public Text log;
    public GameObject 开始游戏;
    public Text 账号;
    public Text 密码;
    public Text user;
    public Text pwd;

    private string userName;
    private string userPwd;

    private void Start()
    {
        userName = PlayerPrefs.GetString("name");
        userPwd = PlayerPrefs.GetString("pwd");
        print(userName);
        print(userPwd);
        if (userName != "")
        {
            账号.text = userName;
            密码.text = userPwd;
        }
    }

    public void 登录()
    {
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        yield return new WaitForSeconds(1.5f);
        WWWForm form = new WWWForm();
        string 玩家账号 = user.text;
        string 玩家密码 = pwd.text;
        if (玩家账号 == "" || 玩家密码 == "")
        {
            玩家账号 = 账号.text;
            玩家密码 = 密码.text;
        }
        print(SystemInfo.deviceUniqueIdentifier);
        form.AddField("user", 玩家账号);
        form.AddField("pwd", 玩家密码);
        form.AddField("action", "login");
        form.AddField("ip", "");
        form.AddField("mac", SystemInfo.deviceUniqueIdentifier);
        form.AddField("md5", "");
        form.AddField("ver", "");
        form.AddField("uuid", "1123456789");
        form.AddField("clientid", "8848865");
        form.AddField("t", Convert.ToInt64((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds).ToString());
        UnityWebRequest webRequest = UnityWebRequest.Post("http://127.0.0.1:8000/api.php?appid=1", form);

        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(webRequest.error);
        }
        else
        {
            string rsp = webRequest.downloadHandler.text;
            print(rsp);
            Root root = JsonConvert.DeserializeObject<Root>(rsp);
            if (root.data.code == 200)
            {
                log.text = Regex.Unescape(root.data.result.ret_info);
                this.gameObject.SetActive(false);
                开始游戏.SetActive(true);
                PlayerPrefs.SetString("name", user.text);
                PlayerPrefs.SetString("pwd", pwd.text);
                全局变量.是否为登录 = true;
            }
            else
            {
                log.text = Regex.Unescape(root.data.result.ret_info);
            }

        }
    }
}


public class Result
{
    public int tokenid { get; set; }
    public string clientid { get; set; }
    public string user { get; set; }
    public string endtime { get; set; }
    public string point { get; set; }
    public string mac { get; set; }
    public string ip { get; set; }
    public string dk { get; set; }
    public string email { get; set; }
    public string userqq { get; set; }
    public string ver { get; set; }
    public string addtime { get; set; }
    public string logintime { get; set; }
    public string data { get; set; }
    public string ret_info { get; set; }
}

public class Data
{
    public int code { get; set; }
    public Result result { get; set; }
    public string uuid { get; set; }
    public string token { get; set; }
    public int t { get; set; }
}

[Serializable]
public class Root
{
    public Data data { get; set; }
    public string sign { get; set; }
}
