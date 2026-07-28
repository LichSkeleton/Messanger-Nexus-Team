using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace NexusTeam.DbSeeder.Services;

/// <summary>
/// Service for verifying Redis connection and performing initial setup.
/// </summary>
public class RedisSeeder
{
    private readonly string connectionString;
    private readonly ILogger<RedisSeeder> logger;

    public RedisSeeder(string connectionString, ILogger<RedisSeeder> logger)
    {
        this.connectionString = connectionString;
        this.logger = logger;
    }

    /// <summary>
    /// Verifies Redis connection and performs health check.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Verifying Redis connection...");

        try
        {
            using var redis = await ConnectionMultiplexer.ConnectAsync(this.connectionString);
            var db = redis.GetDatabase();

            // Perform health check
            var pingResult = await db.PingAsync();
            this.logger.LogInformation("Redis ping: {PingTime}ms", pingResult.TotalMilliseconds);

            // Set a test key to verify write capability
            var testKey = "NexusTeam:seeder:health_check";
            await db.StringSetAsync(testKey, DateTime.UtcNow.ToString("O"), TimeSpan.FromMinutes(1));
            
            var testValue = await db.StringGetAsync(testKey);
            if (testValue.HasValue)
            {
                this.logger.LogInformation("Redis write/read test successful");
            }

            // Get Redis info
            var server = redis.GetServer(redis.GetEndPoints().First());
            var info = await server.InfoAsync("server");
            var versionGroup = info.FirstOrDefault(x => x.Key == "redis_version");
            var version = versionGroup?.FirstOrDefault().Value ?? "unknown";
            this.logger.LogInformation("Connected to Redis version: {Version}", version);

            this.logger.LogInformation("Redis verification completed successfully");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to connect to Redis: {Message}", ex.Message);
            throw;
        }
    }
}
