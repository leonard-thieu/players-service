﻿using System;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Moq;
using Ninject.Extensions.NamedScope;
using toofz.Data;
using toofz.Steam.WebApi;
using Xunit;

namespace toofz.Services.PlayersService.Tests
{
    public class WorkerRoleTests
    {
        public class IntegrationTests : IntegrationTestsBase
        {
            private readonly Mock<ILog> mockLog = new Mock<ILog>();

            [Trait("Category", "Uses file system")]
            [DisplayFact]
            public async Task ExecutesUpdateCycle()
            {
                // Arrange
                for (int i = 1; i <= 10; i++)
                {
                    db.Players.Add(new Player { SteamId = i });
                }
                db.SaveChanges();

                settings.UpdateInterval = TimeSpan.Zero;
                var telemetryClient = new TelemetryClient();
                var runOnce = true;

                var kernel = KernelConfig.CreateKernel();

                kernel.Rebind<string>()
                      .ToConstant(databaseConnectionString)
                      .WhenInjectedInto(typeof(NecroDancerContextOptionsBuilder), typeof(LeaderboardsStoreClient));

                kernel.Rebind<ISteamWebApiClient>()
                      .To<FakeSteamWebApiClient>()
                      .InParentScope();

                var log = mockLog.Object;

                // Act
                using (var worker = new WorkerRole(settings, telemetryClient, runOnce, kernel, log))
                {
                    worker.Start();
                    await worker.Completion;
                }

                // Assert
                Assert.NotEqual(0, db.Players.Count());
                Assert.True(db.Players.Any(l => l.LastUpdate != null));
            }
        }
    }
}
