using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Moq;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
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
            telemetryClient = new TelemetryClient();
            worker = new WorkerRoleAdapter(mockSettings.Object, telemetryClient);
        }

        private readonly Mock<IPlayersSettings> mockSettings = new Mock<IPlayersSettings>();
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

        private class WorkerRoleAdapter : WorkerRole
        {
            public WorkerRoleAdapter(IPlayersSettings settings, TelemetryClient telemetryClient) : base(settings, telemetryClient) { }

            public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
        }
    }
}
