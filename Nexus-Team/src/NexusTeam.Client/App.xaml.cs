namespace NexusTeam.Client
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NexusTeam.Client.Infrastructure;
    using NexusTeam.Client.Models;
    using NexusTeam.Client.Services;
    using NexusTeam.Client.ViewModels;
    using NexusTeam.Client.Views;
    using Serilog;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private IHost? host;

        /// <summary>
        /// Gets the service provider for resolving services.
        /// </summary>
        public IServiceProvider? Services => this.host?.Services;

        /// <inheritdoc/>
        protected override async void OnStartup(StartupEventArgs e)
        {
            this.ConfigureLogging();
            this.ConfigureUnhandledExceptionHandling();

            try
            {
                // Parse CLI arguments
                var config = this.ParseCommandLineArguments(e.Args);
                if (config == null)
                {
                    return;
                }

                // Initialize host with configuration
                this.host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton(config);
                        services.AddApplicationServices(config);
                        services.AddViewModels();
                        services.AddViews();
                    })
                    .Build();

                await this.host.StartAsync();

                var mainWindow = this.Services!.GetRequiredService<MainWindow>();
                this.MainWindow = mainWindow;

                var navigationService = this.Services!.GetRequiredService<INavigationService>();
                var authService = this.Services!.GetRequiredService<IAuthenticationService>();

                // Setup call window management
                var callViewModel = this.Services!.GetRequiredService<CallViewModel>();
                CallWindow? callWindow = null;

                callViewModel.CallStateChanged += (sender, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Log.Information("Call state changed event received: State={State}, CallId={CallId}", e.State, e.CallId);

                        // Show window when call starts
                        if (e.State != NexusTeam.Client.Services.CallState.Idle && e.State != NexusTeam.Client.Services.CallState.Ending && callWindow == null)
                        {
                            Log.Information("Creating and showing call window");
                            try
                            {
                                callWindow = this.Services!.GetRequiredService<CallWindow>();

                                // Set owner to main window so it opens within the client window
                                callWindow.Owner = mainWindow;

                                // Position window relative to main window (like image viewer)
                                // Use Loaded event to ensure main window dimensions are available
                                callWindow.Loaded += (s, args) =>
                                {
                                    if (mainWindow.WindowState == WindowState.Normal)
                                    {
                                        var mainWidth = mainWindow.ActualWidth > 0 ? mainWindow.ActualWidth : mainWindow.Width;
                                        var mainHeight = mainWindow.ActualHeight > 0 ? mainWindow.ActualHeight : mainWindow.Height;
                                        callWindow.Left = mainWindow.Left + ((mainWidth - callWindow.Width) / 2);
                                        callWindow.Top = mainWindow.Top + ((mainHeight - callWindow.Height) / 2);
                                    }
                                    else
                                    {
                                        // If main window is maximized, center on screen
                                        callWindow.Left = (SystemParameters.PrimaryScreenWidth - callWindow.Width) / 2;
                                        callWindow.Top = (SystemParameters.PrimaryScreenHeight - callWindow.Height) / 2;
                                    }
                                };

                                // Handle window closed event to reset variable
                                callWindow.Closed += (s, args) =>
                                {
                                    if (callWindow == s)
                                    {
                                        callWindow = null;
                                        Log.Information("Call window closed event received");
                                    }
                                };

                                callWindow.Show();
                                callWindow.Activate(); // Bring to front
                                Log.Information("Call window shown successfully with Owner set to main window");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to show call window");
                            }
                        }

                        // Close window when call ends
                        if ((e.State == NexusTeam.Client.Services.CallState.Idle || e.State == NexusTeam.Client.Services.CallState.Ending) && callWindow != null)
                        {
                            Log.Information("Closing call window: State={State}, CallId={CallId}", e.State, e.CallId);
                            try
                            {
                                var windowToClose = callWindow;
                                callWindow = null; // Set to null before closing to prevent race conditions
                                windowToClose.Close();
                                Log.Information("Call window closed successfully");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to close call window");
                                callWindow = null; // Ensure it's null even if close fails
                            }
                        }
                    });
                };

                // Try to restore session if user had "Remember Me" enabled
                Log.Information("Attempting to restore user session...");
                var sessionRestored = await authService.TryRestoreSessionAsync();

                if (sessionRestored)
                {
                    Log.Information("Session restored successfully");
                    navigationService.NavigateTo<ChatViewModel>();
                }
                else
                {
                    Log.Information("No session to restore, navigating to welcome screen");
                    navigationService.NavigateTo<WelcomeViewModel>();
                }

                mainWindow.Show();

                Log.Information("Application started with server configuration: {IP}:{Port}", config.IpAddress, config.Port);

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error during application startup");
                MessageBox.Show(
                    $"Failed to start application: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Shutdown(1);
            }
        }

        /// <inheritdoc/>
        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down");

            if (this.host != null)
            {
                try
                {
                    // Dispose messaging service first to cancel background tasks
                    var messagingService = this.Services?.GetService<IMessagingService>() as IDisposable;
                    if (messagingService != null)
                    {
                        Log.Information("Disconnecting messaging service");
                        if (messagingService is IMessagingService ms)
                        {
                            await ms.DisconnectAsync();
                        }

                        messagingService.Dispose();
                    }

                    Log.Information("Stopping host");
                    await this.host.StopAsync(TimeSpan.FromSeconds(5));
                    this.host.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during application shutdown");
                }
            }

            Log.CloseAndFlush();

            base.OnExit(e);
        }

        /// <summary>
        /// Parse and validate command-line arguments.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Server configuration if valid, null otherwise.</returns>
        private ServerConfiguration? ParseCommandLineArguments(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                this.ShowUsageAndExit("Missing or incorrect number of arguments");
                return null;
            }

            var ipAddress = args[0];
            var portStr = args[1];

            // Validate IP address
            if (!IPAddress.TryParse(ipAddress, out _) && ipAddress != "localhost")
            {
                this.ShowUsageAndExit($"Invalid IP address: {ipAddress}");
                return null;
            }

            // Validate port
            if (!int.TryParse(portStr, out int port))
            {
                this.ShowUsageAndExit($"Invalid port: {portStr}");
                return null;
            }

            if (port < 1 || port > 65535)
            {
                this.ShowUsageAndExit($"Port must be between 1 and 65535, got {port}");
                return null;
            }

            var config = new ServerConfiguration
            {
                IpAddress = ipAddress,
                Port = port,
            };

            var validationError = config.Validate();
            if (validationError != null)
            {
                this.ShowUsageAndExit(validationError);
                return null;
            }

            Log.Information("Server configuration parsed: IP={IP}, Port={Port}", ipAddress, port);
            return config;
        }

        /// <summary>
        /// Show usage information and exit application.
        /// </summary>
        /// <param name="errorMessage">Optional error message to display.</param>
        private void ShowUsageAndExit(string? errorMessage = null)
        {
            const string usage = @"Nexus Team - Real-time chat application

Usage: nexus-team <server_ip> <port>

Arguments:
  server_ip  - Server IP address or hostname (e.g., 127.0.0.1, localhost, 192.168.1.100)
  port       - Server port number (1-65535) (e.g., 5251, 8080)

Examples:
  nexus-team 127.0.0.1 5251
  nexus-team localhost 8080
  nexus-team 192.168.1.100 9000";

            var message = usage;
            if (errorMessage != null)
            {
                message = $"Error: {errorMessage}\n\n{usage}";
            }

            MessageBox.Show(message, "Nexus Team", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Shutdown(errorMessage == null ? 0 : 1);
        }

        private void ConfigureLogging()
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Nexus Team",
                "Logs");

            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "nexusteam-client-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Logging configured");
        }

        private void ConfigureUnhandledExceptionHandling()
        {
            // Handle unhandled exceptions in UI thread
            this.DispatcherUnhandledException += (sender, e) =>
            {
                Log.Fatal(e.Exception, "Unhandled exception in UI thread");
                e.Handled = true; // Prevent application from crashing

                MessageBox.Show(
                    $"An unexpected error occurred: {e.Exception.Message}\n\nThe application will continue running.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };

            // Handle unhandled exceptions in background threads
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Unhandled exception in background thread");
                }
                else
                {
                    Log.Fatal("Unhandled exception (non-Exception type): {Exception}", e.ExceptionObject);
                }

                // Note: Can't prevent shutdown for AppDomain.UnhandledException
                // but we can log it before the application terminates
            };

            // Handle unhandled exceptions in tasks
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Log.Error(e.Exception, "Unobserved task exception");
                e.SetObserved(); // Mark exception as observed to prevent app crash
            };

            Log.Information("Unhandled exception handling configured");
        }
    }
}
