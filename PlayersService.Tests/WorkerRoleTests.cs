using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    class WorkerRoleTests
    {
        [TestClass]
        public class CreateToofzApiHandlerMethod
        {
            [TestMethod]
            public void ReturnsToofzApiHandler()
            {
                // Arrange
                var toofzApiUserName = "myUserName";
                var toofzApiPassword = "myPassword";

                // Act
                var handler = WorkerRole.CreateToofzApiHandler(toofzApiUserName, toofzApiPassword);

                // Assert
                Assert.IsInstanceOfType(handler, typeof(HttpMessageHandler));
            }
        }

        [TestClass]
        public class CreateSteamApiHandler
        {
            [TestMethod]
            public void ReturnsCreateSteamApiHandler()
            {
                // Arrange -> Act
                var handler = WorkerRole.CreateSteamApiHandler();

                // Assert
                Assert.IsInstanceOfType(handler, typeof(HttpMessageHandler));
            }
        }

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
    }
}
