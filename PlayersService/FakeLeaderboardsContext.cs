using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using toofz.Data;
using toofz.Steam.WebApi.ISteamUser;

namespace toofz.Services.PlayersService
{
    [ExcludeFromCodeCoverage]
    internal sealed class FakeLeaderboardsContext : ILeaderboardsContext
    {
        public FakeLeaderboardsContext()
        {
            var playerSummariesPath = Path.Combine("Data", "SteamWebApi", "PlayerSummaries");
            var playerSummariesFiles = Directory.GetFiles(playerSummariesPath, "*.json");

            var dbPlayers = new List<Player>();
            foreach (var playerSummariesFile in playerSummariesFiles)
            {
                using (var sr = File.OpenText(playerSummariesFile))
                {
                    var playerSummaries = JsonConvert.DeserializeObject<PlayerSummariesEnvelope>(sr.ReadToEnd());
                    var players = from p in playerSummaries.Response.Players
                                  select new Player { SteamId = p.SteamId };
                    dbPlayers.AddRange(players);
                }
            }

            Players = new FakeDbSet<Player>(dbPlayers);
        }

        public DbSet<Player> Players { get; }

        public DbSet<Leaderboard> Leaderboards => throw new NotImplementedException();
        public DbSet<Entry> Entries => throw new NotImplementedException();
        public DbSet<DailyLeaderboard> DailyLeaderboards => throw new NotImplementedException();
        public DbSet<DailyEntry> DailyEntries => throw new NotImplementedException();
        public DbSet<Replay> Replays => throw new NotImplementedException();
        public DbSet<Product> Products => throw new NotImplementedException();
        public DbSet<Mode> Modes => throw new NotImplementedException();
        public DbSet<Run> Runs => throw new NotImplementedException();
        public DbSet<Character> Characters => throw new NotImplementedException();

        public void Dispose() { }
    }
}
