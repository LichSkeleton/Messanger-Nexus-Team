namespace NexusTeam.Server.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;

    /// <summary>
    /// Middleware for validating JWT tokens and populating user context.
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private const string AuthorizationHeader = "Authorization";
        private const string BearerPrefix = "Bearer ";
        private const string UserIdKey = "UserId";
        private readonly RequestDelegate next;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtAuthenticationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger instance.</param>
        public JwtAuthenticationMiddleware(RequestDelegate next, ILogger logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="jwtTokenService">JWT token service.</param>
        /// <param name="userRepository">User repository.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService, IUserRepository userRepository)
        {
            var authHeader = context.Request.Headers[AuthorizationHeader].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring(BearerPrefix.Length).Trim();

                try
                {
                    var userId = await jwtTokenService.ValidateTokenAsync(token);

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await userRepository.GetByIdAsync(userId);

                        if (user != null)
                        {
                            context.Items[UserIdKey] = userId;
                            context.Items["User"] = user;
                            this.logger.Debug("JWT authentication successful for user {UserId}", userId);
                        }
                        else
                        {
                            this.logger.Warning("JWT token valid but user {UserId} not found", userId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "JWT authentication failed");
                }
            }

            await this.next(context);
        }
    }
}
