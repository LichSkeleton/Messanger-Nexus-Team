namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>
    /// Validator for Bcrypt configuration options.
    /// </summary>
    public class BcryptOptionsValidator : IValidateOptions<BcryptOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, BcryptOptions options)
        {
            if (options.WorkFactor < 4 || options.WorkFactor > 31)
            {
                return ValidateOptionsResult.Fail("Bcrypt work factor must be between 4 and 31");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
