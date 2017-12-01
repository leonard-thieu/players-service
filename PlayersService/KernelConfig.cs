using System;
using System.Data.Entity.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using log4net;
using Microsoft.ApplicationInsights;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.NamedScope;
using Polly;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    [ExcludeFromCodeCoverage]
    internal static class KernelConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        public static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                RegisterServices(kernel);

                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(StandardKernel kernel)
        {
            kernel.Bind<ILog>()
                  .ToConstant(Log);

            kernel.Bind<string>()
                  .ToMethod(GetLeaderboardsConnectionString)
                  .WhenInjectedInto(typeof(LeaderboardsContext), typeof(LeaderboardsStoreClient));

            kernel.Bind<ILeaderboardsContext>()
                  .To<LeaderboardsContext>()
                  .When(DatabaseContainsPlayers)
                  .InParentScope();
            kernel.Bind<ILeaderboardsContext>()
                  .To<FakeLeaderboardsContext>()
                  .InParentScope();

            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<LeaderboardsStoreClient>()
                  .When(SteamWebApiKeyIsSet)
                  .InParentScope();
            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<FakeLeaderboardsStoreClient>()
                  .InParentScope();

            kernel.Bind<HttpMessageHandler>()
                  .ToMethod(GetSteamWebApiClientHandler)
                  .WhenInjectedInto<SteamWebApiClient>()
                  .InParentScope();
            kernel.Bind<ISteamWebApiClient>()
                  .To<SteamWebApiClient>()
                  .When(SteamWebApiKeyIsSet)
                  .InParentScope()
                  .WithPropertyValue(nameof(SteamWebApiClient.SteamWebApiKey), GetSteamWebApiKey);
            kernel.Bind<ISteamWebApiClient>()
                  .To<FakeSteamWebApiClient>()
                  .InParentScope();

            kernel.Bind<PlayersWorker>()
                  .ToSelf()
                  .InScope(c => c);
        }

        private static string GetLeaderboardsConnectionString(IContext c)
        {
            var settings = c.Kernel.Get<IPlayersSettings>();

            if (settings.LeaderboardsConnectionString == null)
            {
                var connectionFactory = new LocalDbConnectionFactory("mssqllocaldb");
                using (var connection = connectionFactory.CreateConnection("NecroDancer"))
                {
                    settings.LeaderboardsConnectionString = new EncryptedSecret(connection.ConnectionString, settings.KeyDerivationIterations);
                    settings.Save();
                }
            }

            return settings.LeaderboardsConnectionString.Decrypt();
        }

        private static bool DatabaseContainsPlayers(IRequest r)
        {
            using (var db = r.ParentContext.Kernel.Get<LeaderboardsContext>())
            {
                return db.Players.Any();
            }
        }

        #region SteamWebApiClient

        private static HttpMessageHandler GetSteamWebApiClientHandler(IContext c)
        {
            var telemetryClient = c.Kernel.Get<TelemetryClient>();
            var log = c.Kernel.Get<ILog>();

            return CreateSteamWebApiClientHandler(new WebRequestHandler(), log, telemetryClient);
        }

        internal static HttpMessageHandler CreateSteamWebApiClientHandler(WebRequestHandler innerHandler, ILog log, TelemetryClient telemetryClient)
        {
            var policy = SteamWebApiClient
                .GetRetryStrategy()
                .WaitAndRetryAsync(
                    3,
                    ExponentialBackoff.GetSleepDurationProvider(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(2)),
                    (ex, duration) =>
                    {
                        telemetryClient.TrackException(ex);
                        if (log.IsDebugEnabled) { log.Debug($"Retrying in {duration}...", ex); }
                    });

            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new TransientFaultHandler(policy),
            });
        }

        private static string GetSteamWebApiKey(IContext c)
        {
            return c.Kernel.Get<IPlayersSettings>().SteamWebApiKey.Decrypt();
        }

        #endregion

        private static bool SteamWebApiKeyIsSet(IRequest r)
        {
            return r.ParentContext.Kernel.Get<IPlayersSettings>().SteamWebApiKey != null;
        }
    }
}
