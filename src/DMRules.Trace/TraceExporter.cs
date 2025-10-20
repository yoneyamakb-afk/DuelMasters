using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DMRules.Trace
{
    /// <summary>
    /// 依存最小のJSONLトレースライター。環境変数 DM_TRACE=1 で有効化。
    /// </summary>
    public static class TraceExporter
    {
        private static readonly BlockingCollection<object> _queue = new(new ConcurrentQueue<object>());
        private static Task? _pump;
        private static CancellationTokenSource? _cts;
        private static string? _filePath;
        private static volatile bool _enabled;
        private static readonly object _initLock = new();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public static void Initialize(TraceOptions? options = null)
        {
            lock (_initLock)
            {
                if (_pump != null) return; // already started

                var enabled = options?.Enabled ?? IsTruthy(Environment.GetEnvironmentVariable("DM_TRACE"));
                _enabled = enabled;
                if (!enabled) return;

                string dir = options?.OutputDir
                    ?? Environment.GetEnvironmentVariable("DM_TRACE_DIR")
                    ?? Path.Combine(Environment.CurrentDirectory, ".trace");
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    // フォールバック（多バイトパス問題等に備える）
                    dir = Path.Combine(Path.GetTempPath(), "dm_trace");
                    Directory.CreateDirectory(dir);
                }

                var prefix = options?.FilePrefix ?? "duel";
                var stamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                _filePath = Path.Combine(dir, $"{prefix}_{stamp}.jsonl");

                _cts = new CancellationTokenSource();
                _pump = Task.Run(() => PumpAsync(_cts.Token));
                AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
            }
        }

        public static void Write(object evt)
        {
            if (!_enabled) return;
            if (_pump == null) Initialize();
            _queue.Add(evt);
        }

        public static void Flush(TimeSpan? timeout = null)
        {
            if (!_enabled || _pump == null) return;
            var t = timeout ?? TimeSpan.FromSeconds(3);
            using var barrier = new ManualResetEventSlim(false);
            _queue.Add(new FlushMarker(barrier));
            barrier.Wait(t);
        }

        public static void Shutdown()
        {
            if (!_enabled) return;
            lock (_initLock)
            {
                try
                {
                    _cts?.Cancel();
                    _queue.CompleteAdding();
                    _pump?.Wait(TimeSpan.FromSeconds(2));
                }
                catch { /* best effort */ }
                finally
                {
                    _cts?.Dispose();
                    _cts = null;
                    _pump = null;
                    _enabled = false;
                }
            }
        }

        private static async Task PumpAsync(CancellationToken ct)
        {
            try
            {
                using var fs = new FileStream(_filePath!, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                foreach (var item in _queue.GetConsumingEnumerable(ct))
                {
                    if (ct.IsCancellationRequested) break;

                    if (item is FlushMarker fm)
                    {
                        try { sw.Flush(); fs.Flush(true); }
                        finally { fm.Barrier.Set(); }
                        continue;
                    }

                    string line = JsonSerializer.Serialize(item, _jsonOptions);
                    await sw.WriteLineAsync(line);
                }

                await sw.FlushAsync();
                fs.Flush(true);
            }
            catch (OperationCanceledException) { /* normal */ }
            catch { /* swallow */ }
        }

        private static bool IsTruthy(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            return s is "1" or "true" or "t" or "on" or "yes" or "y";
        }

        private sealed class FlushMarker
        {
            public System.Threading.ManualResetEventSlim Barrier { get; }
            public FlushMarker(System.Threading.ManualResetEventSlim barrier) => Barrier = barrier;
        }
    }
}
