namespace NexusTeam.Server.Configuration.Validation
{
    using System.Linq;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for CORS configuration options.
    /// </summary>
    public class CorsOptionsValidator : IValidateOptions<CorsOptions>
    {
        /// <summary>
        /// Validates the CORS options.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>The validation result.</returns>
        public ValidateOptionsResult Validate(string? name, CorsOptions options)
        {
            if (options.AllowedOrigins == null || !options.AllowedOrigins.Any())
            {
                return ValidateOptionsResult.Fail("At least one allowed origin must be specified");
            }

            if (string.IsNullOrWhiteSpace(options.PolicyName))
            {
                return ValidateOptionsResult.Fail("Policy name is required");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
