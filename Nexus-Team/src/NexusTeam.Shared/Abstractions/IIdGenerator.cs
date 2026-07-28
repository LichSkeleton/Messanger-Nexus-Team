namespace NexusTeam.Shared.Abstractions
{
    /// <summary>
    /// Abstraction for generating unique identifiers.
    /// Implementations should be registered as singleton in DI container.
    /// Common implementations include GUID, ULID, or distributed ID generators.
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// Generates a new unique identifier as a string.
        /// </summary>
        /// <returns>A unique identifier string.</returns>
        string GenerateId();
    }
}
