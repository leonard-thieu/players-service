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
        /// A Steam Web API key.
        /// </summary>
        public string SteamWebApiKey { get; internal set; } = "";
    }
}
