using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Ninject;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal class WorkerRole : WorkerRoleBase<IPlayersSettings>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        public WorkerRole(IPlayersSettings settings, TelemetryClient telemetryClient) : base("players", settings, telemetryClient)
        {
            kernel = KernelConfig.CreateKernel(settings, telemetryClient);
        }

        private readonly IKernel kernel;

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update players cycle"))
            using (new UpdateActivity(Log, "players cycle"))
            {
                try
                {
                    await UpdatePlayersAsync(cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (Util.FailTelemetry(operation.Telemetry))
                {
                    // Unreachable
                    throw;
                }
            }
        }

        private async Task UpdatePlayersAsync(CancellationToken cancellationToken)
        {
            var worker = kernel.Get<PlayersWorker>();
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update players"))
            using (new UpdateActivity(Log, "players"))
            {
                try
                {
                    var players = await worker.GetPlayersAsync(Settings.PlayersPerUpdate, cancellationToken).ConfigureAwait(false);
                    await worker.UpdatePlayersAsync(players, SteamWebApiClient.MaxPlayerSummariesPerRequest, cancellationToken).ConfigureAwait(false);
                    await worker.StorePlayersAsync(players, cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (HttpRequestStatusException ex)
                {
                    TelemetryClient.TrackException(ex);
                    Log.Error("Failed to complete run due to an error.", ex);
                    operation.Telemetry.Success = false;
                }
                catch (Exception) when (Util.FailTelemetry(operation.Telemetry))
                {
                    // Unreachable
                    throw;
                }
                finally
                {
                    kernel.Release(worker);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                kernel.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
