namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>
    /// MongoDB implementation of the call history repository.
    /// </summary>
    public class MongoCallHistoryRepository : ICallHistoryRepository
    {
        private readonly IMongoCollection<CallHistoryEntry> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCallHistoryRepository"/> class.
        /// </summary>
        /// <param name="mongoClientFactory">The MongoDB client factory.</param>
        public MongoCallHistoryRepository(IMongoClientFactory mongoClientFactory)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<CallHistoryEntry>("call_history");
        }

        /// <inheritdoc/>
        public async Task CreateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default)
        {
            await this.collection.InsertOneAsync(entry, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CallHistoryEntry>> GetByChatIdAsync(string chatId, int limit, CancellationToken cancellationToken = default)
        {
            var filter = Builders<CallHistoryEntry>.Filter.Eq(x => x.ChatId, chatId);
            var sort = Builders<CallHistoryEntry>.Sort.Descending(x => x.StartedAt);
            return await this.collection.Find(filter).Sort(sort).Limit(limit).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CallHistoryEntry>> GetUnseenMissedAsync(string calleeId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<CallHistoryEntry>.Filter.And(
                Builders<CallHistoryEntry>.Filter.Eq(x => x.CalleeId, calleeId),
                Builders<CallHistoryEntry>.Filter.Eq(x => x.SeenByCallee, false),
                Builders<CallHistoryEntry>.Filter.In(x => x.Status, new[] { "Missed", "NoAnswer" }));
            var sort = Builders<CallHistoryEntry>.Sort.Descending(x => x.StartedAt);
            return await this.collection.Find(filter).Sort(sort).Limit(20).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task MarkSeenAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<CallHistoryEntry>.Filter.Eq(x => x.Id, id);
            var update = Builders<CallHistoryEntry>.Update.Set(x => x.SeenByCallee, true);
            await this.collection.UpdateOneAsync(filter, update, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<CallHistoryEntry?> GetByCallIdAsync(string callId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<CallHistoryEntry>.Filter.Eq(x => x.CallId, callId);
            return await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default)
        {
            var filter = Builders<CallHistoryEntry>.Filter.Eq(x => x.Id, entry.Id);
            await this.collection.ReplaceOneAsync(filter, entry, new ReplaceOptions(), cancellationToken);
        }
    }
}
