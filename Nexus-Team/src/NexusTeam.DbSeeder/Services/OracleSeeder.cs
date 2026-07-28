using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Text;

namespace NexusTeam.DbSeeder.Services;

/// <summary>
/// Service for seeding Oracle database with tables and schemas.
/// </summary>
public class OracleSeeder
{
    private readonly string connectionString;
    private readonly ILogger<OracleSeeder> logger;

    public OracleSeeder(string connectionString, ILogger<OracleSeeder> logger)
    {
        this.connectionString = connectionString;
        this.logger = logger;
    }

    /// <summary>
    /// Seeds Oracle database by executing SQL scripts from configuration directory.
    /// </summary>
    public async Task SeedAsync(string configPath, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Starting Oracle seeding process...");
        this.logger.LogInformation("Configuration path: {ConfigPath}", configPath);

        var oracleConfigPath = Path.Combine(configPath, "oracle");
        if (!Directory.Exists(oracleConfigPath))
        {
            this.logger.LogWarning("Oracle config directory not found: {Path}", oracleConfigPath);
            return;
        }

        var sqlFiles = Directory.GetFiles(oracleConfigPath, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToArray();

        this.logger.LogInformation("Found {Count} SQL script files", sqlFiles.Length);

        foreach (var sqlFile in sqlFiles)
        {
            await this.ExecuteSqlFileAsync(sqlFile, cancellationToken);
        }

        this.logger.LogInformation("Oracle seeding completed successfully");
    }

    /// <summary>
    /// Executes a SQL script file with idempotent handling.
    /// </summary>
    private async Task ExecuteSqlFileAsync(string sqlFilePath, CancellationToken cancellationToken)
    {
        try
        {
            this.logger.LogInformation("Processing SQL file: {FileName}", Path.GetFileName(sqlFilePath));

            var sqlContent = await File.ReadAllTextAsync(sqlFilePath, cancellationToken);
            
            // Remove EXIT and COMMIT commands as we'll handle transactions in C#
            sqlContent = sqlContent
                .Replace("EXIT;", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("COMMIT;", string.Empty, StringComparison.OrdinalIgnoreCase);

            // Split into individual statements
            var statements = this.SplitSqlStatements(sqlContent);

            using var connection = new OracleConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken);

            foreach (var statement in statements)
            {
                await this.ExecuteStatementAsync(connection, statement, cancellationToken);
            }

            this.logger.LogInformation("Successfully executed SQL file: {FileName}", Path.GetFileName(sqlFilePath));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to execute SQL file: {FileName}", Path.GetFileName(sqlFilePath));
            throw;
        }
    }

    /// <summary>
    /// Executes a single SQL statement with idempotent error handling.
    /// </summary>
    private async Task ExecuteStatementAsync(
        OracleConnection connection,
        string statement,
        CancellationToken cancellationToken)
    {
        var trimmedStatement = statement.Trim();
        if (string.IsNullOrWhiteSpace(trimmedStatement) || trimmedStatement.StartsWith("--"))
        {
            return;
        }

        try
        {
            using var command = new OracleCommand(trimmedStatement, connection);
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync(cancellationToken);

            // Log what was executed (first 100 chars)
            var preview = trimmedStatement.Length > 100 
                ? trimmedStatement.Substring(0, 100) + "..." 
                : trimmedStatement;
            this.logger.LogDebug("Executed: {Statement}", preview);
        }
        catch (OracleException ex) when (this.IsIdempotentError(ex))
        {
            // Log and continue for expected errors (table/index already exists)
            this.logger.LogInformation("Skipping statement (already exists): {Error}", ex.Message);
        }
        catch (OracleException ex)
        {
            this.logger.LogError(ex, "Failed to execute statement: {Statement}", trimmedStatement.Substring(0, Math.Min(200, trimmedStatement.Length)));
            throw;
        }
    }

    /// <summary>
    /// Splits SQL content into individual statements.
    /// </summary>
    private List<string> SplitSqlStatements(string sqlContent)
    {
        var statements = new List<string>();
        var currentStatement = new StringBuilder();
        var lines = sqlContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("--"))
            {
                continue;
            }

            currentStatement.AppendLine(line);

            // Check if statement is complete (ends with semicolon)
            if (trimmedLine.EndsWith(";"))
            {
                var statement = currentStatement.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    // Remove trailing semicolon for Oracle
                    statement = statement.TrimEnd(';');
                    statements.Add(statement);
                }
                currentStatement.Clear();
            }
        }

        // Add any remaining statement
        if (currentStatement.Length > 0)
        {
            var statement = currentStatement.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(statement))
            {
                statements.Add(statement);
            }
        }

        return statements;
    }

    /// <summary>
    /// Determines if an Oracle exception is idempotent (can be safely ignored).
    /// </summary>
    private bool IsIdempotentError(OracleException ex)
    {
        // ORA-00955: name is already used by an existing object (table/index exists)
        // ORA-01430: column being added already exists in table
        // ORA-02260: table can have only one primary key
        return ex.Number is 955 or 1430 or 2260;
    }
}
