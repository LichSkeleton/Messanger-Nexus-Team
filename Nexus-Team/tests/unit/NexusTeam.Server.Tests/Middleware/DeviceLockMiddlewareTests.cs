namespace NexusTeam.Server.Tests.Middleware
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Middleware;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class DeviceLockMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenDeviceIsLocked_Returns423WithoutCallingNext()
        {
            var nextCalled = false;
            var middleware = new DeviceLockMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var context = AuthenticatedContext();

            await middleware.InvokeAsync(context, new FakeDeviceService { State = DeviceAccessState.Locked });

            Assert.Equal(StatusCodes.Status423Locked, context.Response.StatusCode);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WhenDownstreamThrows_DoesNotConvertExceptionTo503()
        {
            var middleware = new DeviceLockMiddleware(_ => throw new InvalidOperationException("downstream"));
            var context = AuthenticatedContext();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => middleware.InvokeAsync(context, new FakeDeviceService { State = DeviceAccessState.Allowed }));

            Assert.Equal("downstream", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_WhenDeviceStoreFails_Returns503()
        {
            var middleware = new DeviceLockMiddleware(_ => Task.CompletedTask);
            var context = AuthenticatedContext();

            await middleware.InvokeAsync(context, new FakeDeviceService { AccessException = new Exception("store unavailable") });

            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        }

        private static DefaultHttpContext AuthenticatedContext()
        {
            var context = new DefaultHttpContext();
            context.Items["UserId"] = "user-1";
            context.Items["DeviceId"] = "device-1";
            context.Request.Path = "/api/chats";
            context.Response.Body = new System.IO.MemoryStream();
            return context;
        }

        private sealed class FakeDeviceService : IUserDeviceService
        {
            public Exception? AccessException { get; set; }

            public DeviceAccessState State { get; set; } = DeviceAccessState.Allowed;

            public Task<DeviceAccessState> GetAccessStateAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
                => this.AccessException == null ? Task.FromResult(this.State) : Task.FromException<DeviceAccessState>(this.AccessException);

            public Task RegisterLoginAsync(string userId, string deviceId, string deviceName, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<DeviceLockStatusDto> GetStatusAsync(string userId, string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task EnableAsync(string userId, string deviceId, EnableDeviceLockRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task UpdateAsync(string userId, string deviceId, UpdateDeviceLockRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task DisableAsync(string userId, string deviceId, string pin, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<DeviceLockStatusDto> UnlockAsync(string userId, string deviceId, string pin, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task LockNowAsync(string userId, string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task RecordActivityAsync(string userId, string deviceId, DeviceActivityRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task ForgetPinAsync(string userId, string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task RevokeSessionAsync(string userId, string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
