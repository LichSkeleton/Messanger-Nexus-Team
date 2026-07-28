namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Services.Abstractions;
    using SharedFolder = NexusTeam.Shared.Models.ChatFolder;

    /// <summary>
    /// MongoDB implementation of the chat folder repository.
    /// </summary>
    public class MongoChatFolderRepository : IChatFolderRepository
    {
        private readonly IMongoCollection<ChatFolder> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoChatFolderRepository"/> class.
        /// </summary>
        /// <param name="mongoClientFactory">The MongoDB client factory.</param>
        public MongoChatFolderRepository(IMongoClientFactory mongoClientFactory)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<ChatFolder>("chatFolders");
        }

        /// <inheritdoc/>
        public async Task<SharedFolder?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ChatFolder>.Filter.Eq(x => x.Id, id);
            var dataFolder = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return MapToShared(dataFolder);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SharedFolder>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ChatFolder>.Filter.Eq(x => x.UserId, userId);
            var dataFolders = await this.collection.Find(filter).ToListAsync(cancellationToken);
            return dataFolders.Select(MapToShared).Where(f => f != null).Cast<SharedFolder>();
        }

        /// <inheritdoc/>
        public async Task CreateAsync(SharedFolder folder, CancellationToken cancellationToken = default)
        {
            var dataFolder = MapToData(folder);
            await this.collection.InsertOneAsync(dataFolder, null, cancellationToken);
            folder.Id = dataFolder.Id ?? string.Empty;
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(SharedFolder folder, CancellationToken cancellationToken = default)
        {
            var dataFolder = MapToData(folder);
            var filter = Builders<ChatFolder>.Filter.Eq(x => x.Id, folder.Id);
            await this.collection.ReplaceOneAsync(filter, dataFolder, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ChatFolder>.Filter.Eq(x => x.Id, id);
            await this.collection.DeleteOneAsync(filter, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RemoveChatFromAllFoldersAsync(string chatId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ChatFolder>.Filter.AnyEq(x => x.ChatIds, chatId);
            var update = Builders<ChatFolder>.Update.Pull(x => x.ChatIds, chatId);
            await this.collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RemoveChatFromUserFoldersAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ChatFolder>.Filter.And(
                Builders<ChatFolder>.Filter.Eq(x => x.UserId, userId),
                Builders<ChatFolder>.Filter.AnyEq(x => x.ChatIds, chatId));
            var update = Builders<ChatFolder>.Update.Pull(x => x.ChatIds, chatId);
            await this.collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        }

        private static SharedFolder? MapToShared(ChatFolder? dataFolder)
        {
            if (dataFolder == null)
            {
                return null;
            }

            return new SharedFolder
            {
                Id = dataFolder.Id ?? string.Empty,
                Name = dataFolder.Name,
                UserId = dataFolder.UserId,
                ChatIds = dataFolder.ChatIds ?? new List<string>(),
                CreatedAt = dataFolder.CreatedAt,
                UpdatedAt = dataFolder.UpdatedAt,
            };
        }

        private static ChatFolder MapToData(SharedFolder sharedFolder)
        {
            return new ChatFolder
            {
                Id = sharedFolder.Id,
                Name = sharedFolder.Name,
                UserId = sharedFolder.UserId,
                ChatIds = sharedFolder.ChatIds ?? new List<string>(),
                CreatedAt = sharedFolder.CreatedAt,
                UpdatedAt = sharedFolder.UpdatedAt,
            };
        }
    }
}
