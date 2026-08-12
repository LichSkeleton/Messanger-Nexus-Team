namespace NexusTeam.Server.Extensions
{
    using System.Linq;
    using FluentValidation;
    using FluentValidation.AspNetCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Configuration.Validation;
    using NexusTeam.Server.Data;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Data.Repositories.OracleImpl;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Abstractions;
    using CorsOptions = NexusTeam.Server.Configuration.Options.CorsOptions;

    /// <summary>
    /// Extension methods for configuring services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds configuration options with validation to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));
            services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));
            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.Configure<BcryptOptions>(configuration.GetSection(BcryptOptions.SectionName));
            services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
            services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));

            services.AddSingleton<IValidateOptions<OracleOptions>, OracleOptionsValidator>();
            services.AddSingleton<IValidateOptions<MongoOptions>, MongoOptionsValidator>();
            services.AddSingleton<IValidateOptions<RedisOptions>, RedisOptionsValidator>();
            services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
            services.AddSingleton<IValidateOptions<BcryptOptions>, BcryptOptionsValidator>();
            services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();
            services.AddSingleton<IValidateOptions<CorsOptions>, CorsOptionsValidator>();

            return services;
        }

        /// <summary>
        /// Adds data access services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDataAccess(this IServiceCollection services)
        {
            services.AddSingleton<IOracleDataContext, OracleDataContext>();
            services.AddScoped<IUserRepository, OracleUserRepository>();
            services.AddScoped<IChatRepository, Data.Repositories.MongoImpl.MongoChatRepository>();
            services.AddScoped<IChatFolderRepository, Data.Repositories.MongoImpl.MongoChatFolderRepository>();
            services.AddScoped<IMessageRepository, Data.Repositories.MongoImpl.MongoMessageRepository>();
            services.AddScoped<IMessageAttachmentRepository, Data.Repositories.MongoImpl.MongoMessageAttachmentRepository>();
            services.AddScoped<IUserPreferenceRepository, Data.Repositories.MongoImpl.MongoUserPreferenceRepository>();
            services.AddScoped<IGeneratedImageRepository, Data.Repositories.MongoImpl.MongoGeneratedImageRepository>();

            return services;
        }

        /// <summary>
        /// Adds MongoDB services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDb(this IServiceCollection services)
        {
            services.AddSingleton<IMongoClientFactory, MongoClientFactory>();
            return services;
        }

        /// <summary>
        /// Adds Redis services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRedis(this IServiceCollection services)
        {
            services.AddSingleton<IRedisMultiplexer, RedisMultiplexer>();
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddSingleton<IUserStatusService, UserStatusService>();
            return services;
        }

        /// <summary>
        /// Adds core application services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IIdGenerator, UlidGenerator>();
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
            services.AddSingleton<IRateLimitService, RateLimitService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IChatFolderService, ChatFolderService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddSingleton<IAvatarService, AvatarService>();
            services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddScoped<IGeneratedImageService, GeneratedImageService>();
            services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
            services.AddHostedService<PresenceTrackingService>();

            return services;
        }

        /// <summary>
        /// Adds FluentValidation validators to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
            return services;
        }

        /// <summary>
        /// Adds CORS services with custom configuration to avoid conflicts.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
        {
            var corsOptions = new CorsOptions();
            configuration.GetSection(CorsOptions.SectionName).Bind(corsOptions);

            services.AddCors(options =>
            {
                options.AddPolicy(corsOptions.PolicyName, builder =>
                {
                    var policyBuilder = builder
                        .WithOrigins(corsOptions.AllowedOrigins.ToArray())
                        .AllowAnyMethod()
                        .AllowAnyHeader();

                    if (corsOptions.AllowCredentials)
                    {
                        policyBuilder.AllowCredentials();
                    }
                });
            });

            return services;
        }
    }
}
