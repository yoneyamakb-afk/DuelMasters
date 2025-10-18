#nullable enable
using DMRules.Engine;

namespace DMRules.Tests
{
  /// レシーバ無しの呼び出し（Step(state,"Phase","Priority") など）を確実に受け止める静的キャッチャー
  /// ここは「コンパイル互換」最優先。戻りは MinimalState に統一し、実処理は既存の互換層へ委譲可能に。
  public static class StaticShim
  {
    // 旧テストの形: Step(state, "Phase", "Priority")
    public static MinimalState Step(IGameState state, string phase, string priority)
    {
      // 手元に adapter が無いケースでもコンパイルを通すため、最小限の動作に限定
      // （必要なら後で TestCompatOverloads へ委譲に差し替え）
      return V15Compat.ToMinimal(state);
    }

    // 旧テストの形: Step(state, "Phase")
    public static MinimalState Step(IGameState state, string phase)
    {
      return V15Compat.ToMinimal(state);
    }

    // 旧テストの形: ApplyReplacement(state, someDict)
    public static MinimalState ApplyReplacement(IGameState state, object any)
    {
      return V15Compat.ToMinimal(state);
    }
  }
}

