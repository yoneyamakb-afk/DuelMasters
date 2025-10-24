
using System;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;
using DMRules.Effects.Text;
using Xunit;

public class SqliteEffectTextProviderTests
{
    private static void SafeDelete(string path, int retries = 10, int delayMs = 100)
    {
        if (string.IsNullOrEmpty(path)) return;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            catch (IOException) { Thread.Sleep(delayMs); }
            catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
        }
        // 最後の最後でも削除できなければあきらめる（テストを落とさない）
    }

    [Fact]
    public void Can_Read_From_TempDb_ByFaceId()
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var conn = new SqliteConnection(new SqliteConnectionStringBuilder{ DataSource = path }.ToString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
CREATE TABLE card_face (face_id INTEGER PRIMARY KEY, effecttxt TEXT, cardname TEXT);
INSERT INTO card_face(face_id, effecttxt, cardname) VALUES (1, 'on summon: draw 2', 'Sample');
";
                    cmd.ExecuteNonQuery();
                }
            }

            using (var provider = new SqliteEffectTextProvider(path))
            {
                Assert.Equal("on summon: draw 2", provider.GetEffectTextByFaceId(1));
                Assert.Null(provider.GetEffectTextByFaceId(99));
            }
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SafeDelete(path);
        }
    }

    [Fact]
    public void Can_Read_From_TempDb_ByName()
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var conn = new SqliteConnection(new SqliteConnectionStringBuilder{ DataSource = path }.ToString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
CREATE TABLE card_face (face_id INTEGER PRIMARY KEY, effecttxt TEXT, cardname TEXT);
INSERT INTO card_face(face_id, effecttxt, cardname) VALUES (5, 'on attack: draw 1', 'My Card');
";
                    cmd.ExecuteNonQuery();
                }
            }

            using (var provider = new SqliteEffectTextProvider(path))
            {
                Assert.Equal("on attack: draw 1", provider.GetEffectTextByName("My Card"));
                Assert.Null(provider.GetEffectTextByName("Unknown"));
            }
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SafeDelete(path);
        }
    }
}
