namespace toofz.Services.PlayersService
{
    internal sealed class PlayersOptions : Options
    {
        /// <summary>
        /// The number of players to update.
        /// </summary>
        public int? PlayersPerUpdate { get; set; }
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        public string SteamWebApiKey { get; set; } = "";
    }
}
