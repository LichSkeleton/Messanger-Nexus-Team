namespace NexusTeam.Shared.Helpers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Configuration;

    /// <summary>
    /// Implementation of password hashing using BCrypt.
    /// Provides secure password hashing with configurable work factor.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHashingOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHasher"/> class.
        /// </summary>
        /// <param name="options">The password hashing configuration options.</param>
        public PasswordHasher(IOptions<PasswordHashingOptions> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (this.options.WorkFactor < 10)
            {
                throw new ArgumentException("Work factor must be at least 10 for security.", nameof(options));
            }
        }

        /// <inheritdoc/>
        public Task<string> HashPasswordAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or whitespace.", nameof(password));
            }

            return Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, this.options.WorkFactor));
        }

        /// <inheritdoc/>
        public Task<bool> VerifyPasswordAsync(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or whitespace.", nameof(password));
            }

            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentException("Hash cannot be null or whitespace.", nameof(hash));
            }

            return Task.Run(() =>
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, hash);
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
