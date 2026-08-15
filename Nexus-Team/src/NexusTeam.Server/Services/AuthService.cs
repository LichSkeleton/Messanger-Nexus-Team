namespace NexusTeam.Server.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Oracle.ManagedDataAccess.Client;
    using Serilog;

    /// <summary>
    /// Service for user authentication and registration with transactional logic.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJwtTokenService jwtTokenService;
        private readonly IRefreshTokenService refreshTokenService;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly IUserStatusService userStatusService;
        private readonly IAvatarService avatarService;
        private readonly IUserDeviceService? userDeviceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        /// <param name="userRepository">User repository.</param>
        /// <param name="passwordHasher">Password hasher.</param>
        /// <param name="jwtTokenService">JWT token service.</param>
        /// <param name="refreshTokenService">Refresh token service.</param>
        /// <param name="idGenerator">ID generator.</param>
        /// <param name="clock">Clock for timestamps.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="userStatusService">User status service.</param>
        /// <param name="avatarService">Avatar service.</param>
        /// <param name="userDeviceService">User device registration service.</param>
        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            IIdGenerator idGenerator,
            IClock clock,
            ILogger logger,
            IUserStatusService userStatusService,
            IAvatarService avatarService,
            IUserDeviceService? userDeviceService = null)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jwtTokenService = jwtTokenService;
            this.refreshTokenService = refreshTokenService;
            this.idGenerator = idGenerator;
            this.clock = clock;
            this.logger = logger;
            this.userStatusService = userStatusService;
            this.avatarService = avatarService;
            this.userDeviceService = userDeviceService;
        }

        /// <inheritdoc/>
        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingUserByUsername = await this.userRepository.GetByUsernameAsync(request.Username, cancellationToken);
                if (existingUserByUsername != null)
                {
                    this.logger.Warning("Registration failed: username {Username} already exists", request.Username);
                    return new RegisterResponse
                    {
                        Success = false,
                        ErrorMessage = "Username already exists",
                    };
                }

                var existingUserByEmail = await this.userRepository.GetByEmailAsync(request.Email, cancellationToken);
                if (existingUserByEmail != null)
                {
                    this.logger.Warning("Registration failed: email {Email} already exists", request.Email);
                    return new RegisterResponse
                    {
                        Success = false,
                        ErrorMessage = "Email already exists",
                    };
                }

                var passwordHash = await this.passwordHasher.HashPasswordAsync(request.Password);
                var now = this.clock.UtcNow;

                // Set default avatar URL for new users
                var defaultAvatarUrl = "/api/users/avatar/default";

                var user = new User
                {
                    Id = this.idGenerator.GenerateId(),
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    DisplayName = request.DisplayName,
                    AvatarUrl = defaultAvatarUrl,
                    Status = UserStatus.Offline,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                await this.userRepository.CreateAsync(user, cancellationToken);

                this.logger.Information("User registered successfully: {UserId} - {Username}", user.Id, user.Username);

                return new RegisterResponse
                {
                    Success = true,
                    User = await this.MapToUserDtoAsync(user, cancellationToken),
                };
            }
            catch (OracleException ex) when (ex.Number == 1)
            {
                this.logger.Warning(ex, "Registration failed: unique constraint violation");
                return new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Username or email already exists",
                };
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Registration failed with unexpected error");
                return new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Registration failed due to an unexpected error",
                };
            }
        }

        /// <inheritdoc/>
        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = null;

                if (request.UsernameOrEmail.Contains("@", StringComparison.Ordinal))
                {
                    user = await this.userRepository.GetByEmailAsync(request.UsernameOrEmail, cancellationToken);
                }
                else
                {
                    user = await this.userRepository.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken);
                }

                if (user == null)
                {
                    this.logger.Warning("Login failed: user not found for {UsernameOrEmail}", request.UsernameOrEmail);
                    throw new AuthenticationException("Invalid username/email or password");
                }

                var isPasswordValid = await this.passwordHasher.VerifyPasswordAsync(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    this.logger.Warning("Login failed: invalid password for user {UserId}", user.Id);
                    throw new AuthenticationException("Invalid username/email or password");
                }

                if (this.userDeviceService != null)
                {
                    await this.userDeviceService.RegisterLoginAsync(user.Id, request.DeviceId, request.DeviceName, cancellationToken);
                }

                var accessToken = await this.jwtTokenService.GenerateAccessTokenAsync(user, request.DeviceId);
                var refreshToken = await this.refreshTokenService.GenerateRefreshTokenAsync(user.Id, request.DeviceId, cancellationToken);

                this.logger.Information("User logged in successfully: {UserId} - {Username}", user.Id, user.Username);

                return new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600,
                    User = await this.MapToUserDtoAsync(user, cancellationToken),
                };
            }
            catch (AuthenticationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Login failed with unexpected error");
                throw new AuthenticationException("Login failed due to an unexpected error", ex);
            }
        }

        private async Task<UserDto> MapToUserDtoAsync(User user, CancellationToken cancellationToken = default)
        {
            // Get status from Redis instead of Oracle
            var status = await this.userStatusService.GetStatusAsync(user.Id, cancellationToken);

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = status,
                LastSeenAt = user.LastSeenAt,
            };
        }
    }
}
