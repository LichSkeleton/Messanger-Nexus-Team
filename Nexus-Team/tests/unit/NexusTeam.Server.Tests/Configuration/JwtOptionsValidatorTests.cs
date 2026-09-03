namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class JwtOptionsValidatorTests
    {
        private readonly JwtOptionsValidator validator = new JwtOptionsValidator();

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1440, 90)]
        public void Validate_WithBoundaryExpirations_Succeeds(
            int expirationMinutes,
            int refreshExpirationDays)
        {
            var options = CreateValidOptions();
            options.ExpirationMinutes = expirationMinutes;
            options.RefreshTokenExpirationDays = refreshExpirationDays;

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Theory]
        [InlineData("missing-secret", "JWT secret key is required")]
        [InlineData("short-secret", "JWT secret key must be at least 32 characters")]
        [InlineData("missing-issuer", "JWT issuer is required")]
        [InlineData("missing-audience", "JWT audience is required")]
        [InlineData("expiration-low", "JWT token expiration must be between 1 and 1440 minutes")]
        [InlineData("expiration-high", "JWT token expiration must be between 1 and 1440 minutes")]
        [InlineData("refresh-low", "JWT refresh token expiration must be between 1 and 90 days")]
        [InlineData("refresh-high", "JWT refresh token expiration must be between 1 and 90 days")]
        public void Validate_WithInvalidSetting_FailsWithExpectedMessage(
            string invalidSetting,
            string expectedMessage)
        {
            var options = CreateValidOptions();
            ApplyInvalidSetting(options, invalidSetting);

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(result, expectedMessage);
        }

        private static JwtOptions CreateValidOptions()
        {
            return new JwtOptions
            {
                SecretKey = new string('s', 32),
                Issuer = "NexusTeam",
                Audience = "NexusTeam.Client",
                ExpirationMinutes = 60,
                RefreshTokenExpirationDays = 7,
            };
        }

        private static void ApplyInvalidSetting(JwtOptions options, string invalidSetting)
        {
            switch (invalidSetting)
            {
                case "missing-secret":
                    options.SecretKey = " ";
                    break;
                case "short-secret":
                    options.SecretKey = new string('s', 31);
                    break;
                case "missing-issuer":
                    options.Issuer = string.Empty;
                    break;
                case "missing-audience":
                    options.Audience = string.Empty;
                    break;
                case "expiration-low":
                    options.ExpirationMinutes = 0;
                    break;
                case "expiration-high":
                    options.ExpirationMinutes = 1441;
                    break;
                case "refresh-low":
                    options.RefreshTokenExpirationDays = 0;
                    break;
                case "refresh-high":
                    options.RefreshTokenExpirationDays = 91;
                    break;
                default:
                    throw new Xunit.Sdk.XunitException($"Unknown setting: {invalidSetting}");
            }
        }
    }
}
