namespace NexusTeam.Server.Services
{
    using System;
    using NexusTeam.Shared.Abstractions;

    /// <summary>
    /// GUID-based implementation of the ID generator.
    /// </summary>
    public class UlidGenerator : IIdGenerator
    {
        /// <inheritdoc/>
        public string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
