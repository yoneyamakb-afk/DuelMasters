using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DuelMasters.Engine;

namespace DuelMasters.CLI
{
    /// <summary>
    /// trace.json からアクション列を読み取り、GameState に順番に Apply していく簡易リプレイランナー。
    /// - フォーマットは GUI 側が出力している 1 行 1 JSON を想定しています。
    /// - 現時点では ActionType / ActionParam のみを使用し、他の情報は無視します。
    /// </summary>
    internal static class ReplayRunner
    {
        /// <summary>
        /// trace.json からアクション列を読み込み、指定された初期 GameState に順次適用します。
        /// </summary>
        public static GameState Run(GameState initial, string tracePath)
        {
            if (!File.Exists(tracePath))
                throw new FileNotFoundException($"trace ファイルが見つかりません: {tracePath}", tracePath);

            var state = initial;

            foreach (var cmd in LoadCommands(tracePath))
            {
                var intent = ToActionIntent(cmd);
                state = state.Apply(intent);
            }

            return state;
        }

        /// <summary>
        /// trace.json を 1 行ずつ読み取り、ActionType / ActionParam だけを抜き出した中間表現に変換します。
        /// </summary>
        private static IEnumerable<ReplayCommand> LoadCommands(string tracePath)
        {
            foreach (var line in File.ReadLines(tracePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(line);
                }
                catch
                {
                    // 壊れた行はスキップ
                    continue;
                }

                using (doc)
                {
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("ActionType", out var typeProp))
                        continue;

                    var typeName = typeProp.GetString();
                    if (string.IsNullOrWhiteSpace(typeName))
                        continue;

                    int? param = null;
                    if (root.TryGetProperty("ActionParam", out var paramProp))
                    {
                        if (paramProp.ValueKind == JsonValueKind.Number && paramProp.TryGetInt32(out var p))
                            param = p;
                    }

                    yield return new ReplayCommand
                    {
                        ActionType = typeName!,
                        ActionParam = param
                    };
                }
            }
        }

        /// <summary>
        /// JSON から読み込んだ ReplayCommand を Engine の ActionIntent に変換します。
        /// </summary>
        private static ActionIntent ToActionIntent(ReplayCommand cmd)
        {
            if (!Enum.TryParse<ActionType>(cmd.ActionType, ignoreCase: true, out var type))
                throw new InvalidOperationException($"不明な ActionType: {cmd.ActionType}");

            var param = cmd.ActionParam ?? 0;
            return new ActionIntent(type, param);
        }


        /// <summary>
        /// trace.json の 1 行に対応する、最小限の情報を持ったコマンド表現。
        /// </summary>
        private sealed class ReplayCommand
        {
            public string ActionType { get; set; } = string.Empty;
            public int? ActionParam { get; set; }
        }
    }
}
