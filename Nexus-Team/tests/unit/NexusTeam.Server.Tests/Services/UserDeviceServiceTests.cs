namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Xunit;

    public class UserDeviceServiceTests
    {
        [Fact]
        public async Task EnableAsync_WithValidPassword_StoresPepperedPinAndSelectedTimeout()
        {
            var fixture = new Fixture();

            await fixture.Service.EnableAsync("user-1", "device-1", new EnableDeviceLockRequest
            {
                AccountPassword = "password",
                Pin = "1234",
                ConfirmPin = "1234",
                TimeoutSeconds = 300,
            });

            Assert.True(fixture.Device.AutoLockEnabled);
            Assert.Equal(300, fixture.Device.LockTimeoutSeconds);
            Assert.Equal("hash:pepper:1234", fixture.Device.PinHash);
        }

        [Fact]
        public async Task GetAccessStateAsync_AfterInactiveDeadline_LocksDevice()
        {
            var fixture = new Fixture();
            fixture.Device.AutoLockEnabled = true;
            fixture.Device.LockTimeoutSeconds = 30;
            fixture.Device.InactiveSinceUtc = fixture.Clock.UtcNow.AddSeconds(-30);

            var state = await fixture.Service.GetAccessStateAsync("user-1", "device-1");

            Assert.Equal(DeviceAccessState.Locked, state);
            Assert.True(fixture.Device.IsLocked);
        }

        [Fact]
        public async Task UnlockAsync_WithCorrectPin_UnlocksAndResetsAttempts()
        {
            var fixture = new Fixture();
            fixture.Device.AutoLockEnabled = true;
            fixture.Device.IsLocked = true;
            fixture.Device.PinHash = "hash:pepper:1234";
            fixture.Device.FailedPinAttempts = 2;

            var status = await fixture.Service.UnlockAsync("user-1", "device-1", "1234");

            Assert.False(status.IsLocked);
            Assert.Equal(5, status.RemainingAttempts);
            Assert.Equal(0, fixture.Device.FailedPinAttempts);
        }

        [Fact]
        public async Task UnlockAsync_AfterFiveInvalidPins_RevokesDeviceAndRequiresSignIn()
        {
            var fixture = new Fixture();
            fixture.Device.AutoLockEnabled = true;
            fixture.Device.IsLocked = true;
            fixture.Device.PinHash = "hash:pepper:1234";

            for (var attempt = 0; attempt < 5; attempt++)
            {
                await Assert.ThrowsAsync<AuthenticationException>(
                    () => fixture.Service.UnlockAsync("user-1", "device-1", "9999"));
            }

            Assert.True(fixture.Device.RequiresPinReset);
            Assert.NotNull(fixture.Device.RevokedAtUtc);
            Assert.Equal(DeviceAccessState.Revoked, await fixture.Service.GetAccessStateAsync("user-1", "device-1"));
        }

        [Fact]
        public async Task RecordActivityAsync_WithVisibleTab_ClearsInactiveDeadline()
        {
            var fixture = new Fixture();
            fixture.Device.AutoLockEnabled = true;
            fixture.Device.InactiveSinceUtc = fixture.Clock.UtcNow.AddMinutes(-1);

            await fixture.Service.RecordActivityAsync("user-1", "device-1", new DeviceActivityRequest
            {
                TabId = "tab-1",
                IsVisible = true,
            });

            Assert.Null(fixture.Device.InactiveSinceUtc);
            Assert.Contains("tab-1", fixture.Device.VisibleTabIds);
        }

        [Fact]
        public async Task RevokeSessionAsync_MarksDeviceRevokedAndClearsActiveTabs()
        {
            var fixture = new Fixture();
            fixture.Device.VisibleTabIds.Add("visible-tab");
            fixture.Device.ActiveCallTabIds.Add("call-tab");

            await fixture.Service.RevokeSessionAsync("user-1", "device-1");

            Assert.Equal(fixture.Clock.UtcNow, fixture.Device.RevokedAtUtc);
            Assert.Empty(fixture.Device.VisibleTabIds);
            Assert.Empty(fixture.Device.ActiveCallTabIds);
            Assert.Equal(DeviceAccessState.Revoked, await fixture.Service.GetAccessStateAsync("user-1", "device-1"));
        }

        private sealed class Fixture
        {
            public Fixture()
            {
                this.Device = new UserDevice
                {
                    UserId = "user-1",
                    DeviceId = "device-1",
                };
                this.Clock = new MutableClock { UtcNow = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc) };
                var repository = new FakeDeviceRepository(this.Device);
                this.Service = new UserDeviceService(
                    repository,
                    this.Clock,
                    new FakeUserRepository(),
                    new FakePasswordHasher(),
                    Options.Create(new DeviceLockOptions { PinPepper = "pepper" }));
            }

            public MutableClock Clock { get; }

            public UserDevice Device { get; }

            public UserDeviceService Service { get; }
        }

        private sealed class MutableClock : IClock
        {
            public DateTime UtcNow { get; set; }
        }

        private sealed class FakePasswordHasher : IPasswordHasher
        {
            public Task<string> HashPasswordAsync(string password) => Task.FromResult("hash:" + password);

            public Task<bool> VerifyPasswordAsync(string password, string hash) => Task.FromResult(hash == "hash:" + password);
        }

        private sealed class FakeDeviceRepository : IUserDeviceRepository
        {
            private readonly UserDevice device;

            public FakeDeviceRepository(UserDevice device)
            {
                this.device = device;
            }

            public Task<UserDevice?> GetAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
                => Task.FromResult<UserDevice?>(this.device.UserId == userId && this.device.DeviceId == deviceId ? this.device : null);

            public Task UpsertAsync(UserDevice device, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly User user = new User { Id = "user-1", PasswordHash = "hash:password" };

            public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(id == this.user.Id ? this.user : null);

            public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task CreateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
