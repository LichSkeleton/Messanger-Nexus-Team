namespace NexusTeam.Server.Services
{
    using System;
    using NexusTeam.Shared.Abstractions;

    /// <summary>
    /// System implementation of the clock abstraction.
    /// </summary>
    public class SystemClock : IClock
    {
        /// <inheritdoc/>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
