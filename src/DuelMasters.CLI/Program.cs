using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Immutable;
using DuelMasters.Engine;
using DuelMasters.Engine.Integration.M11;

namespace DuelMasters.CLI;

internal static class Program
{
    private static bool _traceConsole = false;
    private static string? _traceJsonPath = null;
    private static string _traceLevel = "basic"; // basic|detail|debug
    private static bool _manual = false;         // 手動対戦モードフラグ
    private static bool _replayGui = false;      // GUIトレースリプレイモードフラグ
    private static CardDatabase? _cardDb;        // カードDB（あれば）

    // ---- イベントトレース (EngineHooks.OnEvent をラップ) ----
    private static void InstallTracing()
    {
        var prev = EngineHooks.OnEvent;
        EngineHooks.OnEvent = ev =>
        {
            if (_traceConsole)
            {
                Console.WriteLine($"[M11] {ev.TimestampUtc:O} {ev.Kind} フェイズ={ev.PhaseName ?? "-"} 手番プレイヤー={ev.ActivePlayerId?.ToString() ?? "-"}");
            }

            if (!string.IsNullOrEmpty(_traceJsonPath))
            {
                try
                {
                    var line = JsonSerializer.Serialize(ev, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    File.AppendAllText(_traceJsonPath!, line + Environment.NewLine);
                }
                catch { /* ログ出力失敗は無視 */ }
            }

            prev?.Invoke(ev);
        };
    }

    // ---- コマンドライン引数 ----
    private static void ParseArgs(string[] args)
    {
        foreach (var a in args)
        {
            if (a.Equals("--trace", StringComparison.OrdinalIgnoreCase))
            {
                _traceConsole = true;
            }
            else if (a.StartsWith("--trace-json=", StringComparison.OrdinalIgnoreCase))
            {
                _traceJsonPath = a.Substring("--trace-json=".Length).Trim('"');
                if (string.IsNullOrWhiteSpace(_traceJsonPath)) _traceJsonPath = "trace.jsonl";
                try { File.WriteAllText(_traceJsonPath!, string.Empty); } catch { }
            }
            else if (a.StartsWith("--trace-level=", StringComparison.OrdinalIgnoreCase))
            {
                _traceLevel = a.Substring("--trace-level=".Length).Trim().ToLowerInvariant();
                if (_traceLevel != "basic" && _traceLevel != "detail" && _traceLevel != "debug") _traceLevel = "basic";
            }
            else if (a.Equals("--manual", StringComparison.OrdinalIgnoreCase))
            {
                // 一人二役の手動対戦モード
                _manual = true;
            }
            else if (a.Equals("--replay-gui", StringComparison.OrdinalIgnoreCase))
            {
                // GUI が artifacts/replays/trace.json に出力したトレースをリプレイするモード
                _replayGui = true;
            }
        }
    }

    // ---- スナップショット / ヘルパ ----
    private sealed class Snapshot
    {
        public int Turn { get; init; }
        public string Phase { get; init; } = "";
        public int ActivePlayer { get; init; }
        public int PriorityPlayer { get; init; }
        public int ConsecutivePasses { get; init; }
        public int StackCount { get; init; }
        public int PendingTriggers { get; init; }
    }

    private static Snapshot Take(GameState g)
    {
        int stackCount;
        try
        {
            stackCount = g.Stack.Count;
        }
        catch
        {
            stackCount = 0;
        }

        int triggers = 0;
        try { triggers = g.PendingTriggers.Length; } catch { triggers = 0; }

        return new Snapshot
        {
            Turn = g.TurnNumber,
            Phase = g.Phase.ToString(),
            ActivePlayer = g.ActivePlayer.Value,
            PriorityPlayer = g.PriorityPlayer.Value,
            ConsecutivePasses = g.ConsecutivePasses,
            StackCount = stackCount,
            PendingTriggers = triggers
        };
    }

    private static void EmitStepTrace(string title, Snapshot before, Snapshot after)
    {
        void diffLine(string key, object a, object b)
        {
            if (!Equals(a, b) && _traceConsole && (_traceLevel == "detail" || _traceLevel == "debug"))
                Console.WriteLine($"  - {key}: {a} -> {b}");
        }

        if (_traceConsole)
            Console.WriteLine($"[ステップ] {title}");

        diffLine("ターン", before.Turn, after.Turn);
        diffLine("フェイズ", before.Phase, after.Phase);
        diffLine("手番プレイヤー", before.ActivePlayer, after.ActivePlayer);
        diffLine("優先権プレイヤー", before.PriorityPlayer, after.PriorityPlayer);
        diffLine("連続パス回数", before.ConsecutivePasses, after.ConsecutivePasses);
        diffLine("スタックの枚数", before.StackCount, after.StackCount);
        diffLine("未処理トリガー数", before.PendingTriggers, after.PendingTriggers);

        if (!string.IsNullOrEmpty(_traceJsonPath))
        {
            try
            {
                var obj = new { kind = "step", title, before, after };
                File.AppendAllText(_traceJsonPath!, JsonSerializer.Serialize(obj) + Environment.NewLine);
            }
            catch { }
        }

        if (_traceConsole && _traceLevel == "debug")
        {
            Console.WriteLine($"  before: Turn={before.Turn}, Phase={before.Phase}, AP={before.ActivePlayer}, PP={before.PriorityPlayer}, Passes={before.ConsecutivePasses}, Stack={before.StackCount}, Trig={before.PendingTriggers}");
            Console.WriteLine($"  after : Turn={after.Turn}, Phase={after.Phase}, AP={after.ActivePlayer}, PP={after.PriorityPlayer}, Passes={after.ConsecutivePasses}, Stack={after.StackCount}, Trig={after.PendingTriggers}");
        }
    }

    private static Deck MakeDeck(int[] ids)
    {
        return new Deck(ImmutableArray.Create<CardId>(ids.Select(i => new CardId(i)).ToArray()));
    }

    // ---- デッキ一覧表示 ----
    private static void PrintDeckList(string label, int[] ids)
    {
        Console.WriteLine();
        Console.WriteLine($"[{label}]");

        if (_cardDb == null)
        {
            Console.WriteLine("  ※カードDBが読み込めなかったため、IDのみ表示します。");
            foreach (var id in ids)
            {
                Console.WriteLine($"  ID={id}");
            }
            return;
        }

        foreach (var id in ids)
        {
            var summary = _cardDb.GetCardSummary(id);
            Console.WriteLine($"  ID={id} : {summary}");
        }
    }

    // ---- デッキファイル読み込み ----
    // solutionRoot\decks\deckA.txt / deckB.txt を探す
    // 1行1つの face_id（整数）。不正行は無視。
    private static int[] LoadDeckIds(string solutionRoot, string label, string fileName, int[] fallback)
    {
        var decksDir = Path.Combine(solutionRoot, "decks");
        var path = Path.Combine(decksDir, fileName);

        if (!File.Exists(path))
        {
            Console.WriteLine($"[Deck] {label} 用のデッキファイルが見つかりませんでした。既定デッキを使用します: {path}");
            return fallback;
        }

        try
        {
            var lines = File.ReadAllLines(path);
            var ids = lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l =>
                {
                    if (int.TryParse(l, out var v))
                    {
                        return (ok: true, value: v, raw: l);
                    }
                    else
                    {
                        Console.WriteLine($"[Deck] {label}: 数値として解釈できない行をスキップします: \"{l}\"");
                        return (ok: false, value: 0, raw: l);
                    }
                })
                .Where(x => x.ok)
                .Select(x => x.value)
                .ToArray();

            if (ids.Length == 0)
            {
                Console.WriteLine($"[Deck] {label}: 有効なIDが1つもありませんでした。既定デッキを使用します。");
                return fallback;
            }

            Console.WriteLine($"[Deck] {label}: {path} から {ids.Length} 枚読み込みました。");
            return ids;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Deck] {label}: 読み込みに失敗しました: {ex.Message} 既定デッキを使用します。");
            return fallback;
        }
    }

    // ---- 自動デモモード ----
    private static void RunAuto(GameState game)
    {
        Console.WriteLine("=== 自動デュエルデモ（従来動作） ===");
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"-- ターン {game.TurnNumber} -- 手番プレイヤー={game.ActivePlayer.Value}");

            var actions = game.GenerateLegalActions(game.PriorityPlayer).ToList();
            Console.WriteLine($"選択可能な行動数: {actions.Count}");
            if (actions.Count == 0) break;

            var before = Take(game);
            var first = actions.First();
            game = game.Apply(first);
            var after = Take(game);
            EmitStepTrace($"行動適用 ({first.Type})", before, after);
        }

        Console.WriteLine("=== 自動デュエル終了 ===");
        if (!string.IsNullOrEmpty(_traceJsonPath))
        {
            Console.WriteLine($"トレース出力先: {_traceJsonPath}");
        }
    }

    // ---- 手動入力用ヘルパ ----
    private static int ReadActionIndex(int max)
    {
        while (true)
        {
            Console.Write($"行動番号を入力してください (0〜{max - 1}、未入力なら0): ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                return 0;
            }

            if (int.TryParse(line, out var idx) && idx >= 0 && idx < max)
            {
                return idx;
            }

            Console.WriteLine("入力が不正です。番号を入力し直してください。");
        }
    }

    // ---- 手動対戦モード（一人二役） ----
    private static void RunManual(GameState game)
    {
        Console.WriteLine("=== 手動デュエルモード（一人で両プレイヤーを操作） ===");

        for (int step = 0; step < 100; step++)
        {
            Console.WriteLine();
            Console.WriteLine($"== ステップ {step + 1} ==");
            Console.WriteLine($"ターン={game.TurnNumber}, フェイズ={game.Phase}, 手番プレイヤー={game.ActivePlayer.Value}, 優先権プレイヤー={game.PriorityPlayer.Value}");

            var actions = game.GenerateLegalActions(game.PriorityPlayer).ToList();
            if (actions.Count == 0)
            {
                Console.WriteLine("選択可能な行動がありません。デュエルを終了します。");
                break;
            }

            Console.WriteLine("選択可能な行動一覧:");
            for (int i = 0; i < actions.Count; i++)
            {
                Console.WriteLine($"  {i}: {actions[i].Type}");
            }

            var before = Take(game);
            var idx = ReadActionIndex(actions.Count);
            var chosen = actions[idx];

            Console.WriteLine($"選択した行動: #{idx} {chosen.Type}");

            game = game.Apply(chosen);
            var after = Take(game);
            EmitStepTrace($"行動適用 ({chosen.Type})", before, after);
        }

        Console.WriteLine("=== 手動デュエル終了 ===");
        if (!string.IsNullOrEmpty(_traceJsonPath))
        {
            Console.WriteLine($"トレース出力先: {_traceJsonPath}");
        }
    }

    // ---- GUI トレースリプレイモード ----
    private static void RunReplayFromGuiTrace(GameState initial, string solutionRoot)
    {
        Console.WriteLine("=== GUIトレース リプレイモード ===");
        var tracePath = Path.Combine(solutionRoot, "artifacts", "replays", "trace.json");
        Console.WriteLine($"trace ファイル: {tracePath}");

        if (!File.Exists(tracePath))
        {
            Console.WriteLine("trace.json が見つかりませんでした。");
            return;
        }

        try
        {
            // ★ ここにあった initial = initial.With... の4行は削除してOK

            var before = Take(initial);
            var finalState = ReplayRunner.Run(initial, tracePath);
            var after = Take(finalState);

            EmitStepTrace("GUIトレース再生 完了", before, after);
            Console.WriteLine($"リプレイ完了: 最終ターン={finalState.TurnNumber}, 手番プレイヤー={finalState.ActivePlayer.Value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"リプレイ中にエラーが発生しました: {ex.Message}");
        }
    }



    private static void Main(string[] args)
    {
        ParseArgs(args);
        InstallTracing();

        Console.WriteLine("=== デュエルマスターズ シミュレーター CLI (M11.6 / M29+M31) ===");
        string modeText;
        if (_replayGui)
            modeText = "モード: GUIトレースリプレイ";
        else if (_manual)
            modeText = "モード: 手動（あなたが両方のプレイヤーを操作します）";
        else
            modeText = "モード: 自動デモ";

        Console.WriteLine(modeText);

        // 実行ディレクトリ（bin\Debug\net8.0）から 5階層上に戻ってルートの Duelmasters.db を参照する
        var baseDir = AppContext.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(
            baseDir, "..", "..", "..", "..", ".."));
        var dbPath = Path.Combine(solutionRoot, "Duelmasters.db");

        if (CardDatabase.TryLoad(dbPath, out var db))
        {
            _cardDb = db;
        }
        else
        {
            _cardDb = null;
        }

        // 既定デッキ（DBに存在することが確認済みのID群）
        var defaultDeckAIds = new[] { 1, 3, 4, 5, 9 };
        var defaultDeckBIds = new[] { 6, 7, 8, 9, 10 };

        // decks\deckA.txt / deckB.txt があればそちらを優先
        var deckAIds = LoadDeckIds(solutionRoot, "プレイヤーA", "deckA.txt", defaultDeckAIds);
        var deckBIds = LoadDeckIds(solutionRoot, "プレイヤーB", "deckB.txt", defaultDeckBIds);

        var deckA = MakeDeck(deckAIds);
        var deckB = MakeDeck(deckBIds);

        Console.WriteLine();
        Console.WriteLine("デュエル開始前にデッキを確認します。");

        PrintDeckList("プレイヤーA", deckAIds);
        PrintDeckList("プレイヤーB", deckBIds);

        var game = GameState.Create(deckA, deckB, seed: 1234);

        Console.WriteLine();
        Console.WriteLine($"ゲーム初期化完了: ターン {game.TurnNumber}, 手番プレイヤー={game.ActivePlayer.Value}");

        if (_replayGui)
        {
            RunReplayFromGuiTrace(game, solutionRoot);
        }
        else if (_manual)
        {
            RunManual(game);
        }
        else
        {
            RunAuto(game);
        }
    }
}
