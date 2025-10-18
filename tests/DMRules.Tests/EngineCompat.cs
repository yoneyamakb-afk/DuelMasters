#nullable enable
using System;
using System.Reflection;
using DMRules.Engine;

namespace DMRules.Engine
{
  /// 旧テストの呼び方を v15 に橋渡しする静的ヘルパー（常に MinimalState を返す）
  public static class EngineCompat
  {
    // ---- 文字列版 Step ----
    public static MinimalState Step(object adapter, IGameState state, string phase, string priority)
      => Step(adapter, state, Parse<Phase>(phase), Parse<Priority>(priority));

    public static MinimalState Step(object adapter, IGameState state, string phase)
      => Step(adapter, state, Parse<Phase>(phase));

    // ---- 列挙版 Step（置換後でも安全に動くように用意）----
    public static MinimalState Step(object adapter, IGameState state, Phase phase, Priority priority)
      => InvokeStep(adapter, state, phase, priority);

    public static MinimalState Step(object adapter, IGameState state, Phase phase)
      => InvokeStep(adapter, state, phase, null);

    // ---- ApplyReplacement（辞書/任意型を吸収）----
    public static MinimalState ApplyReplacement(object adapter, IGameState state, object arg)
    {
      var at = adapter.GetType();
      var m  = at.GetMethod("ApplyReplacement", new[] { typeof(IGameState), typeof(object) })
           ?? at.GetMethod("ApplyReplacement");
      if (m is not null)
      {
        var r = m.Invoke(adapter, new object[] { state, arg });
        if (r is IGameState gs) return ToMinimal(gs);
      }
      return InvokeApplyLike(adapter, arg, state).ToMinimal();
    }

    // ===== 内部ユーティリティ =====
    private static MinimalState InvokeStep(object adapter, IGameState state, Phase p, Priority? pr)
    {
      var at = adapter.GetType();
      var m  = pr is null
        ? at.GetMethod("Step", new[] { typeof(IGameState), typeof(Phase) })
        : at.GetMethod("Step", new[] { typeof(IGameState), typeof(Phase), typeof(Priority) });

      if (m is not null)
      {
        var args = pr is null ? new object[] { state, p } : new object[] { state, p, pr.Value };
        var r = m.Invoke(adapter, args);
        if (r is IGameState gs) return ToMinimal(gs);
      }
      return ToMinimal(state);
    }

    private static IGameState InvokeApplyLike(object adapter, object? obj, IGameState state)
    {
      var at = adapter.GetType();
      foreach (var n in new[]{ "Apply","Execute","ApplyEffect","ApplyGameState","Run","Step" })
      {
        var m = at.GetMethod(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (m is null) continue;
        var ps = m.GetParameters();
        if (ps.Length == 2 && typeof(IGameState).IsAssignableFrom(ps[1].ParameterType))
        { var r = m.Invoke(adapter, new object?[]{ obj, state }); return r as IGameState ?? state; }
        if (ps.Length == 1 && typeof(IGameState).IsAssignableFrom(ps[0].ParameterType))
        { var r = m.Invoke(adapter, new object?[]{ state }); return r as IGameState ?? state; }
      }
      return state;
    }

    private static T Parse<T>(string s) where T : struct, Enum
    { if (Enum.TryParse<T>(s, true, out var v)) return v; throw new ArgumentException($"Unknown {typeof(T).Name}: '{s}'"); }

    private static MinimalState ToMinimal(IGameState gs)
    {
      try { return Compat.FromGameState(gs); }
      catch
      {
        var t = typeof(MinimalState);
        var ctor = t.GetConstructor(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance, null, new[]{ typeof(IGameState) }, null);
        if (ctor is not null) return (MinimalState)ctor.Invoke(new object[]{ gs });
        var m = t.GetMethod("From", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static, null, new[]{ typeof(IGameState) }, null);
        if (m is not null && m.ReturnType == typeof(MinimalState)) return (MinimalState)m.Invoke(null, new object[]{ gs })!;
        return (MinimalState)(object)gs; // 実体が MinimalState の場合に限り成立
      }
    }
  }
}
