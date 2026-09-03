namespace NexusTeam.Server.Middleware
{
    using System;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Shared.Exceptions;
    using Serilog;

    /// <summary>
    /// Middleware for handling exceptions globally and logging them.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.next(context);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "An unhandled exception occurred while processing request {RequestPath}", context.Request.Path);
                await this.HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, includeDetail) = exception switch
            {
                ValidationException => ((int)HttpStatusCode.BadRequest, exception.Message, true),
                DuplicateChatException => ((int)HttpStatusCode.Conflict, exception.Message, true),
                DuplicateUserException => ((int)HttpStatusCode.Conflict, exception.Message, true),
                AuthenticationException => ((int)HttpStatusCode.Unauthorized, exception.Message, true),
                UnauthorizedException => ((int)HttpStatusCode.Unauthorized, exception.Message, true),
                NotFoundException => ((int)HttpStatusCode.NotFound, exception.Message, true),
                _ => ((int)HttpStatusCode.InternalServerError, "An error occurred while processing your request.", false),
            };

            context.Response.StatusCode = statusCode;

            var response = new
            {
                StatusCode = statusCode,
                Message = message,
                Detail = includeDetail ? exception.Message : message,
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}
