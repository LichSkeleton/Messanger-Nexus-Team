namespace NexusTeam.Server.Data
{
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using Oracle.ManagedDataAccess.Client;

    /// <summary>
    /// Implementation of Oracle database context using OracleConnection.
    /// </summary>
    public class OracleDataContext : IOracleDataContext
    {
        private readonly OracleOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleDataContext"/> class.
        /// </summary>
        /// <param name="options">Oracle configuration options.</param>
        public OracleDataContext(IOptions<OracleOptions> options)
        {
            this.options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new OracleConnection(this.options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <inheritdoc/>
        public IDbTransaction BeginTransaction(IDbConnection connection)
        {
            return connection.BeginTransaction();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
