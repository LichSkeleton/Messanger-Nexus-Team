namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Server.Services;
    using Xunit;

    public class ServerInfrastructureHelpersTests
    {
        [Fact]
        public void UlidGenerator_ReturnsNonEmptyGuidString()
        {
            var generator = new UlidGenerator();

            var id = generator.GenerateId();

            Assert.True(Guid.TryParse(id, out var parsed));
            Assert.NotEqual(Guid.Empty, parsed);
        }

        [Fact]
        public void UlidGenerator_AcrossMultipleCalls_ReturnsUniqueValues()
        {
            var generator = new UlidGenerator();
            var values = new HashSet<string>();

            for (var index = 0; index < 100; index++)
            {
                values.Add(generator.GenerateId());
            }

            Assert.Equal(100, values.Count);
        }

        [Fact]
        public void SystemClock_ReturnsCurrentUtcTime()
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
