namespace NexusTeam.Server.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;

    public class UserDeviceService : IUserDeviceService
    {
        private static readonly int[] AllowedTimeouts = { 30, 60, 300, 900, 1800 };

        private readonly IUserDeviceRepository repository;
        private readonly IClock clock;
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly string pinPepper;

        public UserDeviceService(IUserDeviceRepository repository, IClock clock, IUserRepository userRepository, IPasswordHasher passwordHasher, IOptions<DeviceLockOptions> options)
        {
            this.repository = repository;
            this.clock = clock;
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.pinPepper = options.Value.PinPepper;
        }

        public async Task RegisterLoginAsync(string userId, string deviceId, string deviceName, CancellationToken cancellationToken = default)
        {
            var now = this.clock.UtcNow;
            var device = await this.repository.GetAsync(userId, deviceId, cancellationToken) ?? new UserDevice
            {
                UserId = userId,
                DeviceId = deviceId,
                CreatedAtUtc = now,
            };
            device.DeviceName = deviceName;
            device.LastSeenAtUtc = now;
            device.UpdatedAtUtc = now;
            device.RevokedAtUtc = null;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task<DeviceLockStatusDto> GetStatusAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            await this.ApplyDeadlineAsync(device, cancellationToken);
            return this.MapStatus(device);
        }

        public async Task EnableAsync(string userId, string deviceId, EnableDeviceLockRequest request, CancellationToken cancellationToken = default)
        {
            this.ValidatePin(request.Pin, request.ConfirmPin);
            this.ValidateTimeout(request.TimeoutSeconds);
            var user = await this.userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new AuthenticationException("User not found");
            if (!await this.passwordHasher.VerifyPasswordAsync(request.AccountPassword, user.PasswordHash))
            {
                throw new AuthenticationException("Invalid account password");
            }

            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            device.PinHash = await this.passwordHasher.HashPasswordAsync(this.Pepper(request.Pin));
            device.AutoLockEnabled = true;
            device.LockTimeoutSeconds = request.TimeoutSeconds;
            device.IsLocked = false;
            device.RequiresPinReset = false;
            device.RevokedAtUtc = null;
            device.FailedPinAttempts = 0;
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task UpdateAsync(string userId, string deviceId, UpdateDeviceLockRequest request, CancellationToken cancellationToken = default)
        {
            this.ValidateTimeout(request.TimeoutSeconds);
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            await this.RequireValidPinAsync(device, request.CurrentPin, cancellationToken);
            if (request.NewPin != null || request.ConfirmNewPin != null)
            {
                this.ValidatePin(request.NewPin ?? string.Empty, request.ConfirmNewPin ?? string.Empty);
                device.PinHash = await this.passwordHasher.HashPasswordAsync(this.Pepper(request.NewPin!));
            }

            device.LockTimeoutSeconds = request.TimeoutSeconds;
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task DisableAsync(string userId, string deviceId, string pin, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            await this.RequireValidPinAsync(device, pin, cancellationToken);
            device.AutoLockEnabled = false;
            device.IsLocked = false;
            device.PinHash = null;
            device.InactiveSinceUtc = null;
            device.VisibleTabIds.Clear();
            device.ActiveCallTabIds.Clear();
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task<DeviceLockStatusDto> UnlockAsync(string userId, string deviceId, string pin, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            if (device.RequiresPinReset || device.RevokedAtUtc != null)
            {
                throw new AuthenticationException("Account sign-in required");
            }

            await this.RequireValidPinAsync(device, pin, cancellationToken);
            device.IsLocked = false;
            device.InactiveSinceUtc = null;
            device.FailedPinAttempts = 0;
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
            return this.MapStatus(device);
        }

        public async Task LockNowAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            if (!device.AutoLockEnabled)
            {
                throw new DomainException("Auto-lock is not enabled");
            }

            device.IsLocked = true;
            device.InactiveSinceUtc = this.clock.UtcNow;
            device.VisibleTabIds.Clear();
            device.ActiveCallTabIds.Clear();
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task RecordActivityAsync(string userId, string deviceId, DeviceActivityRequest request, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            if (!device.AutoLockEnabled || device.IsLocked)
            {
                return;
            }

            device.VisibleTabIds.Remove(request.TabId);
            device.ActiveCallTabIds.Remove(request.TabId);
            if (request.IsVisible)
            {
                device.VisibleTabIds.Add(request.TabId);
            }

            if (request.HasActiveCall)
            {
                device.ActiveCallTabIds.Add(request.TabId);
            }

            device.InactiveSinceUtc = device.VisibleTabIds.Count > 0 || device.ActiveCallTabIds.Count > 0 ? null : device.InactiveSinceUtc ?? this.clock.UtcNow;
            device.LastSeenAtUtc = this.clock.UtcNow;
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task ForgetPinAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            device.IsLocked = true;
            device.RequiresPinReset = true;
            device.RevokedAtUtc = this.clock.UtcNow;
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task RevokeSessionAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var device = await this.RequireDeviceAsync(userId, deviceId, cancellationToken);
            device.RevokedAtUtc = this.clock.UtcNow;
            device.VisibleTabIds.Clear();
            device.ActiveCallTabIds.Clear();
            device.UpdatedAtUtc = this.clock.UtcNow;
            await this.repository.UpsertAsync(device, cancellationToken);
        }

        public async Task<DeviceAccessState> GetAccessStateAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var device = await this.repository.GetAsync(userId, deviceId, cancellationToken);
            if (device == null)
            {
                return DeviceAccessState.Missing;
            }

            await this.ApplyDeadlineAsync(device, cancellationToken);
            if (device.RevokedAtUtc != null || device.RequiresPinReset)
            {
                return DeviceAccessState.Revoked;
            }

            return device.IsLocked ? DeviceAccessState.Locked : DeviceAccessState.Allowed;
        }

        private async Task<UserDevice> RequireDeviceAsync(string userId, string deviceId, CancellationToken cancellationToken)
        {
            return await this.repository.GetAsync(userId, deviceId, cancellationToken) ?? throw new AuthenticationException("Device is not registered");
        }

        private async Task ApplyDeadlineAsync(UserDevice device, CancellationToken cancellationToken)
        {
            if (device.AutoLockEnabled && !device.IsLocked && device.InactiveSinceUtc.HasValue &&
                this.clock.UtcNow >= device.InactiveSinceUtc.Value.AddSeconds(device.LockTimeoutSeconds))
            {
                device.IsLocked = true;
                device.UpdatedAtUtc = this.clock.UtcNow;
                await this.repository.UpsertAsync(device, cancellationToken);
            }
        }

        private async Task RequireValidPinAsync(UserDevice device, string pin, CancellationToken cancellationToken)
        {
            if (!this.IsPin(pin) || device.PinHash == null || !await this.passwordHasher.VerifyPasswordAsync(this.Pepper(pin), device.PinHash))
            {
                device.FailedPinAttempts++;
                if (device.FailedPinAttempts >= 5)
                {
                    device.RequiresPinReset = true;
                    device.RevokedAtUtc = this.clock.UtcNow;
                }

                device.UpdatedAtUtc = this.clock.UtcNow;
                await this.repository.UpsertAsync(device, cancellationToken);
                throw new AuthenticationException(device.RequiresPinReset ? "Account sign-in required" : "Invalid PIN");
            }

            device.FailedPinAttempts = 0;
        }

        private string Pepper(string pin) => this.pinPepper + ":" + pin;

        private bool IsPin(string pin) => pin.Length == 4 && System.Linq.Enumerable.All(pin, char.IsDigit);

        private void ValidatePin(string pin, string confirmation)
        {
            if (!this.IsPin(pin))
            {
                throw new DomainException("PIN must contain exactly 4 digits");
            }

            if (pin != confirmation)
            {
                throw new DomainException("PIN confirmation does not match");
            }
        }

        private void ValidateTimeout(int timeout)
        {
            if (System.Array.IndexOf(AllowedTimeouts, timeout) < 0)
            {
                throw new DomainException("Invalid auto-lock timeout");
            }
        }

        private DeviceLockStatusDto MapStatus(UserDevice device) => new DeviceLockStatusDto
        {
            Enabled = device.AutoLockEnabled,
            TimeoutSeconds = device.LockTimeoutSeconds,
            IsLocked = device.IsLocked,
            RequiresPinReset = device.RequiresPinReset,
            RemainingAttempts = System.Math.Max(0, 5 - device.FailedPinAttempts),
        };
    }
}
