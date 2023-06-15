using System.Reflection;
using Dapper;
using MySqlConnector;

namespace TessBox.DotNet.MySql;

public enum TableType
{
    no_exist,
    table,
    view,
    temp
}

public static class MySqlConnectionMigrationExtension
{
    public static async Task<int> GetVersionAsync(this MySqlConnection connection)
    {
        // check if table exist
        var tableType = await connection.GetTableTypeAsync(connection.Database, "sys_version");
        if (tableType == TableType.no_exist)
            return 0;

        var version = await connection.QueryAsync<int>("SELECT max(version) FROM sys_version");
        return version.FirstOrDefault();
    }

    public static Task<int> ProgressMigrationAsync(this MySqlConnection connection)
    {
        return connection.ProgressMigrationAsync(Assembly.GetCallingAssembly());
    }

    public static async Task<int> ProgressMigrationAsync(
        this MySqlConnection connection,
        Assembly scriptsAssembly
    )
    {
        await connection.EnsureIsOpenedAsync();

        // check database version
        var version = await connection.GetVersionAsync();

        // migration
        var migration = new MySqlMigration(scriptsAssembly);
        if (migration.Version <= version)
            return version;

        var transaction = await connection.BeginTransactionAsync();
        try
        {
            await connection.ExecuteAsync(
                await migration.GetScriptFromAsync(version),
                transaction: transaction
            );
            if (version == 0)
            {
                await connection.ExecuteAsync(
                    @"
 CREATE TABLE IF NOT EXISTS sys_version (
    version INT,
    creationDate DATETIME DEFAULT CURRENT_TIMESTAMP
);
",
                    transaction: transaction
                );
            }

            await connection.ExecuteAsync(
                $"INSERT INTO sys_version(version) VALUES ({migration.Version})",
                transaction: transaction
            );

            await transaction.CommitAsync();

            return migration.Version;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
