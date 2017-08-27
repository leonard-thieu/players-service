using System;
using System.Diagnostics.CodeAnalysis;

namespace toofz.NecroDancer.Leaderboards.PlayersService
{
    [ExcludeFromCodeCoverage]
    sealed class EnvironmentAdapter : IEnvironment
    {
        /// <summary>
        /// Gets a value indicating whether the current process is running in user interactive mode.
        /// </summary>
        public bool UserInteractive => Environment.UserInteractive;
    }
}
