namespace NexusTeam.Client.Infrastructure
{
    using System;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;
    using NexusTeam.Client.Models;
    using NexusTeam.Client.Services;
    using NexusTeam.Client.ViewModels;
    using NexusTeam.Client.Views;
    using Serilog;

    /// <summary>
    /// Configures dependency injection for the application.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds application services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="serverConfig">Server configuration from CLI arguments.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, ServerConfiguration serverConfig)
        {
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ILogger>(sp => Log.Logger);
            services.AddSingleton<ICredentialStorageService, CredentialStorageService>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();

            // Register AuthenticationService as singleton with named HttpClient
            services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri(serverConfig.HttpBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddSingleton<IAuthenticationService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("AuthClient");
                var credStore = sp.GetRequiredService<ICredentialStorageService>();
                var logger = sp.GetRequiredService<ILogger>();
                return new AuthenticationService(httpClient, credStore, logger);
            });

            services.AddTransient<AuthenticationMessageHandler>();

            // OfflineMessageQueue requires IAuthenticationService for per-user isolation
            services.AddSingleton<IOfflineMessageQueue>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger>();
                var authService = sp.GetRequiredService<IAuthenticationService>();
                return new OfflineMessageQueue(logger, authService);
            });

            // MessagingService as Singleton with dedicated HttpClient
            services.AddHttpClient("MessagingClient", client =>
            {
                client.BaseAddress = new Uri(serverConfig.HttpBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticationMessageHandler>();

            services.AddSingleton<IMessagingService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("MessagingClient");
                var logger = sp.GetRequiredService<ILogger>();
                var offlineQueue = sp.GetRequiredService<IOfflineMessageQueue>();
                var errorHandler = sp.GetRequiredService<IErrorHandlingService>();
                var authService = sp.GetRequiredService<IAuthenticationService>();
                var config = sp.GetRequiredService<ServerConfiguration>();
                return new MessagingService(httpClient, logger, offlineQueue, errorHandler, authService, config);
            });

            // UserDirectoryService with handler
            services.AddHttpClient<IUserDirectoryService, UserDirectoryService>(client =>
            {
                client.BaseAddress = new Uri(serverConfig.HttpBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticationMessageHandler>();

            // FileAttachmentService with authenticated HttpClient
            services.AddHttpClient("FileAttachmentClient", client =>
            {
                client.BaseAddress = new Uri(serverConfig.HttpBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for file uploads
            })
            .AddHttpMessageHandler<AuthenticationMessageHandler>();

            services.AddSingleton<IFileAttachmentService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("FileAttachmentClient");
                var logger = sp.GetRequiredService<ILogger>();
                var errorHandler = sp.GetRequiredService<IErrorHandlingService>();
                return new FileAttachmentService(httpClient, logger, errorHandler);
            });

            services.AddSingleton<IImageCompressionService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger>();
                return new ImageCompressionService(logger);
            });

            services.AddSingleton<IDocumentPreviewService, DocumentPreviewService>();
            services.AddSingleton<IAttachmentPreviewService, AttachmentPreviewService>();

            // AvatarService with authenticated HttpClient
            services.AddHttpClient("AvatarClient", client =>
            {
                client.BaseAddress = new Uri(serverConfig.HttpBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(2); // Timeout for avatar uploads
            })
            .AddHttpMessageHandler<AuthenticationMessageHandler>();

            services.AddSingleton<IAvatarService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("AvatarClient");
                var compressionService = sp.GetRequiredService<IImageCompressionService>();
                var logger = sp.GetRequiredService<ILogger>();
                return new AvatarService(httpClient, compressionService, logger);
            });

            // ImageGeneratorService with dedicated HttpClient for Pollinations API
            services.AddHttpClient("PollinationsClient", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2); // 2 minutes for image generation
            });

            services.AddHttpClient("GeneratorApiClient", client =>
            {
                client.BaseAddress = new Uri(serverConfig.HttpBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(2);
            })
            .AddHttpMessageHandler<AuthenticationMessageHandler>();

            services.AddSingleton<IImageGeneratorService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var pollinationsClient = factory.CreateClient("PollinationsClient");
                var apiClient = factory.CreateClient("GeneratorApiClient");
                var logger = sp.GetRequiredService<ILogger>();
                var errorHandler = sp.GetRequiredService<IErrorHandlingService>();
                return new ImageGeneratorService(pollinationsClient, apiClient, logger, errorHandler);
            });

            // TranslationService with dedicated HttpClient for DeepL API
            services.AddHttpClient("TranslationClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddSingleton<ITranslationService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("TranslationClient");
                var logger = sp.GetRequiredService<ILogger>();
                return new TranslationService(httpClient, logger);
            });

            // CallService for voice calls via WebRTC
            services.AddSingleton<ICallService>(sp =>
            {
                var messagingService = sp.GetRequiredService<IMessagingService>();
                var authService = sp.GetRequiredService<IAuthenticationService>();
                var userDirectoryService = sp.GetRequiredService<IUserDirectoryService>();
                var logger = sp.GetRequiredService<ILogger>();
                var config = sp.GetRequiredService<ServerConfiguration>();
                return new CallService(messagingService, authService, userDirectoryService, logger, config);
            });

            // VoiceMessageService for recording voice messages
            services.AddSingleton<IVoiceMessageService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger>();
                return new VoiceMessageService(logger);
            });

            return services;
        }

        /// <summary>
        /// Adds view models to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<WelcomeViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddSingleton<CallViewModel>(sp =>
            {
                var callService = sp.GetRequiredService<ICallService>();
                var userDirectoryService = sp.GetRequiredService<IUserDirectoryService>();
                var avatarService = sp.GetRequiredService<IAvatarService>();
                var logger = sp.GetRequiredService<ILogger>();
                return new CallViewModel(callService, userDirectoryService, avatarService, logger);
            });
            services.AddTransient<ChatViewModel>();
            services.AddTransient<GeneratorViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<FilesListViewModel>();
            services.AddTransient<ImagesGridViewModel>();

            return services;
        }

        /// <summary>
        /// Adds views to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddViews(this IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddTransient<WelcomeView>();
            services.AddTransient<LoginView>();
            services.AddTransient<RegisterView>();
            services.AddTransient<ChatView>();
            services.AddTransient<GeneratorView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<CallWindow>();

            return services;
        }
    }
}
