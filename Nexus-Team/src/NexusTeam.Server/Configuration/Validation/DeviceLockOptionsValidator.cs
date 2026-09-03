namespace NexusTeam.Server.Configuration.Validation
{
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;

    /// <summary>Validates device-lock security configuration.</summary>
    public class DeviceLockOptionsValidator : IValidateOptions<DeviceLockOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, DeviceLockOptions options)
        {
            return options.PinPepper.Length >= 32
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail("Device-lock PIN pepper must contain at least 32 characters");
        }
    }
}
