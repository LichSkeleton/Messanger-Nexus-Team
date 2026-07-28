namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Service implementation for fetching available users.
    /// </summary>
    public class UserDirectoryService : IUserDirectoryService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDirectoryService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        public UserDirectoryService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = Log.ForContext<UserDirectoryService>();
        }

        /// <inheritdoc/>
        public async Task<List<UserDto>> GetAvailableUsersAsync()
        {
            try
            {
                var response = await this.httpClient.GetAsync("api/users");

                if (!response.IsSuccessStatusCode)
                {
                    this.logger.Warning("GetAvailableUsers failed with status {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch users: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                this.logger.Information("Fetched {UserCount} available users", users?.Count ?? 0);

                return users ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error fetching available users");
                throw;
            }
        }
    }
}
