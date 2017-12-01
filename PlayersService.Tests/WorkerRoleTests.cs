using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Moq;
using Ninject.Extensions.NamedScope;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    public class WorkerRoleTests
    {
        public class IntegrationTests : DatabaseTestsBase
        {
            private readonly string settingsFileName = Path.GetTempFileName();
            private readonly Mock<ILog> mockLog = new Mock<ILog>();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (File.Exists(settingsFileName)) { File.Delete(settingsFileName); }
                }

                base.Dispose(disposing);
            }

            [Trait("Category", "Uses file system")]
            [Fact]
            public async Task ExecutesUpdateCycle()
            {
                // Arrange
                for (int i = 1; i <= 10; i++)
                {
                    db.Players.Add(new Player { SteamId = i });
                }
                db.SaveChanges();

                var settings = Settings.Default;
                // Should only loop once
                foreach (SettingsProvider provider in settings.Providers)
                {
                    var ssp = (ServiceSettingsProvider)provider;
                    ssp.GetSettingsReader = () => File.OpenText(settingsFileName);
                    ssp.GetSettingsWriter = () => File.CreateText(settingsFileName);
                }
                settings.UpdateInterval = TimeSpan.Zero;
                var telemetryClient = new TelemetryClient();
                var runOnce = true;

                var kernel = KernelConfig.CreateKernel();

                var connectionString = StorageHelper.GetDatabaseConnectionString();
                kernel.Rebind<string>()
                      .ToConstant(connectionString)
                      .WhenInjectedInto(typeof(LeaderboardsContext), typeof(LeaderboardsStoreClient));

                kernel.Rebind<ILeaderboardsStoreClient>()
                      .To<LeaderboardsStoreClient>()
                      .InParentScope();

                kernel.Rebind<ISteamWebApiClient>()
                      .To<FakeSteamWebApiClient>()
                      .InParentScope();

                var log = mockLog.Object;
                var worker = new WorkerRole(settings, telemetryClient, runOnce, kernel, log);

                // Act
                worker.Start();
                await worker.Completion;

                // Assert
                Assert.NotEqual(0, db.Players.Count());
                Assert.True(db.Players.Any(l => l.LastUpdate != null));
            }
        }
    }
}
