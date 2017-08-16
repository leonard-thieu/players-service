using toofz.NecroDancer.Leaderboards.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    sealed class PlayerSettings : Settings
    {
        public int PlayersPerUpdate { get; set; }
    }
}
