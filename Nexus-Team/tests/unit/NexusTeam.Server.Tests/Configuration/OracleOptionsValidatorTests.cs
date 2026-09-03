namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class OracleOptionsValidatorTests
    {
        private readonly OracleOptionsValidator validator = new OracleOptionsValidator();

        [Theory]
        [InlineData(1, 0)]
        [InlineData(300, 5)]
        public void Validate_WithBoundaryValues_Succeeds(int commandTimeout, int retryAttempts)
        {
            var options = CreateValidOptions();
            options.CommandTimeout = commandTimeout;
            options.MaxRetryAttempts = retryAttempts;

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Theory]
        [InlineData("connection", "Oracle connection string is required")]
        [InlineData("timeout-low", "Oracle command timeout must be between 1 and 300 seconds")]
        [InlineData("timeout-high", "Oracle command timeout must be between 1 and 300 seconds")]
        [InlineData("retry-low", "Oracle max retry attempts must be between 0 and 5")]
        [InlineData("retry-high", "Oracle max retry attempts must be between 0 and 5")]
        public void Validate_WithInvalidSetting_Fails(
            string invalidSetting,
            string expectedMessage)
        {
            var options = CreateValidOptions();
            ApplyInvalidSetting(options, invalidSetting);

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(result, expectedMessage);
        }

        private static OracleOptions CreateValidOptions()
        {
            return new OracleOptions
            {
                ConnectionString = "User Id=nexus;Data Source=oracle:1521/FREEPDB1",
                CommandTimeout = 30,
                MaxRetryAttempts = 3,
            };
        }

        private static void ApplyInvalidSetting(OracleOptions options, string invalidSetting)
        {
            switch (invalidSetting)
            {
                case "connection": options.ConnectionString = " "; break;
                case "timeout-low": options.CommandTimeout = 0; break;
                case "timeout-high": options.CommandTimeout = 301; break;
                case "retry-low": options.MaxRetryAttempts = -1; break;
                case "retry-high": options.MaxRetryAttempts = 6; break;
                default: throw new Xunit.Sdk.XunitException($"Unknown setting: {invalidSetting}");
            }
        }
    }
}
