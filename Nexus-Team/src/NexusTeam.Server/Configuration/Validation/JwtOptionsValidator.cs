namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for JWT configuration options.
    /// </summary>
    public class JwtOptionsValidator : IValidateOptions<JwtOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, JwtOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                return ValidateOptionsResult.Fail("JWT secret key is required");
            }

            if (options.SecretKey.Length < 32)
            {
                return ValidateOptionsResult.Fail("JWT secret key must be at least 32 characters");
            }

            if (string.IsNullOrWhiteSpace(options.Issuer))
            {
                return ValidateOptionsResult.Fail("JWT issuer is required");
            }

            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                return ValidateOptionsResult.Fail("JWT audience is required");
            }

            if (options.ExpirationMinutes < 1 || options.ExpirationMinutes > 1440)
            {
                return ValidateOptionsResult.Fail("JWT token expiration must be between 1 and 1440 minutes");
            }

            if (options.RefreshTokenExpirationDays < 1 || options.RefreshTokenExpirationDays > 90)
            {
                return ValidateOptionsResult.Fail("JWT refresh token expiration must be between 1 and 90 days");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
