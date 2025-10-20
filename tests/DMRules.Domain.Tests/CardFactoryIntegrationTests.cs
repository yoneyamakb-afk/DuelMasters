using Xunit;
using DMRules.Data;
using DMRules.Domain;
using System.Linq;

namespace DMRules.Domain.Tests;

public class CardFactoryIntegrationTests
{
    [Fact]
    public void Factory_Does_NotThrow_For_First10_IfTableExists()
    {
        if (!SqliteIntrospector.TableExists(AppConfig.GetDatabasePath(), "card_face"))
            return;

        var repo = new CardRepository();
        var rows = repo.GetAll(limit: 10);
        foreach (var r in rows)
        {
            var card = CardFactory.FromRecord(r);
            Assert.False(string.IsNullOrWhiteSpace(card.Name));
            Assert.NotNull(card.Civilizations);
            Assert.NotNull(card.Types);
        }
    }
}
