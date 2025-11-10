using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DuelMasters.GUI.Game
{
    /// <summary>
    /// GUI とゲームエンジンをつなぐためのアダプタ層。
    /// 
    /// 現時点では GameState / CardDatabase など実エンジンの具体型が
    /// こちらからは参照できないため、
    /// ・GUIの形
    /// ・進行ボタンのハンドリング
    /// を確認するためのスタブ実装になっています。
    ///
    /// 実際のGameStateに接続する場合は、以下のスタブ実装部分を
    /// あなたの環境の GameState / PlayerState / Card 情報取得処理に
    /// 置き換えてください。
    /// </summary>
    public sealed class GameController
    {
        private int _turn = 1;
        private bool _isPlayerATurn = true;

        private PlayerSnapshot _playerA;
        private PlayerSnapshot _playerB;

        public GameController()
        {
            // 初期値（40枚デッキ・シールド5枚を想定したスタブ）
            _playerA = new PlayerSnapshot(5, 0, 5, new List<string>());
            _playerB = new PlayerSnapshot(5, 0, 5, new List<string>());
        }

        /// <summary>
        /// 初期化処理。
        /// 本来はここで Duelmasters.db と decks/deckA.txt / deckB.txt を読み、
        /// GameState.Create(...) を呼び出して初期状態を構築する想定です。
        /// </summary>
        public Task<(GameSnapshot snapshot, string message)> InitializeAsync()
        {
            // TODO: 実エンジン接続版ではここで GameState を生成し、
            //       Snapshot を GameState から構築してください。
            var snapshot = BuildSnapshot();
            var message = "デュエルを初期化しました。（現在はスタブ実装。GameState接続時はここで初期状態を反映してください）";
            return Task.FromResult((snapshot, message));
        }

        /// <summary>
        /// 「ターン進行」ボタン押下時の処理（簡易進行モード）。
        /// - 手番プレイヤーがカードを1枚ドロー
        /// - ターン開始時点で手札が1枚以上あった場合のみ、1枚をマナチャージ
        ///   （= 手札-1, マナ+1）
        /// という簡易ルールで、視覚的に「ドロー → 条件付きマナチャージ」を再現します。
        /// </summary>
        public Task<(GameSnapshot snapshot, string message)> StepAsync()
        {
            if (_isPlayerATurn)
            {
                var pa = _playerA;

                // ターン開始時点の手札枚数を記録（チャージ可否判定用）
                var hadCardBeforeDraw = pa.HandCount > 0;

                // 1ドロー
                pa = pa with { HandCount = pa.HandCount + 1 };

                // 開始時点で手札が1枚以上あった場合のみ、1枚をマナにチャージ
                if (hadCardBeforeDraw)
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

                var hadCardBeforeDraw = pb.HandCount > 0;

                // 1ドロー
                pb = pb with { HandCount = pb.HandCount + 1 };

                if (hadCardBeforeDraw)
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
            var message = $"ターン{prevTurn}を終了し、ターン{_turn}へ進みました。（スタブ進行: ドロー→条件付きマナチャージ）";
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
