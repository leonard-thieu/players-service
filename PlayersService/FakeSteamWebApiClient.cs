﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamUser;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    internal sealed class FakeSteamWebApiClient : ISteamWebApiClient
    {
        public FakeSteamWebApiClient()
        {
            var playerSummariesPath = Path.Combine("Data", "SteamWebApi", "PlayerSummaries");
            playerSummaries = Directory.GetFiles(playerSummariesPath, "*.json");
        }

        private readonly string[] playerSummaries;

        public string SteamWebApiKey { get; set; }

        public Task<PlayerSummariesEnvelope> GetPlayerSummariesAsync(
            IEnumerable<long> steamIds,
            IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            var length = playerSummaries.Length;
            var i = (int)(steamIds.Sum(s => s % length) % length);
            using (var sr = File.OpenText(playerSummaries[i]))
            {
                var playerSummaries = JsonConvert.DeserializeObject<PlayerSummariesEnvelope>(sr.ReadToEnd());
                progress?.Report(sr.BaseStream.Length);

                return Task.FromResult(playerSummaries);
            }
        }

        public Task<UgcFileDetailsEnvelope> GetUgcFileDetailsAsync(
            uint appId,
            long ugcId,
            IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
