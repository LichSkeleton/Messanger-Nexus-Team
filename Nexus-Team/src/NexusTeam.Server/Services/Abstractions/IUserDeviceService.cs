namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>Manages registration and application-lock state for browser devices.</summary>
    public interface IUserDeviceService
    {
        /// <summary>Registers or refreshes a device after account sign-in.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="deviceName">Display label.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task RegisterLoginAsync(string userId, string deviceId, string deviceName, CancellationToken cancellationToken = default);

        /// <summary>Gets current lock settings and state.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Current device-lock status.</returns>
        Task<DeviceLockStatusDto> GetStatusAsync(string userId, string deviceId, CancellationToken cancellationToken = default);

        /// <summary>Enables locking after account-password verification.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="request">Enable request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task EnableAsync(string userId, string deviceId, EnableDeviceLockRequest request, CancellationToken cancellationToken = default);

        /// <summary>Updates the timeout or PIN.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="request">Update request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task UpdateAsync(string userId, string deviceId, UpdateDeviceLockRequest request, CancellationToken cancellationToken = default);

        /// <summary>Disables locking after PIN verification.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="pin">Current PIN.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task DisableAsync(string userId, string deviceId, string pin, CancellationToken cancellationToken = default);

        /// <summary>Unlocks a device after PIN verification.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="pin">Current PIN.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated device-lock status.</returns>
        Task<DeviceLockStatusDto> UnlockAsync(string userId, string deviceId, string pin, CancellationToken cancellationToken = default);

        /// <summary>Locks the device immediately.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task LockNowAsync(string userId, string deviceId, CancellationToken cancellationToken = default);

        /// <summary>Records browser-tab visibility and active-call state.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="request">Activity report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task RecordActivityAsync(string userId, string deviceId, DeviceActivityRequest request, CancellationToken cancellationToken = default);

        /// <summary>Revokes the device session because the PIN was forgotten.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task ForgetPinAsync(string userId, string deviceId, CancellationToken cancellationToken = default);

        /// <summary>Revokes the current device session without requiring a PIN reset.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task RevokeSessionAsync(string userId, string deviceId, CancellationToken cancellationToken = default);

        /// <summary>Gets the authorization state used by request middleware.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The current access state.</returns>
        Task<DeviceAccessState> GetAccessStateAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
    }
}
