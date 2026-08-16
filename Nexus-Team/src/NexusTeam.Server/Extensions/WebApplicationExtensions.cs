namespace NexusTeam.Server.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using NexusTeam.Server.Middleware;
    using Serilog;

    /// <summary>
    /// Extension methods for configuring the WebApplication middleware pipeline.
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Adds custom middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <returns>The web application for chaining.</returns>
        public static WebApplication UseCustomMiddleware(this WebApplication app)
        {
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<JwtAuthenticationMiddleware>();
            app.UseMiddleware<DeviceLockMiddleware>();
            app.UseMiddleware<WebSocketHandler>();

            return app;
        }
    }
}
