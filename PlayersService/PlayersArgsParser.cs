using System;
using System.IO;
using System.Reflection;
using Mono.Options;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal sealed class PlayersArgsParser : ArgsParser<PlayersOptions, IPlayersSettings>
    {
        internal const string DefaultLeaderboardsConnectionString = "Data Source=localhost;Initial Catalog=NecroDancer;Integrated Security=SSPI;";

        public PlayersArgsParser(TextReader inReader, TextWriter outWriter, TextWriter errorWriter) : base(inReader, outWriter, errorWriter) { }

        protected override string EntryAssemblyFileName { get; } = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        protected override void OnParsing(Type settingsType, OptionSet optionSet, PlayersOptions options)
        {
            base.OnParsing(settingsType, optionSet, options);

            optionSet.Add("players=", GetDescription(settingsType, nameof(Settings.PlayersPerUpdate)), (int players) => options.PlayersPerUpdate = players);
            optionSet.Add("toofz=", GetDescription(settingsType, nameof(Settings.ToofzApiBaseAddress)), api => options.ToofzApiBaseAddress = api);
            optionSet.Add("connection:", GetDescription(settingsType, nameof(Settings.LeaderboardsConnectionString)), connection => options.LeaderboardsConnectionString = connection);
            optionSet.Add("apikey:", GetDescription(settingsType, nameof(Settings.SteamWebApiKey)), apikey => options.SteamWebApiKey = apikey);
        }

        protected override void OnParsed(PlayersOptions options, IPlayersSettings settings)
        {
            base.OnParsed(options, settings);

            var iterations = settings.KeyDerivationIterations;

            #region PlayersPerUpdate

            var playersPerUpdate = options.PlayersPerUpdate;
            if (playersPerUpdate != null)
            {
                settings.PlayersPerUpdate = playersPerUpdate.Value;
            }

            #endregion

            #region ToofzApiBaseAddress

            var toofzApiBaseAddress = options.ToofzApiBaseAddress;
            if (!string.IsNullOrEmpty(toofzApiBaseAddress))
            {
                settings.ToofzApiBaseAddress = toofzApiBaseAddress;
            }

            #endregion

            #region LeaderboardsConnectionString

            var leaderboardsConnectionString = options.LeaderboardsConnectionString;
            if (leaderboardsConnectionString == null)
            {
                leaderboardsConnectionString = ReadOption("Leaderboards connection string");
            }

            if (leaderboardsConnectionString != "")
            {
                settings.LeaderboardsConnectionString = new EncryptedSecret(leaderboardsConnectionString, iterations);
            }
            else if (settings.LeaderboardsConnectionString == null)
            {
                settings.LeaderboardsConnectionString = new EncryptedSecret(DefaultLeaderboardsConnectionString, iterations);
            }

            #endregion

            #region SteamWebApiKey

            var steamWebApiKey = options.SteamWebApiKey;
            if (ShouldPromptForRequiredSetting(steamWebApiKey, settings.SteamWebApiKey))
            {
                steamWebApiKey = ReadOption("Steam Web API key");
            }

            if (steamWebApiKey != "")
            {
                settings.SteamWebApiKey = new EncryptedSecret(steamWebApiKey, iterations);
            }

            #endregion
        }
    }
}
