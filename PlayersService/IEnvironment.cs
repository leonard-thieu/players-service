namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    interface IEnvironment
    {
        /// <summary>
        /// Gets a value indicating whether the current process is running in user interactive mode.
        /// </summary>
        bool UserInteractive { get; }
    }
}
