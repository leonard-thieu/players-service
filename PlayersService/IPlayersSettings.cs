using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    public interface IPlayersSettings : ISettings
    {
        int PlayersPerUpdate { get; set; }
        string ToofzApiBaseAddress { get; set; }
        string PlayersUserName { get; set; }
        EncryptedSecret PlayersPassword { get; set; }
        EncryptedSecret SteamWebApiKey { get; set; }
        string PlayersInstrumentationKey { get; set; }
    }
}