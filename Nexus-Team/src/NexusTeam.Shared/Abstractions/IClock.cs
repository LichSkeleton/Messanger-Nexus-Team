namespace NexusTeam.Shared.Abstractions
{
    using System;

    /// <summary>
    /// Abstraction for system time to enable testability.
    /// Implementations should be registered as singleton in DI container.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        DateTime UtcNow { get; }
    }
}
