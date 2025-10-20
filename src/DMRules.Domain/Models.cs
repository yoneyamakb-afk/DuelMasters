// Domain models for normalized card data (minimal for M4B)
namespace DMRules.Domain;

public sealed class Card
{
    public required string Name { get; init; }
    public int? Cost { get; init; }
    public int? Power { get; init; }
    public IReadOnlyList<string> Civilizations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Types { get; init; } = Array.Empty<string>();

    // Raw fields for traceability
    public string? RawCivilTxt { get; init; }
    public string? RawTypeTxt  { get; init; }
    public string? RawPowerTxt { get; init; }
    public string? RawCostTxt  { get; init; }

    public long? FaceId { get; init; }
}
