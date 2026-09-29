using Newtonsoft.Json.Linq;

namespace Dwsg.Host;

public sealed class PhpSessionProof
{
    public string User { get; set; }
    public int TokenId { get; set; }
    public string SessionToken { get; set; }
    public string ClientId { get; set; }
    public string Mac { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Md5 { get; set; } = "";
    public string Version { get; set; } = "";
}

public sealed class VerifiedAccount
{
    public string AccountId { get; init; }
    public long ExpiresUtcMs { get; init; }
}

public sealed class PhpSessionRejected : Exception
{
    public PhpSessionRejected(string message) : base(message) { }
}

public sealed class PhpAuthentication : IDisposable
{
    private readonly HttpClient http;
    private readonly Uri issuer;
    private readonly int appId;
    public PhpAuthentication(Uri issuer)
    {
        if (issuer.Scheme != "http" && issuer.Scheme != "https") throw new ArgumentException("Invalid auth URL");
        if (!string.IsNullOrEmpty(issuer.UserInfo)) throw new ArgumentException("Auth URL must not contain credentials");
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(issuer.Query);
        if (!query.TryGetValue("appid", out var value) || !int.TryParse(value, out appId) || appId <= 0)
            throw new ArgumentException("Auth URL must specify appid");
        this.issuer = issuer;
        http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(10) };
    }
    public async Task<VerifiedAccount> VerifyAsync(PhpSessionProof proof, bool renew, CancellationToken cancellation = default)
    {
        if (proof == null || string.IsNullOrWhiteSpace(proof.User) || proof.User.Length > 128 || proof.TokenId <= 0 ||
            string.IsNullOrEmpty(proof.ClientId) || proof.ClientId.Length > 128 || proof.SessionToken == null ||
            proof.SessionToken.Length != 64 || proof.SessionToken.Any(c => !(c >= 'a' && c <= 'f' || c >= '0' && c <= '9')))
            throw new PhpSessionRejected("会话无效，请重新登录。");
        if (renew) await CallAsync(proof, "heartbeat", cancellation);
        var result = await CallAsync(proof, "gameauth", cancellation);
        var accountId = result.Value<string>("accountId");
        var prefix = "php:" + appId + ":";
        var expires = result.Value<long?>("sessionExpiresUtcMs") ?? 0;
        if (result.Value<int?>("appid") != appId || accountId == null || !accountId.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(accountId.Substring(prefix.Length), out var userId) || userId <= 0 ||
            expires <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            throw new PhpSessionRejected("会话已到期，请重新登录。");
        return new VerifiedAccount { AccountId = accountId, ExpiresUtcMs = expires };
    }
    private async Task<JObject> CallAsync(PhpSessionProof proof, string action, CancellationToken cancellation)
    {
        var fields = new Dictionary<string, string> { ["user"] = proof.User, ["tokenid"] = proof.TokenId.ToString(),
            ["session_token"] = proof.SessionToken, ["clientid"] = proof.ClientId, ["action"] = action,
            ["mac"] = proof.Mac ?? "", ["ip"] = proof.Ip ?? "", ["md5"] = proof.Md5 ?? "", ["ver"] = proof.Version ?? "",
            ["uuid"] = Guid.NewGuid().ToString("N"), ["t"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() };
        using var content = new FormUrlEncodedContent(fields);
        using var response = await http.PostAsync(issuer, content, cancellation);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellation);
        if (body.Length > 65536) throw new HttpRequestException("Invalid auth response");
        var data = JObject.Parse(body)["data"] as JObject;
        if (data == null) throw new HttpRequestException("Auth response must use JSON mode");
        if (data.Value<int?>("code") != 200) throw new PhpSessionRejected(data["result"]?.Value<string>("ret_info") ?? "认证失败，请重新登录。");
        return data["result"] as JObject ?? throw new HttpRequestException("Invalid auth result");
    }
    public void Dispose() { http.Dispose(); }
}
