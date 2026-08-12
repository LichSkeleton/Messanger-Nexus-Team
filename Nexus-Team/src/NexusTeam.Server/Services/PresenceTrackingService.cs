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
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// Background service that reconciles persisted presence with active WebSocket connections.
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
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Error updating user presence");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            this.logger.Information("Presence tracking service stopped");
        }

        private async Task UpdateUserPresenceAsync(CancellationToken cancellationToken)
        {
            var connectedUserIds = this.connectionManager.GetConnectedUserIds().ToList();
            this.logger.Debug("Reconciling presence for {Count} connected users", connectedUserIds.Count);

            using var scope = this.scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var userStatusService = scope.ServiceProvider.GetRequiredService<IUserStatusService>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();

            foreach (var userId in connectedUserIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var user = await userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    continue;
                }

                user.LastSeenAt = clock.UtcNow;
                await userRepository.UpdateAsync(user, cancellationToken);

                var status = await userStatusService.GetStatusAsync(userId, cancellationToken);
                if (status == UserStatus.Offline)
                {
                    // Connected sockets imply online unless the user explicitly chose Invisible.
                    await userStatusService.SetStatusAsync(userId, UserStatus.Online, cancellationToken);
                }
            }
        }
    }
}
