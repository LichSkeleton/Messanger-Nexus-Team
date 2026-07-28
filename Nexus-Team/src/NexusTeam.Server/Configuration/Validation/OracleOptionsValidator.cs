namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for Oracle configuration options.
    /// </summary>
    public class OracleOptionsValidator : IValidateOptions<OracleOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, OracleOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail("Oracle connection string is required");
            }

            if (options.CommandTimeout < 1 || options.CommandTimeout > 300)
            {
                return ValidateOptionsResult.Fail("Oracle command timeout must be between 1 and 300 seconds");
            }

            if (options.MaxRetryAttempts < 0 || options.MaxRetryAttempts > 5)
            {
                return ValidateOptionsResult.Fail("Oracle max retry attempts must be between 0 and 5");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
