namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>
    /// MongoDB implementation of the user preference repository.
    /// </summary>
    public class MongoUserPreferenceRepository : IUserPreferenceRepository
    {
        private readonly IMongoCollection<UserPreference> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoUserPreferenceRepository"/> class.
        /// </summary>
        /// <param name="mongoClientFactory">The MongoDB client factory.</param>
        public MongoUserPreferenceRepository(IMongoClientFactory mongoClientFactory)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<UserPreference>("userPreferences");

            var indexKeys = Builders<UserPreference>.IndexKeys.Ascending(x => x.UserId);
            var indexModel = new CreateIndexModel<UserPreference>(indexKeys, new CreateIndexOptions { Unique = true });
            this.collection.Indexes.CreateOne(indexModel);
        }

        /// <inheritdoc/>
        public async Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserPreference>.Filter.Eq(x => x.UserId, userId);
            return await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task CreateAsync(UserPreference preference, CancellationToken cancellationToken = default)
        {
            await this.collection.InsertOneAsync(preference, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserPreference>.Filter.Eq(x => x.UserId, preference.UserId);
            await this.collection.ReplaceOneAsync(filter, preference, new ReplaceOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserPreference>.Filter.Eq(x => x.UserId, userId);
            await this.collection.DeleteOneAsync(filter, cancellationToken);
        }
    }
}
