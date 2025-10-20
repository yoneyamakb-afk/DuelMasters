using Xunit;
using DMRules.Data;
using System.Linq;

public class CardRepositoryTests : ConfigFixture
{
    [Fact]
    public void Can_Query_By_Id_If_FaceId_Exists()
    {
        if (!SqliteIntrospector.TableExists(DbPath, "card_face"))
            return;

        if (!SqliteIntrospector.ColumnExists(DbPath, "card_face", "face_id"))
            return;

        var repo = new CardRepository(DbPath);
        var first = repo.GetAll(limit: 1).FirstOrDefault();
        if (first is null || first.FaceId is null) return;

        var again = repo.GetById(first.FaceId.Value);
        Assert.NotNull(again);
        Assert.Equal(first.FaceId, again!.FaceId);
    }

    [Fact]
    public void Can_Query_By_Name_If_Column_Exists()
    {
        if (!SqliteIntrospector.TableExists(DbPath, "card_face"))
            return;

        if (!SqliteIntrospector.ColumnExists(DbPath, "card_face", "cardname"))
            return;

        var repo = new CardRepository(DbPath);
        var first = repo.GetAll(limit: 1).FirstOrDefault();
        if (first is null || string.IsNullOrWhiteSpace(first.CardName)) return;

        var list = repo.GetByName(first.CardName!);
        Assert.NotEmpty(list);
    }
}
