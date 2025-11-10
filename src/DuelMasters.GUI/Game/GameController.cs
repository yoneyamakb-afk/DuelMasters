using System.Collections.Generic;
using System.Threading.Tasks;

namespace DuelMasters.GUI.Game
{
    /// <summary>
    /// GUI とゲームエンジンをつなぐためのアダプタ層（簡易スタブ版）。
    ///
    /// 実際の GameState / CardDatabase に接続する場合は、
    /// InitializeAsync / StepAsync 内の処理をあなたの環境の実装に差し替えてください。
    /// </summary>
    public sealed class GameController : IGameController
    {
        private int _turn = 1;
        private bool _isPlayerATurn = true;

        private PlayerSnapshot _playerA;
        private PlayerSnapshot _playerB;

        public GameController()
        {
            // 40枚デッキ・手札5・シールド5 を想定したスタブ初期値
            // 山札 = 40 - 5(手札) - 5(シールド) = 30
            _playerA = new PlayerSnapshot(5, 0, 5, 30, new List<string>());
            _playerB = new PlayerSnapshot(5, 0, 5, 30, new List<string>());
        }

        /// <summary>
        /// 初期化処理。
        /// 本来はここで Duelmasters.db と decks/deckA.txt / deckB.txt を読み、
        /// GameState.Create(...) を呼び出して初期状態を構築する想定です。
        /// </summary>
        public Task<(GameSnapshot snapshot, string message)> InitializeAsync()
        {
            var snapshot = BuildSnapshot();
            var message = "デュエルを初期化しました。（現在はスタブ実装。GameState接続時はここで初期状態を反映してください）";
            return Task.FromResult((snapshot, message));
        }

        /// <summary>
        /// 「ターン進行」ボタン押下時の処理（簡易進行モード）。
        /// - 山札が残っていれば1ドロー（山札-1, 手札+1）
        /// - ドロー後の手札が1枚以上なら、その中から1枚をマナチャージ（手札-1, マナ+1）
        /// という簡易ルールで、ドロー→マナチャージの流れを再現します。
        /// </summary>
        public Task<(GameSnapshot snapshot, string message)> StepAsync()
        {
            if (_isPlayerATurn)
            {
                var pa = _playerA;

                // 山札があれば1ドロー
                if (pa.DeckCount > 0)
                {
                    pa = pa with
                    {
                        DeckCount = pa.DeckCount - 1,
                        HandCount = pa.HandCount + 1
                    };
                }

                // ドロー後に手札が1枚以上あれば1枚チャージ
                if (pa.HandCount > 0)
                {
                    pa = pa with
                    {
                        HandCount = pa.HandCount - 1,
                        ManaCount = pa.ManaCount + 1
                    };
                }

                _playerA = pa;
            }
            else
            {
                var pb = _playerB;

                if (pb.DeckCount > 0)
                {
                    pb = pb with
                    {
                        DeckCount = pb.DeckCount - 1,
                        HandCount = pb.HandCount + 1
                    };
                }

                if (pb.HandCount > 0)
                {
                    pb = pb with
                    {
                        HandCount = pb.HandCount - 1,
                        ManaCount = pb.ManaCount + 1
                    };
                }

                _playerB = pb;
            }

            var prevTurn = _turn;
            _turn++;
            _isPlayerATurn = !_isPlayerATurn;

            var snapshot = BuildSnapshot();
            var message = $"ターン{prevTurn}を終了し、ターン{_turn}へ進みました。（スタブ進行: ドロー→マナチャージ）";
            return Task.FromResult((snapshot, message));
        }

        private GameSnapshot BuildSnapshot()
        {
            var phase = "メイン"; // 簡易表記。実エンジン接続時はGameStateから取得。
            var activePlayer = _isPlayerATurn ? "プレイヤーA" : "プレイヤーB";

            return new GameSnapshot(
                Turn: _turn,
                Phase: phase,
                ActivePlayer: activePlayer,
                PlayerA: _playerA,
                PlayerB: _playerB
            );
        }
    }

    /// <summary>
    /// GUIに渡すプレイヤー状態のスナップショット。
    /// </summary>
    public sealed record PlayerSnapshot(
        int HandCount,
        int ManaCount,
        int ShieldCount,
        int DeckCount,
        IReadOnlyList<string> BattleZoneCards
    );

    /// <summary>
    /// GUIに渡すゲーム全体のスナップショット。
    /// </summary>
    public sealed record GameSnapshot(
        int Turn,
        string Phase,
        string ActivePlayer,
        PlayerSnapshot PlayerA,
        PlayerSnapshot PlayerB
    );
}
