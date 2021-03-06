﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Moq;
using toofz.Data;
using toofz.Steam.WebApi;
using toofz.Steam.WebApi.ISteamUser;
using Xunit;

namespace toofz.Services.PlayersService.Tests
{
    public class PlayersWorkerTests
    {
        public PlayersWorkerTests()
        {
            var context = new NecroDancerContext(necroDancerContextOptions);

            worker = new PlayersWorker(context, mockSteamWebApiClient.Object, mockStoreClient.Object, telemetryClient);
        }

        private readonly Mock<ISteamWebApiClient> mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
        private readonly Mock<ILeaderboardsStoreClient> mockStoreClient = new Mock<ILeaderboardsStoreClient>();
        private readonly TelemetryClient telemetryClient = new TelemetryClient();
        private readonly PlayersWorker worker;

        private readonly DbContextOptions<NecroDancerContext> necroDancerContextOptions = new DbContextOptionsBuilder<NecroDancerContext>()
            .UseInMemoryDatabase(databaseName: Constants.NecroDancerContextName)
            .Options;

        public class Constructor
        {
            [DisplayFact]
            public void ReturnsInstance()
            {
                // Arrange
                var db = Mock.Of<ILeaderboardsContext>();
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var storeClient = Mock.Of<ILeaderboardsStoreClient>();
                var telemetryClient = new TelemetryClient();

                // Act
                var worker = new PlayersWorker(db, steamWebApiClient, storeClient, telemetryClient);

                // Assert
                Assert.IsAssignableFrom<PlayersWorker>(worker);
            }
        }

        public class GetPlayersAsyncMethod : PlayersWorkerTests
        {
            private readonly CancellationToken cancellationToken = CancellationToken.None;

            [DisplayFact]
            public async Task ReturnsPlayers()
            {
                // Arrange
                var limit = 100;

                // Act
                var players = await worker.GetPlayersAsync(limit, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Player>>(players);
            }
        }

        public class UpdatePlayersAsyncMethod : PlayersWorkerTests
        {
            private readonly List<Player> players = new List<Player>();
            private int playersPerRequest = 100;
            private readonly CancellationToken cancellationToken = CancellationToken.None;

            [DisplayFact]
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
                await worker.UpdatePlayersAsync(players, playersPerRequest, cancellationToken);

                // Assert
                mockSteamWebApiClient.Verify(s => s.GetPlayerSummariesAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<IProgress<long>>(), cancellationToken), Times.Exactly(2));
            }

            [DisplayFact]
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
                await worker.UpdatePlayersAsync(players, playersPerRequest, cancellationToken);

                // Assert
                Assert.True(player.Exists == true);
                Assert.NotNull(player.LastUpdate);
                Assert.Equal("myPersonaName", player.Name);
                Assert.Equal("http://example.org/", player.Avatar);
            }

            [DisplayFact]
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
                await worker.UpdatePlayersAsync(players, playersPerRequest, cancellationToken);


                // Assert
                Assert.True(player.Exists == false);
                Assert.NotNull(player.LastUpdate);
            }
        }

        public class StorePlayersAsyncMethod : PlayersWorkerTests
        {
            private readonly List<Player> players = new List<Player>();
            private readonly CancellationToken cancellationToken = CancellationToken.None;

            [DisplayFact]
            public async Task StoresPlayers()
            {
                // Arrange
                var players = new List<Player>();
                mockStoreClient.Setup(c => c.BulkUpsertAsync(players, null, cancellationToken)).ReturnsAsync(players.Count);

                // Act
                await worker.StorePlayersAsync(players, cancellationToken);

                // Assert
                mockStoreClient.Verify(c => c.BulkUpsertAsync(players, null, cancellationToken), Times.Once);
            }
        }
    }
}
