namespace NexusTeam.Server.Tests.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Middleware;
    using Xunit;

    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsExpectedSecurityHeaders()
        {
            var context = new DefaultHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
            Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"]);
            Assert.Equal(
                "strict-origin-when-cross-origin",
                context.Response.Headers["Referrer-Policy"]);
            Assert.Equal(
                "geolocation=(), microphone=(), camera=()",
                context.Response.Headers["Permissions-Policy"]);
            Assert.Equal(
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;",
                context.Response.Headers["Content-Security-Policy"]);
        }

        [Fact]
        public async Task InvokeAsync_WhenContentSecurityPolicyExists_PreservesExistingValue()
        {
            var context = new DefaultHttpContext();
            context.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            Assert.Equal(
                "default-src 'none'",
                context.Response.Headers["Content-Security-Policy"]);
        }

        [Fact]
        public async Task InvokeAsync_CallsNextExactlyOnce()
        {
            var calls = 0;
            var context = new DefaultHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ =>
            {
                calls++;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task InvokeAsync_WhenNextThrows_AddsHeadersBeforePropagatingException()
        {
            var context = new DefaultHttpContext();
            var middleware = new SecurityHeadersMiddleware(
                _ => throw new InvalidOperationException("downstream failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        }
    }
}
