using System.Configuration;

namespace toofz.Services.PlayersService.Properties
{
    [SettingsProvider(typeof(ServiceSettingsProvider))]
    partial class Settings : IPlayersSettings { }
}
