namespace NexusTeam.Server.Tests.Configuration
{
    using Microsoft.Extensions.Options;
    using Xunit;

    internal static class OptionValidationAssertions
    {
        public static void ShouldSucceed(ValidateOptionsResult result)
        {
            Assert.True(result.Succeeded, result.FailureMessage);
        }

        public static void ShouldFailWith(ValidateOptionsResult result, string expectedMessage)
        {
            Assert.True(result.Failed);
            Assert.Contains(expectedMessage, result.Failures);
        }
    }
}
