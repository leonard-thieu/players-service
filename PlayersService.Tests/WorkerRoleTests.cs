using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.PlayersService.Tests.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamUser;
using toofz.NecroDancer.Leaderboards.toofz;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    class WorkerRoleTests
    {
        [TestClass]
        public class OnStartMethod
        {
            [TestMethod]
            public void ToofzApiUserNameIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    ToofzApiUserName = null,
                    ToofzApiPassword = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRole(settings);

                // Act -> Assert
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [TestMethod]
            public void ToofzApiUserNameIsEmpty_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    ToofzApiUserName = "",
                    ToofzApiPassword = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRole(settings);

                // Act -> Assert
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [TestMethod]
            public void ToofzApiPasswordIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    ToofzApiUserName = "myUserName",
                    ToofzApiPassword = null,
                };
                var workerRole = new WorkerRole(settings);

                // Act -> Assert
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }
        }

        [TestClass]
        public class RunAsyncOverrideMethod
        {
            [TestMethod]
            public async Task SteamWebApiKeyIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    SteamWebApiKey = null,
                };
                var workerRole = new WorkerRoleAdapter(settings);
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            class WorkerRoleAdapter : WorkerRole
            {
                public WorkerRoleAdapter(IPlayersSettings settings) : base(settings) { }

                public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
            }
        }

        [TestClass]
        public class UpdatePlayersAsyncMethod
        {
            [TestMethod]
            public async Task ApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new StubPlayersSettings();
                var workerRole = new WorkerRole(settings);
                IToofzApiClient toofzApiClient = null;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var limit = 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdatePlayersAsync(toofzApiClient, steamWebApiClient, limit);
                });
            }

            [TestMethod]
            public async Task SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new StubPlayersSettings();
                var workerRole = new WorkerRole(settings);
                var toofzApiClient = Mock.Of<IToofzApiClient>();
                ISteamWebApiClient steamWebApiClient = null;
                var limit = 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdatePlayersAsync(toofzApiClient, steamWebApiClient, limit);
                });
            }

            [TestMethod]
            public async Task LimitIsNegative_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var settings = new StubPlayersSettings();
                var workerRole = new WorkerRole(settings);
                var toofzApiClient = Mock.Of<IToofzApiClient>();
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var limit = -1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return workerRole.UpdatePlayersAsync(toofzApiClient, steamWebApiClient, limit);
                });
            }

            [TestMethod]
            public async Task UpdatesPlayers()
            {
                // Arrange
                var settings = new StubPlayersSettings();
                var workerRole = new WorkerRole(settings);
                var mockToofzApiClient = new Mock<IToofzApiClient>();
                mockToofzApiClient
                    .Setup(c => c.GetPlayersAsync(It.IsAny<GetPlayersParams>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new PlayersEnvelope()));
                mockToofzApiClient
                    .Setup(c => c.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new BulkStoreDTO()));
                var toofzApiClient = mockToofzApiClient.Object;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var limit = 1;

                // Act
                await workerRole.UpdatePlayersAsync(toofzApiClient, steamWebApiClient, limit);

                // Assert
                mockToofzApiClient.Verify(apiClient => apiClient.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), It.IsAny<CancellationToken>()));
            }
        }

        [TestClass]
        public class GetStalePlayersAsyncMethod
        {
            [TestMethod]
            public async Task ReturnsPlayersToUpdate()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var mockToofzApiClient = new Mock<IToofzApiClient>();
                mockToofzApiClient
                    .Setup(c => c.GetPlayersAsync(It.IsAny<GetPlayersParams>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new PlayersEnvelope()));
                var toofzApiClient = mockToofzApiClient.Object;
                var limit = 100;
                var cancellationToken = CancellationToken.None;

                // Act
                var stalePlayers = await workerRole.GetStalePlayersAsync(toofzApiClient, limit, cancellationToken);

                // Assert
                Assert.IsInstanceOfType(stalePlayers, typeof(IEnumerable<PlayerDTO>));
            }
        }

        [TestClass]
        public class DownloadPlayersAsyncMethod
        {
            [TestMethod]
            public async Task StalePlayersCountGreaterThanPlayersPerRequest_RequestsPlayersInBatches()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var playerSummariesEnvelope = new PlayerSummariesEnvelope { Response = new PlayerSummaries() };
                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
                mockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(playerSummariesEnvelope));
                var steamWebApiClient = mockSteamWebApiClient.Object;
                var stalePlayers = new List<PlayerDTO>
                {
                    new PlayerDTO(),
                    new PlayerDTO(),
                };
                var playersPerRequest = 1;
                var cancellationToken = CancellationToken.None;

                // Act
                await workerRole.DownloadPlayersAsync(steamWebApiClient, stalePlayers, playersPerRequest, cancellationToken);

                // Assert
                mockSteamWebApiClient.Verify(s => s.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            }

            [TestMethod]
            public async Task ReturnsPlayers()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var playerSummariesEnvelope = new PlayerSummariesEnvelope { Response = new PlayerSummaries() };
                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
                mockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(playerSummariesEnvelope));
                var steamWebApiClient = mockSteamWebApiClient.Object;
                var stalePlayers = new List<PlayerDTO>
                {
                    new PlayerDTO(),
                    new PlayerDTO(),
                };
                var playersPerRequest = 100;
                var cancellationToken = CancellationToken.None;

                // Act
                var players = await workerRole.DownloadPlayersAsync(steamWebApiClient, stalePlayers, playersPerRequest, cancellationToken);

                // Assert
                Assert.IsInstanceOfType(players, typeof(IEnumerable<Player>));
            }
        }

        [TestClass]
        public class CreateStubsForNonExistingPlayersMethod
        {
            [TestMethod]
            public void PlayerExists_SetsExistsToTrue()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var stalePlayers = new List<PlayerDTO>
                {
                    new PlayerDTO { Id = 1 },
                };
                var player = new Player
                {
                    SteamId = 1,
                    Exists = false,
                };
                var players = new List<Player> { player };

                // Act
                workerRole.CreateStubsForNonExistingPlayers(stalePlayers, players);

                // Assert
                Assert.IsTrue(player.Exists.Value);
            }

            [TestMethod]
            public void PlayerDoesNotExist_CreatesStubPlayer()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var stalePlayers = new List<PlayerDTO>
                {
                    new PlayerDTO { Id = 1 },
                };
                var players = new List<Player>();

                // Act
                var players2 = workerRole.CreateStubsForNonExistingPlayers(stalePlayers, players);

                // Assert
                var player = players2.First();
                Assert.IsFalse(player.Exists.Value);
                Assert.AreEqual(1, player.SteamId);
                Assert.IsNotNull(player.LastUpdate);
            }
        }

        [TestClass]
        public class GetPlayersAsyncMethod
        {
            [TestMethod]
            public async Task ReturnsPlayers()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var envelope = JsonConvert.DeserializeObject<PlayerSummariesEnvelope>(Resources.GetPlayerSummaries);
                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
                mockSteamWebApiClient
                    .Setup(s => s.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(envelope));
                var steamWebApiClient = mockSteamWebApiClient.Object;
                var ids = new List<long>();
                IProgress<long> progress = null;
                var cancellationToken = CancellationToken.None;

                // Act
                var players = await workerRole.GetPlayersAsync(steamWebApiClient, ids, progress, cancellationToken);

                // Assert
                Assert.IsInstanceOfType(players, typeof(IEnumerable<Player>));
            }
        }

        [TestClass]
        public class StorePlayersAsyncMethod
        {
            [TestMethod]
            public async Task StoresPlayers()
            {
                // Arrange
                var settings = Mock.Of<IPlayersSettings>();
                var workerRole = new WorkerRole(settings);
                var mockToofzApiClient = new Mock<IToofzApiClient>();
                mockToofzApiClient
                    .Setup(c => c.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new BulkStoreDTO()));
                var toofzApiClient = mockToofzApiClient.Object;
                var players = new List<Player>();
                var cancellationToken = CancellationToken.None;

                // Act
                await workerRole.StorePlayersAsync(toofzApiClient, players, cancellationToken);

                // Assert
                mockToofzApiClient.Verify(c => c.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }
    }
}
