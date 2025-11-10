using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DuelMasters.Engine;

namespace DuelMasters.GUI.Game
{
    /// <summary>
    /// 実エンジン (DuelMasters.Engine.GameState) と GUI をつなぐ IGameController 実装。
    /// GameState のゾーン構造から、各プレイヤーの手札 / マナ / シールド / 山札 / バトルゾーン枚数を
    /// PlayerSnapshot に反映します。
    ///
    /// StepAsync は「1ターン分の自動進行」を担当し、
    /// - ターン開始時に手番プレイヤーが 1 ドロー
    /// - 優先権プレイヤーの合法行動を、ターンが切り替わるまで自動で適用
    /// という処理を行います。
    /// </summary>
    public sealed class EngineGameController : IGameController
    {
        private GameState? _game;
        private string _solutionRoot = string.Empty;
        private CardDatabase? _cardDb;

        public Task<(GameSnapshot snapshot, string message)> InitializeAsync()
        {
            var baseDir = AppContext.BaseDirectory;
            _solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

            // カードDB読み込み（あればカード名を表示するために使用）
            var dbPath = Path.Combine(_solutionRoot, "Duelmasters.db");
            if (CardDatabase.TryLoad(dbPath, out var db))
            {
                _cardDb = db;
                Console.WriteLine($"[GUI-CardDB] 読み込み成功: {dbPath}");
            }
            else
            {
                _cardDb = null;
                Console.WriteLine($"[GUI-CardDB] 読み込み失敗: {dbPath}");
            }


            var defaultDeckAIds = new[] { 1, 3, 4, 5, 9 };
            var defaultDeckBIds = new[] { 6, 7, 8, 9, 10 };

            var deckAIds = LoadDeckIds("プレイヤーA", "deckA.txt", defaultDeckAIds);
            var deckBIds = LoadDeckIds("プレイヤーB", "deckB.txt", defaultDeckBIds);

            var deckA = MakeDeck(deckAIds);
            var deckB = MakeDeck(deckBIds);

            _game = GameState.Create(deckA, deckB, seed: 1234);

            var snapshot = MakeSnapshot(_game);
            var message = $"GameState 初期化完了: ターン={_game.TurnNumber}, 手番プレイヤー={_game.ActivePlayer.Value}";
            return Task.FromResult((snapshot, message));
        }

        /// <summary>
        /// 1回呼び出すごとに「現在の手番プレイヤーのターンを最後まで進める」簡易モード。
        /// - 開始時に手番プレイヤーが1ドロー
        /// - 優先権プレイヤーの行動を自動選択し、ターン番号が変わるまで適用
        /// </summary>
        public Task<(GameSnapshot snapshot, string message)> StepAsync()
        {
            if (_game == null)
                return Task.FromResult((MakeSnapshot(null), "GameState が初期化されていません。"));

            var startTurn = _game.TurnNumber;
            var startPlayer = _game.ActivePlayer;

            // 1. ターン開始時の 1 ドロー（簡易ルール）
            _game = DrawFor(_game, startPlayer);

            int steps = 0;
            const int maxStepsPerTurn = 100;

            while (steps < maxStepsPerTurn)
            {
                steps++;

                // ゲーム終了していそうなら抜ける（GameOverResult は null でないときに決着）
                if (_game.GameOverResult.HasValue)
                    break;

                var actions = _game.GenerateLegalActions(_game.PriorityPlayer).ToList();
                if (actions.Count == 0)
                {
                    break;
                }

                var chosen = ChooseAction(actions);
                _game = _game.Apply(chosen);

                // ターン番号が変わったら、このターンは終了とみなす
                if (_game.TurnNumber != startTurn)
                {
                    break;
                }
            }

            var snapshot = MakeSnapshot(_game);
            var message = $"ターン {startTurn} を {steps} ステップ進めました。現在ターン={_game.TurnNumber}, 手番プレイヤー={_game.ActivePlayer.Value}";
            return Task.FromResult((snapshot, message));
        }

        /// <summary>
        /// 自動選択用の簡易ポリシー:
        /// - SummonDummyFromHand / BuffOwnCreature / DestroyOpponentCreature / ResolveTop を優先
        /// - それ以外が無ければ PassPriority
        /// </summary>
        private static ActionIntent ChooseAction(IReadOnlyList<ActionIntent> actions)
        {
            // 優先度順に探す
            ActionType[] priority =
            {
                ActionType.SummonDummyFromHand,
                ActionType.BuffOwnCreature,
                ActionType.DestroyOpponentCreature,
                ActionType.ResolveTop
            };

            foreach (var t in priority)
            {
                var found = actions.FirstOrDefault(a => a.Type == t);
                if (!EqualityComparer<ActionIntent>.Default.Equals(found, default))
                    return found;
            }

            // どれも無ければ PassPriority（必ず先頭に入っている）
            return actions[0];
        }

        // ===== ヘルパ =====

        private Deck MakeDeck(int[] ids)
        {
            var cards = ids.Select(i => new CardId(i)).ToArray();
            return new Deck(ImmutableArray.Create(cards));
        }

        private int[] LoadDeckIds(string label, string fileName, int[] fallback)
        {
            var decksDir = Path.Combine(_solutionRoot, "decks");
            var path = Path.Combine(decksDir, fileName);

            if (!File.Exists(path))
            {
                Console.WriteLine($"[GUI-Deck] {label} 用のデッキファイルが見つかりませんでした。既定デッキを使用します: {path}");
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
                            return (ok: true, value: v);
                        Console.WriteLine($"[GUI-Deck] {label}: 数値として解釈できない行をスキップします: {l}");
                        return (ok: false, value: 0);
                    })
                    .Where(x => x.ok)
                    .Select(x => x.value)
                    .ToArray();

                if (ids.Length == 0)
                {
                    Console.WriteLine($"[GUI-Deck] {label}: 有効なIDが1つもありませんでした。既定デッキを使用します。");
                    return fallback;
                }

                Console.WriteLine($"[GUI-Deck] {label}: {path} から {ids.Length} 枚読み込みました。");
                return ids;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GUI-Deck] {label}: 読み込みに失敗しました: {ex.Message} 既定デッキを使用します。");
                return fallback;
            }
        }

        /// <summary>
        /// GameState.Draw(PlayerId) とほぼ同等の処理を、外側から呼べるようにしたヘルパ。
        /// デッキ切れ時の勝敗判定は簡略化し、ここでは何もしないでそのまま返します。
        /// </summary>
        private GameState DrawFor(GameState state, PlayerId p)
        {
            var ps = state.Players[p.Value];
            if (ps.Deck.Cards.Length == 0)
                return state; // デッキ切れ時の敗北処理はここでは行わない

            var top = ps.Deck.Cards[0];
            var nd = new Zone(ZoneKind.Deck, ps.Deck.Cards.RemoveAt(0));
            var nh = new Zone(ZoneKind.Hand, ps.Hand.Cards.Add(top));
            var np = ps with { Deck = nd, Hand = nh };
            return state.With(players: state.Players.SetItem(p.Value, np));
        }

        private GameSnapshot MakeSnapshot(GameState? g)
        {
            if (g == null)
            {
                return new GameSnapshot(
                    Turn: 0,
                    Phase: "未初期化",
                    ActivePlayer: "-",
                    PlayerA: new PlayerSnapshot(0, 0, 0, 0, new List<string>()),
                    PlayerB: new PlayerSnapshot(0, 0, 0, 0, new List<string>())
                );
            }

            // PlayerState は配列の 0 番目を先攻(A)、1 番目を後攻(B) とみなす
            var pA = g.Players[0];
            var pB = g.Players[1];

            var playerASnapshot = new PlayerSnapshot(
                HandCount: pA.Hand.Cards.Length,
                ManaCount: pA.Mana.Cards.Length,
                ShieldCount: pA.Shield.Cards.Length,
                DeckCount: pA.Deck.Cards.Length,
                BattleZoneCards: MakeBattleZoneList(pA)
            );

            var playerBSnapshot = new PlayerSnapshot(
                HandCount: pB.Hand.Cards.Length,
                ManaCount: pB.Mana.Cards.Length,
                ShieldCount: pB.Shield.Cards.Length,
                DeckCount: pB.Deck.Cards.Length,
                BattleZoneCards: MakeBattleZoneList(pB)
            );

            var v = g.ActivePlayer.Value;
            string activeName;
            if (v == 0 || v == 1)
            {
                activeName = v == 0 ? "プレイヤーA" : "プレイヤーB";
            }
            else if (v == 2)
            {
                activeName = "プレイヤーB";
            }
            else
            {
                activeName = "-";
            }

            return new GameSnapshot(
                Turn: g.TurnNumber,
                Phase: g.Phase.ToString(),
                ActivePlayer: activeName,
                PlayerA: playerASnapshot,
                PlayerB: playerBSnapshot
            );
        }

        private IReadOnlyList<string> MakeBattleZoneList(PlayerState player)
        {
            var cards = player.Battle.Cards;
            var count = cards.Length;
            if (count == 0) return Array.Empty<string>();

            // カードDBがあればカード名を表示、なければ ID のみ表示
            if (_cardDb == null)
            {
                var fallback = new List<string>(count);
                for (int i = 0; i < count; i++)
                {
                    fallback.Add($"ID={cards[i].Value}");
                }
                return fallback;
            }

            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(_cardDb.GetCardSummary(cards[i].Value));
            }

            return list;
        }
    }
}
