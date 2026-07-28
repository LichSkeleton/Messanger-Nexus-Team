namespace NexusTeam.Server.Services.Abstractions
{
    using MongoDB.Driver;

    /// <summary>
    /// Factory for creating MongoDB client and database instances.
    /// </summary>
    public interface IMongoClientFactory
    {
        /// <summary>
        /// Gets the MongoDB client instance.
        /// </summary>
        /// <returns>The MongoDB client.</returns>
        IMongoClient GetClient();

        /// <summary>
        /// Gets the MongoDB database instance.
        /// </summary>
        /// <returns>The MongoDB database.</returns>
        IMongoDatabase GetDatabase();
    }
}
