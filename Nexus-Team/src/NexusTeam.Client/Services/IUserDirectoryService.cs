namespace NexusTeam.Client.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for fetching available users for chat selection.
    /// </summary>
    public interface IUserDirectoryService
    {
        /// <summary>
        /// Fetches all available users excluding the current user.
        /// </summary>
        /// <returns>List of UserDto objects.</returns>
        /// <exception cref="System.Net.Http.HttpRequestException">Thrown when API call fails.</exception>
        Task<List<UserDto>> GetAvailableUsersAsync();
    }
}
