using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Dwsg.Network
{
    // A LAN game must reach the configured local server even when Windows has a web proxy.
    // This handler is local to the game; it does not alter OS or other applications' settings.
    public static class GameHttp
    {
        public sealed class Reply
        {
            public bool Success;
            public string Text;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private static readonly HttpClient Direct = new HttpClient(new HttpClientHandler
        { UseProxy = false, AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
#endif

        public static bool IsLocalServer(Uri uri)
        {
            if (uri.IsLoopback) return true;
            if (!IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address)) return false;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            var bytes = address.GetAddressBytes();
            if (bytes.Length == 4) return bytes[0] == 10 || bytes[0] == 192 && bytes[1] == 168 ||
                bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 || bytes[0] == 169 && bytes[1] == 254;
            return address.IsIPv6LinkLocal || (bytes[0] & 0xfe) == 0xfc;
        }

        public static IEnumerator Post(Uri uri, byte[] body, string contentType, string connectionId, Action<Reply> completed)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (IsLocalServer(uri))
            {
                using (var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        var task = SendDirect(uri, body, contentType, connectionId, cancellation.Token);
                        while (!task.IsCompleted) yield return null;
                        completed(task.Status == TaskStatus.RanToCompletion ? task.Result : new Reply());
                    }
                    finally { cancellation.Cancel(); }
                }
                yield break;
            }
#endif
            using (var request = new UnityWebRequest(uri.AbsoluteUri, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", contentType);
                if (connectionId != null) request.SetRequestHeader("X-Dwsg-Connection", connectionId);
                request.timeout = 15;
                yield return request.SendWebRequest();
                completed(new Reply { Success = request.result == UnityWebRequest.Result.Success, Text = request.downloadHandler.text });
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private static async Task<Reply> SendDirect(Uri uri, byte[] body, string contentType, string connectionId, CancellationToken cancel)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
                {
                    request.Content = new ByteArrayContent(body);
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                    if (connectionId != null) request.Headers.TryAddWithoutValidation("X-Dwsg-Connection", connectionId);
                    using (var response = await Direct.SendAsync(request, cancel).ConfigureAwait(false))
                        return new Reply { Success = response.IsSuccessStatusCode,
                            Text = await response.Content.ReadAsStringAsync().ConfigureAwait(false) };
                }
            }
            catch (HttpRequestException) { return new Reply(); }
            catch (OperationCanceledException) { return new Reply(); }
        }
#endif
    }
}
