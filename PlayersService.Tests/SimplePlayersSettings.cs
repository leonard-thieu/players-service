using System;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    sealed class SimplePlayersSettings : IPlayersSettings
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

        public void Reload()
        {
            PlayersPerUpdate = default(int);
            ToofzApiBaseAddress = default(string);
            ToofzApiUserName = default(string);
            ToofzApiPassword = default(EncryptedSecret);
            SteamWebApiKey = default(EncryptedSecret);
            UpdateInterval = default(TimeSpan);
            DelayBeforeGC = default(TimeSpan);
            InstrumentationKey = default(string);
            KeyDerivationIterations = default(int);
        }

        public void Save() { }
    }
}
