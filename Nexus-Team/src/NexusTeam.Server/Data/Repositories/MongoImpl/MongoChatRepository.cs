namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Enums;
    using DataChat = NexusTeam.Server.Data.Models.Chat;
    using SharedChat = NexusTeam.Shared.Models.Chat;

    /// <summary>
    /// MongoDB implementation of the chat repository.
    /// </summary>
    public class MongoChatRepository : IChatRepository
    {
        private readonly IMongoCollection<DataChat> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoChatRepository"/> class.
        /// </summary>
        /// <param name="mongoClientFactory">The MongoDB client factory.</param>
        public MongoChatRepository(IMongoClientFactory mongoClientFactory)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<DataChat>("chats");
        }

        /// <inheritdoc/>
        public async Task<SharedChat?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataChat>.Filter.Eq(x => x.Id, id);
            var dataChat = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return MapToShared(dataChat);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SharedChat>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataChat>.Filter.AnyEq(x => x.Participants, userId);
            var dataChats = await this.collection.Find(filter).ToListAsync(cancellationToken);
            return dataChats.Select(MapToShared).Where(c => c != null).Cast<SharedChat>();
        }

        /// <inheritdoc/>
        public async Task CreateAsync(SharedChat chat, CancellationToken cancellationToken = default)
        {
            var dataChat = MapToData(chat);
            await this.collection.InsertOneAsync(dataChat, null, cancellationToken);
            chat.Id = dataChat.Id!; // Update ID in shared model
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(SharedChat chat, CancellationToken cancellationToken = default)
        {
            var dataChat = MapToData(chat);
            var filter = Builders<DataChat>.Filter.Eq(x => x.Id, chat.Id);
            await this.collection.ReplaceOneAsync(filter, dataChat, new ReplaceOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataChat>.Filter.Eq(x => x.Id, id);
            await this.collection.DeleteOneAsync(filter, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataChat>.Filter.Eq(x => x.Id, chatId);
            var update = Builders<DataChat>.Update.AddToSet(x => x.Participants, userId)
                                                  .Set(x => x.UpdatedAt, DateTime.UtcNow);
            await this.collection.UpdateOneAsync(filter, update, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RemoveParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataChat>.Filter.Eq(x => x.Id, chatId);
            var update = Builders<DataChat>.Update.Pull(x => x.Participants, userId)
                                                  .Set(x => x.UpdatedAt, DateTime.UtcNow);
            await this.collection.UpdateOneAsync(filter, update, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> ChatNameExistsForUserAsync(string name, string userId, CancellationToken cancellationToken = default)
        {
            // This logic might need adjustment based on requirements.
            // Assuming checking if a user is part of a chat with this name.
            var filter = Builders<DataChat>.Filter.And(
                Builders<DataChat>.Filter.Eq(x => x.Name, name),
                Builders<DataChat>.Filter.AnyEq(x => x.Participants, userId));
            return await this.collection.Find(filter).AnyAsync(cancellationToken);
        }

        private static SharedChat? MapToShared(DataChat? dataChat)
        {
            if (dataChat == null)
            {
                return null;
            }

            ChatType type;
            if (!string.IsNullOrEmpty(dataChat.Id)
                && dataChat.Id.StartsWith("saved-", StringComparison.Ordinal))
            {
                type = ChatType.SavedMessages;
            }
            else if (string.Equals(dataChat.Type, "savedmessages", StringComparison.OrdinalIgnoreCase))
            {
                type = ChatType.SavedMessages;
            }
            else if (string.Equals(dataChat.Type, "private", StringComparison.OrdinalIgnoreCase))
            {
                type = ChatType.DirectMessage;
            }
            else
            {
                Enum.TryParse(dataChat.Type, true, out type);
            }

            return new SharedChat
            {
                Id = dataChat.Id ?? string.Empty,
                Name = dataChat.Name,
                Type = type,
                Description = dataChat.Description ?? string.Empty,
                AvatarUrl = dataChat.AvatarUrl ?? string.Empty,
                ParticipantIds = dataChat.Participants,
                CreatedBy = dataChat.CreatedBy,
                CreatedAt = dataChat.CreatedAt,
                UpdatedAt = dataChat.UpdatedAt,
                LastMessageAt = dataChat.LastMessageAt,
            };
        }

        private static DataChat MapToData(SharedChat sharedChat)
        {
            string typeStr = sharedChat.Type == ChatType.DirectMessage
                || sharedChat.Type == ChatType.SavedMessages
                ? "private"
                : sharedChat.Type.ToString().ToLower();

            return new DataChat
            {
                Id = sharedChat.Id,
                Name = sharedChat.Name ?? string.Empty,
                Type = typeStr,
                Description = string.IsNullOrWhiteSpace(sharedChat.Description) ? null : sharedChat.Description,
                AvatarUrl = string.IsNullOrWhiteSpace(sharedChat.AvatarUrl) ? null : sharedChat.AvatarUrl,
                Participants = sharedChat.ParticipantIds,
                CreatedBy = sharedChat.CreatedBy,
                CreatedAt = sharedChat.CreatedAt,
                UpdatedAt = sharedChat.UpdatedAt,
                LastMessageAt = sharedChat.LastMessageAt,
            };
        }
    }
}
