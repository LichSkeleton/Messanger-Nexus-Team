namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for Redis configuration options.
    /// </summary>
    public class RedisOptionsValidator : IValidateOptions<RedisOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, RedisOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail("Redis connection string is required");
            }

            if (options.DefaultDatabase < -1 || options.DefaultDatabase > 15)
            {
                return ValidateOptionsResult.Fail("Redis database index must be between -1 and 15");
            }

            if (options.ConnectTimeout < 1000 || options.ConnectTimeout > 30000)
            {
                return ValidateOptionsResult.Fail("Redis connect timeout must be between 1000 and 30000 milliseconds");
            }

            if (options.SyncTimeout < 1000 || options.SyncTimeout > 30000)
            {
                return ValidateOptionsResult.Fail("Redis sync timeout must be between 1000 and 30000 milliseconds");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
