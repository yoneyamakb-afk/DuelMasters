#nullable enable
using System;
using System.Reflection;
using DMRules.Engine;

namespace DMRules.Tests
{
  public static class V15Compat
  {
    // --- enum 文字列→型 ---
    public static Phase ParsePhase(string name)
    {
      if (Enum.TryParse<Phase>(name, true, out var p)) return p;
      throw new ArgumentException($"Unknown Phase: {name}");
    }
    public static Priority ParsePriority(string name)
    {
      if (Enum.TryParse<Priority>(name, true, out var pr)) return pr;
      throw new ArgumentException($"Unknown Priority: {name}");
    }

    // --- string/char 互換ヘルパ ---
    public static int FindIndex(this string s, Func<char, bool> predicate)
    {
      if (s is null) return -1;
      for (int i = 0; i < s.Length; i++) if (predicate(s[i])) return i;
      return -1;
    }
    public static bool StartsWith(this char c, char head) => c == head;
    public static bool StartsWith(this char c, string head)
    {
      if (string.IsNullOrEmpty(head)) return false;
      return c == head[0];
    }
    public static bool StartsWith(this string s, string head)
    {
      if (string.IsNullOrEmpty(head)) return true;
      return s?.StartsWith(head, StringComparison.Ordinal) ?? false;
    }

    // --- IGameState → MinimalState（安定フック or 反射） ---
    public static MinimalState ToMinimal(this IGameState gs)
    {
      try { return Compat.FromGameState(gs); }
      catch
      {
        var t = typeof(MinimalState);
        var ctor = t.GetConstructor(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance, null, new[]{ typeof(IGameState) }, null);
        if (ctor is not null) return (MinimalState)ctor.Invoke(new object[]{ gs });
        var m = t.GetMethod("From", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static, null, new[]{ typeof(IGameState) }, null);
        if (m is not null && m.ReturnType == typeof(MinimalState)) return (MinimalState)m.Invoke(null, new object[]{ gs })!;
        throw;
      }
    }
  }

  // --- 旧名メソッドの互換拡張（IEngineAdapter） ---
  public static class EngineAdapterCompatExtensions
  {
    public static IGameState OnZoneEnterApplyContinuousEffects(this IEngineAdapter adapter, IGameState state)
    {
      if (adapter is null) throw new ArgumentNullException(nameof(adapter));
      if (state is null) throw new ArgumentNullException(nameof(state));

      string[] candidates = { "OnZoneEnterApplyContinuousEffects", "ApplyContinuousEffectsOnZoneEnter", "ApplyContinuousEffects", "OnZoneEnter" };
      var at = adapter.GetType();
      foreach (var name in candidates)
      {
        var m = at.GetMethod(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (m is null) continue;
        var ps = m.GetParameters();
        if (ps.Length == 1 && typeof(IGameState).IsAssignableFrom(ps[0].ParameterType))
        {
          var r = m.Invoke(adapter, new object[]{ state });
          if (r is IGameState gs) return gs;
          return state;
        }
      }
      return state;
    }
  }

  // --- テスト用スタブ（IEngineAdapter の足りない実装を埋める）---
  public class EngineAdapterStub : IEngineAdapter
  {
    private readonly IEngineAdapter? _inner;
    public EngineAdapterStub() { }
    public EngineAdapterStub(IEngineAdapter inner) { _inner = inner; }

    public IGameState Apply(object? _unused, IGameState state)
    {
      var a = _inner ?? this;
      var at = a.GetType();
      string[] names = { "Apply", "Execute", "ApplyEffect", "ApplyGameState", "Run", "Step" };
      foreach (var n in names)
      {
        var m = at.GetMethod(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (m is null) continue;
        var ps = m.GetParameters();
        if (ps.Length == 2 && typeof(IGameState).IsAssignableFrom(ps[1].ParameterType))
        {
          var r = m.Invoke(a, new object?[]{ null, state });
          if (r is IGameState gs) return gs;
          return state;
        }
        if (ps.Length == 1 && typeof(IGameState).IsAssignableFrom(ps[0].ParameterType))
        {
          var r = m.Invoke(a, new object?[]{ state });
          if (r is IGameState gs) return gs;
          return state;
        }
      }
      return state;
    }

    // v15 で追加されたメンバー（ログから型を合わせる）
    public State CreateInitial() => default!;
    public IGameState Audit => null!;
  }
}

