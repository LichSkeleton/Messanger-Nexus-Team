namespace NexusTeam.Shared.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Shared.Helpers;
    using Xunit;

    public class GuidIdGeneratorTests
    {
        [Fact]
        public void GenerateId_ReturnsNonEmptyGuidString()
        {
            var generator = new GuidIdGenerator();

            var id = generator.GenerateId();

            Assert.True(Guid.TryParse(id, out var parsed));
            Assert.NotEqual(Guid.Empty, parsed);
        }

        [Fact]
        public void GenerateId_AcrossMultipleCalls_ReturnsUniqueValues()
        {
            var generator = new GuidIdGenerator();
            var ids = new HashSet<string>();

            for (var index = 0; index < 100; index++)
            {
                ids.Add(generator.GenerateId());
            }

            Assert.Equal(100, ids.Count);
        }
    }
}
