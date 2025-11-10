using System;
using System.IO;
using System.Text.Json;

namespace DuelMasters.GUI.Game
{
    /// <summary>
    /// artifacts/replays/trace.json に 1 行 1 JSON のリプレイトレースを書き出すクラス。
    /// 例: {"Turn":0,"Phase":"Main","ActivePlayerId":0,...}
    /// </summary>
    internal sealed class ReplayRecorder
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _options;

        public string FilePath => _filePath;

        public ReplayRecorder(string solutionRoot)
        {
            var dir = Path.Combine(solutionRoot, "artifacts", "replays");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "trace.json");

            // 起動ごとにファイルをクリア
            try
            {
                File.WriteAllText(_filePath, string.Empty);
            }
            catch
            {
                // ログ出力失敗は無視（GUI 動作を止めない）
            }

            _options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public void Append(ReplayTraceEntry entry)
        {
            try
            {
                var json = JsonSerializer.Serialize(entry, _options);
                File.AppendAllText(_filePath, json + Environment.NewLine);
            }
            catch
            {
                // ログ出力失敗は無視
            }
        }
    }
}
