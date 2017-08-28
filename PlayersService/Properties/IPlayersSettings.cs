using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Properties
{
    interface IPlayersSettings : ISettings
    {
        /// <summary>
        /// The number of players to update.
        /// </summary>
        int PlayersPerUpdate { get; set; }
        /// <summary>
        /// The base address of toofz API.
        /// </summary>
        string ToofzApiBaseAddress { get; set; }
        /// <summary>
        /// The user name used to log on to toofz API.
        /// </summary>
        string ToofzApiUserName { get; set; }
        /// <summary>
        /// The password used to log on to toofz API.
        /// </summary>
        EncryptedSecret ToofzApiPassword { get; set; }
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        EncryptedSecret SteamWebApiKey { get; set; }
    }
}