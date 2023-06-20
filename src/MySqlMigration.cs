using System.Reflection;
using Dapper;
using MySqlConnector;

namespace TessBox.DotNet.MySql;

public sealed class MySqlMigration
{
    private readonly MigrationItemProvider _migrationItemProvider;

    private readonly MySqlConnection _connection;

    private readonly string _migrationName;

    public MySqlMigration(string connectionString)
        : this(Assembly.GetCallingAssembly(), connectionString) { }

    public MySqlMigration(Assembly scriptAssembly, string connectionString)
    {
        _migrationItemProvider = new MigrationItemProvider(scriptAssembly);
        _migrationName =
            scriptAssembly.GetName().Name ?? throw new Exception("Migration name not define");
        _connection = new MySqlConnection(connectionString);
    }

    public async Task<int> GetVersionAsync()
    {
        // check if table exist
        var tableType = await _connection.GetTableTypeAsync(_connection.Database, "sys_version");
        if (tableType == TableType.no_exist)
            return 0;

        var parameters = new Dictionary<string, object> { { "@name", _migrationName } };
        var version = await _connection.QueryAsync<int>(
            "SELECT max(version) FROM sys_version WHERE name=@name",
            parameters
        );

        return version.FirstOrDefault();
    }

    public async Task<int> ProgressMigrationAsync()
    {
        await _connection.EnsureIsOpenedAsync();

        // check database version
        var version = await GetVersionAsync();

        // migration
        if (_migrationItemProvider.Version <= version)
            return version;

        var transaction = await _connection.BeginTransactionAsync();
        try
        {
            await _connection.ExecuteAsync(
                await _migrationItemProvider.GetScriptFromAsync(version),
                transaction: transaction
            );
            if (version == 0)
            {
                await _connection.ExecuteAsync(
                    @"
 CREATE TABLE IF NOT EXISTS sys_version (
    name VARCHAR(100),
    version INT,
    creationDate DATETIME DEFAULT CURRENT_TIMESTAMP
);
",
                    transaction: transaction
                );
            }

            var parameters = new Dictionary<string, object>
            {
                { "@name", _migrationName },
                { "@version", _migrationItemProvider.Version }
            };
            await _connection.ExecuteAsync(
                $"INSERT INTO sys_version(name, version) VALUES (@name, @version )",
                parameters,
                transaction: transaction
            );

            await transaction.CommitAsync();

            return _migrationItemProvider.Version;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RunScript(string name)
    {
        await _connection.EnsureIsOpenedAsync();

        var script = await _migrationItemProvider.GetScriptAsync(name);
        var transaction = await _connection.BeginTransactionAsync();
        try
        {
            await _connection.ExecuteAsync(script, transaction);
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
