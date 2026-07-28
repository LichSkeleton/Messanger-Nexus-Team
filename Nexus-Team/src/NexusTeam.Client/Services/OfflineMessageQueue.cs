namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteDB;
    using Serilog;

    /// <summary>
    /// Implementation of offline message queue using LiteDB for persistence.
    /// </summary>
    public class OfflineMessageQueue : IOfflineMessageQueue, IDisposable
    {
        private readonly ILogger logger;
        private readonly IAuthenticationService authenticationService;
        private readonly LiteDatabase database;
        private readonly ILiteCollection<QueuedMessage> collection;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OfflineMessageQueue"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="authenticationService">Authentication service for user context.</param>
        /// <param name="databasePath">Optional path to the database file.</param>
        public OfflineMessageQueue(ILogger logger, IAuthenticationService authenticationService, string? databasePath = null)
        {
            this.logger = logger;
            this.authenticationService = authenticationService;

            var path = databasePath ?? this.GetDefaultDatabasePath();
            this.EnsureDirectoryExists(path);

            try
            {
                // Use connection string with connection=shared to allow multiple connections
                var connectionString = $"Filename={path};Connection=shared";
                this.database = new LiteDatabase(connectionString);
                this.collection = this.database.GetCollection<QueuedMessage>("queued_messages");
                this.collection.EnsureIndex(x => x.MessageId, true);
                this.collection.EnsureIndex(x => x.QueuedAt);

                this.logger.Information("Offline message queue initialized with database at {Path}", path);
            }
            catch (LiteDB.LiteException ex) when (ex.Message.Contains("locked") || ex.Message.Contains("being used"))
            {
                this.logger.Error(ex, "Database is locked by another process. Another instance of the application may be running.");
                throw new InvalidOperationException(
                    "Cannot access database. Another instance of the application may be running. Please close all instances and try again.",
                    ex);
            }
        }

        /// <inheritdoc/>
        public Task EnqueueAsync(QueuedMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                this.collection.Upsert(message);
                this.logger.Debug("Message {MessageId} enqueued", message.MessageId);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to enqueue message {MessageId}", message.MessageId);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<List<QueuedMessage>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = this.collection.Query()
                    .OrderBy(x => x.QueuedAt)
                    .ToList();

                this.logger.Debug("Retrieved {Count} queued messages", messages.Count);
                return Task.FromResult(messages);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to retrieve queued messages");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = this.collection.Delete(new BsonValue(messageId));
                if (deleted)
                {
                    this.logger.Debug("Message {MessageId} removed from queue", messageId);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to remove message {MessageId} from queue", messageId);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = this.collection.DeleteAll();
                this.logger.Information("Cleared {Count} messages from queue", deleted);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to clear message queue");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var count = this.collection.Count();
                return Task.FromResult(count);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get queue count");
                throw;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the service.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.database?.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private string GetDefaultDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var nexusTeamPath = Path.Combine(appDataPath, "NexusTeam", "Data");

            // CRITICAL: Create per-user database file to isolate offline messages
            var userId = this.authenticationService.CurrentUser?.Id ?? "anonymous";
            var sanitizedUserId = this.SanitizeFileName(userId);

            return Path.Combine(nexusTeamPath, $"offline_messages_{sanitizedUserId}.db");
        }

        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }

        private void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
