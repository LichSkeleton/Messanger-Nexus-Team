namespace NexusTeam.Client.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Implementation of credential storage using DPAPI encryption.
    /// </summary>
    public class CredentialStorageService : ICredentialStorageService
    {
        private readonly ILogger logger;
        private readonly string credentialsPath;
        private readonly string tokensPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialStorageService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public CredentialStorageService(ILogger logger)
        {
            this.logger = logger;

            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NexusTeam");

            Directory.CreateDirectory(appDataPath);

            this.credentialsPath = Path.Combine(appDataPath, ".credentials");
            this.tokensPath = Path.Combine(appDataPath, ".tokens");
        }

        /// <inheritdoc/>
        public void SaveCredentials(string usernameOrEmail, string password)
        {
            try
            {
                var data = new { UsernameOrEmail = usernameOrEmail, Password = password };
                var json = JsonSerializer.Serialize(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                var encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(this.credentialsPath, encryptedBytes);
                this.logger.Information("Credentials saved securely");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save credentials");
                throw;
            }
        }

        /// <inheritdoc/>
        public (string UsernameOrEmail, string Password)? GetSavedCredentials()
        {
            try
            {
                if (!File.Exists(this.credentialsPath))
                {
                    return null;
                }

                var encryptedBytes = File.ReadAllBytes(this.credentialsPath);
                var bytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(bytes);
                var data = JsonSerializer.Deserialize<CredentialData>(json);

                if (data == null || string.IsNullOrEmpty(data.UsernameOrEmail) || string.IsNullOrEmpty(data.Password))
                {
                    return null;
                }

                this.logger.Debug("Retrieved saved credentials");
                return (data.UsernameOrEmail, data.Password);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to retrieve credentials");
                return null;
            }
        }

        /// <inheritdoc/>
        public void SaveTokens(string accessToken, string? refreshToken, UserDto user)
        {
            try
            {
                var data = new TokenData
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = user,
                };

                var json = JsonSerializer.Serialize(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                var encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(this.tokensPath, encryptedBytes);
                this.logger.Information("Tokens saved securely");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save tokens");
                throw;
            }
        }

        /// <inheritdoc/>
        public (string AccessToken, string? RefreshToken, UserDto User)? GetSavedTokens()
        {
            try
            {
                if (!File.Exists(this.tokensPath))
                {
                    return null;
                }

                var encryptedBytes = File.ReadAllBytes(this.tokensPath);
                var bytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(bytes);
                var data = JsonSerializer.Deserialize<TokenData>(json);

                if (data == null || string.IsNullOrEmpty(data.AccessToken) || data.User == null)
                {
                    return null;
                }

                this.logger.Debug("Retrieved saved tokens");
                return (data.AccessToken, data.RefreshToken, data.User);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to retrieve tokens");
                return null;
            }
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            try
            {
                if (File.Exists(this.credentialsPath))
                {
                    File.Delete(this.credentialsPath);
                }

                if (File.Exists(this.tokensPath))
                {
                    File.Delete(this.tokensPath);
                }

                this.logger.Information("All stored credentials and tokens cleared");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to clear stored data");
            }
        }

        /// <inheritdoc/>
        public void ClearCredentials()
        {
            try
            {
                if (File.Exists(this.credentialsPath))
                {
                    File.Delete(this.credentialsPath);
                    this.logger.Information("Stored credentials cleared");
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to clear credentials");
            }
        }

        /// <inheritdoc/>
        public bool HasSavedCredentials()
        {
            return File.Exists(this.credentialsPath);
        }

        private class CredentialData
        {
            public string UsernameOrEmail { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;
        }

        private class TokenData
        {
            public string AccessToken { get; set; } = string.Empty;

            public string? RefreshToken { get; set; }

            public UserDto User { get; set; } = new UserDto();
        }
    }
}
