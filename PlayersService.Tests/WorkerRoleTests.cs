using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    class WorkerRoleTests
    {
        [TestClass]
        public class UpdatePlayersAsync
        {
            [TestMethod]
            public async Task ApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new SimplePlayersSettings();
                var workerRole = new WorkerRole(settings);

                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdatePlayersAsync(
                        null,
                        mockSteamWebApiClient.Object,
                        1);
                });
            }

            [TestMethod]
            public async Task SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new SimplePlayersSettings();
                var workerRole = new WorkerRole(settings);

                var mockIToofzApiClient = new Mock<IToofzApiClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdatePlayersAsync(
                        mockIToofzApiClient.Object,
                        null,
                        1);
                });
            }

            [TestMethod]
            public async Task LimitIsNegative_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var settings = new SimplePlayersSettings();
                var workerRole = new WorkerRole(settings);

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return workerRole.UpdatePlayersAsync(
                        mockIToofzApiClient.Object,
                        mockSteamWebApiClient.Object,
                        -1);
                });
            }

            [TestMethod]
            public async Task UpdatesPlayers()
            {
                // Arrange
                var settings = new SimplePlayersSettings();
                var workerRole = new WorkerRole(settings);

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                mockIToofzApiClient
                    .Setup(toofzApiClient => toofzApiClient.GetPlayersAsync(It.IsAny<GetPlayersParams>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new PlayersEnvelope()));
                mockIToofzApiClient
                    .Setup(toofzApiClient => toofzApiClient.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new BulkStoreDTO()));

                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();

                // Act
                await workerRole.UpdatePlayersAsync(
                    mockIToofzApiClient.Object,
                    mockSteamWebApiClient.Object,
                    1);

                // Assert
                mockIToofzApiClient.Verify(apiClient => apiClient.PostPlayersAsync(It.IsAny<IEnumerable<Player>>(), It.IsAny<CancellationToken>()));
            }
        }
    }
}
