namespace NexusTeam.Server.Services
{
    using Microsoft.Extensions.Options;
    using MongoDB.Driver;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>
    /// Factory implementation for MongoDB client and database instances.
    /// </summary>
    public class MongoClientFactory : IMongoClientFactory
    {
        private readonly IMongoClient client;
        private readonly IMongoDatabase database;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoClientFactory"/> class.
        /// </summary>
        /// <param name="options">MongoDB configuration options.</param>
        public MongoClientFactory(IOptions<MongoOptions> options)
        {
            var mongoOptions = options.Value;
            var settings = MongoClientSettings.FromConnectionString(mongoOptions.ConnectionString);
            settings.ConnectTimeout = System.TimeSpan.FromSeconds(mongoOptions.ConnectionTimeout);
            settings.ServerSelectionTimeout = System.TimeSpan.FromSeconds(mongoOptions.ServerSelectionTimeout);

            this.client = new MongoClient(settings);
            this.database = this.client.GetDatabase(mongoOptions.DatabaseName);
        }

        /// <inheritdoc/>
        public IMongoClient GetClient()
        {
            return this.client;
        }

        /// <inheritdoc/>
        public IMongoDatabase GetDatabase()
        {
            return this.database;
        }
    }
}
