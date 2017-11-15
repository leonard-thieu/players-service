using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    public class WorkerRoleTests
    {
        public class CreateToofzApiHandlerMethod
        {
            [Fact]
            public void ReturnsToofzApiHandler()
            {
                // Arrange
                var toofzApiUserName = "myUserName";
                var toofzApiPassword = "myPassword";

                // Act
                var handler = WorkerRole.CreateToofzApiHandler(toofzApiUserName, toofzApiPassword);

                // Assert
                Assert.IsAssignableFrom<HttpMessageHandler>(handler);
            }
        }

        public class CreateSteamApiHandler
        {
            [Fact]
            public void ReturnsCreateSteamApiHandler()
            {
                // Arrange
                var telemetryClient = new TelemetryClient();

                // Act
                var handler = WorkerRole.CreateSteamApiHandler(telemetryClient);

                // Assert
                Assert.IsAssignableFrom<HttpMessageHandler>(handler);
            }
        }

        public class OnStartMethod
        {
            private readonly TelemetryClient telemetryClient = new TelemetryClient();

            [Fact]
            public void ToofzApiUserNameIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    ToofzApiUserName = null,
                    ToofzApiPassword = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRole(settings, telemetryClient);

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [Fact]
            public void ToofzApiUserNameIsEmpty_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    ToofzApiUserName = "",
                    ToofzApiPassword = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRole(settings, telemetryClient);

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [Fact]
            public void ToofzApiPasswordIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubPlayersSettings
                {
                    ToofzApiUserName = "myUserName",
                    ToofzApiPassword = null,
                };
                var workerRole = new WorkerRole(settings, telemetryClient);

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }
        }

        public class RunAsyncOverrideMethod
        {
            [Fact]
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
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
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
