using System.Configuration;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Properties
{
    [SettingsProvider(typeof(ServiceSettingsProvider))]
    partial class Settings : IPlayersSettings { }
}
