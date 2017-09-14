using System;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    sealed class StubPlayersSettings : IPlayersSettings
    {
        public int PlayersPerUpdate { get; set; }
        public string ToofzApiBaseAddress { get; set; }
        public string ToofzApiUserName { get; set; }
        public EncryptedSecret ToofzApiPassword { get; set; }
        public EncryptedSecret SteamWebApiKey { get; set; }
        public TimeSpan UpdateInterval { get; set; }
        public TimeSpan DelayBeforeGC { get; set; }
        public string InstrumentationKey { get; set; }
        public int KeyDerivationIterations { get; set; }

        public void Reload() { }

        public void Save() { }
    }
}
