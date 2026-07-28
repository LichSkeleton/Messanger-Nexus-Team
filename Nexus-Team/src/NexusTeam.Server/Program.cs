namespace NexusTeam.Server
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Extensions;
    using Serilog;

    /// <summary>
    /// Main entry point for the NexusTeam server application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method to configure and run the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code.</returns>
        public static int Main(string[] args)
        {
            try
            {
                // Parse CLI arguments
                if (args.Length != 1)
                {
                    System.Console.WriteLine("Usage: nexusteam_server <port>");
                    System.Console.WriteLine("Example: nexusteam_server 5251");
                    return 1;
                }

                if (!int.TryParse(args[0], out int port))
                {
                    System.Console.WriteLine($"Error: Invalid port number: {args[0]}");
                    System.Console.WriteLine("Usage: nexusteam_server <port>");
                    return 1;
                }

                if (port < 1 || port > 65535)
                {
                    System.Console.WriteLine($"Error: Port must be between 1 and 65535, got {port}");
                    return 1;
                }

                // Check if port is available
                if (!IsPortAvailable(port))
                {
                    System.Console.WriteLine($"Error: Port {port} is already in use");
                    return 1;
                }

                var builder = WebApplication.CreateBuilder(args);

                // Configure Serilog logging
                builder.AddSerilogLogging();

                Log.Information("Starting NexusTeam Server");

                // Register Serilog.ILogger as singleton for dependency injection
                builder.Services.AddSingleton(Log.Logger);

                // Register configuration options
                builder.Services.AddConfigurationOptions(builder.Configuration);

                // Register data access services
                builder.Services.AddDataAccess();
                builder.Services.AddMongoDb();
                builder.Services.AddRedis();

                // Register core business services
                builder.Services.AddCoreServices();

                // Register validators
                builder.Services.AddValidators();

                // Configure CORS
                builder.Services.AddCorsServices(builder.Configuration);

                // Add controllers and API documentation
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "NexusTeam Server API",
                        Version = "v1",
                        Description = "Real-time chat application API with authentication and messaging capabilities",
                        Contact = new OpenApiContact
                        {
                            Name = "NexusTeam Team",
                        },
                    });

                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer",
                                },
                            },
                            Array.Empty<string>()
                        },
                    });

                    // Include XML comments for Swagger documentation
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        options.IncludeXmlComments(xmlPath);
                    }
                });

                // Build the application
                var app = builder.Build();

                // Get CORS configuration
                var corsOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<CorsOptions>>().Value;

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // Correct middleware order:
                app.UseHttpsRedirection();
                app.UseCors(corsOptions.PolicyName);
                app.UseWebSockets();
                app.UseCustomMiddleware();

                // Map controllers
                app.MapControllers();

                // Health check endpoints
                app.MapGet("/", () => new
                {
                    Service = "NexusTeam Server",
                    Status = "Running",
                    Version = "1.0.0",
                });

                app.MapGet("/health", () => new
                {
                    Status = "Healthy",
                    Timestamp = System.DateTime.UtcNow,
                });

                // Configure Kestrel to listen on the specified port
                app.Urls.Clear();
                app.Urls.Add($"http://0.0.0.0:{port}");

                System.Console.WriteLine($"Server started on port {port}");
                System.Console.WriteLine($"Process ID: {Environment.ProcessId}");

                Log.Information("NexusTeam Server started successfully on port {Port}, PID: {ProcessId}", port, Environment.ProcessId);

                // Run the application
                app.Run();

                return 0;
            }
            catch (System.Exception ex)
            {
                Log.Fatal(ex, "NexusTeam Server terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Check if a port is available.
        /// </summary>
        /// <param name="port">Port to check.</param>
        /// <returns>True if port is available, false otherwise.</returns>
        private static bool IsPortAvailable(int port)
        {
            try
            {
                using (var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork,
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Tcp))
                {
                    socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
