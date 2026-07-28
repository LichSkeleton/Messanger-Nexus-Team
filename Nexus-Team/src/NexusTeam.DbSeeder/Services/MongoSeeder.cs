using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using NexusTeam.DbSeeder.Models;

namespace NexusTeam.DbSeeder.Services;

/// <summary>
/// Service for seeding MongoDB collections with schemas, validators, and indexes.
/// </summary>
public class MongoSeeder
{
    private readonly IMongoClient mongoClient;
    private readonly IMongoDatabase database;
    private readonly ILogger<MongoSeeder> logger;
    private readonly bool enableSharding;
    private readonly HashSet<string> shardCollections;

    public MongoSeeder(
        string connectionString,
        string databaseName,
        ILogger<MongoSeeder> logger,
        bool enableSharding = false,
        string[]? shardCollections = null)
    {
        this.mongoClient = new MongoClient(connectionString);
        this.database = this.mongoClient.GetDatabase(databaseName);
        this.logger = logger;
        this.enableSharding = enableSharding;
        this.shardCollections = new HashSet<string>(shardCollections ?? Array.Empty<string>());
    }

    /// <summary>
    /// Seeds all MongoDB collections from JSON configuration files.
    /// </summary>
    public async Task SeedAsync(string configPath, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Starting MongoDB seeding process...");
        this.logger.LogInformation("Configuration path: {ConfigPath}", configPath);

        var mongoConfigPath = Path.Combine(configPath, "mongodb");
        if (!Directory.Exists(mongoConfigPath))
        {
            this.logger.LogWarning("MongoDB config directory not found: {Path}", mongoConfigPath);
            return;
        }

        var jsonFiles = Directory.GetFiles(mongoConfigPath, "*_config.json");
        this.logger.LogInformation("Found {Count} MongoDB configuration files", jsonFiles.Length);

        // Initialize sharded cluster if enabled
        if (this.enableSharding)
        {
            await this.InitializeShardingAsync(cancellationToken);
        }

        foreach (var jsonFile in jsonFiles)
        {
            await this.ProcessCollectionConfigAsync(jsonFile, cancellationToken);
        }

        this.logger.LogInformation("MongoDB seeding completed successfully");
    }

    /// <summary>
    /// Initializes MongoDB sharding for the database.
    /// Replica sets are pre-initialized by mongo-init container, so we only add shards here.
    /// </summary>
    private async Task InitializeShardingAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.logger.LogInformation("Initializing MongoDB sharded cluster (replica sets pre-initialized by mongo-init)...");

            var adminDb = this.mongoClient.GetDatabase("admin");

            // Replica sets are already initialized by mongo-init container
            // We just need to add shards to the mongos cluster
            this.logger.LogInformation("Adding shards to mongos cluster...");
            await this.AddShardAsync(adminDb, "shard1ReplSet/mongo-shard1:27017", cancellationToken);
            await this.AddShardAsync(adminDb, "shard2ReplSet/mongo-shard2:27017", cancellationToken);

            // Enable sharding on database
            var enableShardingCommand = new BsonDocument
            {
                { "enableSharding", this.database.DatabaseNamespace.DatabaseName }
            };
            await adminDb.RunCommandAsync<BsonDocument>(enableShardingCommand, cancellationToken: cancellationToken);
            this.logger.LogInformation("Enabled sharding for database: {Database}", this.database.DatabaseNamespace.DatabaseName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "AlreadyInitialized" || ex.Message.Contains("already"))
        {
            this.logger.LogInformation("Sharded cluster already initialized, skipping...");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to initialize sharding (may already be configured): {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Adds a shard to the cluster.
    /// </summary>
    private async Task AddShardAsync(IMongoDatabase adminDb, string shardConnectionString, CancellationToken cancellationToken)
    {
        try
        {
            var command = new BsonDocument { { "addShard", shardConnectionString } };
            await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
            this.logger.LogInformation("Added shard: {Shard}", shardConnectionString);
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("already") || ex.CodeName == "OperationFailed")
        {
            this.logger.LogInformation("Shard {Shard} already added", shardConnectionString);
        }
    }

    /// <summary>
    /// Processes a single collection configuration file.
    /// </summary>
    private async Task ProcessCollectionConfigAsync(string jsonFilePath, CancellationToken cancellationToken)
    {
        try
        {
            this.logger.LogInformation("Processing configuration file: {FileName}", Path.GetFileName(jsonFilePath));

            var jsonContent = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
            var config = JsonSerializer.Deserialize<MongoCollectionConfig>(jsonContent);

            if (config == null || string.IsNullOrWhiteSpace(config.CollectionName))
            {
                this.logger.LogWarning("Invalid configuration in file: {FileName}", Path.GetFileName(jsonFilePath));
                return;
            }

            await this.CreateOrUpdateCollectionAsync(config, cancellationToken);
            await this.CreateIndexesAsync(config, cancellationToken);

            // Enable sharding for specific collections
            if (this.enableSharding && this.shardCollections.Contains(config.CollectionName))
            {
                await this.EnableCollectionShardingAsync(config.CollectionName, cancellationToken);
            }

            this.logger.LogInformation("Successfully processed collection: {CollectionName}", config.CollectionName);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to process configuration file: {FileName}", Path.GetFileName(jsonFilePath));
            throw;
        }
    }

    /// <summary>
    /// Creates or updates a MongoDB collection with validator.
    /// </summary>
    private async Task CreateOrUpdateCollectionAsync(MongoCollectionConfig config, CancellationToken cancellationToken)
    {
        var collectionNames = (await this.database.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToList();
        var exists = collectionNames.Contains(config.CollectionName);

        if (exists)
        {
            this.logger.LogInformation("Collection {CollectionName} already exists, updating validator...", config.CollectionName);
            await this.UpdateValidatorAsync(config, cancellationToken);
        }
        else
        {
            this.logger.LogInformation("Creating collection {CollectionName}...", config.CollectionName);
            await this.CreateCollectionAsync(config, cancellationToken);
        }
    }

    /// <summary>
    /// Creates a new collection with validator.
    /// </summary>
    private async Task CreateCollectionAsync(MongoCollectionConfig config, CancellationToken cancellationToken)
    {
        var validatorDoc = config.GetValidatorBsonDocument();
        
        if (validatorDoc != null)
        {
            // Use RunCommand to create collection with validator
            var command = new BsonDocument
            {
                { "create", config.CollectionName },
                { "validator", validatorDoc },
                { "validationLevel", config.ValidationLevel },
                { "validationAction", config.ValidationAction }
            };

            await this.database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
        }
        else
        {
            // Create collection without validator
            await this.database.CreateCollectionAsync(config.CollectionName, cancellationToken: cancellationToken);
        }

        this.logger.LogInformation("Created collection: {CollectionName}", config.CollectionName);
    }

    /// <summary>
    /// Updates the validator for an existing collection.
    /// </summary>
    private async Task UpdateValidatorAsync(MongoCollectionConfig config, CancellationToken cancellationToken)
    {
        var validatorDoc = config.GetValidatorBsonDocument();
        if (validatorDoc == null)
        {
            return;
        }

        var command = new BsonDocument
        {
            { "collMod", config.CollectionName },
            { "validator", validatorDoc },
            { "validationLevel", config.ValidationLevel },
            { "validationAction", config.ValidationAction }
        };

        await this.database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
        this.logger.LogInformation("Updated validator for collection: {CollectionName}", config.CollectionName);
    }

    /// <summary>
    /// Creates indexes for a collection (idempotent).
    /// </summary>
    private async Task CreateIndexesAsync(MongoCollectionConfig config, CancellationToken cancellationToken)
    {
        if (config.Indexes == null || config.Indexes.Count == 0)
        {
            return;
        }

        var collection = this.database.GetCollection<BsonDocument>(config.CollectionName);
        var existingIndexes = await (await collection.Indexes.ListAsync(cancellationToken)).ToListAsync(cancellationToken);
        var existingIndexNames = existingIndexes
            .Select(idx => idx.GetValue("name", string.Empty).AsString)
            .ToHashSet();

        foreach (var indexConfig in config.Indexes)
        {
            if (existingIndexNames.Contains(indexConfig.Name))
            {
                this.logger.LogInformation("Index {IndexName} already exists on {CollectionName}, skipping...", indexConfig.Name, config.CollectionName);
                continue;
            }

            var indexKeysDoc = indexConfig.GetKeyBsonDocument();
            var indexModel = new CreateIndexModel<BsonDocument>(
                indexKeysDoc,
                new CreateIndexOptions
                {
                    Name = indexConfig.Name,
                    Unique = indexConfig.Unique
                });

            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
            this.logger.LogInformation("Created index {IndexName} on {CollectionName}", indexConfig.Name, config.CollectionName);
        }
    }

    /// <summary>
    /// Enables sharding for a specific collection.
    /// </summary>
    private async Task EnableCollectionShardingAsync(string collectionName, CancellationToken cancellationToken)
    {
        try
        {
            var adminDb = this.mongoClient.GetDatabase("admin");
            var fullCollectionName = $"{this.database.DatabaseNamespace.DatabaseName}.{collectionName}";

            // Determine shard key based on collection
            var shardKey = collectionName switch
            {
                "messages" => new BsonDocument { { "chatId", 1 }, { "_id", 1 } },
                "attachments" => new BsonDocument { { "messageId", 1 }, { "_id", 1 } },
                _ => new BsonDocument { { "_id", "hashed" } }
            };

            var command = new BsonDocument
            {
                { "shardCollection", fullCollectionName },
                { "key", shardKey }
            };

            await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
            this.logger.LogInformation("Enabled sharding for collection: {CollectionName} with key: {ShardKey}", collectionName, shardKey.ToJson());
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("already sharded"))
        {
            this.logger.LogInformation("Collection {CollectionName} already sharded", collectionName);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to enable sharding for collection {CollectionName}: {Message}", collectionName, ex.Message);
        }
    }
}
