// src/DuelMasters.CLI/EngineHooks.cs
using System;

namespace DuelMasters.CLI
{
    public sealed class EngineEvent
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string? Kind { get; set; }
        public string? PhaseName { get; set; }
        public int? ActivePlayerId { get; set; }
    }

    public static class EngineHooks
    {
        public delegate void EngineEventHandler(EngineEvent e);
        public static EngineEventHandler? OnEvent;

        public static void Init() { }
        public static void Shutdown() { }

        public static void Raise(EngineEvent e) => OnEvent?.Invoke(e);
    }
}
