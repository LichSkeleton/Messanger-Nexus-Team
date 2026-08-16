namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class DeviceLockOptionsValidatorTests
    {
        private readonly DeviceLockOptionsValidator validator = new DeviceLockOptionsValidator();

        [Fact]
        public void Validate_WithStrongPepper_Succeeds()
        {
            var result = this.validator.Validate(null, new DeviceLockOptions { PinPepper = new string('x', 32) });

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Fact]
        public void Validate_WithShortPepper_Fails()
        {
            var result = this.validator.Validate(null, new DeviceLockOptions { PinPepper = "too-short" });

            OptionValidationAssertions.ShouldFailWith(result, "Device-lock PIN pepper must contain at least 32 characters");
        }
    }
}
