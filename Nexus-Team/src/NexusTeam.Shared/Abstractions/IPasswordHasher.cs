namespace NexusTeam.Shared.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction for password hashing and verification.
    /// Implementations should be registered as singleton in DI container.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password asynchronously.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>A task that represents the asynchronous operation, containing the hashed password.</returns>
        Task<string> HashPasswordAsync(string password);

        /// <summary>
        /// Verifies a password against a hash asynchronously.
        /// </summary>
        /// <param name="password">The plain text password to verify.</param>
        /// <param name="hash">The hash to verify against.</param>
        /// <returns>A task that represents the asynchronous operation, containing true if the password matches, false otherwise.</returns>
        Task<bool> VerifyPasswordAsync(string password, string hash);
    }
}
