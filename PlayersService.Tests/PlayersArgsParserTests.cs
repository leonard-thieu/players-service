using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    class PlayersArgsParserTests
    {
        [TestClass]
        public class Parse
        {
            public Parse()
            {
                inReader = mockInReader.Object;
                parser = new PlayersArgsParser(inReader, outWriter, errorWriter);

                mockSettings
                    .SetupAllProperties()
                    .SetupProperty(s => s.PlayersUserName, "myPlayersUserName")
                    .SetupProperty(s => s.PlayersPassword, new EncryptedSecret("myPlayersPassword"))
                    .SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("mySteamWebApiKey"));
                settings = mockSettings.Object;
            }

            Mock<TextReader> mockInReader = new Mock<TextReader>(MockBehavior.Strict);
            TextReader inReader;
            TextWriter outWriter = new StringWriter();
            TextWriter errorWriter = new StringWriter();
            PlayersArgsParser parser;
            Mock<IPlayersSettings> mockSettings = new Mock<IPlayersSettings>();
            IPlayersSettings settings;

            [TestMethod]
            public void PlayersIsSpecified_SetsPlayersPerUpdateToPlayers()
            {
                // Arrange
                string[] args = new[] { "--players=10" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(10, settings.PlayersPerUpdate);
            }

            [TestMethod]
            public void ToofzIsSpecified_SetsToofzApiBaseAddressToToofz()
            {
                // Arrange
                string[] args = new[] { "--toofz=http://localhost/" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("http://localhost/", settings.ToofzApiBaseAddress);
            }

            [TestMethod]
            public void UserNameIsSpecified_SetPlayersUserNameToUserName()
            {
                // Arrange
                string[] args = new[] { "--username=myUser" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("myUser", settings.PlayersUserName);
            }

            [TestMethod]
            public void PasswordIsSpecified_SetsPlayersPasswordToEncryptedPassword()
            {
                // Arrange
                string[] args = new[] { "--password=myPassword" };
                var encrypted = new EncryptedSecret("myPassword");

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(encrypted.Decrypt(), settings.PlayersPassword.Decrypt());
            }

            [TestMethod]
            public void ApikeyIsSpecified_SetsSteamWebApiKeyToEncryptedApikey()
            {
                // Arrange
                string[] args = new[] { "--apikey=myApiKey" };
                var encrypted = new EncryptedSecret("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [TestMethod]
            public void IkeyIsSpecified_SetsPlayersInstrumentationKeyToIkey()
            {
                // Arrange
                string[] args = new[] { "--ikey=myInstrumentationKey" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("myInstrumentationKey", settings.PlayersInstrumentationKey);
            }
        }
    }
}
