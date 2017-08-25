﻿using System;
using System.IO;
using System.Linq;
using log4net;
using Microsoft.ApplicationInsights.Extensibility;
using toofz.NecroDancer.Leaderboards.PlayersService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    static class Program
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static int Main(string[] args)
        {
            Log.Debug("Initialized logging.");
            Secrets.Iterations = 200000;

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var settings = Settings.Default;

            // Args are only allowed while running as a console as they may require user input.
            if (Environment.UserInteractive && args.Any())
            {
                var parser = new PlayersArgsParser(Console.In, Console.Out, Console.Error);

                return parser.Parse(args, settings);
            }

            var instrumentationKey = settings.PlayersInstrumentationKey;
            if (!string.IsNullOrEmpty(instrumentationKey)) { TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey; }
            else
            {
                Log.Warn($"The setting 'PlayersInstrumentationKey' is not set. Telemetry is disabled.");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            Application.Run<WorkerRole, IPlayersSettings>();

            return 0;
        }
    }
}
