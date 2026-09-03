namespace NexusTeam.Server.Tests.Middleware
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Middleware;
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;
    using Xunit;

    public class RequestLoggingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_LogsRequestAndResponseMetadata()
        {
            var sink = new CollectingSink();
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(sink)
                .CreateLogger();
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/chats";
            context.Request.QueryString = new QueryString("?page=2");
            var middleware = new RequestLoggingMiddleware(
                downstreamContext =>
                {
                    downstreamContext.Response.StatusCode = StatusCodes.Status204NoContent;
                    return Task.CompletedTask;
                },
                logger);

            await middleware.InvokeAsync(context);

            Assert.Equal(2, sink.Events.Count);
            Assert.Equal(LogEventLevel.Information, sink.Events[0].Level);
            Assert.Equal("GET", GetScalarValue(sink.Events[0], "Method"));
            Assert.Equal("/api/chats", GetScalarValue(sink.Events[0], "Path"));
            Assert.Equal("?page=2", GetScalarValue(sink.Events[0], "QueryString"));
            Assert.Equal(204, GetScalarValue(sink.Events[1], "StatusCode"));
            Assert.True(sink.Events[1].Properties.ContainsKey("ElapsedMilliseconds"));
        }

        [Fact]
        public async Task InvokeAsync_CallsNextExactlyOnce()
        {
            var calls = 0;
            var sink = new CollectingSink();
            using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
            var middleware = new RequestLoggingMiddleware(
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                logger);

            await middleware.InvokeAsync(new DefaultHttpContext());

            Assert.Equal(1, calls);
        }

        private static object? GetScalarValue(LogEvent logEvent, string propertyName)
        {
            var scalar = Assert.IsType<ScalarValue>(logEvent.Properties[propertyName]);
            return scalar.Value;
        }

        private sealed class CollectingSink : ILogEventSink
        {
            public List<LogEvent> Events { get; } = new List<LogEvent>();

            public void Emit(LogEvent logEvent)
            {
                this.Events.Add(logEvent);
            }
        }
    }
}
