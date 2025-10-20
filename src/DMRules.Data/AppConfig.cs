using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DMRules.Data
{
    public static class AppConfig
    {
        private static IConfigurationRoot? _config;
        public static IConfigurationRoot Config => _config ??= new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "DM_")
            .Build();

        public static string GetDatabasePath()
        {
            var env = Environment.GetEnvironmentVariable("DM_DB_PATH");
            if (!string.IsNullOrWhiteSpace(env)) return env!;
            var fromJson = Config["Database:Path"];
            if (!string.IsNullOrWhiteSpace(fromJson)) return fromJson!;
            return "Duelmasters.db";
        }
    }
}
