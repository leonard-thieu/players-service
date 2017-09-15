using System;
using System.IO;
using System.Reflection;
using Mono.Options;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    sealed class PlayersArgsParser : ArgsParser<PlayersOptions, IPlayersSettings>
    {
        public PlayersArgsParser(TextReader inReader, TextWriter outWriter, TextWriter errorWriter) : base(inReader, outWriter, errorWriter) { }

        protected override string EntryAssemblyFileName { get; } = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        protected override void OnParsing(Type settingsType, OptionSet optionSet, PlayersOptions options)
        {
            base.OnParsing(settingsType, optionSet, options);

            optionSet.Add("players=", GetDescription(settingsType, nameof(Settings.PlayersPerUpdate)), (int players) => options.PlayersPerUpdate = players);
            optionSet.Add("toofz=", GetDescription(settingsType, nameof(Settings.ToofzApiBaseAddress)), api => options.ToofzApiBaseAddress = api);
            optionSet.Add("username=", GetDescription(settingsType, nameof(Settings.ToofzApiUserName)), username => options.ToofzApiUserName = username);
            optionSet.Add("password:", GetDescription(settingsType, nameof(Settings.ToofzApiPassword)), password => options.ToofzApiPassword = password);
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

            #region ToofzApiUserName

            var toofzApiUserName = options.ToofzApiUserName;
            if (!string.IsNullOrEmpty(toofzApiUserName))
            {
                settings.ToofzApiUserName = toofzApiUserName;
            }
            else if (string.IsNullOrEmpty(settings.ToofzApiUserName))
            {
                settings.ToofzApiUserName = ReadOption("toofz API user name");
            }

            #endregion

            #region ToofzApiPassword

            var toofzApiPassword = options.ToofzApiPassword;
            if (ShouldPromptForRequiredSetting(toofzApiPassword, settings.ToofzApiPassword))
            {
                toofzApiPassword = ReadOption("toofz API password");
            }

            if (toofzApiPassword != "")
            {
                settings.ToofzApiPassword = new EncryptedSecret(toofzApiPassword, iterations);
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
