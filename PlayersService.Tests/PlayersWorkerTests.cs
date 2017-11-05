using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            ToofzApiClient = MockToofzApiClient.Object;
            SteamWebApiClient = MockSteamWebApiClient.Object;
        }

        internal PlayersWorker Worker { get; set; } = new PlayersWorker();
        public Mock<IToofzApiClient> MockToofzApiClient { get; set; } = new Mock<IToofzApiClient>();
        public IToofzApiClient ToofzApiClient { get; set; }
        public Mock<ISteamWebApiClient> MockSteamWebApiClient { get; set; } = new Mock<ISteamWebApiClient>();
        public ISteamWebApiClient SteamWebApiClient { get; set; }
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public class Constructor
        {
            [Fact]
            public void ReturnsInstance()
            {
                // Arrange -> Act
                var worker = new PlayersWorker();

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
                MockToofzApiClient
                    .Setup(c => c.GetPlayersAsync(It.IsAny<GetPlayersParams>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playersEnvelope);
                var limit = 100;

                // Act
                var players = await Worker.GetPlayersAsync(ToofzApiClient, limit, CancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Player>>(players);
            }
        }

        public class UpdatePlayersAsyncMethod : PlayersWorkerTests
        {
            public List<Player> Players { get; set; } = new List<Player>();
            public int PlayersPerRequest { get; set; } = 100;

            [Fact]
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
                MockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                var player = new Player { SteamId = 1 };
                Players.Add(player);

                // Act
                await Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);

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
                MockSteamWebApiClient
                    .Setup(c => c.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), CancellationToken))
                    .ReturnsAsync(playerSummariesEnvelope);
                var player = new Player { SteamId = 1 };
                Players.Add(player);

                // Act
                await Worker.UpdatePlayersAsync(SteamWebApiClient, Players, PlayersPerRequest, CancellationToken);


                // Assert
                Assert.True(player.Exists == false);
                Assert.NotNull(player.LastUpdate);
            }
        }

        public class StorePlayersAsyncMethod : PlayersWorkerTests
        {
            public List<Player> Players { get; set; } = new List<Player>();

            [Fact]
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
