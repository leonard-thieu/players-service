using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal sealed class PlayersWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlayersWorker));

        public PlayersWorker(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        private readonly TelemetryClient telemetryClient;

        public async Task<IEnumerable<Player>> GetPlayersAsync(
            IToofzApiClient toofzApiClient,
            int limit,
            CancellationToken cancellationToken)
        {
            var @params = new GetPlayersParams
            {
                Limit = limit,
                Sort = "updated_at",
            };
            var response = await toofzApiClient
                .GetPlayersAsync(@params, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var players = (from p in response.Players
                           select new Player
                           {
                               SteamId = p.Id,
                               Name = p.DisplayName,
                               Avatar = p.Avatar,
                           })
                           .ToList();

            return players;
        }

        public async Task UpdatePlayersAsync(
            ISteamWebApiClient steamWebApiClient,
            IEnumerable<Player> players,
            int playersPerRequest,
            CancellationToken cancellationToken)
        {
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("Download players"))
            using (var activity = new DownloadActivity(Log, "players"))
            {
                try
                {
                    var requests = new List<Task>();
                    var count = players.Count();
                    for (int i = 0; i < count; i += playersPerRequest)
                    {
                        var ids = players
                            .Skip(i)
                            .Take(playersPerRequest)
                            .ToList();
                        var request = UpdatePlayersAsync(steamWebApiClient, ids, activity, cancellationToken);
                        requests.Add(request);
                    }

                    await Task.WhenAll(requests).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception)
                {
                    operation.Telemetry.Success = false;
                    throw;
                }
            }
        }

        private async Task UpdatePlayersAsync(
            ISteamWebApiClient steamWebApiClient,
            IEnumerable<Player> players,
            IProgress<long> progress,
            CancellationToken cancellationToken)
        {
            var ids = players.Select(p => p.SteamId).ToList();

            var playerSummaries = await steamWebApiClient
                .GetPlayerSummariesAsync(ids, progress, cancellationToken)
                .ConfigureAwait(false);

            var joined = from p in players
                         join s in playerSummaries.Response.Players on p.SteamId equals s.SteamId into ps
                         from s in ps.DefaultIfEmpty()
                         select new
                         {
                             Player = p,
                             Summary = s,
                         };

            foreach (var ps in joined)
            {
                var p = ps.Player;
                var s = ps.Summary;

                p.LastUpdate = DateTime.UtcNow;
                if (s != null)
                {
                    p.Exists = true;
                    p.Name = s.PersonaName;
                    p.Avatar = s.Avatar;
                }
                else
                {
                    p.Exists = false;
                }
            }
        }

        public async Task StorePlayersAsync(
            IToofzApiClient toofzApiClient,
            IEnumerable<Player> players,
            CancellationToken cancellationToken)
        {
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("Store players"))
            using (var activity = new StoreActivity(Log, "players"))
            {
                try
                {
                    var bulkStore = await toofzApiClient.PostPlayersAsync(players, cancellationToken).ConfigureAwait(false);
                    activity.Report(bulkStore.RowsAffected);

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
