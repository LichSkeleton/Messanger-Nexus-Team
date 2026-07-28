namespace NexusTeam.Shared.Helpers
{
    using System;
    using NexusTeam.Shared.Abstractions;

    /// <summary>
    /// Default implementation of IClock using system time.
    /// Returns actual system time for production use.
    /// </summary>
    public class SystemClock : IClock
    {
        /// <inheritdoc/>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
