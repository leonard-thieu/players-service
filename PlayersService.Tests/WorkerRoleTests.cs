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
        }

        private readonly Mock<IPlayersSettings> mockSettings = new Mock<IPlayersSettings>();
        private readonly IPlayersSettings settings;

        public class CreateSteamWebApiClientMethod
        {
            [Fact]
            public void ReturnsInstance()
            {
                // Arrange
                var apiKey = "myApiKey";
                var telemetryClient = new TelemetryClient();

                // Act
                var client = WorkerRole.CreateSteamWebApiClient(apiKey, telemetryClient);

                // Assert
                Assert.IsAssignableFrom<ISteamWebApiClient>(client);
            }
        }

        public class RunAsyncOverrideMethod : WorkerRoleTests
        {
            public RunAsyncOverrideMethod()
            {
                worker = new WorkerRoleAdapter(settings);
            }

            private readonly CancellationToken cancellationToken = CancellationToken.None;
            private readonly WorkerRoleAdapter worker;

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

            private class WorkerRoleAdapter : WorkerRole
            {
                public WorkerRoleAdapter(IPlayersSettings settings) : base(settings, new TelemetryClient()) { }

                public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
            }
        }
    }
}
