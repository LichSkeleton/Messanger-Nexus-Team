namespace NexusTeam.Server.Controllers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Models;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;
    using Serilog;

    /// <summary>
    /// Controller for authentication endpoints.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly IRateLimitService rateLimitService;
        private readonly ISessionService sessionService;
        private readonly ILogger logger;
        private readonly IRefreshTokenService? refreshTokenService;
        private readonly IUserDeviceService? userDeviceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        /// <param name="rateLimitService">Rate limit service.</param>
        /// <param name="sessionService">Session service.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="refreshTokenService">Device-bound refresh-token service.</param>
        /// <param name="userDeviceService">Per-device session service.</param>
        public AuthController(
            IAuthService authService,
            IRateLimitService rateLimitService,
            ISessionService sessionService,
            ILogger logger,
            IRefreshTokenService? refreshTokenService = null,
            IUserDeviceService? userDeviceService = null)
        {
            this.authService = authService;
            this.rateLimitService = rateLimitService;
            this.sessionService = sessionService;
            this.logger = logger;
            this.refreshTokenService = refreshTokenService;
            this.userDeviceService = userDeviceService;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="request">Registration request data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Registration response.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(RegisterResponse), 400)]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var response = await this.authService.RegisterAsync(request, cancellationToken);

            if (!response.Success)
            {
                return this.BadRequest(response);
            }

            this.logger.Information("User registration successful via API");
            return this.Ok(response);
        }

        /// <summary>
        /// Authenticates a user and returns JWT tokens.
        /// </summary>
        /// <param name="request">Login request data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Login response with access and refresh tokens.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 429)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var identifier = request.UsernameOrEmail;
            var isAllowed = await this.rateLimitService.IsLoginAllowedAsync(identifier, cancellationToken);
            if (!isAllowed)
            {
                var resetTime = await this.rateLimitService.GetLoginRateLimitResetTimeAsync(identifier, cancellationToken);
                this.logger.Warning("Rate limit exceeded for login attempt: {Identifier}", identifier);
                return this.StatusCode(429, new ErrorResponse
                {
                    Error = "Too many login attempts",
                    Details = $"Please try again in {resetTime} seconds",
                });
            }

            try
            {
                var response = await this.authService.LoginAsync(request, cancellationToken);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    this.Response.Cookies.Append("nexus_refresh", response.RefreshToken, RefreshCookieOptions());
                    response.RefreshToken = null;
                }

                this.logger.Information("User login successful via API");
                return this.Ok(response);
            }
            catch (AuthenticationException ex)
            {
                this.logger.Warning(ex, "Authentication failed");
                return this.Unauthorized(new ErrorResponse { Error = ex.Message });
            }
        }

        /// <summary>Rotates a device refresh token and returns a new access token.</summary>
        /// <param name="refreshTokens">Refresh-token service.</param>
        /// <param name="jwtTokens">JWT service.</param>
        /// <param name="users">User repository.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A refreshed login response.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromServices] IRefreshTokenService refreshTokens,
            [FromServices] IJwtTokenService jwtTokens,
            [FromServices] IUserRepository users,
            CancellationToken cancellationToken)
        {
            if (!this.Request.Cookies.TryGetValue("nexus_refresh", out var token))
            {
                return this.Unauthorized();
            }

            var identity = await refreshTokens.ValidateRefreshTokenIdentityAsync(token, cancellationToken);
            if (identity == null)
            {
                return this.Unauthorized();
            }

            var user = await users.GetByIdAsync(identity.UserId, cancellationToken);
            if (user == null)
            {
                return this.Unauthorized();
            }

            await refreshTokens.RevokeRefreshTokenAsync(token, cancellationToken);
            var rotated = await refreshTokens.GenerateRefreshTokenAsync(identity.UserId, identity.DeviceId, cancellationToken);
            this.Response.Cookies.Append("nexus_refresh", rotated, RefreshCookieOptions());
            return this.Ok(new LoginResponse
            {
                AccessToken = await jwtTokens.GenerateAccessTokenAsync(user, identity.DeviceId),
                ExpiresIn = 3600,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl,
                    Status = user.Status,
                    LastSeenAt = user.LastSeenAt,
                },
            });
        }

        /// <summary>
        /// Logs out the current user and invalidates their session.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Success response.</returns>
        [HttpPost("logout")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                this.logger.Warning("Logout attempt without authentication");
                return this.Unauthorized();
            }

            try
            {
                if (this.Request.Cookies.TryGetValue("nexus_refresh", out var refreshToken))
                {
                    if (this.refreshTokenService != null)
                    {
                        await this.refreshTokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
                    }

                    this.Response.Cookies.Delete("nexus_refresh", new Microsoft.AspNetCore.Http.CookieOptions { Path = "/api/auth" });
                }
                else
                {
                    // Compatibility for older sessions which pre-date device-bound refresh tokens.
                    await this.sessionService.RemoveSessionAsync(userId, cancellationToken);
                }

                if (this.HttpContext.Items["DeviceId"] is string deviceId &&
                    !string.IsNullOrWhiteSpace(deviceId) &&
                    this.userDeviceService != null)
                {
                    await this.userDeviceService.RevokeSessionAsync(userId, deviceId, cancellationToken);
                }

                this.logger.Information("User {UserId} logged out successfully", userId);
                return this.Ok(new { message = "Logged out successfully" });
            }
            catch (System.Exception ex)
            {
                this.logger.Error(ex, "Error during logout for user {UserId}", userId);

                // Still return OK since the user should be logged out on the client side
                return this.Ok(new { message = "Logged out successfully" });
            }
        }

        private static Microsoft.AspNetCore.Http.CookieOptions RefreshCookieOptions() => new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            Secure = false,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        };
    }
}
