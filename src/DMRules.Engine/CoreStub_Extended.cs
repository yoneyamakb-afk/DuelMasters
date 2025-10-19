using System.Collections.Immutable;

namespace DMRules.Engine
{
    // 既存エンジンと衝突しない最小の列挙体
    public enum Phase { Setup = 0, StartOfTurn, Main, Attack, End }
    public enum PlayerId { P0 = 0, P1 = 1 }

    // 既存のTrace周りで参照されるエントリ
    public readonly record struct TraceEntry(long Ordinal, string Kind, string Detail);

    /// <summary>
    /// 既存エンジンが参照するメンバーだけを提供する "参照専用" GameState スタブ。
    /// ※ IChooser/IGameAction/ITriggeredAbility/IReplacementEffect は再定義しない（既存実装を使う）。
    /// </summary>
    public sealed class GameState
    {
        // フェーズ/ターン
        public Phase Phase { get; init; } = Phase.Setup;
        public PlayerId ActivePlayer { get; init; } = PlayerId.P0;
        public PlayerId NonActivePlayer => ActivePlayer == PlayerId.P0 ? PlayerId.P1 : PlayerId.P0;

        // エンジン・パイプライン（型は既存実装を参照）
        public ImmutableStack<IGameAction> Stack { get; init; } = ImmutableStack<IGameAction>.Empty;
        public ImmutableList<ITriggeredAbility> TriggersAP { get; init; } = ImmutableList<ITriggeredAbility>.Empty;
        public ImmutableList<ITriggeredAbility> TriggersNAP { get; init; } = ImmutableList<ITriggeredAbility>.Empty;
        public ImmutableList<IReplacementEffect> ReplacementEffects { get; init; } = ImmutableList<IReplacementEffect>.Empty;
        public long NextSequence { get; init; } = 0;
        public IChooser Chooser { get; init; } = new DefaultChooser(); // 既存実装が提供

        // ゾーン/フラグ
        public int BattlefieldCount { get; init; } = 0;
        public int GraveyardCount { get; init; } = 0;
        public bool IsLegal { get; init; } = true;
        public bool IsTerminal { get; init; } = false;

        // Trace
        public ImmutableList<TraceEntry> Trace { get; init; } = ImmutableList<TraceEntry>.Empty;
        public long NextTraceId { get; init; } = 0;

        public GameState() { }

        public GameState(
            Phase phase,
            PlayerId activePlayer = PlayerId.P0,
            ImmutableStack<IGameAction>? stack = null,
            ImmutableList<ITriggeredAbility>? triggersAP = null,
            ImmutableList<ITriggeredAbility>? triggersNAP = null,
            ImmutableList<IReplacementEffect>? replacementEffects = null,
            long nextSequence = 0,
            IChooser? chooser = null,
            int battlefieldCount = 0,
            int graveyardCount = 0,
            bool isLegal = true,
            bool isTerminal = false,
            ImmutableList<TraceEntry>? trace = null,
            long nextTraceId = 0)
        {
            Phase = phase;
            ActivePlayer = activePlayer;
            Stack = stack ?? ImmutableStack<IGameAction>.Empty;
            TriggersAP = triggersAP ?? ImmutableList<ITriggeredAbility>.Empty;
            TriggersNAP = triggersNAP ?? ImmutableList<ITriggeredAbility>.Empty;
            ReplacementEffects = replacementEffects ?? ImmutableList<IReplacementEffect>.Empty;
            NextSequence = nextSequence;
            Chooser = chooser!;
            BattlefieldCount = battlefieldCount;
            GraveyardCount = graveyardCount;
            IsLegal = isLegal;
            IsTerminal = isTerminal;
            Trace = trace ?? ImmutableList<TraceEntry>.Empty;
            NextTraceId = nextTraceId;
        }

        public GameState With(
            Phase? phase = null,
            PlayerId? activePlayer = null,
            ImmutableStack<IGameAction>? stack = null,
            ImmutableList<ITriggeredAbility>? triggersAP = null,
            ImmutableList<ITriggeredAbility>? triggersNAP = null,
            ImmutableList<IReplacementEffect>? replacementEffects = null,
            long? nextSequence = null,
            IChooser? chooser = null,
            int? battlefieldCount = null,
            int? graveyardCount = null,
            bool? isLegal = null,
            bool? isTerminal = null,
            ImmutableList<TraceEntry>? trace = null,
            long? nextTraceId = null)
        {
            return new GameState(
                phase ?? Phase,
                activePlayer ?? ActivePlayer,
                stack ?? Stack,
                triggersAP ?? TriggersAP,
                triggersNAP ?? TriggersNAP,
                replacementEffects ?? ReplacementEffects,
                nextSequence ?? NextSequence,
                chooser ?? Chooser,
                battlefieldCount ?? BattlefieldCount,
                graveyardCount ?? GraveyardCount,
                isLegal ?? IsLegal,
                isTerminal ?? IsTerminal,
                trace ?? Trace,
                nextTraceId ?? NextTraceId
            );
        }

        public GameState AddTrace(string kind, string detail)
        {
            var added = Trace.Add(new TraceEntry(NextTraceId, kind, detail));
            return With(trace: added, nextTraceId: NextTraceId + 1);
        }
    }
}
