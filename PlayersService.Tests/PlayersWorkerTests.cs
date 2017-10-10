using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamUser;
using toofz.NecroDancer.Leaderboards.toofz;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    class PlayersWorkerTests
    {
        public PlayersWorkerTests()
        {
            ToofzApiClient = MockToofzApiClient.Object;
            SteamWebApiClient = MockSteamWebApiClient.Object;
        }

        public PlayersWorker Worker { get; set; } = new PlayersWorker();
        public Mock<IToofzApiClient> MockToofzApiClient { get; set; } = new Mock<IToofzApiClient>();
        public IToofzApiClient ToofzApiClient { get; set; }
        public Mock<ISteamWebApiClient> MockSteamWebApiClient { get; set; } = new Mock<ISteamWebApiClient>();
        public ISteamWebApiClient SteamWebApiClient { get; set; }
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        [TestClass]
        public class Constructor
        {
            [TestMethod]
            public void ReturnsInstance()
            {
                // Arrange -> Act
                var worker = new PlayersWorker();

                // Assert
                Assert.IsInstanceOfType(worker, typeof(PlayersWorker));
            }
        }

        [TestClass]
        public class GetPlayersAsyncMethod : PlayersWorkerTests
        {
            [TestMethod]
            public async Task ToofzApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                ToofzApiClient = null;
                var limit = 100;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return Worker.GetPlayersAsync(ToofzApiClient, limit, CancellationToken);
                });
            }

            [TestMethod]
            public async Task LimitIsNonpositive_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var limit = 0;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return Worker.GetPlayersAsync(ToofzApiClient, limit, CancellationToken);
                });
            }

            [TestMethod]
            public async Task ReturnsPlayers()
            {
                // Arrange
                var playersEnvelope = new PlayersEnvelope { Players = new List<PlayerDTO>() };
                MockToofzApiClient
                    .Setup(c => c.GetPlayersAsync(It.IsAny<GetPlayersParams>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playersEnvelope);
                var limit = 100;

                // Act
                var players = await Worker.GetPlayersAsync(ToofzApiClient, limit, CancellationToken);

                // Assert
                Assert.IsInstanceOfType(players, typeof(IEnumerable<Player>));
            }
        }

        [TestClass]
        public class UpdatePlayersAsyncMethod : PlayersWorkerTests
        {
            public List<Player> Players { get; set; } = new List<Player>();
            public int PlayersPerRequest { get; set; } = 100;

            [TestMethod]
            public async Task SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                SteamWebApiClient = null;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);
                });
            }

            [TestMethod]
            public async Task PlayersIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                Players = null;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);
                });
            }

            [TestMethod]
            public async Task PlayersPerRequestIsNonpositive_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                PlayersPerRequest = 0;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);
                });
            }

            [TestMethod]
            public async Task PlayersPerRequestIsGreaterThanMaxPlayerSummariesPerRequest_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                PlayersPerRequest = Steam.WebApi.SteamWebApiClient.MaxPlayerSummariesPerRequest + 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);
                });
            }

            [TestMethod]
            public async Task StalePlayersCountGreaterThanPlayersPerRequest_RequestsPlayersInBatches()
            {
                // Arrange
                var playerSummariesEnvelope = new PlayerSummariesEnvelope
                {
                    Response = new PlayerSummaries { Players = new List<PlayerSummary>() },
                };
                MockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                Players.AddRange(new[]
                {
                    new Player(),
                    new Player(),
                });
                PlayersPerRequest = 1;

                // Act
                await Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);

                // Assert
                MockSteamWebApiClient.Verify(s => s.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), CancellationToken), Times.Exactly(2));
            }

            [TestMethod]
            public async Task HasMatchingPlayerSummary_UpdatesPlayer()
            {
                // Arrange
                var playerSummariesEnvelope = new PlayerSummariesEnvelope
                {
                    Response = new PlayerSummaries
                    {
                        Players = new List<PlayerSummary>
                        {
                            new PlayerSummary
                            {
                                SteamId = 1,
                                PersonaName = "myPersonaName",
                                Avatar = "http://example.org/",
                            },
                        },
                    },
                };
                MockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                var player = new Player { SteamId = 1 };
                Players.Add(player);

                // Act
                await Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);

                // Assert
                Assert.IsTrue(player.Exists == true);
                Assert.IsNotNull(player.LastUpdate);
                Assert.AreEqual("myPersonaName", player.Name);
                Assert.AreEqual("http://example.org/", player.Avatar);
            }

            [TestMethod]
            public async Task DoesNotHaveMatchingPlayerSummary_MarksPlayerAsNonExistent()
            {
                // Arrange
                var playerSummariesEnvelope = new PlayerSummariesEnvelope
                {
                    Response = new PlayerSummaries { Players = new List<PlayerSummary>() },
                };
                MockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                var player = new Player { SteamId = 1 };
                Players.Add(player);

                // Act
                await Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);


                // Assert
                Assert.IsTrue(player.Exists == false);
                Assert.IsNotNull(player.LastUpdate);
            }
        }

        [TestClass]
        public class StorePlayersAsyncMethod : PlayersWorkerTests
        {
            public List<Player> Players { get; set; } = new List<Player>();

            [TestMethod]
            public async Task ToofzApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                ToofzApiClient = null;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return Worker.StorePlayersAsync(ToofzApiClient, Players, CancellationToken);
                });
            }

            [TestMethod]
            public async Task PlayersIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                Players = null;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return Worker.StorePlayersAsync(ToofzApiClient, Players, CancellationToken);
                });
            }

            [TestMethod]
            public async Task StoresPlayers()
            {
                // Arrange
                MockToofzApiClient
                    .Setup(c => c.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), CancellationToken))
                    .ReturnsAsync(new BulkStoreDTO());
                var players = new List<Player>();

                // Act
                await Worker.StorePlayersAsync(ToofzApiClient, players, CancellationToken);

                // Assert
                MockToofzApiClient.Verify(c => c.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), CancellationToken), Times.Once);
            }
        }
    }
}
