namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;

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
        /// <param name="logger">Logger instance.</param>
        public MongoCallHistoryRepository(IMongoClientFactory mongoClientFactory, ILogger logger)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<CallHistoryEntry>("call_history");

            // This constructor runs once per WebSocket connection (the repository is request-scoped and
            // resolved during the WS handshake), so anything thrown here takes down every new connection —
            // index maintenance must never be allowed to do that.
            try
            {
                // A unique index build fails outright if the collection already contains duplicate
                // CallId values (e.g. left over from before this index existed). Clean those up first,
                // keeping the earliest entry per call, so the index below can actually succeed.
                var duplicateIds = this.collection.Find(FilterDefinition<CallHistoryEntry>.Empty).ToList()
                    .GroupBy(x => x.CallId)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderBy(x => x.StartedAt).Skip(1).Select(x => x.Id))
                    .ToList();
                if (duplicateIds.Count > 0)
                {
                    this.collection.DeleteMany(Builders<CallHistoryEntry>.Filter.In(x => x.Id, duplicateIds));
                    logger.Warning("Removed {Count} duplicate call_history entries before rebuilding the unique CallId index", duplicateIds.Count);
                }

                // Enforced at the database level so that two near-simultaneous reports of the same call
                // (caller and callee both ending it within the same instant) can never both "win" the
                // check-then-create race and create duplicate entries/summary messages.
                var indexKeys = Builders<CallHistoryEntry>.IndexKeys.Ascending(x => x.CallId);
                var indexModel = new CreateIndexModel<CallHistoryEntry>(indexKeys, new CreateIndexOptions { Unique = true });
                this.collection.Indexes.CreateOne(indexModel);
            }
            catch (MongoException ex)
            {
                // Degrade gracefully: TryCreateAsync below still works without the index, it just loses
                // its extra race-safety against duplicate summary messages until this succeeds on a later
                // connection (e.g. once the underlying data/permissions issue is resolved).
                logger.Warning(ex, "Could not (re)build the unique CallId index on call_history; continuing without it");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TryCreateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default)
        {
            try
            {
                await this.collection.InsertOneAsync(entry, null, cancellationToken);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Another request already created the entry for this callId first — that request
                // (and only that request) is responsible for posting the call-summary message.
                return false;
            }
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
        public async Task<CallHistoryEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<CallHistoryEntry>.Filter.Eq(x => x.Id, id);
            return await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
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
