namespace NexusTeam.Server.Controllers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Models;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;

    [ApiController]
    [Route("api/device-lock")]
    public class DeviceLockController : ControllerBase
    {
        private readonly IUserDeviceService deviceService;

        public DeviceLockController(IUserDeviceService deviceService)
        {
            this.deviceService = deviceService;
        }

        [HttpGet("status")]
        public async Task<ActionResult<DeviceLockStatusDto>> Status(CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            return this.Ok(await this.deviceService.GetStatusAsync(userId, deviceId, cancellationToken));
        }

        [HttpPost("enable")]
        public async Task<ActionResult> Enable([FromBody] EnableDeviceLockRequest request, CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.deviceService.EnableAsync(userId, deviceId, request, cancellationToken);
                return this.NoContent();
            }
            catch (AuthenticationException ex)
            {
                return this.Unauthorized(new ErrorResponse { Error = ex.Message });
            }
            catch (DomainException ex)
            {
                return this.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] UpdateDeviceLockRequest request, CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.deviceService.UpdateAsync(userId, deviceId, request, cancellationToken);
                return this.NoContent();
            }
            catch (DomainException ex)
            {
                return this.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }

        [HttpPost("disable")]
        public async Task<ActionResult> Disable([FromBody] VerifyDevicePinRequest request, CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.deviceService.DisableAsync(userId, deviceId, request.Pin, cancellationToken);
                return this.NoContent();
            }
            catch (DomainException ex)
            {
                return this.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }

        [HttpPost("unlock")]
        public async Task<ActionResult<DeviceLockStatusDto>> Unlock([FromBody] VerifyDevicePinRequest request, CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            try
            {
                return this.Ok(await this.deviceService.UnlockAsync(userId, deviceId, request.Pin, cancellationToken));
            }
            catch (AuthenticationException ex)
            {
                DeviceLockStatusDto? status = null;
                try
                {
                    status = await this.deviceService.GetStatusAsync(userId, deviceId, cancellationToken);
                }
                catch (DomainException)
                {
                    // The original authentication failure is the response contract.
                }

                return this.Unauthorized(new { error = ex.Message, code = status?.RequiresPinReset == true ? "PIN_RESET_REQUIRED" : "INVALID_PIN", remainingAttempts = status?.RemainingAttempts ?? 0 });
            }
        }

        [HttpPost("lock")]
        public async Task<ActionResult> Lock(CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            await this.deviceService.LockNowAsync(userId, deviceId, cancellationToken);
            return this.NoContent();
        }

        [HttpPost("activity")]
        public async Task<ActionResult> Activity([FromBody] DeviceActivityRequest request, CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            await this.deviceService.RecordActivityAsync(userId, deviceId, request, cancellationToken);
            return this.NoContent();
        }

        [HttpPost("forgot")]
        public async Task<ActionResult> Forgot(CancellationToken cancellationToken)
        {
            if (!this.TryIdentity(out var userId, out var deviceId))
            {
                return this.Unauthorized();
            }

            await this.deviceService.ForgetPinAsync(userId, deviceId, cancellationToken);
            this.Response.Cookies.Delete("nexus_refresh");
            return this.NoContent();
        }

        private bool TryIdentity(out string userId, out string deviceId)
        {
            userId = this.HttpContext.Items["UserId"] as string ?? string.Empty;
            deviceId = this.HttpContext.Items["DeviceId"] as string ?? string.Empty;
            return userId.Length > 0 && deviceId.Length > 0;
        }
    }
}
