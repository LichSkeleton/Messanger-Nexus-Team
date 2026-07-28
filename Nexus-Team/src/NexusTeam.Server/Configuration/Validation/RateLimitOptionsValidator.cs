namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for rate limit configuration options.
    /// </summary>
    public class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
    {
        /// <summary>
        /// Validates the rate limit options.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>The validation result.</returns>
        public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
        {
            if (options.LoginMaxAttempts <= 0)
            {
                return ValidateOptionsResult.Fail("LoginMaxAttempts must be greater than 0");
            }

            if (options.LoginWindowSeconds <= 0)
            {
                return ValidateOptionsResult.Fail("LoginWindowSeconds must be greater than 0");
            }

            if (options.MessageMaxAttempts <= 0)
            {
                return ValidateOptionsResult.Fail("MessageMaxAttempts must be greater than 0");
            }

            if (options.MessageWindowSeconds <= 0)
            {
                return ValidateOptionsResult.Fail("MessageWindowSeconds must be greater than 0");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
