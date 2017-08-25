using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    sealed class PlayersOptions : Options
    {
        public int? PlayersPerUpdate { get; internal set; }
        public string ToofzApiBaseAddress { get; internal set; }
        public string PlayersUserName { get; internal set; }
        public string PlayersPassword { get; internal set; } = "";
        public string SteamWebApiKey { get; internal set; } = "";
        public string PlayersInstrumentationKey { get; internal set; }
    }
}
