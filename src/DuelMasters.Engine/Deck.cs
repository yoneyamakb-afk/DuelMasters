using System.Collections.Immutable;

namespace DuelMasters.Engine;

public sealed record Deck(ImmutableArray<CardId> Cards)
{
    public CardId Top => Cards[0];
    public Deck RemoveTop() => new Deck(Cards.RemoveAt(0));
}
