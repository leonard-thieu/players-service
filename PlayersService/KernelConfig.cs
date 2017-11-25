using System;
using System.Data.SqlClient;
using System.Net.Http;
using log4net;
using Microsoft.ApplicationInsights;
using Ninject;
using Ninject.Extensions.NamedScope;
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
        public static IKernel CreateKernel(TelemetryClient telemetryClient)
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<TelemetryClient>().ToConstant(telemetryClient);
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
            kernel.Bind<ILog>().ToConstant(Log);
            kernel.Bind<IPlayersSettings>().ToConstant(Settings.Default);

            kernel.Bind<string>().ToMethod(c =>
            {
                var settings = c.Kernel.Get<IPlayersSettings>();

                if (settings.LeaderboardsConnectionString == null)
                    throw new InvalidOperationException($"{nameof(Settings.LeaderboardsConnectionString)} is not set.");

                return settings.LeaderboardsConnectionString.Decrypt();
            }).WhenInjectedInto(typeof(LeaderboardsContext), typeof(SqlConnection));

            kernel.Bind<ILeaderboardsContext>().To<LeaderboardsContext>().InParentScope();
            kernel.Bind<ILeaderboardsStoreClient>().To<LeaderboardsStoreClient>().InParentScope();

            RegisterSteamWebApiClient(kernel);

            kernel.Bind<PlayersWorker>().ToSelf().InScope(c => c);
        }

        private static void RegisterSteamWebApiClient(StandardKernel kernel)
        {
            kernel.Bind<HttpMessageHandler>().ToMethod(c =>
            {
                var telemetryClient = c.Kernel.Get<TelemetryClient>();
                var log = c.Kernel.Get<ILog>();

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

                return HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
                {
                    new LoggingHandler(),
                    new GZipHandler(),
                    new TransientFaultHandler(policy),
                });
            }).WhenInjectedInto(typeof(SteamWebApiClient)).InParentScope();
            kernel.Bind<ISteamWebApiClient>().To<SteamWebApiClient>().InParentScope().WithPropertyValue(nameof(SteamWebApiClient.SteamWebApiKey), c =>
            {
                var settings = c.Kernel.Get<IPlayersSettings>();

                if (settings.SteamWebApiKey == null)
                    throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");

                return settings.SteamWebApiKey.Decrypt();
            });
        }
    }
}
