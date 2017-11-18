using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal sealed class PlayersOptions : Options
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
        /// The connection string used to connect to the leaderboards database.
        /// </summary>
        public string LeaderboardsConnectionString { get; internal set; } = "";
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        public string SteamWebApiKey { get; internal set; } = "";
    }
}
