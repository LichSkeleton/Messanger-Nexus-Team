namespace NexusTeam.Server.Services
{
    using System;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;

    /// <summary>
    /// Service for managing refresh tokens in Redis with expiry tracking.
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private const string RefreshTokenPrefix = "refresh_token:";
        private readonly ICacheService cacheService;
        private readonly JwtOptions jwtOptions;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshTokenService"/> class.
        /// </summary>
        /// <param name="cacheService">Cache service for Redis operations.</param>
        /// <param name="jwtOptions">JWT configuration options.</param>
        /// <param name="logger">Logger instance.</param>
        public RefreshTokenService(ICacheService cacheService, IOptions<JwtOptions> jwtOptions, ILogger logger)
        {
            this.cacheService = cacheService;
            this.jwtOptions = jwtOptions.Value;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var refreshToken = Convert.ToBase64String(randomBytes);
            var cacheKey = RefreshTokenPrefix + refreshToken;
            var expiration = TimeSpan.FromDays(this.jwtOptions.RefreshTokenExpirationDays);

            await this.cacheService.SetAsync(cacheKey, userId, expiration, cancellationToken);

            this.logger.Information("Generated refresh token for user {UserId} with expiration {ExpirationDays} days", userId, this.jwtOptions.RefreshTokenExpirationDays);

            return refreshToken;
        }

        /// <inheritdoc/>
        public async Task<string?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var cacheKey = RefreshTokenPrefix + refreshToken;
            var userId = await this.cacheService.GetAsync<string>(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(userId))
            {
                this.logger.Warning("Refresh token validation failed: token not found or expired");
                return null;
            }

            this.logger.Debug("Refresh token validated successfully for user {UserId}", userId);
            return userId;
        }

        /// <inheritdoc/>
        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var cacheKey = RefreshTokenPrefix + refreshToken;
            await this.cacheService.RemoveAsync(cacheKey, cancellationToken);

            this.logger.Information("Revoked refresh token");
        }
    }
}
