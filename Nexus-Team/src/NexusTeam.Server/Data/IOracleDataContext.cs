namespace NexusTeam.Server.Data
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction for Oracle database connection and transaction management.
    /// </summary>
    public interface IOracleDataContext : IDisposable
    {
        /// <summary>
        /// Creates and opens a new database connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An open database connection.</returns>
        Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>A database transaction.</returns>
        IDbTransaction BeginTransaction(IDbConnection connection);
    }
}
