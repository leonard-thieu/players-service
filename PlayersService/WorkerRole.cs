using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    class WorkerRole : WorkerRoleBase<IPlayersSettings>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        public WorkerRole(IPlayersSettings settings) : base("players", settings) { }

        OAuth2Handler toofzOAuth2Handler;
        HttpMessageHandler toofzApiHandlers;

        protected override void OnStart(string[] args)
        {
            if (string.IsNullOrEmpty(Settings.ToofzApiUserName))
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiUserName)} is not set.");
            if (Settings.ToofzApiPassword == null)
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiPassword)} is not set.");

            var toofzApiUserName = Settings.ToofzApiUserName;
            var toofzApiPassword = Settings.ToofzApiPassword.Decrypt();

            toofzOAuth2Handler = new OAuth2Handler(toofzApiUserName, toofzApiPassword);
            toofzApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new ToofzHttpErrorHandler(),
                toofzOAuth2Handler,
            });

            base.OnStart(args);
        }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (new UpdateActivity(Log, "players"))
            {
                if (Settings.SteamWebApiKey == null)
                    throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");

                var toofzApiBaseAddress = new Uri(Settings.ToofzApiBaseAddress);
                var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();
                var playersPerUpdate = Settings.PlayersPerUpdate;

                var worker = new PlayersWorker();

                using (var toofzApiClient = new ToofzApiClient(toofzApiHandlers, disposeHandler: false))
                {
                    toofzApiClient.BaseAddress = toofzApiBaseAddress;

                    var players = await worker.GetPlayersAsync(toofzApiClient, playersPerUpdate, cancellationToken).ConfigureAwait(false);

                    var steamApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
                    {
                        new LoggingHandler(),
                        new GZipHandler(),
                        new SteamWebApiTransientFaultHandler(),
                    });
                    using (var steamWebApiClient = new SteamWebApiClient(steamApiHandlers))
                    {
                        steamWebApiClient.SteamWebApiKey = steamWebApiKey;

                        await worker.UpdatePlayersAsync(steamWebApiClient, players, SteamWebApiClient.MaxPlayerSummariesPerRequest, cancellationToken).ConfigureAwait(false);
                    }

                    await worker.StorePlayersAsync(toofzApiClient, players, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
