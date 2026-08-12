namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Serilog;
    using Xunit;

    public class PreferencesControllerTests
    {
        [Fact]
        public async Task GetPreferences_WithoutUser_ReturnsUnauthorized()
        {
            var fixture = new Fixture();
            var result = await fixture.Controller.GetPreferences(default);
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetPreferences_WhenMissing_CreatesDefaultsWithClock()
        {
            var fixture = new Fixture("user-1");

            var result = await fixture.Controller.GetPreferences(default);

            var dto = Assert.IsType<UserPreferenceDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.True(dto.NotificationsEnabled);
            Assert.True(dto.SoundEnabled);
            Assert.Equal("light", dto.Theme);
            Assert.Equal(fixture.Now, fixture.Repository.Created!.CreatedAt);
        }

        [Fact]
        public async Task GetPreferences_WhenExisting_MapsWithoutCreating()
        {
            var fixture = new Fixture("user-1");
            fixture.Repository.Current = new UserPreference { Id = "id", UserId = "user-1", Theme = "dark" };

            var result = await fixture.Controller.GetPreferences(default);

            Assert.Equal("dark", Assert.IsType<UserPreferenceDto>(Assert.IsType<OkObjectResult>(result.Result).Value).Theme);
            Assert.Null(fixture.Repository.Created);
        }

        [Fact]
        public async Task UpdatePreferences_WithInvalidModel_ReturnsBadRequest()
        {
            var fixture = new Fixture("user-1");
            fixture.Controller.ModelState.AddModelError("theme", "invalid");

            var result = await fixture.Controller.UpdatePreferences(new UserPreferenceDto(), default);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdatePreferences_WhenMissing_CreatesAndIgnoresDtoUserId()
        {
            var fixture = new Fixture("actual-user");
            var dto = new UserPreferenceDto { UserId = "attacker", Theme = "dark", Language = "tr", MutedChats = new List<string> { "chat-1" } };

            var result = await fixture.Controller.UpdatePreferences(dto, default);

            var saved = fixture.Repository.Created!;
            Assert.Equal("actual-user", saved.UserId);
            Assert.Equal("dark", saved.Theme);
            Assert.Equal(fixture.Now, saved.UpdatedAt);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdatePreferences_WhenExisting_UpdatesRepository()
        {
            var fixture = new Fixture("user-1");
            fixture.Repository.Current = new UserPreference { Id = "id", UserId = "user-1" };

            await fixture.Controller.UpdatePreferences(new UserPreferenceDto { NotificationsEnabled = false, SoundEnabled = false }, default);

            Assert.Same(fixture.Repository.Current, fixture.Repository.Updated);
            Assert.False(fixture.Repository.Updated!.NotificationsEnabled);
        }

        private sealed class Fixture
        {
            public readonly DateTime Now = new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);
            public Fixture(string? userId = null)
            {
                this.Controller = new PreferencesController(this.Repository, new FixedClock(this.Now), new LoggerConfiguration().CreateLogger())
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                };
                if (userId != null) this.Controller.HttpContext.Items["UserId"] = userId;
            }
            public FakeRepository Repository { get; } = new FakeRepository();
            public PreferencesController Controller { get; }
        }

        private sealed class FixedClock : IClock { public FixedClock(DateTime now) => this.UtcNow = now; public DateTime UtcNow { get; } }

        private sealed class FakeRepository : IUserPreferenceRepository
        {
            public UserPreference? Current { get; set; }
            public UserPreference? Created { get; private set; }
            public UserPreference? Updated { get; private set; }
            public Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.Current);
            public Task CreateAsync(UserPreference preference, CancellationToken cancellationToken = default) { this.Created = preference; return Task.CompletedTask; }
            public Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default) { this.Updated = preference; return Task.CompletedTask; }
            public Task DeleteAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
