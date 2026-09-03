namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;
    using Serilog;
    using Xunit;

    public class AuthControllerTests
    {
        [Fact]
        public async Task Register_WithInvalidModel_ReturnsBadRequestWithoutServiceCall()
        {
            var fixture = new Fixture();
            fixture.Controller.ModelState.AddModelError("username", "required");
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.Register(new RegisterRequest(), default)).Result);
            Assert.False(fixture.Auth.RegisterCalled);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task Register_MapsServiceSuccessToOkAndFailureToBadRequest(bool success, bool expectOk)
        {
            var fixture = new Fixture();
            fixture.Auth.RegisterResponse = new RegisterResponse { Success = success };
            var action = (await fixture.Controller.Register(new RegisterRequest(), default)).Result;
            Assert.Equal(expectOk, action is OkObjectResult);
            Assert.Equal(!expectOk, action is BadRequestObjectResult);
        }

        [Fact]
        public async Task Login_WhenRateLimited_Returns429WithoutAuthentication()
        {
            var fixture = new Fixture();
            fixture.Rate.Allowed = false;
            fixture.Rate.ResetSeconds = 42;

            var result = Assert.IsType<ObjectResult>((await fixture.Controller.Login(new LoginRequest { UsernameOrEmail = "user" }, default)).Result);

            Assert.Equal(429, result.StatusCode);
            Assert.False(fixture.Auth.LoginCalled);
        }

        [Fact]
        public async Task Login_WhenAuthenticationFails_ReturnsUnauthorized()
        {
            var fixture = new Fixture();
            fixture.Auth.LoginError = new AuthenticationException("bad credentials");
            Assert.IsType<UnauthorizedObjectResult>((await fixture.Controller.Login(new LoginRequest { UsernameOrEmail = "user" }, default)).Result);
        }

        [Fact]
        public async Task Login_WhenAllowed_ReturnsServiceResponse()
        {
            var fixture = new Fixture();
            fixture.Auth.LoginResponse = new LoginResponse { AccessToken = "token" };
            var result = Assert.IsType<OkObjectResult>((await fixture.Controller.Login(new LoginRequest { UsernameOrEmail = "user" }, default)).Result);
            Assert.Same(fixture.Auth.LoginResponse, result.Value);
        }

        [Fact]
        public async Task Logout_WithoutUser_ReturnsUnauthorized()
        {
            Assert.IsType<UnauthorizedResult>(await new Fixture().Controller.Logout(default));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Logout_WithUser_ReturnsOkEvenWhenSessionRemovalFails(bool fails)
        {
            var fixture = new Fixture("user-1");
            fixture.Session.FailRemoval = fails;
            Assert.IsType<OkObjectResult>(await fixture.Controller.Logout(default));
            Assert.Equal("user-1", fixture.Session.RemovedUser);
        }

        private sealed class Fixture
        {
            public Fixture(string? userId = null)
            {
                this.Controller = new AuthController(this.Auth, this.Rate, this.Session, new LoggerConfiguration().CreateLogger())
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                };
                if (userId != null) this.Controller.HttpContext.Items["UserId"] = userId;
            }
            public FakeAuth Auth { get; } = new FakeAuth();
            public FakeRate Rate { get; } = new FakeRate();
            public FakeSession Session { get; } = new FakeSession();
            public AuthController Controller { get; }
        }

        private sealed class FakeAuth : IAuthService
        {
            public RegisterResponse RegisterResponse { get; set; } = new RegisterResponse { Success = true };
            public LoginResponse LoginResponse { get; set; } = new LoginResponse();
            public AuthenticationException? LoginError { get; set; }
            public bool RegisterCalled { get; private set; }
            public bool LoginCalled { get; private set; }
            public Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) { this.RegisterCalled = true; return Task.FromResult(this.RegisterResponse); }
            public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) { this.LoginCalled = true; return this.LoginError == null ? Task.FromResult(this.LoginResponse) : Task.FromException<LoginResponse>(this.LoginError); }
        }

        private sealed class FakeRate : IRateLimitService
        {
            public bool Allowed { get; set; } = true;
            public int ResetSeconds { get; set; }
            public Task<bool> IsLoginAllowedAsync(string identifier, CancellationToken cancellationToken = default) => Task.FromResult(this.Allowed);
            public Task<bool> IsMessageSendAllowedAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<int> GetLoginRateLimitResetTimeAsync(string identifier, CancellationToken cancellationToken = default) => Task.FromResult(this.ResetSeconds);
            public Task<int> GetMessageRateLimitResetTimeAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        }

        private sealed class FakeSession : ISessionService
        {
            public bool FailRemoval { get; set; }
            public string? RemovedUser { get; private set; }
            public Task<string> CreateSessionAsync(string userId, string connectionId, CancellationToken cancellationToken = default) => Task.FromResult("token");
            public Task UpdateHeartbeatAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveSessionAsync(string userId, CancellationToken cancellationToken = default) { this.RemovedUser = userId; return this.FailRemoval ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask; }
            public Task QueueMessageAsync(string userId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<IEnumerable<string>> GetQueuedMessagesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            public Task ClearMessageQueueAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        }
    }
}
