using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    sealed class WorkerRole : WorkerRoleBase<IPlayersSettings>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        public WorkerRole(IPlayersSettings settings) : base("players", settings) { }

        TelemetryClient telemetryClient;
        OAuth2Handler toofzOAuth2Handler;
        HttpMessageHandler toofzApiHandlers;

        protected override void OnStart(string[] args)
        {
            telemetryClient = new TelemetryClient();
            toofzOAuth2Handler = new OAuth2Handler();
            toofzApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new HttpRequestStatusHandler(),
                toofzOAuth2Handler,
            });

            base.OnStart(args);
        }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            if (Settings.SteamWebApiKey == null)
            {
                throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");
            }
            var steamWebApiKey = Settings.SteamWebApiKey;

            if (string.IsNullOrEmpty(Settings.ToofzApiUserName))
            {
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiUserName)} is not set.");
            }
            toofzOAuth2Handler.UserName = Settings.ToofzApiUserName;
            if (Settings.ToofzApiPassword == null)
            {
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiPassword)} is not set.");
            }
            toofzOAuth2Handler.Password = Settings.ToofzApiPassword.Decrypt();

            var steamApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
            {
                new LoggingHandler(),
                new SteamWebApiTransientFaultHandler(telemetryClient),
            });

            using (var toofzApiClient = new ToofzApiClient(toofzApiHandlers))
            using (var steamWebApiClient = new SteamWebApiClient(steamApiHandlers))
            {
                toofzApiClient.BaseAddress = new Uri(Settings.ToofzApiBaseAddress);

                steamWebApiClient.SteamWebApiKey = steamWebApiKey.Decrypt();

                await UpdatePlayersAsync(
                    toofzApiClient,
                    steamWebApiClient,
                    Settings.PlayersPerUpdate,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        internal async Task UpdatePlayersAsync(
            IToofzApiClient toofzApiClient,
            ISteamWebApiClient steamWebApiClient,
            int limit,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (toofzApiClient == null)
                throw new ArgumentNullException(nameof(toofzApiClient));
            if (steamWebApiClient == null)
                throw new ArgumentNullException(nameof(steamWebApiClient));
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            using (new UpdateNotifier(Log, "players"))
            {
                var response = await toofzApiClient
                    .GetPlayersAsync(new GetPlayersParams
                    {
                        Limit = limit,
                        Sort = "updated_at",
                    }, cancellationToken)
                    .ConfigureAwait(false);
                var steamIds = (from p in response.players
                                select p.id)
                               .ToList();

                var players = new ConcurrentBag<Player>();
                using (var download = new DownloadNotifier(Log, "players"))
                {
                    var requests = new List<Task>();
                    for (int i = 0; i < steamIds.Count; i += SteamWebApiClient.MaxPlayerSummariesPerRequest)
                    {
                        var ids = steamIds
                            .Skip(i)
                            .Take(SteamWebApiClient.MaxPlayerSummariesPerRequest);
                        var request = MapPlayers();
                        requests.Add(request);

                        async Task MapPlayers()
                        {
                            var playerSummaries = await steamWebApiClient
                                .GetPlayerSummariesAsync(ids, download.Progress, cancellationToken)
                                .ConfigureAwait(false);

                            foreach (var p in playerSummaries.Response.Players)
                            {
                                players.Add(new Player
                                {
                                    SteamId = p.SteamId,
                                    Name = p.PersonaName,
                                    Avatar = p.Avatar,
                                });
                            }

                        }
                    }
                    await Task.WhenAll(requests).ConfigureAwait(false);
                }

                Debug.Assert(!players.Any(p => p == null));

                // TODO: Document purpose.
                var playersIncludingNonExisting = steamIds.GroupJoin(
                    players,
                    id => id,
                    p => p.SteamId,
                    (id, ps) =>
                    {
                        var p = ps.SingleOrDefault();
                        if (p != null)
                        {
                            p.Exists = true;
                        }
                        else
                        {
                            p = new Player
                            {
                                SteamId = id,
                                Exists = false,
                            };
                        }
                        p.LastUpdate = DateTime.UtcNow;
                        return p;
                    });

                using (var activity = new StoreNotifier(Log, "players"))
                {
                    var bulkStore = await toofzApiClient.PostPlayersAsync(playersIncludingNonExisting, cancellationToken).ConfigureAwait(false);
                    activity.Progress.Report(bulkStore.rows_affected);
                }
            }
        }
    }
}
