namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for MongoDB configuration options.
    /// </summary>
    public class MongoOptionsValidator : IValidateOptions<MongoOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, MongoOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail("MongoDB connection string is required");
            }

            if (string.IsNullOrWhiteSpace(options.DatabaseName))
            {
                return ValidateOptionsResult.Fail("MongoDB database name is required");
            }

            if (options.ConnectionTimeout < 1 || options.ConnectionTimeout > 60)
            {
                return ValidateOptionsResult.Fail("MongoDB connection timeout must be between 1 and 60 seconds");
            }

            if (options.ServerSelectionTimeout < 1 || options.ServerSelectionTimeout > 60)
            {
                return ValidateOptionsResult.Fail("MongoDB server selection timeout must be between 1 and 60 seconds");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
