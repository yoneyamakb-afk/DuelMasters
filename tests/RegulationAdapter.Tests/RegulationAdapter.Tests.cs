using System;
using System.Collections.Generic;
using Xunit;
using RegulationAdapter;

namespace RegulationAdapter.Tests
{
    public class FakeCard
    {
        public string CardId { get; set; } = "TEST";
        public string Name { get; set; } = "TestCard";
        public int Power { get; set; } = 1000;
        public bool IsTapped { get; set; }
        public bool IsShieldTrigger { get; set; }
        public bool IsBlocker { get; set; }
        public bool HasSpeedAttacker { get; set; }
        public void Tap() => IsTapped = true;
        public override string ToString() => $"{Name}({CardId}) P:{Power}";
    }

    public class FakeGameState
    {
        public List<string> Log { get; } = new();
        public void Draw(int n) => Log.Add($"Draw({n})");
        public void Draw(string who, int n) => Log.Add($"Draw({who},{n})");
        public void ApplyPowerMod(object card, int delta, string duration)
        {
            if (card is FakeCard c) c.Power += delta;
            Log.Add($"PowerMod({delta},{duration})");
        }
        public void BuffUntilEOT(object card, int delta, string duration) => ApplyPowerMod(card, delta, duration);
    }

    public class FakeEvent
    {
        public string Action { get; set; } = "None";
        public FakeCard? SourceCard { get; set; }
        public FakeCard? TargetCard { get; set; }
        public bool Handled { get; set; }
    }

    public class AdapterFlagTests
    {
        [Fact]
        public void ApplyFlags_SetsBlockerSpeedAttackerShieldTrigger()
        {
            var game = new FakeGameState();
            var card = new FakeCard { CardId = "TEST_FLAG", Name = "FlagCard" };
            Adapter.ApplyCardStaticFlags(game, card);
            Assert.True(card.IsBlocker);
            Assert.True(card.HasSpeedAttacker);
            Assert.True(card.IsShieldTrigger);
        }
    }

    public class AdapterTriggeredTests
    {
        [Fact]
        public void OnAttack_PowerModPlus2000()
        {
            var game = new FakeGameState();
            var card = new FakeCard { CardId = "TEST_TRIGGER", Name = "TriggerCard", Power = 1000 };
            var e = new FakeEvent { Action = "Attack", SourceCard = card };
            Adapter.OnEvent(game, e, card);
            Assert.Equal(3000, card.Power);
        }
    }

    public class AdapterReplacementTests
    {
        [Fact]
        public void WouldBeDestroyed_ReplacedByTap()
        {
            var game = new FakeGameState();
            var card = new FakeCard { CardId = "TEST_REPLACE", Name = "ReplaceCard" };
            var e = new FakeEvent { Action = "Destroyed", SourceCard = card, TargetCard = card };
            Adapter.OnEvent(game, e, card);
            Assert.True(e.Handled);
            Assert.True(card.IsTapped);
        }
    }
}
