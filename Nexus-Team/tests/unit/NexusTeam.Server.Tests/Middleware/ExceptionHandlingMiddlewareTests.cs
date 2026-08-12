namespace NexusTeam.Server.Tests.Middleware
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Middleware;
    using NexusTeam.Shared.Exceptions;
    using Serilog;
    using Xunit;

    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenNextSucceeds_DoesNotReplaceResponse()
        {
            var nextWasCalled = false;
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new ExceptionHandlingMiddleware(
                context =>
                {
                    nextWasCalled = true;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.CompletedTask;
                },
                logger);
            var context = CreateContext();

            await middleware.InvokeAsync(context);

            Assert.True(nextWasCalled);
            Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
            Assert.Equal(0, context.Response.Body.Length);
        }

        [Theory]
        [InlineData("validation", StatusCodes.Status400BadRequest, "Input is invalid")]
        [InlineData("duplicate-chat", StatusCodes.Status409Conflict, "Chat already exists")]
        [InlineData("duplicate-user", StatusCodes.Status409Conflict, "User already exists")]
        [InlineData("authentication", StatusCodes.Status401Unauthorized, "Invalid credentials")]
        public async Task InvokeAsync_WithKnownException_ReturnsExpectedJsonResponse(
            string exceptionType,
            int expectedStatus,
            string expectedMessage)
        {
            var context = await InvokeWithExceptionAsync(
                CreateException(exceptionType, expectedMessage));

            Assert.Equal(expectedStatus, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            using var document = await ReadResponseAsync(context);
            Assert.Equal(expectedStatus, document.RootElement.GetProperty("StatusCode").GetInt32());
            Assert.Equal(expectedMessage, document.RootElement.GetProperty("Message").GetString());
            Assert.Equal(expectedMessage, document.RootElement.GetProperty("Detail").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WithUnexpectedException_ReturnsGenericServerError()
        {
            var context = await InvokeWithExceptionAsync(
                new InvalidOperationException("database password leaked"));

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            using var document = await ReadResponseAsync(context);
            Assert.Equal(
                "An error occurred while processing your request.",
                document.RootElement.GetProperty("Message").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WithUnexpectedException_DoesNotExposeInternalDetail()
        {
            const string SensitiveDetail = "database password leaked";
            var context = await InvokeWithExceptionAsync(
                new InvalidOperationException(SensitiveDetail));

            using var document = await ReadResponseAsync(context);
            Assert.NotEqual(
                SensitiveDetail,
                document.RootElement.GetProperty("Detail").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedException_ReturnsUnauthorized()
        {
            var context = await InvokeWithExceptionAsync(
                new UnauthorizedException("Token is missing"));

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithNotFoundException_ReturnsNotFound()
        {
            var context = await InvokeWithExceptionAsync(
                new NotFoundException("Chat was not found"));

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        }

        private static DefaultHttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<DefaultHttpContext> InvokeWithExceptionAsync(Exception exception)
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new ExceptionHandlingMiddleware(_ => throw exception, logger);
            var context = CreateContext();

            await middleware.InvokeAsync(context);

            return context;
        }

        private static async Task<JsonDocument> ReadResponseAsync(DefaultHttpContext context)
        {
            context.Response.Body.Position = 0;
            return await JsonDocument.ParseAsync(context.Response.Body);
        }

        private static Exception CreateException(string exceptionType, string message)
        {
            return exceptionType switch
            {
                "validation" => new ValidationException(message),
                "duplicate-chat" => new DuplicateChatException(message, new Exception("cause")),
                "duplicate-user" => new DuplicateUserException(message),
                "authentication" => new AuthenticationException(message),
                _ => throw new Xunit.Sdk.XunitException($"Unknown exception type: {exceptionType}"),
            };
        }
    }
}
