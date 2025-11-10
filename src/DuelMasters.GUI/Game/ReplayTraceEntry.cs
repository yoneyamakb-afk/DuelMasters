using System.Collections.Generic;

namespace DuelMasters.GUI.Game
{
    /// <summary>
    /// GUI 用の簡易リプレイトレース 1 行分。
    /// 1 行 1 JSON オブジェクトとして trace.json に書き出されます。
    /// </summary>
    internal sealed class ReplayTraceEntry
    {
        // ゲーム状態
        public int Turn { get; set; }
        public string Phase { get; set; } = string.Empty;
        public int ActivePlayerId { get; set; }
        public int PriorityPlayerId { get; set; }

        // 実行されたアクション
        public string ActionType { get; set; } = string.Empty;
        public int? ActionParam { get; set; }

        // 各プレイヤーのざっくりした状態
        public int PlayerAHand { get; set; }
        public int PlayerAMana { get; set; }
        public int PlayerAShield { get; set; }
        public int PlayerADeck { get; set; }

        public int PlayerBHand { get; set; }
        public int PlayerBMana { get; set; }
        public int PlayerBShield { get; set; }
        public int PlayerBDeck { get; set; }

        public string? Note { get; set; }

        public static ReplayTraceEntry From(DuelMasters.Engine.GameState g, DuelMasters.Engine.ActionIntent action, string? note = null)
        {
            var pA = g.Players[0];
            var pB = g.Players[1];

            return new ReplayTraceEntry
            {
                Turn = g.TurnNumber,
                Phase = g.Phase.ToString(),
                ActivePlayerId = g.ActivePlayer.Value,
                PriorityPlayerId = g.PriorityPlayer.Value,
                ActionType = action.Type.ToString(),
                ActionParam = action.Param,
                PlayerAHand = pA.Hand.Cards.Length,
                PlayerAMana = pA.Mana.Cards.Length,
                PlayerAShield = pA.Shield.Cards.Length,
                PlayerADeck = pA.Deck.Cards.Length,
                PlayerBHand = pB.Hand.Cards.Length,
                PlayerBMana = pB.Mana.Cards.Length,
                PlayerBShield = pB.Shield.Cards.Length,
                PlayerBDeck = pB.Deck.Cards.Length,
                Note = note
            };
        }
    }
}
