namespace NexusTeam.Server.Middleware
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>Rejects authenticated device requests after the device has locked.</summary>
    public class DeviceLockMiddleware
    {
        private readonly RequestDelegate next;

        public DeviceLockMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserDeviceService devices)
        {
            var path = context.Request.Path;
            if (path.StartsWithSegments("/api/auth") || path.StartsWithSegments("/api/device-lock") ||
                path.StartsWithSegments("/health") || context.Items["UserId"] is not string userId)
            {
                await this.next(context);
                return;
            }

            if (context.Items["DeviceId"] is not string deviceId || string.IsNullOrWhiteSpace(deviceId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Device-bound authentication required", code = "DEVICE_REQUIRED" });
                return;
            }

            DeviceAccessState state;
            try
            {
                state = await devices.GetAccessStateAsync(userId, deviceId, context.RequestAborted);
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "Device security state is unavailable", code = "DEVICE_SECURITY_UNAVAILABLE" });
                return;
            }

            if (state == DeviceAccessState.Allowed)
            {
                await this.next(context);
                return;
            }

            context.Response.StatusCode = state == DeviceAccessState.Locked ? StatusCodes.Status423Locked : StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = state == DeviceAccessState.Locked ? "Device is locked" : "Device session is not valid",
                code = state == DeviceAccessState.Locked ? "DEVICE_LOCKED" : "DEVICE_SESSION_INVALID",
            });
        }
    }
}
