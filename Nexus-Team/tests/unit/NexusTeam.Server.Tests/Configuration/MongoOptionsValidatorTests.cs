namespace NexusTeam.Server.Tests.Configuration
{
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using Xunit;

    public class MongoOptionsValidatorTests
    {
        private readonly MongoOptionsValidator validator = new MongoOptionsValidator();

        [Theory]
        [InlineData(1, 1)]
        [InlineData(60, 60)]
        public void Validate_WithBoundaryTimeouts_Succeeds(
            int connectionTimeout,
            int selectionTimeout)
        {
            var options = CreateValidOptions();
            options.ConnectionTimeout = connectionTimeout;
            options.ServerSelectionTimeout = selectionTimeout;

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldSucceed(result);
        }

        [Theory]
        [InlineData("connection", "MongoDB connection string is required")]
        [InlineData("database", "MongoDB database name is required")]
        [InlineData("connection-timeout-low", "MongoDB connection timeout must be between 1 and 60 seconds")]
        [InlineData("connection-timeout-high", "MongoDB connection timeout must be between 1 and 60 seconds")]
        [InlineData("selection-timeout-low", "MongoDB server selection timeout must be between 1 and 60 seconds")]
        [InlineData("selection-timeout-high", "MongoDB server selection timeout must be between 1 and 60 seconds")]
        public void Validate_WithInvalidSetting_Fails(
            string invalidSetting,
            string expectedMessage)
        {
            var options = CreateValidOptions();
            ApplyInvalidSetting(options, invalidSetting);

            var result = this.validator.Validate(null, options);

            OptionValidationAssertions.ShouldFailWith(result, expectedMessage);
        }

        private static MongoOptions CreateValidOptions()
        {
            return new MongoOptions
            {
                ConnectionString = "mongodb://mongos:27017",
                DatabaseName = "NexusTeam",
                ConnectionTimeout = 10,
                ServerSelectionTimeout = 5,
            };
        }

        private static void ApplyInvalidSetting(MongoOptions options, string invalidSetting)
        {
            switch (invalidSetting)
            {
                case "connection": options.ConnectionString = string.Empty; break;
                case "database": options.DatabaseName = " "; break;
                case "connection-timeout-low": options.ConnectionTimeout = 0; break;
                case "connection-timeout-high": options.ConnectionTimeout = 61; break;
                case "selection-timeout-low": options.ServerSelectionTimeout = 0; break;
                case "selection-timeout-high": options.ServerSelectionTimeout = 61; break;
                default: throw new Xunit.Sdk.XunitException($"Unknown setting: {invalidSetting}");
            }
        }
    }
}
