using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Moq;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    public class WorkerRoleTests
    {
        public WorkerRoleTests()
        {
            mockSettings.SetupAllProperties();
            mockSettings.SetupProperty(s => s.LeaderboardsConnectionString, new EncryptedSecret("a", 1));
            mockSettings.SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("a", 1));
            settings = mockSettings.Object;
            telemetryClient = new TelemetryClient();
            worker = new WorkerRoleAdapter(settings, telemetryClient);
        }

        private readonly Mock<IPlayersSettings> mockSettings = new Mock<IPlayersSettings>();
        private readonly IPlayersSettings settings;
        private readonly TelemetryClient telemetryClient;
        private readonly WorkerRoleAdapter worker;

        public class RunAsyncOverrideMethod : WorkerRoleTests
        {
            private readonly CancellationToken cancellationToken = CancellationToken.None;

            [Fact]
            public async Task SteamWebApiKeyIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                mockSettings.SetupProperty(s => s.SteamWebApiKey, null);

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return worker.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [Fact]
            public async Task LeaderboardsConnectionStringIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                mockSettings.SetupProperty(s => s.LeaderboardsConnectionString, null);

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return worker.PublicRunAsyncOverride(cancellationToken);
                });
            }
        }

        public class CreateSteamWebApiClientMethod : WorkerRoleTests
        {
            [Fact]
            public void ReturnsInstance()
            {
                // Arrange
                var apiKey = "myApiKey";

                // Act
                var client = worker.CreateSteamWebApiClient(apiKey);

                // Assert
                Assert.IsAssignableFrom<ISteamWebApiClient>(client);
            }
        }

        private class WorkerRoleAdapter : WorkerRole
        {
            public WorkerRoleAdapter(IPlayersSettings settings, TelemetryClient telemetryClient) : base(settings, telemetryClient) { }

            public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
        }
    }
}
