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
    using DataAttachment = NexusTeam.Server.Data.Models.MessageAttachment;
    using SharedAttachment = NexusTeam.Shared.Models.MessageAttachment;

    /// <summary>
    /// MongoDB implementation of the message attachment repository.
    /// </summary>
    public class MongoMessageAttachmentRepository : IMessageAttachmentRepository
    {
        private readonly IMongoCollection<DataAttachment> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoMessageAttachmentRepository"/> class.
        /// </summary>
        /// <param name="mongoClientFactory">The MongoDB client factory.</param>
        public MongoMessageAttachmentRepository(IMongoClientFactory mongoClientFactory)
        {
            var database = mongoClientFactory.GetDatabase();
            this.collection = database.GetCollection<DataAttachment>("attachments");
        }

        /// <inheritdoc/>
        public async Task AddAsync(SharedAttachment attachment, CancellationToken cancellationToken = default)
        {
            var dataAttachment = MapToData(attachment);
            await this.collection.InsertOneAsync(dataAttachment, null, cancellationToken);
            attachment.Id = dataAttachment.Id!;
        }

        /// <inheritdoc/>
        public async Task<SharedAttachment?> GetByIdAsync(string attachmentId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataAttachment>.Filter.Eq(x => x.Id, attachmentId);
            var dataAttachment = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return MapToShared(dataAttachment);
        }

        /// <inheritdoc/>
        public async Task<List<SharedAttachment>> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataAttachment>.Filter.Eq(x => x.MessageId, messageId);
            var dataAttachments = await this.collection.Find(filter).ToListAsync(cancellationToken);
            return dataAttachments.Select(MapToShared).Where(a => a != null).Cast<SharedAttachment>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(SharedAttachment attachment, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataAttachment>.Filter.Eq(x => x.Id, attachment.Id);
            var dataAttachment = MapToData(attachment);
            await this.collection.ReplaceOneAsync(filter, dataAttachment, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string attachmentId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataAttachment>.Filter.Eq(x => x.Id, attachmentId);
            await this.collection.DeleteOneAsync(filter, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<SharedAttachment>> GetByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataAttachment>.Filter.In(x => x.MessageId, messageIds);
            var dataAttachments = await this.collection.Find(filter).ToListAsync(cancellationToken);
            return dataAttachments.Select(MapToShared).Where(a => a != null).Cast<SharedAttachment>().ToList();
        }

        /// <inheritdoc/>
        public async Task<List<string>> DeleteByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DataAttachment>.Filter.In(x => x.MessageId, messageIds);

            // First get all attachment IDs for returning
            var attachments = await this.collection.Find(filter)
                .Project(x => x.Id)
                .ToListAsync(cancellationToken);

            var attachmentIds = attachments.Where(id => id != null).Cast<string>().ToList();

            // Then delete all attachments from database
            await this.collection.DeleteManyAsync(filter, cancellationToken);

            return attachmentIds;
        }

        private static SharedAttachment? MapToShared(DataAttachment? dataAttachment)
        {
            if (dataAttachment == null)
            {
                return null;
            }

            // Map MongoDB attachmentType (0-5, 99) to AttachmentType enum
            AttachmentType attachmentType = dataAttachment.AttachmentType switch
            {
                0 => AttachmentType.Image,
                1 => AttachmentType.Video,
                2 => AttachmentType.Audio,
                3 => AttachmentType.Document,
                4 => AttachmentType.Archive,
                5 => AttachmentType.Code,
                99 => AttachmentType.Other,
                _ => AttachmentType.Other,
            };

            return new SharedAttachment
            {
                Id = dataAttachment.Id ?? string.Empty,
                MessageId = dataAttachment.MessageId,
                FileName = dataAttachment.FileName,
                FilePath = dataAttachment.FilePath,
                ThumbnailPath = dataAttachment.ThumbnailPath,
                FileSize = dataAttachment.FileSize,
                ContentType = dataAttachment.ContentType,
                AttachmentType = attachmentType,
                UploadedAt = dataAttachment.UploadedAt,
            };
        }

        private static DataAttachment MapToData(SharedAttachment sharedAttachment)
        {
            // Map AttachmentType enum to MongoDB attachmentType (0-5, 99)
            int attachmentType = sharedAttachment.AttachmentType switch
            {
                AttachmentType.Image => 0,
                AttachmentType.Video => 1,
                AttachmentType.Audio => 2,
                AttachmentType.Document => 3,
                AttachmentType.Archive => 4,
                AttachmentType.Code => 5,
                AttachmentType.Other => 99,
                _ => 99,
            };

            return new DataAttachment
            {
                Id = sharedAttachment.Id,
                MessageId = sharedAttachment.MessageId,
                FileName = sharedAttachment.FileName,
                FilePath = sharedAttachment.FilePath,
                ThumbnailPath = sharedAttachment.ThumbnailPath,
                FileSize = sharedAttachment.FileSize,
                ContentType = sharedAttachment.ContentType,
                AttachmentType = attachmentType,
                UploadedAt = sharedAttachment.UploadedAt,
            };
        }
    }
}
