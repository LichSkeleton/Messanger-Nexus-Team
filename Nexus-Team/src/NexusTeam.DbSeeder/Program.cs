using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using NexusTeam.DbSeeder.Services;

namespace NexusTeam.DbSeeder;

/// <summary>
/// Database seeder console application for NexusTeam.
/// Initializes MongoDB (with sharding), Oracle, and Redis databases.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("NexusTeam Database Seeder - .NET 8 C# Console Application");
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        // Build configuration from environment variables
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Read configuration
        var config = new SeederConfiguration
        {
            MongoConnectionString = configuration["MONGODB_CONNECTION_STRING"] ?? "mongodb://localhost:27017",
            MongoDatabase = configuration["MONGODB_DATABASE"] ?? "NexusTeam",
            OracleConnectionString = configuration["ORACLE_CONNECTION_STRING"] ?? throw new InvalidOperationException("ORACLE_CONNECTION_STRING not set"),
            RedisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379",
            ConfigPath = configuration["CONFIG_PATH"] ?? "/app/configs",
            RetryCount = int.Parse(configuration["RETRY_COUNT"] ?? "10"),
            RetryDelaySeconds = int.Parse(configuration["RETRY_DELAY_SECONDS"] ?? "5"),
            EnableSharding = bool.Parse(configuration["ENABLE_SHARDING"] ?? "true"),
            ShardCollections = configuration["SHARD_COLLECTIONS"]?.Split(',') ?? Array.Empty<string>()
        };

        logger.LogInformation("Configuration loaded:");
        logger.LogInformation("  MongoDB: {Connection}", config.MongoConnectionString);
        logger.LogInformation("  Oracle: {Connection}", MaskConnectionString(config.OracleConnectionString));
        logger.LogInformation("  Redis: {Connection}", config.RedisConnectionString);
        logger.LogInformation("  Config Path: {Path}", config.ConfigPath);
        logger.LogInformation("  Retry Count: {Count}", config.RetryCount);
        logger.LogInformation("  Retry Delay: {Seconds}s", config.RetryDelaySeconds);
        logger.LogInformation("  Enable Sharding: {Enabled}", config.EnableSharding);
        logger.LogInformation("  Shard Collections: {Collections}", string.Join(", ", config.ShardCollections));
        Console.WriteLine();

        // Define retry policy using Polly
        var retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = config.RetryCount,
                Delay = TimeSpan.FromSeconds(config.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Retry attempt {Attempt} of {MaxRetries} after {Delay}ms delay. Error: {Error}",
                        args.AttemptNumber,
                        config.RetryCount,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        try
        {
            // Verify config path exists
            if (!Directory.Exists(config.ConfigPath))
            {
                logger.LogError("Configuration directory not found: {Path}", config.ConfigPath);
                logger.LogError("Please ensure the config directory is mounted as a volume.");
                return 1;
            }

            // Seed Redis
            logger.LogInformation("Step 1/4: Seeding Redis...");
            await retryPipeline.ExecuteAsync(async ct =>
            {
                var redisSeeder = new RedisSeeder(config.RedisConnectionString, serviceProvider.GetRequiredService<ILogger<RedisSeeder>>());
                await redisSeeder.SeedAsync(ct);
            });
            Console.WriteLine();

            // Seed Oracle
            logger.LogInformation("Step 2/4: Seeding Oracle...");
            await retryPipeline.ExecuteAsync(async ct =>
            {
                var oracleSeeder = new OracleSeeder(config.OracleConnectionString, serviceProvider.GetRequiredService<ILogger<OracleSeeder>>());
                await oracleSeeder.SeedAsync(config.ConfigPath, ct);
            });
            Console.WriteLine();

            // Seed MongoDB
            logger.LogInformation("Step 3/4: Seeding MongoDB...");
            await retryPipeline.ExecuteAsync(async ct =>
            {
                var mongoSeeder = new MongoSeeder(
                    config.MongoConnectionString,
                    config.MongoDatabase,
                    serviceProvider.GetRequiredService<ILogger<MongoSeeder>>(),
                    config.EnableSharding,
                    config.ShardCollections);
                await mongoSeeder.SeedAsync(config.ConfigPath, ct);
            });
            Console.WriteLine();

            // Seed demo data (fixed users + chat history) - always from scratch
            logger.LogInformation("Step 4/4: Seeding demo data (users + chat history)...");
            await retryPipeline.ExecuteAsync(async ct =>
            {
                var dataSeeder = new DataSeeder(
                    config.OracleConnectionString,
                    config.MongoConnectionString,
                    config.MongoDatabase,
                    serviceProvider.GetRequiredService<ILogger<DataSeeder>>());
                await dataSeeder.SeedAsync(ct);
            });
            Console.WriteLine();

            Console.WriteLine("================================================================================");
            Console.WriteLine("Success: Database seeding completed successfully!");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("All databases are ready for use:");
            Console.WriteLine("  [OK] Redis - Cache, Sessions, Presence");
            Console.WriteLine("  [OK] Oracle - Users, Email Verifications");
            Console.WriteLine("  [OK] MongoDB - Chats, Messages, Attachments, User Preferences");
            if (config.EnableSharding)
            {
                Console.WriteLine($"  [OK] MongoDB Sharding - Enabled for: {string.Join(", ", config.ShardCollections)}");
            }
            Console.WriteLine();
            Console.WriteLine("Demo accounts (password for all: " + DataSeeder.DemoPassword + "):");
            Console.WriteLine("  - Pavalo");
            Console.WriteLine("  - Olen");
            Console.WriteLine("  - Vlad");
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database seeding failed: {Message}", ex.Message);
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("Failed: Database seeding failed!");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            return 1;
        }
    }

    /// <summary>
    /// Masks sensitive information in connection strings for logging.
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        // Mask password in connection string
        var passwordIndex = connectionString.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        if (passwordIndex == -1)
        {
            return connectionString;
        }

        var endIndex = connectionString.IndexOf(';', passwordIndex);
        if (endIndex == -1)
        {
            endIndex = connectionString.Length;
        }

        var masked = connectionString.Substring(0, passwordIndex) + "Password=***" + connectionString.Substring(endIndex);
        return masked;
    }
}

/// <summary>
/// Configuration for the database seeder.
/// </summary>
internal class SeederConfiguration
{
    public string MongoConnectionString { get; set; } = string.Empty;
    public string MongoDatabase { get; set; } = string.Empty;
    public string OracleConnectionString { get; set; } = string.Empty;
    public string RedisConnectionString { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int RetryDelaySeconds { get; set; }
    public bool EnableSharding { get; set; }
    public string[] ShardCollections { get; set; } = Array.Empty<string>();
}
