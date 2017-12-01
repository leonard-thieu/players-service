using System;
using System.Configuration;
using System.Data.Entity.Infrastructure;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    internal static class StorageHelper
    {
        public static string GetDatabaseConnectionString()
        {
            var connectionString = GetConnectionString(nameof(LeaderboardsContext));
            if (connectionString != null) { return connectionString; }

            var connectionFactory = new LocalDbConnectionFactory("mssqllocaldb");
            using (var connection = connectionFactory.CreateConnection("TestPlayersService"))
            {
                return connection.ConnectionString;
            }
        }

        private static string GetConnectionString(string baseName)
        {
            return Environment.GetEnvironmentVariable($"{baseName}TestConnectionString", EnvironmentVariableTarget.Machine) ??
                ConfigurationManager.ConnectionStrings[baseName]?.ConnectionString;
        }
    }
}
