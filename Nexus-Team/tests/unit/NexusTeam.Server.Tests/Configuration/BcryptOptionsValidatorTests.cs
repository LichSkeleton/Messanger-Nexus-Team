namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class BcryptOptionsValidatorTests
    {
        private readonly BcryptOptionsValidator validator = new BcryptOptionsValidator();

        [Theory]
        [InlineData(4)]
        [InlineData(31)]
        public void Validate_WithBoundaryWorkFactor_Succeeds(int workFactor)
        {
            var result = this.validator.Validate(null, new BcryptOptions { WorkFactor = workFactor });

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(32)]
        public void Validate_WithWorkFactorOutsideRange_Fails(int workFactor)
        {
            var result = this.validator.Validate(null, new BcryptOptions { WorkFactor = workFactor });

            OptionValidationAssertions.ShouldFailWith(
                result,
                "Bcrypt work factor must be between 4 and 31");
        }
    }
}
