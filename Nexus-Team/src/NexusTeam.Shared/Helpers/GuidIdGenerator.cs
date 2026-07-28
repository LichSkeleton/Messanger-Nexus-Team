namespace NexusTeam.Shared.Helpers
{
    using System;
    using NexusTeam.Shared.Abstractions;

    /// <summary>
    /// Default implementation of IIdGenerator using GUIDs.
    /// Generates unique identifiers using GUID format.
    /// </summary>
    public class GuidIdGenerator : IIdGenerator
    {
        /// <inheritdoc/>
        public string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
