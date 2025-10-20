using Xunit;
using DMRules.Data;

public class ConfigFixture
{
    public string DbPath { get; }

    public ConfigFixture()
    {
        // Use env var DM_DB_PATH if set; otherwise appsettings.json default
        DbPath = AppConfig.GetDatabasePath();
    }
}
