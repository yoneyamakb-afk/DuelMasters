using Xunit;
using DMRules.Data;

public class SqliteIntrospectionTests : ConfigFixture
{
    [Fact]
    public void Can_Read_Schema_And_Tables()
    {
        var schema = SqliteIntrospector.ReadSchema(DbPath);
        Assert.NotEmpty(schema);
        Assert.Contains(schema, t => t.Name.Equals("card_face", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Card_Face_Should_Have_Rows()
    {
        if (!SqliteIntrospector.TableExists(DbPath, "card_face"))
            return; // table optional in some dumps

        var repo = new CardRepository(DbPath);
        var rows = repo.GetAll(limit: 5);
        Assert.NotEmpty(rows);
    }
}
