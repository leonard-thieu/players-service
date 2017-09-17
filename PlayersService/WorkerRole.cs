using System;
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
            if (Settings.SteamWebApiKey == null)
                throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");

            var toofzApiBaseAddress = new Uri(Settings.ToofzApiBaseAddress);
            var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();

            var steamApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new SteamWebApiTransientFaultHandler(),
                new ContentLengthHandler(),
            });

            using (var toofzApiClient = new ToofzApiClient(toofzApiHandlers, disposeHandler: false))
            using (var steamWebApiClient = new SteamWebApiClient(steamApiHandlers))
            {
                toofzApiClient.BaseAddress = toofzApiBaseAddress;
                steamWebApiClient.SteamWebApiKey = steamWebApiKey;

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
                var stalePlayers = await GetStalePlayersAsync(toofzApiClient, limit, cancellationToken).ConfigureAwait(false);
                var players = await DownloadPlayersAsync(steamWebApiClient, stalePlayers, SteamWebApiClient.MaxPlayerSummariesPerRequest, cancellationToken);
                players = CreateStubsForNonExistingPlayers(stalePlayers, players);

                await StorePlayersAsync(toofzApiClient, players, cancellationToken);
            }
        }

        internal async Task<IEnumerable<PlayerDTO>> GetStalePlayersAsync(
            IToofzApiClient toofzApiClient,
            int limit,
            CancellationToken cancellationToken)
        {
            var response = await toofzApiClient
                .GetPlayersAsync(new GetPlayersParams
                {
                    Limit = limit,
                    Sort = "updated_at",
                }, cancellationToken)
                .ConfigureAwait(false);

            return response.Players;
        }

        internal async Task<IEnumerable<Player>> DownloadPlayersAsync(
            ISteamWebApiClient steamWebApiClient,
            IEnumerable<PlayerDTO> stalePlayers,
            int playersPerRequest,
            CancellationToken cancellationToken)
        {
            var players = new List<Player>(stalePlayers.Count());

            using (var downloadNotifier = new DownloadNotifier(Log, "players"))
            {
                var requests = new List<Task<IEnumerable<Player>>>();
                for (int i = 0; i < stalePlayers.Count(); i += playersPerRequest)
                {
                    var ids = stalePlayers
                        .Skip(i)
                        .Take(playersPerRequest)
                        .Select(p => p.Id)
                        .ToList();
                    var request = GetPlayersAsync(steamWebApiClient, ids, downloadNotifier, cancellationToken);
                    requests.Add(request);
                }

                var batches = await Task.WhenAll(requests).ConfigureAwait(false);
                foreach (var batch in batches)
                {
                    players.AddRange(batch);
                }
            }

            Debug.Assert(!players.Any(p => p == null));

            return players;
        }

        internal async Task<IEnumerable<Player>> GetPlayersAsync(
            ISteamWebApiClient steamWebApiClient,
            IEnumerable<long> ids,
            IProgress<long> progress,
            CancellationToken cancellationToken)
        {
            var players = new List<Player>(ids.Count());

            var playerSummaries = await steamWebApiClient
                .GetPlayerSummariesAsync(ids, progress, cancellationToken)
                .ConfigureAwait(false);

            foreach (var p in playerSummaries.Response.Players)
            {
                players.Add(new Player
                {
                    SteamId = p.SteamId,
                    Name = p.PersonaName,
                    Avatar = p.Avatar,
                    LastUpdate = DateTime.UtcNow,
                });
            }

            return players;
        }

        // Create a stub player if Steam didn't return a response for a Steam ID
        internal IEnumerable<Player> CreateStubsForNonExistingPlayers(IEnumerable<PlayerDTO> stalePlayers, IEnumerable<Player> players)
        {
            return (from s in stalePlayers
                    join p in players on s.Id equals p.SteamId into ps
                    from p in ps.DefaultIfEmpty()
                    select new
                    {
                        StalePlayer = s,
                        Player = p,
                    })
                   .Select(sp =>
                   {
                       var player = sp.Player;
                       if (player != null)
                       {
                           player.Exists = true;
                       }
                       else
                       {
                           player = new Player
                           {
                               SteamId = sp.StalePlayer.Id,
                               Exists = false,
                               LastUpdate = DateTime.UtcNow,
                           };
                       }

                       return player;
                   })
                   .ToList();
        }

        internal async Task StorePlayersAsync(
            IToofzApiClient toofzApiClient,
            IEnumerable<Player> players,
            CancellationToken cancellationToken)
        {
            using (var activity = new StoreNotifier(Log, "players"))
            {
                var bulkStore = await toofzApiClient.PostPlayersAsync(players, cancellationToken).ConfigureAwait(false);
                activity.Report(bulkStore.RowsAffected);
            }
        }
    }
}
