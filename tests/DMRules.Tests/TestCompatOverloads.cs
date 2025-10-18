#nullable enable
using System;
using System.Reflection;
using DMRules.Engine;

namespace DMRules.Tests
{
  /// 旧テストの書き方を v15 に橋渡しするブリッジ
  public static class TestCompatOverloads
  {
    private static Phase ToPhase(string s) => V15Compat.ParsePhase(s);
    private static Priority ToPriority(string s) => V15Compat.ParsePriority(s);
    public static MinimalState ToMinimalSafe(this IGameState s)
      => s is MinimalState m ? m : V15Compat.ToMinimal(s);

    // --- インスタンス拡張としても、静的呼び出しとしても使えるよう両方用意 ---
    // 1) 拡張（this object）
    public static MinimalState Step(this object adapter, IGameState state, string phase, string priority)
      => Step(adapter, state, phase, priority, _staticCall:false);
    public static MinimalState Step(this object adapter, IGameState state, string phase)
      => Step(adapter, state, phase, _staticCall:false);
    public static MinimalState ApplyReplacement(this object adapter, IGameState state, object arg)
      => ApplyReplacement(adapter, state, arg, _staticCall:false);
    public static MinimalState Apply(this object adapter, object? obj, IGameState state)
      => InvokeApplyLike(adapter, obj, state).ToMinimalSafe();

    // 2) 静的呼び出し（解決順に依らず確実に使える）
    public static MinimalState Step(object adapter, IGameState state, string phase, string priority, bool _staticCall=true)
    {
      var p = ToPhase(phase); var pr = ToPriority(priority);
      var at = adapter.GetType();
      var m = at.GetMethod("Step", new[] { typeof(IGameState), typeof(Phase), typeof(Priority) });
      if (m is not null) { var r = m.Invoke(adapter, new object[] { state, p, pr }); if (r is IGameState gs) return gs.ToMinimalSafe(); }
      return state.ToMinimalSafe();
    }
    public static MinimalState Step(object adapter, IGameState state, string phase, bool _staticCall=true)
    {
      var p = ToPhase(phase);
      var at = adapter.GetType();
      var m = at.GetMethod("Step", new[] { typeof(IGameState), typeof(Phase) });
      if (m is not null) { var r = m.Invoke(adapter, new object[] { state, p }); if (r is IGameState gs) return gs.ToMinimalSafe(); }
      return state.ToMinimalSafe();
    }
    public static MinimalState ApplyReplacement(object adapter, IGameState state, object arg, bool _staticCall=true)
    {
      var at = adapter.GetType();
      var m = at.GetMethod("ApplyReplacement", new[] { typeof(IGameState), typeof(object) })
           ?? at.GetMethod("ApplyReplacement");
      if (m is not null) { var r = m.Invoke(adapter, new object[] { state, arg }); if (r is IGameState gs) return gs.ToMinimalSafe(); }
      return InvokeApplyLike(adapter, arg, state).ToMinimalSafe();
    }

    private static IGameState InvokeApplyLike(object adapter, object? obj, IGameState state)
    {
      var at = adapter.GetType();
      foreach (var n in new[]{ "Apply","Execute","ApplyEffect","ApplyGameState","Run","Step" })
      {
        var m = at.GetMethod(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (m is null) continue;
        var ps = m.GetParameters();
        if (ps.Length == 2 && typeof(IGameState).IsAssignableFrom(ps[1].ParameterType)) { var r = m.Invoke(adapter, new object?[]{ obj, state }); if (r is IGameState g1) return g1; return state; }
        if (ps.Length == 1 && typeof(IGameState).IsAssignableFrom(ps[0].ParameterType)) { var r = m.Invoke(adapter, new object?[]{ state }); if (r is IGameState g2) return g2; return state; }
      }
      return state;
    }
  }
}

