using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

public class 登录脚本 : MonoBehaviour
{
    public Text log;
    public GameObject 开始游戏;
    public Text 账号;
    public Text 密码;
    public Text user;
    public Text pwd;
    public string 服务器地址 = "http://127.0.0.1:18080/api.php?appid=1";
    public string 游戏服务器地址 = "";
    public string 联机世界 = "main";
    public static string 会话令牌 { get; private set; }
    public static int 心跳ID { get; private set; }
    private bool 正在登录;
    private string 客户端ID = Guid.NewGuid().ToString("N");
    private void Awake()
    {
        GameNetwork.Configure(游戏服务器地址, Environment.GetEnvironmentVariable("DWSG_WORLD_ID") ?? 联机世界);
    }

    public void 准备登录入口()
    {
        // 原场景默认隐藏登录布局，因此不能依靠它的 Awake 选择联机模式。
        GameNetwork.Configure(游戏服务器地址, Environment.GetEnvironmentVariable("DWSG_WORLD_ID") ?? 联机世界);
        if (!GameNetwork.Enabled || 全局变量.是否为登录) return;
        if (开始游戏 != null) 开始游戏.SetActive(false);
        gameObject.SetActive(true);
    }

    private void Start()
    {
        var 账号输入 = 账号.GetComponentInParent<InputField>();
        if (账号输入 != null) 账号输入.text = PlayerPrefs.GetString("name", "");
        else 账号.text = PlayerPrefs.GetString("name", "");
        var 密码输入 = pwd.GetComponentInParent<InputField>();
        if (密码输入 != null)
        {
            密码输入.contentType = InputField.ContentType.Password;
            密码输入.text = "";
        }
        密码.text = "";
        PlayerPrefs.DeleteKey("pwd");
    }

    private static string 读取输入(Text 元件)
    {
        if (元件 == null) return "";
        var 输入 = 元件.GetComponentInParent<InputField>();
        return 输入 != null ? 输入.text : 元件.text;
    }

    public void 登录()
    {
        if (!正在登录) StartCoroutine(Login());
    }

    public static bool 尝试解析响应(string 响应, out Root 结果, out string 提示)
    {
        结果 = null;
        提示 = "服务器返回了无效响应，请稍后重试。";
        try
        {
            结果 = JsonConvert.DeserializeObject<Root>(响应);
            if (结果 == null || 结果.data == null || 结果.data.result == null) return false;
            提示 = 结果.data.result.ret_info ?? "服务器未提供结果说明。";
            if (结果.data.code != 200) return false;
            if (结果.data.result.tokenid <= 0 || string.IsNullOrEmpty(结果.data.result.session_token) ||
                结果.data.result.session_token.Length != 64)
            {
                提示 = "服务器会话无效，请检查后台版本。";
                return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
        catch (ArgumentException) { return false; }
    }

    IEnumerator Login()
    {
        正在登录 = true;
        全局变量.是否为登录 = false;
        会话令牌 = null;
        心跳ID = 0;
        try
        {
            string 当前账号 = 读取输入(user);
            string 当前密码 = 读取输入(pwd);
            string 玩家账号 = !string.IsNullOrEmpty(当前账号) ? 当前账号.Trim() : 读取输入(账号).Trim();
            string 玩家密码 = !string.IsNullOrEmpty(当前密码) ? 当前密码 : 读取输入(密码);
            if (玩家账号.Length == 0 || 玩家密码.Length == 0)
            {
                log.text = "请输入账号和密码。";
                yield break;
            }
            string 地址 = Environment.GetEnvironmentVariable("DWSG_AUTH_URL");
            if (string.IsNullOrEmpty(地址)) 地址 = PlayerPrefs.GetString("DWSG_AUTH_URL", "");
            if (string.IsNullOrEmpty(地址)) 地址 = 服务器地址;
            Uri 服务器;
            if (!Uri.TryCreate(地址, UriKind.Absolute, out 服务器) ||
                (服务器.Scheme != "http" && 服务器.Scheme != "https"))
            {
                log.text = "服务器地址配置错误。";
                yield break;
            }
            WWWForm form = new WWWForm();
            form.AddField("user", 玩家账号);
            form.AddField("pwd", 玩家密码);
            form.AddField("action", "login");
            form.AddField("ip", "");
            form.AddField("mac", SystemInfo.deviceUniqueIdentifier);
            form.AddField("md5", "");
            form.AddField("ver", "");
            form.AddField("uuid", Guid.NewGuid().ToString("N"));
            form.AddField("clientid", 客户端ID);
            form.AddField("t", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            using (UnityWebRequest 请求 = UnityWebRequest.Post(地址, form))
            {
                请求.timeout = 15;
                yield return 请求.SendWebRequest();
                if (请求.result != UnityWebRequest.Result.Success)
                {
                    log.text = "连接服务器失败，请检查网络和服务器地址。";
                    yield break;
                }
                Root 结果;
                string 提示;
                if (!尝试解析响应(请求.downloadHandler.text, out 结果, out 提示))
                {
                    log.text = 提示;
                    yield break;
                }
                会话令牌 = 结果.data.result.session_token;
                心跳ID = 结果.data.result.tokenid;
                if (GameNetwork.Enabled)
                {
                    var 凭据 = new JObject { ["user"] = 玩家账号, ["tokenId"] = 心跳ID, ["sessionToken"] = 会话令牌,
                        ["clientId"] = 客户端ID, ["mac"] = SystemInfo.deviceUniqueIdentifier, ["ip"] = "", ["md5"] = "", ["version"] = "" };
                    GameResult 联机结果 = null;
                    yield return GameNetwork.Connect(凭据, value => 联机结果 = value);
                    if (联机结果 == null || 联机结果.Code != GameCodes.Ok && 联机结果.Code != GameCodes.RoleRequired)
                    {
                        log.text = 联机结果?.Message ?? "连接游戏服务器失败，请稍后重试。";
                        yield break;
                    }
                }
                PlayerPrefs.SetString("name", 玩家账号);
                全局变量.是否为登录 = true;
                log.text = 提示;
                开始游戏.SetActive(true);
                gameObject.SetActive(false);
            }
        }
        finally { 正在登录 = false; }
    }
}

public class Result
{
    public int tokenid { get; set; }
    public string session_token { get; set; }
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
    public long t { get; set; }
}

[Serializable]
public class Root
{
    public Data data { get; set; }
    public string sign { get; set; }
}
