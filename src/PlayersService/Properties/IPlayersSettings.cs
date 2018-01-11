namespace toofz.Services.PlayersService.Properties
{
    internal interface IPlayersSettings : ISettings
    {
        /// <summary>
        /// The number of players to update.
        /// </summary>
        int PlayersPerUpdate { get; set; }
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        EncryptedSecret SteamWebApiKey { get; set; }
    }
}