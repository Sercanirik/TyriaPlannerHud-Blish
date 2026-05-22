using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using TyriaPlanner.Hud.Settings;

namespace TyriaPlanner.Hud.Services
{
    // Listens to /api/addon/stream for server-pushed "refresh" events.
    // Each event triggers the caller's onRefresh callback · the poller
    // uses it to do an out-of-cycle fetch so users see new guild events
    // within a second instead of waiting up to 45 s for the next poll.
    //
    // Resilient by design · drops on auth/network failure, exponential
    // backoff up to 5 minutes, automatic reconnect. The polling fallback
    // keeps the addon working even when the stream never establishes.
    public sealed class StreamSubscriber : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<StreamSubscriber>();

        private readonly ModuleSettings _settings;
        private readonly Action _onRefresh;
        private CancellationTokenSource _cancel;
        private Task _loop;

        public StreamSubscriber(ModuleSettings settings, Action onRefresh)
        {
            _settings = settings;
            _onRefresh = onRefresh;
        }

        public void Start()
        {
            Stop();
            _cancel = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_cancel.Token));
        }

        public void Stop()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
                _cancel.Dispose();
                _cancel = null;
            }
            _loop = null;
        }

        private async Task RunAsync(CancellationToken cancel)
        {
            int backoffSeconds = 5;
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    await ListenOnceAsync(cancel).ConfigureAwait(false);
                    backoffSeconds = 5;
                }
                catch (TaskCanceledException) { return; }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Stream disconnected; reconnecting in {0}s", backoffSeconds);
                }

                try { await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancel).ConfigureAwait(false); }
                catch (TaskCanceledException) { return; }
                backoffSeconds = Math.Min(backoffSeconds * 2, 300);
            }
        }

        private async Task ListenOnceAsync(CancellationToken cancel)
        {
            var baseUrl = _settings.ApiBaseUrl.Value;
            var bearer = _settings.CachedBearer.Value;
            if (string.IsNullOrWhiteSpace(bearer) || string.IsNullOrWhiteSpace(baseUrl))
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cancel).ConfigureAwait(false);
                return;
            }

            // Long timeout for the stream itself · the SSE connection is
            // supposed to stay open. Heartbeats from the server keep
            // intermediate proxies happy.
            using (var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("TyriaPlanner.Hud-Stream/0.9 (Blish HUD)");
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                using (var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + "/api/addon/stream"))
                {
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                    using (var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false))
                    {
                        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                            res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            Logger.Warn("Stream auth rejected ({0}); polling fallback will keep working.", res.StatusCode);
                            await Task.Delay(TimeSpan.FromSeconds(60), cancel).ConfigureAwait(false);
                            return;
                        }
                        res.EnsureSuccessStatusCode();

                        using (var stream = await res.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var reader = new StreamReader(stream))
                        {
                            string line;
                            string currentEvent = null;
                            while (!cancel.IsCancellationRequested && (line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                            {
                                if (line.StartsWith(":"))
                                {
                                    continue;
                                }
                                if (line.StartsWith("event:"))
                                {
                                    currentEvent = line.Substring(6).Trim();
                                    continue;
                                }
                                if (line.StartsWith("data:"))
                                {
                                    if (currentEvent == "refresh")
                                    {
                                        try { _onRefresh?.Invoke(); }
                                        catch (Exception ex) { Logger.Warn(ex, "Stream callback threw."); }
                                    }
                                    currentEvent = null;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
