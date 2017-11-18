using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal class WorkerRole : WorkerRoleBase<IPlayersSettings>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        internal static ISteamWebApiClient CreateSteamWebApiClient(string apiKey, TelemetryClient telemetryClient)
        {
            var handler = HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new SteamWebApiTransientFaultHandler(telemetryClient),
            });

            return new SteamWebApiClient(handler, telemetryClient) { SteamWebApiKey = apiKey };
        }

        public WorkerRole(IPlayersSettings settings, TelemetryClient telemetryClient) : base("players", settings, telemetryClient) { }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update players cycle"))
            using (new UpdateActivity(Log, "players"))
            {
                try
                {
                    if (Settings.SteamWebApiKey == null)
                        throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");
                    if (Settings.LeaderboardsConnectionString == null)
                        throw new InvalidOperationException($"{nameof(Settings.LeaderboardsConnectionString)} is not set.");

                    var playersPerUpdate = Settings.PlayersPerUpdate;
                    var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();
                    var leaderboardsConnectionString = Settings.LeaderboardsConnectionString.Decrypt();

                    var worker = new PlayersWorker(TelemetryClient);

                    IEnumerable<Player> players;
                    using (var db = new LeaderboardsContext(leaderboardsConnectionString))
                    {
                        players = await worker.GetPlayersAsync(db, playersPerUpdate, cancellationToken).ConfigureAwait(false);
                    }

                    using (var steamWebApiClient = CreateSteamWebApiClient(steamWebApiKey, TelemetryClient))
                    {
                        await worker.UpdatePlayersAsync(steamWebApiClient, players, SteamWebApiClient.MaxPlayerSummariesPerRequest, cancellationToken).ConfigureAwait(false);
                    }

                    using (var connection = new SqlConnection(leaderboardsConnectionString))
                    {
                        var storeClient = new LeaderboardsStoreClient(connection);
                        await worker.StorePlayersAsync(storeClient, players, cancellationToken).ConfigureAwait(false);
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
