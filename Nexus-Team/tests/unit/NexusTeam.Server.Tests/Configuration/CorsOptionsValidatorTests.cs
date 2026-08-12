namespace NexusTeam.Server.Tests.Configuration
{
    using System.Collections.Generic;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class CorsOptionsValidatorTests
    {
        private readonly CorsOptionsValidator validator = new CorsOptionsValidator();

        [Fact]
        public void Validate_WithOriginAndPolicyName_Succeeds()
        {
            var options = new CorsOptions
            {
                AllowedOrigins = new List<string> { "https://app.example.com" },
                PolicyName = "NexusTeamCorsPolicy",
            };

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Fact]
        public void Validate_WithNoOrigins_Fails()
        {
            var options = new CorsOptions { AllowedOrigins = new List<string>() };

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(
                result,
                "At least one allowed origin must be specified");
        }

        [Fact]
        public void Validate_WithBlankPolicyName_Fails()
        {
            var options = new CorsOptions
            {
                AllowedOrigins = new List<string> { "https://app.example.com" },
                PolicyName = " ",
            };

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(result, "Policy name is required");
        }
    }
}
