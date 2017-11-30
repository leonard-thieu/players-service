using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using static toofz.NecroDancer.Leaderboards.PlayersService.Util;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal sealed class PlayersWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlayersWorker));

        public PlayersWorker(
            ILeaderboardsContext db,
            ISteamWebApiClient steamWebApiClient,
            ILeaderboardsStoreClient storeClient,
            TelemetryClient telemetryClient)
        {
            this.db = db;
            this.steamWebApiClient = steamWebApiClient;
            this.storeClient = storeClient;
            this.telemetryClient = telemetryClient;
        }

        private readonly ILeaderboardsContext db;
        private readonly ISteamWebApiClient steamWebApiClient;
        private readonly ILeaderboardsStoreClient storeClient;
        private readonly TelemetryClient telemetryClient;

        public Task<List<Player>> GetPlayersAsync(
            int limit,
            CancellationToken cancellationToken)
        {
            return (from p in db.Players.AsNoTracking()
                    orderby p.LastUpdate
                    select p)
                    .Take(() => limit)
                    .ToListAsync(cancellationToken);
        }

        public async Task UpdatePlayersAsync(
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
                        var request = UpdatePlayersAsync(ids, activity, cancellationToken);
                        requests.Add(request);
                    }

                    await Task.WhenAll(requests).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (FailTelemetry(operation.Telemetry))
                {
                    // Unreachable
                    throw;
                }
            }
        }

        private async Task UpdatePlayersAsync(
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
            IEnumerable<Player> players,
            CancellationToken cancellationToken)
        {
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("Store players"))
            using (var activity = new StoreActivity(Log, "players"))
            {
                try
                {
                    var rowsAffected = await storeClient.BulkUpsertAsync(players, cancellationToken).ConfigureAwait(false);
                    activity.Report(rowsAffected);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (FailTelemetry(operation.Telemetry))
                {
                    // Unreachable
                    throw;
                }
            }
        }
    }
}
