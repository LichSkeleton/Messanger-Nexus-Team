namespace NexusTeam.Server.Services
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Models;
    using Serilog;

    /// <summary>
    /// Service for generating and validating JWT tokens using HS256 algorithm.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions options;
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly TokenValidationParameters validationParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
        /// </summary>
        /// <param name="options">JWT configuration options.</param>
        /// <param name="clock">Clock for getting current time.</param>
        /// <param name="logger">Logger instance.</param>
        public JwtTokenService(IOptions<JwtOptions> options, IClock clock, ILogger logger)
        {
            this.options = options.Value;
            this.clock = clock;
            this.logger = logger;

            var key = Encoding.UTF8.GetBytes(this.options.SecretKey);
            this.validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = this.options.Issuer,
                ValidAudience = this.options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5),
            };
        }

        /// <inheritdoc/>
        public Task<string> GenerateAccessTokenAsync(User user)
        {
            var key = Encoding.UTF8.GetBytes(this.options.SecretKey);
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = this.clock.UtcNow;
            var expiration = now.AddMinutes(this.options.ExpirationMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var token = new JwtSecurityToken(
                issuer: this.options.Issuer,
                audience: this.options.Audience,
                claims: claims,
                notBefore: now,
                expires: expiration,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            this.logger.Information("Generated JWT token for user {UserId}", user.Id);

            return Task.FromResult(tokenString);
        }

        /// <inheritdoc/>
        public Task<string?> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, this.validationParameters, out var validatedToken);

                // ASP.NET Core can transform claim names during validation
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    // Fallback for standard JWT "sub" claim
                    userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    // Another fallback for string "sub"
                    userId = principal.FindFirst("sub")?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    this.logger.Warning("JWT token validation failed: missing user ID claim");

                    // Log all available claims for debugging
                    var allClaims = string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}"));
                    this.logger.Warning("Available claims: {Claims}", allClaims);

                    return Task.FromResult<string?>(null);
                }

                this.logger.Debug("JWT token validated successfully for user {UserId}", userId);
                return Task.FromResult<string?>(userId);
            }
            catch (SecurityTokenException ex)
            {
                this.logger.Warning(ex, "JWT token validation failed: {Message}", ex.Message);
                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Unexpected error during JWT token validation");
                return Task.FromResult<string?>(null);
            }
        }
    }
}
