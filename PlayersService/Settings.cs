using System.Configuration;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Properties
{
    [SettingsProvider(typeof(ServiceSettingsProvider))]
    partial class Settings : IPlayersSettings { }
}
