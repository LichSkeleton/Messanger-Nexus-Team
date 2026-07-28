namespace NexusTeam.Server.Data.Repositories.OracleImpl
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;
    using Oracle.ManagedDataAccess.Client;

    /// <summary>
    /// Oracle implementation of the user repository.
    /// </summary>
    public class OracleUserRepository : IUserRepository
    {
        private readonly IOracleDataContext dataContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleUserRepository"/> class.
        /// </summary>
        /// <param name="dataContext">The Oracle data context.</param>
        public OracleUserRepository(IOracleDataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, username, email, password_hash, display_name, avatar_url, created_at, updated_at, last_seen_at FROM users WHERE id = :id";

            var parameter = new OracleParameter("id", OracleDbType.Varchar2) { Value = id };
            command.Parameters.Add(parameter);

            using var reader = await ((OracleCommand)command).ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return this.MapUser(reader);
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, username, email, password_hash, display_name, avatar_url, created_at, updated_at, last_seen_at FROM users WHERE username = :username";

            var parameter = new OracleParameter("username", OracleDbType.Varchar2) { Value = username };
            command.Parameters.Add(parameter);

            using var reader = await ((OracleCommand)command).ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return this.MapUser(reader);
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, username, email, password_hash, display_name, avatar_url, created_at, updated_at, last_seen_at FROM users WHERE email = :email";

            var parameter = new OracleParameter("email", OracleDbType.Varchar2) { Value = email };
            command.Parameters.Add(parameter);

            using var reader = await ((OracleCommand)command).ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return this.MapUser(reader);
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var users = new List<User>();
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, username, email, password_hash, display_name, avatar_url, created_at, updated_at, last_seen_at FROM users ORDER BY created_at DESC";

            using var reader = await ((OracleCommand)command).ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(this.MapUser(reader));
            }

            return users;
        }

        /// <inheritdoc/>
        public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO users (id, username, email, password_hash, display_name, avatar_url, status, created_at, updated_at, last_seen_at)
                            VALUES (:id, :username, :email, :password_hash, :display_name, :avatar_url, 0, :created_at, :updated_at, :last_seen_at)";

            command.Parameters.Add(new OracleParameter("id", OracleDbType.Varchar2) { Value = user.Id });
            command.Parameters.Add(new OracleParameter("username", OracleDbType.Varchar2) { Value = user.Username });
            command.Parameters.Add(new OracleParameter("email", OracleDbType.Varchar2) { Value = user.Email });
            command.Parameters.Add(new OracleParameter("password_hash", OracleDbType.Varchar2) { Value = user.PasswordHash });
            command.Parameters.Add(new OracleParameter("display_name", OracleDbType.Varchar2) { Value = user.DisplayName });
            command.Parameters.Add(new OracleParameter("avatar_url", OracleDbType.Varchar2) { Value = (object?)user.AvatarUrl ?? DBNull.Value });

            // Status is now stored in Redis, not Oracle - always insert 0 (Offline) as default
            command.Parameters.Add(new OracleParameter("created_at", OracleDbType.TimeStamp) { Value = user.CreatedAt });
            command.Parameters.Add(new OracleParameter("updated_at", OracleDbType.TimeStamp) { Value = user.UpdatedAt });
            command.Parameters.Add(new OracleParameter("last_seen_at", OracleDbType.TimeStamp) { Value = (object?)user.LastSeenAt ?? DBNull.Value });

            await ((OracleCommand)command).ExecuteNonQueryAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = @"UPDATE users SET username = :username, email = :email, password_hash = :password_hash,
                                    display_name = :display_name, avatar_url = :avatar_url,
                                    updated_at = :updated_at, last_seen_at = :last_seen_at WHERE id = :id";

            command.Parameters.Add(new OracleParameter("username", OracleDbType.Varchar2) { Value = user.Username });
            command.Parameters.Add(new OracleParameter("email", OracleDbType.Varchar2) { Value = user.Email });
            command.Parameters.Add(new OracleParameter("password_hash", OracleDbType.Varchar2) { Value = user.PasswordHash });
            command.Parameters.Add(new OracleParameter("display_name", OracleDbType.Varchar2) { Value = user.DisplayName });
            command.Parameters.Add(new OracleParameter("avatar_url", OracleDbType.Varchar2) { Value = (object?)user.AvatarUrl ?? DBNull.Value });

            // Status is now stored in Redis, not Oracle - don't update it
            command.Parameters.Add(new OracleParameter("updated_at", OracleDbType.TimeStamp) { Value = user.UpdatedAt });
            command.Parameters.Add(new OracleParameter("last_seen_at", OracleDbType.TimeStamp) { Value = (object?)user.LastSeenAt ?? DBNull.Value });
            command.Parameters.Add(new OracleParameter("id", OracleDbType.Varchar2) { Value = user.Id });

            await ((OracleCommand)command).ExecuteNonQueryAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            using var connection = await this.dataContext.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM users WHERE id = :id";

            var parameter = new OracleParameter("id", OracleDbType.Varchar2) { Value = id };
            command.Parameters.Add(parameter);

            await ((OracleCommand)command).ExecuteNonQueryAsync(cancellationToken);
        }

        private User MapUser(IDataReader reader)
        {
            return new User
            {
                Id = reader.GetString(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                DisplayName = reader.GetString(4),
                AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5),

                // Status is now stored in Redis, not Oracle - default to Offline
                Status = NexusTeam.Shared.Enums.UserStatus.Offline,

                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.GetDateTime(7),
                LastSeenAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            };
        }
    }
}
