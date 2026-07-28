namespace NexusTeam.Server.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Serilog;

    /// <summary>
    /// Extension methods for configuring the WebApplicationBuilder.
    /// </summary>
    public static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds Serilog logging to the application builder.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for chaining.</returns>
        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            builder.Host.UseSerilog();

            return builder;
        }
    }
}
