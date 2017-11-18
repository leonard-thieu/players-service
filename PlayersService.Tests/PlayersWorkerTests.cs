using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Moq;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamUser;
using toofz.NecroDancer.Leaderboards.toofz;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    public class PlayersWorkerTests
    {
        public PlayersWorkerTests()
        {
            worker = new PlayersWorker(telemetryClient);
            toofzApiClient = mockToofzApiClient.Object;
            steamWebApiClient = mockSteamWebApiClient.Object;
        }

        private TelemetryClient telemetryClient = new TelemetryClient();
        private PlayersWorker worker;
        private Mock<IToofzApiClient> mockToofzApiClient = new Mock<IToofzApiClient>();
        private IToofzApiClient toofzApiClient;
        private Mock<ISteamWebApiClient> mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
        private ISteamWebApiClient steamWebApiClient;
        private CancellationToken cancellationToken = CancellationToken.None;

        public class Constructor
        {
            [Fact]
            public void ReturnsInstance()
            {
                // Arrange
                var telemetryClient = new TelemetryClient();

                // Act
                var worker = new PlayersWorker(telemetryClient);

                // Assert
                Assert.IsAssignableFrom<PlayersWorker>(worker);
            }
        }

        public class GetPlayersAsyncMethod : PlayersWorkerTests
        {
            [Fact]
            public async Task ReturnsPlayers()
            {
                // Arrange
                var playersEnvelope = new PlayersEnvelope { Players = new List<PlayerDTO>() };
                mockToofzApiClient
                    .Setup(c => c.GetPlayersAsync(It.IsAny<GetPlayersParams>(), It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(playersEnvelope);
                var limit = 100;

                // Act
                var players = await worker.GetPlayersAsync(toofzApiClient, limit, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Player>>(players);
            }
        }

        public class UpdatePlayersAsyncMethod : PlayersWorkerTests
        {
            private List<Player> players = new List<Player>();
            private int playersPerRequest = 100;

            [Fact]
            public async Task StalePlayersCountGreaterThanPlayersPerRequest_RequestsPlayersInBatches()
            {
                // Arrange
                var playerSummariesEnvelope = new PlayerSummariesEnvelope
                {
                    Response = new PlayerSummaries { Players = new List<PlayerSummary>() },
                };
                mockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                players.AddRange(new[]
                {
                    new Player(),
                    new Player(),
                });
                playersPerRequest = 1;

                // Act
                await worker.UpdatePlayersAsync(steamWebApiClient, players, playersPerRequest, cancellationToken);

                // Assert
                mockSteamWebApiClient.Verify(s => s.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), cancellationToken), Times.Exactly(2));
            }

            [Fact]
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
                mockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                var player = new Player { SteamId = 1 };
                players.Add(player);

                // Act
                await worker.UpdatePlayersAsync(steamWebApiClient, players, playersPerRequest, cancellationToken);

                // Assert
                Assert.True(player.Exists == true);
                Assert.NotNull(player.LastUpdate);
                Assert.Equal("myPersonaName", player.Name);
                Assert.Equal("http://example.org/", player.Avatar);
            }

            [Fact]
            public async Task DoesNotHaveMatchingPlayerSummary_MarksPlayerAsNonExistent()
            {
                // Arrange
                var playerSummariesEnvelope = new PlayerSummariesEnvelope
                {
                    Response = new PlayerSummaries { Players = new List<PlayerSummary>() },
                };
                mockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                var player = new Player { SteamId = 1 };
                players.Add(player);

                // Act
                await worker.UpdatePlayersAsync(steamWebApiClient, players, playersPerRequest, cancellationToken);


                // Assert
                Assert.True(player.Exists == false);
                Assert.NotNull(player.LastUpdate);
            }
        }

        public class StorePlayersAsyncMethod : PlayersWorkerTests
        {
            public StorePlayersAsyncMethod()
            {
                storeClient = mockStoreClient.Object;
            }

            private Mock<ILeaderboardsStoreClient> mockStoreClient = new Mock<ILeaderboardsStoreClient>();
            private ILeaderboardsStoreClient storeClient;
            private List<Player> players = new List<Player>();

            [Fact]
            public async Task StoresPlayers()
            {
                // Arrange
                var players = new List<Player>();
                mockStoreClient
                    .Setup(c => c.BulkUpsertAsync(players, cancellationToken))
                    .ReturnsAsync(players.Count);

                // Act
                await worker.StorePlayersAsync(storeClient, players, cancellationToken);

                // Assert
                mockStoreClient.Verify(c => c.BulkUpsertAsync(players, cancellationToken), Times.Once);
            }
        }
    }
}
