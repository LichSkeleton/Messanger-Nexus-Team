namespace NexusTeam.Server.Services
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// Background service for tracking user presence and status.
    /// </summary>
    public class PresenceTrackingService : BackgroundService
    {
        private readonly IWebSocketConnectionManager connectionManager;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceTrackingService"/> class.
        /// </summary>
        /// <param name="connectionManager">WebSocket connection manager.</param>
        /// <param name="scopeFactory">Service scope factory for creating scoped services.</param>
        /// <param name="logger">Logger.</param>
        public PresenceTrackingService(
            IWebSocketConnectionManager connectionManager,
            IServiceScopeFactory scopeFactory,
            ILogger logger)
        {
            this.connectionManager = connectionManager;
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.Information("Presence tracking service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await this.UpdateUserPresenceAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Error updating user presence");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            this.logger.Information("Presence tracking service stopped");
        }

        private async Task UpdateUserPresenceAsync(CancellationToken cancellationToken)
        {
            this.logger.Debug("Updating user presence");

            // Create a scope to get scoped services (like IUserRepository)
            using (var scope = this.scopeFactory.CreateScope())
            {
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            }

            await Task.CompletedTask;
        }
    }
}
