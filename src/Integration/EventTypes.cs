using System;
namespace DuelMasters.Engine.Integration.M11
{
    public enum EngineEventKind { EngineStart, PhaseBegin, PhaseEnd, TurnBegin, TurnEnd, CardEnteredZone, CardLeftZone }
    public sealed class EngineEvent
    {
        public EngineEventKind Kind { get; set; }
        public int? ActivePlayerId { get; set; }
        public string? PhaseName { get; set; }
        public object? Payload { get; set; }
        public DateTime TimestampUtc { get; } = DateTime.UtcNow;
    }
}
