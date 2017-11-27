using System;
using System.Data.Entity.Infrastructure;
using System.Net.Http;
using log4net;
using Microsoft.ApplicationInsights;
using Ninject;
using Ninject.Extensions.NamedScope;
using Ninject.Syntax;
using Polly;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal static class KernelConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        public static IKernel CreateKernel(IPlayersSettings settings, TelemetryClient telemetryClient)
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<IPlayersSettings>()
                      .ToConstant(settings);
                kernel.Bind<TelemetryClient>()
                      .ToConstant(telemetryClient);

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
                  .ToMethod(c =>
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
                  })
                  .WhenInjectedInto(typeof(LeaderboardsContext), typeof(LeaderboardsStoreClient));

            kernel.Bind<ILeaderboardsContext>()
                  .To<LeaderboardsContext>()
                  .InParentScope();

            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<LeaderboardsStoreClient>()
                  .WhenSteamWebApiKeyIsSet()
                  .InParentScope();
            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<FakeLeaderboardsStoreClient>()
                  .InParentScope();

            RegisterSteamWebApiClient(kernel);

            kernel.Bind<PlayersWorker>()
                  .ToSelf()
                  .InScope(c => c);
        }

        #region SteamWebApiClient

        private static void RegisterSteamWebApiClient(StandardKernel kernel)
        {
            kernel.Bind<HttpMessageHandler>()
                  .ToMethod(c =>
                  {
                      var telemetryClient = c.Kernel.Get<TelemetryClient>();
                      var log = c.Kernel.Get<ILog>();

                      return CreateSteamWebApiClientHandler(new WebRequestHandler(), log, telemetryClient);
                  })
                .WhenInjectedInto(typeof(SteamWebApiClient))
                .InParentScope();
            kernel.Bind<ISteamWebApiClient>()
                  .To<SteamWebApiClient>()
                  .WhenSteamWebApiKeyIsSet()
                  .InParentScope()
                  .WithPropertyValue(nameof(SteamWebApiClient.SteamWebApiKey), c => c.Kernel.Get<IPlayersSettings>().SteamWebApiKey.Decrypt());

            kernel.Bind<ISteamWebApiClient>()
                  .To<FakeSteamWebApiClient>()
                  .InParentScope();
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

        #endregion
    }

    internal static class IBindingWhenSyntaxExtensions
    {
        public static IBindingInNamedWithOrOnSyntax<T> WhenSteamWebApiKeyIsSet<T>(this IBindingWhenSyntax<T> binding)
        {
            return binding.When(r => r.ParentContext.Kernel.Get<IPlayersSettings>().SteamWebApiKey != null);
        }
    }
}
