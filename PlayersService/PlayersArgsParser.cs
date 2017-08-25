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
            optionSet.Add("username=", GetDescription(settingsType, nameof(Settings.PlayersUserName)), username => options.PlayersUserName = username);
            optionSet.Add("password:", GetDescription(settingsType, nameof(Settings.PlayersPassword)), password => options.PlayersPassword = password);
            optionSet.Add("apikey:", GetDescription(settingsType, nameof(Settings.SteamWebApiKey)), apikey => options.SteamWebApiKey = apikey);
            optionSet.Add("ikey=", GetDescription(settingsType, nameof(Settings.PlayersInstrumentationKey)), key => options.PlayersInstrumentationKey = key);
        }

        protected override void OnParsed(PlayersOptions options, IPlayersSettings settings)
        {
            base.OnParsed(options, settings);

            #region PlayersPerUpdate

            if (options.PlayersPerUpdate != null)
            {
                settings.PlayersPerUpdate = options.PlayersPerUpdate.Value;
            }

            #endregion

            #region ToofzApiBaseAddress

            if (!string.IsNullOrEmpty(options.ToofzApiBaseAddress))
            {
                settings.ToofzApiBaseAddress = options.ToofzApiBaseAddress;
            }

            #endregion

            #region PlayersUserName

            if (!string.IsNullOrEmpty(options.PlayersUserName))
            {
                settings.PlayersUserName = options.PlayersUserName;
            }

            while (string.IsNullOrEmpty(settings.PlayersUserName))
            {
                OutWriter.Write("toofz API user name: ");
                settings.PlayersUserName = InReader.ReadLine();
            }

            #endregion

            #region PlayersPassword

            if (!string.IsNullOrEmpty(options.PlayersPassword))
            {
                settings.PlayersPassword = new EncryptedSecret(options.PlayersPassword);
            }

            // When PlayersPassword == null, the user has indicated that they wish to be prompted to enter the password.
            while (settings.PlayersPassword == null || options.PlayersPassword == null)
            {
                OutWriter.Write("toofz API password: ");
                options.PlayersPassword = InReader.ReadLine();
                if (!string.IsNullOrEmpty(options.PlayersPassword))
                {
                    settings.PlayersPassword = new EncryptedSecret(options.PlayersPassword);
                }
            }

            #endregion

            #region SteamWebApiKey

            if (!string.IsNullOrEmpty(options.SteamWebApiKey))
            {
                settings.SteamWebApiKey = new EncryptedSecret(options.SteamWebApiKey);
            }

            // When SteamWebApiKey == null, the user has indicated that they wish to be prompted to enter the password.
            while (settings.SteamWebApiKey == null || options.SteamWebApiKey == null)
            {
                OutWriter.Write("Steam Web API key: ");
                options.SteamWebApiKey = InReader.ReadLine();
                if (!string.IsNullOrEmpty(options.SteamWebApiKey))
                {
                    settings.SteamWebApiKey = new EncryptedSecret(options.SteamWebApiKey);
                }
            }

            #endregion

            #region PlayersInstrumentationKey

            if (!string.IsNullOrEmpty(options.PlayersInstrumentationKey))
            {
                settings.PlayersInstrumentationKey = options.PlayersInstrumentationKey;
            }

            #endregion
        }
    }
}
