namespace NexusTeam.Shared.Tests.Helpers
{
    using System;
    using NexusTeam.Shared.Helpers;
    using Xunit;

    public class SystemClockTests
    {
        [Fact]
        public void UtcNow_ReturnsCurrentUtcTime()
        {
            var clock = new SystemClock();
            var before = DateTime.UtcNow;

            var result = clock.UtcNow;

            var after = DateTime.UtcNow;
            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.InRange(result, before, after);
        }
    }
}
