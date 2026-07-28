namespace NexusTeam.Server.Services.Abstractions
{
    using StackExchange.Redis;

    /// <summary>
    /// Abstraction for Redis connection multiplexer.
    /// </summary>
    public interface IRedisMultiplexer
    {
        /// <summary>
        /// Gets the Redis connection multiplexer.
        /// </summary>
        /// <returns>The connection multiplexer.</returns>
        IConnectionMultiplexer GetMultiplexer();

        /// <summary>
        /// Gets a Redis database instance.
        /// </summary>
        /// <param name="db">The database index, or -1 for default.</param>
        /// <returns>The Redis database.</returns>
        IDatabase GetDatabase(int db = -1);
    }
}
