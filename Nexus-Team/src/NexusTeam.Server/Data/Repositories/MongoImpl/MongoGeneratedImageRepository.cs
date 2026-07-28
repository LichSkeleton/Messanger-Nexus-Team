namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>
    /// MongoDB implementation of the generated image repository.
    /// </summary>
    public class MongoGeneratedImageRepository : IGeneratedImageRepository
    {
        private readonly IMongoCollection<GeneratedImage> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoGeneratedImageRepository"/> class.
        /// </summary>
        /// <param name="mongoClientFactory">The MongoDB client factory.</param>
        public MongoGeneratedImageRepository(IMongoClientFactory mongoClientFactory)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<GeneratedImage>("generated_images");
        }

        /// <inheritdoc/>
        public async Task<GeneratedImage?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GeneratedImage>.Filter.And(
                Builders<GeneratedImage>.Filter.Eq(x => x.Id, id),
                Builders<GeneratedImage>.Filter.Eq(x => x.IsDeleted, false));
            return await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GeneratedImage>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GeneratedImage>.Filter.And(
                Builders<GeneratedImage>.Filter.Eq(x => x.UserId, userId),
                Builders<GeneratedImage>.Filter.Eq(x => x.IsDeleted, false));

            var sort = Builders<GeneratedImage>.Sort.Descending(x => x.GeneratedAt);

            return await this.collection
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task CreateAsync(GeneratedImage image, CancellationToken cancellationToken = default)
        {
            await this.collection.InsertOneAsync(image, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(GeneratedImage image, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GeneratedImage>.Filter.Eq(x => x.Id, image.Id);
            await this.collection.ReplaceOneAsync(filter, image, new ReplaceOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GeneratedImage>.Filter.Eq(x => x.Id, id);
            var update = Builders<GeneratedImage>.Update.Set(x => x.IsDeleted, true);
            await this.collection.UpdateOneAsync(filter, update, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetRecentPromptsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GeneratedImage>.Filter.And(
                Builders<GeneratedImage>.Filter.Eq(x => x.UserId, userId),
                Builders<GeneratedImage>.Filter.Eq(x => x.IsDeleted, false));

            var sort = Builders<GeneratedImage>.Sort.Descending(x => x.GeneratedAt);

            var images = await this.collection
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            return images
                .Select(x => (x.Prompt ?? string.Empty).Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct();
        }
    }
}
