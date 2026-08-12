namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class RateLimitOptionsValidatorTests
    {
        private readonly RateLimitOptionsValidator validator = new RateLimitOptionsValidator();

        [Fact]
        public void Validate_WithPositiveLimitsAndWindows_Succeeds()
        {
            var result = this.validator.Validate(null, CreateValidOptions());

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Theory]
        [InlineData("login-attempts", "LoginMaxAttempts must be greater than 0")]
        [InlineData("login-window", "LoginWindowSeconds must be greater than 0")]
        [InlineData("message-attempts", "MessageMaxAttempts must be greater than 0")]
        [InlineData("message-window", "MessageWindowSeconds must be greater than 0")]
        public void Validate_WithNonPositiveSetting_Fails(
            string invalidSetting,
            string expectedMessage)
        {
            var options = CreateValidOptions();
            ApplyInvalidSetting(options, invalidSetting);

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(result, expectedMessage);
        }

        private static RateLimitOptions CreateValidOptions()
        {
            return new RateLimitOptions
            {
                LoginMaxAttempts = 5,
                LoginWindowSeconds = 300,
                MessageMaxAttempts = 60,
                MessageWindowSeconds = 60,
            };
        }

        private static void ApplyInvalidSetting(RateLimitOptions options, string invalidSetting)
        {
            switch (invalidSetting)
            {
                case "login-attempts": options.LoginMaxAttempts = 0; break;
                case "login-window": options.LoginWindowSeconds = -1; break;
                case "message-attempts": options.MessageMaxAttempts = 0; break;
                case "message-window": options.MessageWindowSeconds = -1; break;
                default: throw new Xunit.Sdk.XunitException($"Unknown setting: {invalidSetting}");
            }
        }
    }
}
