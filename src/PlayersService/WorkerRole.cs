﻿using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Ninject;
using toofz.Data;
using toofz.Services.PlayersService.Properties;
using toofz.Steam.WebApi;

namespace toofz.Services.PlayersService
{
    internal sealed class WorkerRole : WorkerRoleBase<IPlayersSettings>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        public WorkerRole(IPlayersSettings settings, TelemetryClient telemetryClient)
            : this(settings, telemetryClient, runOnce: false, kernel: null, log: null) { }

        internal WorkerRole(IPlayersSettings settings, TelemetryClient telemetryClient, bool runOnce, IKernel kernel, ILog log) :
            base("players", settings, telemetryClient, runOnce)
        {
            kernel = kernel ?? KernelConfig.CreateKernel();
            kernel.Bind<IPlayersSettings>()
                  .ToConstant(settings);
            kernel.Bind<TelemetryClient>()
                  .ToConstant(telemetryClient);
            this.kernel = kernel;

            this.log = log ?? Log;
        }

        private readonly IKernel kernel;
        private readonly ILog log;

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update players cycle"))
            using (new UpdateActivity(log, "players cycle"))
            {
                try
                {
                    await UpdatePlayersAsync(cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (operation.Telemetry.MarkAsUnsuccessful()) { }
            }
        }

        private async Task UpdatePlayersAsync(CancellationToken cancellationToken)
        {
            var worker = kernel.Get<PlayersWorker>();
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update players"))
            using (new UpdateActivity(log, "players"))
            {
                try
                {
                    var players = await worker.GetPlayersAsync(Settings.PlayersPerUpdate, cancellationToken).ConfigureAwait(false);
                    await worker.UpdatePlayersAsync(players, SteamWebApiClient.MaxPlayerSummariesPerRequest, cancellationToken).ConfigureAwait(false);
                    await worker.StorePlayersAsync(players, cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception ex)
                    when (SteamWebApiClient.IsTransient(ex) ||
                          LeaderboardsStoreClient.IsTransient(ex))
                {
                    TelemetryClient.TrackException(ex);
                    log.Error("Failed to complete run due to an error.", ex);
                    operation.Telemetry.Success = false;
                }
                catch (Exception) when (operation.Telemetry.MarkAsUnsuccessful()) { }
                finally
                {
                    kernel.Release(worker);
                }
            }
        }

        #region IDisposable Implementation

        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (disposed) { return; }

            if (disposing)
            {
                try
                {
                    kernel.Dispose();
                }
                catch (Exception) { }
            }

            disposed = true;

            base.Dispose(disposing);
        }

        #endregion
    }
}
