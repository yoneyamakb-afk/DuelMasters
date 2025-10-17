using System;
using System.Linq;
using System.Collections.Immutable;
using DuelMasters.Engine;

class Program
{
    static void Main(string[] args)
    {
        string dbPath = "Duelmasters.db";
        using var db = System.IO.File.Exists(dbPath) ? new SqliteCardDatabase(dbPath) : null;
        var sim = new Simulator(db);

        var a = new Deck(ImmutableArray.CreateRange(Enumerable.Range(0,40).Select(i => new CardId(i))));
        var b = new Deck(ImmutableArray.CreateRange(Enumerable.Range(1000,40).Select(i => new CardId(i))));
        var s = sim.InitialState(a,b,7);

        // P0召喚 → パス・パス → P1召喚
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));

        // 双方のクリーチャーに -20000 を与えて確実に破壊させる
        var p0inst = s.Players[0].BattleIds.FirstOrDefault();
        var p1inst = s.Players[1].BattleIds.FirstOrDefault();
        s = s with
        {
            ContinuousEffects = s.ContinuousEffects
                .Add(new PowerBuff(new PlayerId(0), p0inst, -20000, s.TurnNumber))
                .Add(new PowerBuff(new PlayerId(1), p1inst, -20000, s.TurnNumber))
        };

        s = s.RunStateBasedActions();
        Console.WriteLine("After forced SBA, stack size: " + s.Stack.Count);

        // パス・パス → SBA処理 → 誘発(APNAP)をスタックへ
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        Console.WriteLine("Demo finished.");
        Console.WriteLine("Stack size after SBA & triggers: " + s.Stack.Count);

        // パス・パス後にSBAを明示的に再評価
        s = s.RunStateBasedActions();

        // 結果の表示
        Console.WriteLine("After SBA re-evaluation, stack size: " + s.Stack.Count);

        // スタックの内容を確認（デバッグ出力）
        foreach (var item in s.Stack.Items)
        {
            Console.WriteLine($"  kind={item.Kind}, controller={item.Controller.Value}, info={item.Info}");
        }
    }
}
