using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal class WorkerRole : WorkerRoleBase<IPlayersSettings>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        internal static HttpMessageHandler CreateToofzApiHandler(string toofzApiUserName, string toofzApiPassword)
        {
            return HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new ToofzHttpErrorHandler(),
                new OAuth2Handler(toofzApiUserName, toofzApiPassword),
            });
        }

        internal static HttpMessageHandler CreateSteamApiHandler(TelemetryClient telemetryClient)
        {
            return HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new SteamWebApiTransientFaultHandler(telemetryClient),
            });
        }

        public WorkerRole(IPlayersSettings settings, TelemetryClient telemetryClient) : base("players", settings, telemetryClient) { }

        private HttpMessageHandler toofzApiHandler;

        protected override void OnStart(string[] args)
        {
            if (string.IsNullOrEmpty(Settings.ToofzApiUserName))
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiUserName)} is not set.");
            if (Settings.ToofzApiPassword == null)
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiPassword)} is not set.");

            var toofzApiUserName = Settings.ToofzApiUserName;
            var toofzApiPassword = Settings.ToofzApiPassword.Decrypt();

            toofzApiHandler = CreateToofzApiHandler(toofzApiUserName, toofzApiPassword);

            base.OnStart(args);
        }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update players cycle"))
            using (new UpdateActivity(Log, "players"))
            {
                try
                {
                    if (Settings.SteamWebApiKey == null)
                        throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");

                    var toofzApiBaseAddress = new Uri(Settings.ToofzApiBaseAddress);
                    var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();
                    var playersPerUpdate = Settings.PlayersPerUpdate;

                    var worker = new PlayersWorker(TelemetryClient);

                    using (var toofzApiClient = new ToofzApiClient(toofzApiHandler, false, TelemetryClient))
                    {
                        toofzApiClient.BaseAddress = toofzApiBaseAddress;

                        var players = await worker.GetPlayersAsync(toofzApiClient, playersPerUpdate, cancellationToken).ConfigureAwait(false);

                        using (var steamWebApiClient = new SteamWebApiClient(CreateSteamApiHandler(TelemetryClient), TelemetryClient))
                        {
                            steamWebApiClient.SteamWebApiKey = steamWebApiKey;

                            await worker.UpdatePlayersAsync(steamWebApiClient, players, SteamWebApiClient.MaxPlayerSummariesPerRequest, cancellationToken).ConfigureAwait(false);
                        }

                        await worker.StorePlayersAsync(toofzApiClient, players, cancellationToken).ConfigureAwait(false);
                    }

                    operation.Telemetry.Success = true;
                }
                catch (Exception)
                {
                    operation.Telemetry.Success = false;
                    throw;
                }
            }
        }
    }
}
