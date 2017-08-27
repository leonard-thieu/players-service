using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    sealed class PlayersOptions : Options
    {
        /// <summary>
        /// The number of players to update.
        /// </summary>
        public int? PlayersPerUpdate { get; internal set; }
        /// <summary>
        /// The base address of toofz API.
        /// </summary>
        public string ToofzApiBaseAddress { get; internal set; }
        /// <summary>
        /// The user name used to log on to toofz API.
        /// </summary>
        public string ToofzApiUserName { get; internal set; }
        /// <summary>
        /// The password used to log on to toofz API.
        /// </summary>
        public string ToofzApiPassword { get; internal set; } = "";
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        public string SteamWebApiKey { get; internal set; } = "";
    }
}
