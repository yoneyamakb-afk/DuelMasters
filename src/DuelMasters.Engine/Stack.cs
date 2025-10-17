using System.Collections.Immutable;

namespace DuelMasters.Engine;

public enum StackItemKind { Dummy, BreakShield, DestroyCreature, BuffPower, TriggerDemo }

public sealed record StackItem(StackItemKind Kind, PlayerId Controller, TargetSpec Target, string? Info);

public sealed record StackState(ImmutableArray<StackItem> Items)
{
    public static readonly StackState Empty = new StackState(ImmutableArray<StackItem>.Empty);
    public int Count => Items.Length;
    public StackState Push(StackItem item) => new StackState(Items.Add(item));
    public (StackItem item, StackState rest)? Pop()
    {
        if (Items.Length == 0) return null;
        var it = Items[^1];
        return (it, new StackState(Items.RemoveAt(Items.Length - 1)));
    }
}
