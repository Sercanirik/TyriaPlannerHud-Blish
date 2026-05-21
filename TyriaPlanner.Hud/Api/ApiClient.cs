using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Newtonsoft.Json;
namespace TyriaPlanner.Hud.Api
{
    public sealed class ApiClient : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<ApiClient>();
        private readonly HttpClient _http;
        public ApiClient()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15),
            };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("TyriaPlanner.Hud/0.2 (Blish HUD)");
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<string> ExchangeAsync(string baseUrl, string gw2ApiKey, CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(gw2ApiKey) || string.IsNullOrWhiteSpace(baseUrl)) return null;
            var body = JsonConvert.SerializeObject(new
            {
                gw2_api_key = gw2ApiKey,
                client_id = "tyria-hud-blish",
                label = "Blish HUD",
            });
            using (var req = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/api/addon/exchange"))
            {
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                using (var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancel)
                    .ConfigureAwait(false))
                {
                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        res.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var errBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Logger.Warn("Tyria Planner addon exchange rejected: {0} {1}", (int)res.StatusCode, errBody);
                        return null;
                    }
                    res.EnsureSuccessStatusCode();
                    var payload = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var parsed = JsonConvert.DeserializeObject<ExchangeResponse>(payload);
                    return parsed?.access_token;
                }
            }
        }
        private class ExchangeResponse
        {
            public string access_token { get; set; }
        }
        public async Task<UpcomingResponse> FetchBrowseAsync(
            string baseUrl,
            string bearer,
            CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(bearer)) return null;
            using (var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + "/api/addon/browse"))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                using (var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancel)
                    .ConfigureAwait(false))
                {
                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return null;
                    }
                    res.EnsureSuccessStatusCode();
                    var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<UpcomingResponse>(body);
                }
            }
        }
        public async Task<UpcomingResponse> FetchUpcomingAsync(
            string baseUrl,
            string bearer,
            DateTimeOffset? since,
            CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(bearer)) return null;
            var url = baseUrl.TrimEnd('/') + "/api/addon/upcoming";
            if (since.HasValue)
            {
                url += "?since=" + Uri.EscapeDataString(since.Value.UtcDateTime.ToString("o"));
            }
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                using (var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancel)
                    .ConfigureAwait(false))
                {
                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return null;
                    }
                    res.EnsureSuccessStatusCode();
                    var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<UpcomingResponse>(body);
                }
            }
        }
        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
