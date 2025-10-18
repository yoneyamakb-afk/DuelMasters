using System.Collections.Immutable;

namespace DuelMasters.Engine;

public sealed record Zone(ZoneKind Kind, ImmutableArray<CardId> Cards);


