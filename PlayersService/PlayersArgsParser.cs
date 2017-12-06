using System;
using System.IO;
using System.Reflection;
using Mono.Options;
using toofz.Services.PlayersService.Properties;

namespace toofz.Services.PlayersService
{
    internal sealed class PlayersArgsParser : ArgsParser<PlayersOptions, IPlayersSettings>
    {
        public PlayersArgsParser(TextReader inReader, TextWriter outWriter, TextWriter errorWriter) : base(inReader, outWriter, errorWriter) { }

        protected override string EntryAssemblyFileName { get; } = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        protected override void OnParsing(Type settingsType, OptionSet optionSet, PlayersOptions options)
        {
            base.OnParsing(settingsType, optionSet, options);

            optionSet.Add("players=", GetDescription(settingsType, nameof(Settings.PlayersPerUpdate)), (int players) => options.PlayersPerUpdate = players);
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

            #region SteamWebApiKey

            var steamWebApiKey = options.SteamWebApiKey;
            if (ShouldPrompt(steamWebApiKey))
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
