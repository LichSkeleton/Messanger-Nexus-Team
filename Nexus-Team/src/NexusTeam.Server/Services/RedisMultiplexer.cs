namespace NexusTeam.Server.Services
{
    using System;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services.Abstractions;
    using StackExchange.Redis;

    /// <summary>
    /// Implementation of Redis connection multiplexer wrapper.
    /// </summary>
    public class RedisMultiplexer : IRedisMultiplexer, IDisposable
    {
        private readonly IConnectionMultiplexer multiplexer;
        private readonly int defaultDatabase;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMultiplexer"/> class.
        /// </summary>
        /// <param name="options">Redis configuration options.</param>
        public RedisMultiplexer(IOptions<RedisOptions> options)
        {
            var redisOptions = options.Value;
            var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            configurationOptions.ConnectTimeout = redisOptions.ConnectTimeout;
            configurationOptions.SyncTimeout = redisOptions.SyncTimeout;
            configurationOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;

            this.multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            this.defaultDatabase = redisOptions.DefaultDatabase;
        }

        /// <inheritdoc/>
        public IConnectionMultiplexer GetMultiplexer()
        {
            return this.multiplexer;
        }

        /// <inheritdoc/>
        public IDatabase GetDatabase(int db = -1)
        {
            var dbIndex = db == -1 ? this.defaultDatabase : db;
            return this.multiplexer.GetDatabase(dbIndex);
        }

        /// <summary>
        /// Disposes the Redis connection multiplexer.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the managed resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.multiplexer?.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}
