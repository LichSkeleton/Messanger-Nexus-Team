namespace NexusTeam.Server.Tests.Services
{
    using System;
    using Microsoft.Extensions.Options;
    using MongoDB.Driver;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services;
    using Xunit;

    public class MongoClientFactoryTests
    {
        [Fact]
        public void Constructor_ConfiguresClientTimeoutsAndDatabaseNameWithoutConnecting()
        {
            var factory = new MongoClientFactory(Options.Create(new MongoOptions
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "nexus_tests",
                ConnectionTimeout = 12,
                ServerSelectionTimeout = 7,
            }));

            var client = Assert.IsType<MongoClient>(factory.GetClient());
            Assert.Equal(TimeSpan.FromSeconds(12), client.Settings.ConnectTimeout);
            Assert.Equal(TimeSpan.FromSeconds(7), client.Settings.ServerSelectionTimeout);
            Assert.Equal("nexus_tests", factory.GetDatabase().DatabaseNamespace.DatabaseName);
            Assert.Same(factory.GetClient(), factory.GetClient());
            Assert.Same(factory.GetDatabase(), factory.GetDatabase());
        }

        [Theory]
        [InlineData("")]
        [InlineData("not a mongodb url")]
        public void Constructor_WithInvalidConnectionString_Throws(string connectionString)
        {
            Assert.ThrowsAny<Exception>(() => new MongoClientFactory(Options.Create(new MongoOptions
            {
                ConnectionString = connectionString,
                DatabaseName = "db",
            })));
        }
    }
}
