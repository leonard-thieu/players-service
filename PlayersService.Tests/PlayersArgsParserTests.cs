using System.IO;
using Moq;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    public class PlayersArgsParserTests
    {
        public class Parse
        {
            public Parse()
            {
                inReader = mockInReader.Object;
                parser = new PlayersArgsParser(inReader, outWriter, errorWriter);

                mockSettings.SetupAllProperties();
                mockSettings.SetupProperty(s => s.KeyDerivationIterations, 1);
                mockSettings.SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("a", 1));
                mockSettings.SetupProperty(s => s.LeaderboardsConnectionString, new EncryptedSecret("a", 1));
                settings = mockSettings.Object;
            }

            private Mock<TextReader> mockInReader = new Mock<TextReader>(MockBehavior.Strict);
            private TextReader inReader;
            private TextWriter outWriter = new StringWriter();
            private TextWriter errorWriter = new StringWriter();
            private PlayersArgsParser parser;
            private Mock<IPlayersSettings> mockSettings = new Mock<IPlayersSettings>();
            private IPlayersSettings settings;

            [Fact]
            public void HelpFlagIsSpecified_ShowUsageInformation()
            {
                // Arrange
                string[] args = { "--help" };
                IPlayersSettings settings = Settings.Default;
                settings.Reload();

                // Act
                parser.Parse(args, settings);

                // Assert
                var output = outWriter.ToString();
                Assert.Equal(@"
Usage: PlayersService.exe [options]

options:
  --help                Shows usage information.
  --interval=VALUE      The minimum amount of time that should pass between each cycle.
  --delay=VALUE         The amount of time to wait after a cycle to perform garbage collection.
  --ikey=VALUE          An Application Insights instrumentation key.
  --iterations=VALUE    The number of rounds to execute a key derivation function.
  --connection[=VALUE]  The connection string used to connect to the leaderboards database.
  --players=VALUE       The number of players to update.
  --apikey[=VALUE]      A Steam Web API key.
", output, ignoreLineEndingDifferences: true);
            }

            #region PlayersPerUpdate

            [Fact]
            public void PlayersIsSpecified_SetsPlayersPerUpdate()
            {
                // Arrange
                string[] args = { "--players=10" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal(10, settings.PlayersPerUpdate);
            }

            #endregion

            #region SteamWebApiKey

            [Fact]
            public void ApikeyIsSpecified_SetsSteamWebApiKey()
            {
                // Arrange
                string[] args = { "--apikey=myApiKey" };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", 1);
                Assert.Equal(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [Fact]
            public void ApikeyFlagIsSpecified_PromptsUserForApikeyAndSetsSteamWebApiKey()
            {
                // Arrange
                string[] args = { "--apikey" };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", 1);
                Assert.Equal(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [Fact]
            public void ApikeyFlagIsNotSpecifiedAndSteamWebApiKeyIsSet_DoesNotSetSteamWebApiKey()
            {
                // Arrange
                string[] args = { };

                // Act
                parser.Parse(args, settings);

                // Assert
                mockSettings.VerifySet(s => s.SteamWebApiKey = It.IsAny<EncryptedSecret>(), Times.Never);
            }

            #endregion
        }
    }
}
