using System.Data.Common;
using System.Reflection;
using Dapper;
using MySqlConnector;

namespace TessBox.DotNet.MySql;

public sealed class MySqlMigration
{
    private readonly MigrationItemProvider _migrationItemProvider;

    private readonly string _connectionString;

    private readonly string _migrationName;

    public MySqlMigration(string connectionString)
        : this(Assembly.GetCallingAssembly(), connectionString) { }

    public MySqlMigration(Assembly scriptAssembly, string connectionString)
        : this(
            scriptAssembly,
            connectionString,
            scriptAssembly.GetName().Name ?? throw new Exception("Migration name not define")
        )
    { }

    public MySqlMigration(string connectionString, string name)
        : this(Assembly.GetCallingAssembly(), connectionString, name) { }

    public MySqlMigration(Assembly scriptAssembly, string connectionString, string name)
    {
        _migrationItemProvider = new MigrationItemProvider(scriptAssembly);
        _migrationName = name;
        _connectionString = connectionString;
    }

    public async Task<int> GetVersionAsync()
    {
        // check if table exist
        await EnsureSysVersionTableExist();

        // get last version
        var connection = CreateConnection();
        var parameters = new Dictionary<string, object> { { "@name", _migrationName } };
        var version = await connection.QueryAsync<int?>(
            "SELECT max(version_to) FROM sys_version WHERE name=@name",
            parameters
        );

        return version.FirstOrDefault() ?? 0;
    }

    public async Task<int> ProgressMigrationAsync()
    {
        var connection = CreateConnection();
        await connection.EnsureIsOpenedAsync();

        // check database version
        var versionFrom = await GetVersionAsync();
        var versionTo = _migrationItemProvider.Version;
        // migration
        if (versionTo <= versionFrom)
            return versionFrom;

        // ensure version table exist
        await EnsureSysVersionTableExist();

        // run migration
        var transaction = await connection.BeginTransactionAsync();
        try
        {
            // execute script
            await connection.ExecuteAsync(
                await _migrationItemProvider.GetScriptFromAsync(versionFrom),
                transaction: transaction
            );

            // update version
            var parameters = new Dictionary<string, object>
            {
                { "@name", _migrationName },
                { "@versionFrom", versionFrom },
                { "@versionTo", versionTo }
            };
            await connection.ExecuteAsync(
                $"INSERT INTO sys_version(name, version_from, version_to) VALUES (@name, @versionFrom, @versionTo )",
                parameters,
                transaction: transaction
            );

            await transaction.CommitAsync();

            return versionTo;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RunScriptAsync(string name)
    {
        var connection = CreateConnection();
        await connection.EnsureIsOpenedAsync();

        var script = await _migrationItemProvider.GetScriptAsync(name);
        var transaction = await connection.BeginTransactionAsync();
        try
        {
            await connection.ExecuteAsync(script, transaction: transaction);
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureSysVersionTableExist()
    {
        var connection = CreateConnection();

        // test if table exist
        var tableType = await connection.GetTableTypeAsync(connection.Database, "sys_version");

        if (tableType == TableType.table)
            return;

        // create table
        await connection.ExecuteAsync(
            @"
 CREATE TABLE IF NOT EXISTS sys_version (
    name VARCHAR(100),
    version_from INT,
    version_to INT,
    creationDate DATETIME DEFAULT CURRENT_TIMESTAMP
);
"
        );
    }

    private MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);

    }
}
