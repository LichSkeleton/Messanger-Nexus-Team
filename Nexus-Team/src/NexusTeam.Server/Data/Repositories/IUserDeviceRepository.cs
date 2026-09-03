namespace NexusTeam.Server.Data.Repositories
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;

    /// <summary>Persists per-user browser device state.</summary>
    public interface IUserDeviceRepository
    {
        /// <summary>Gets one user/device record.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The device, or null when it is not registered.</returns>
        Task<UserDevice?> GetAsync(string userId, string deviceId, CancellationToken cancellationToken = default);

        /// <summary>Creates or replaces one user/device record.</summary>
        /// <param name="device">Device state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task UpsertAsync(UserDevice device, CancellationToken cancellationToken = default);
    }
}
