namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class RedisOptionsValidatorTests
    {
        private readonly RedisOptionsValidator validator = new RedisOptionsValidator();

        [Theory]
        [InlineData(-1, 1000, 1000)]
        [InlineData(15, 30000, 30000)]
        public void Validate_WithBoundaryValues_Succeeds(
            int database,
            int connectTimeout,
            int syncTimeout)
        {
            var options = CreateValidOptions();
            options.DefaultDatabase = database;
            options.ConnectTimeout = connectTimeout;
            options.SyncTimeout = syncTimeout;

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Theory]
        [InlineData("connection", "Redis connection string is required")]
        [InlineData("database-low", "Redis database index must be between -1 and 15")]
        [InlineData("database-high", "Redis database index must be between -1 and 15")]
        [InlineData("connect-low", "Redis connect timeout must be between 1000 and 30000 milliseconds")]
        [InlineData("connect-high", "Redis connect timeout must be between 1000 and 30000 milliseconds")]
        [InlineData("sync-low", "Redis sync timeout must be between 1000 and 30000 milliseconds")]
        [InlineData("sync-high", "Redis sync timeout must be between 1000 and 30000 milliseconds")]
        public void Validate_WithInvalidSetting_Fails(
            string invalidSetting,
            string expectedMessage)
        {
            var options = CreateValidOptions();
            ApplyInvalidSetting(options, invalidSetting);

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(result, expectedMessage);
        }

        private static RedisOptions CreateValidOptions()
        {
            return new RedisOptions
            {
                ConnectionString = "redis:6379",
                DefaultDatabase = 0,
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
            };
        }

        private static void ApplyInvalidSetting(RedisOptions options, string invalidSetting)
        {
            switch (invalidSetting)
            {
                case "connection": options.ConnectionString = " "; break;
                case "database-low": options.DefaultDatabase = -2; break;
                case "database-high": options.DefaultDatabase = 16; break;
                case "connect-low": options.ConnectTimeout = 999; break;
                case "connect-high": options.ConnectTimeout = 30001; break;
                case "sync-low": options.SyncTimeout = 999; break;
                case "sync-high": options.SyncTimeout = 30001; break;
                default: throw new Xunit.Sdk.XunitException($"Unknown setting: {invalidSetting}");
            }
        }
    }
}
