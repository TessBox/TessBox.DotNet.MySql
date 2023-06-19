using System.Data;
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

public static class MySqlConnectionExtension
{
    public static async Task EnsureIsOpenedAsync(this MySqlConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
    }

    public static async Task<TableType> GetTableTypeAsync(
        this MySqlConnection connection,
        string databaseName,
        string tableName
    )
    {
        var parameters = new DynamicParameters();
        parameters.Add("in_db", databaseName);
        parameters.Add("in_table", tableName);
        parameters.Add("out_exists", dbType: DbType.String, direction: ParameterDirection.Output);

        await connection.QueryFirstOrDefaultAsync<string>(
            "sys.table_exists",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var result = parameters.Get<string>("out_exists");
        return result switch
        {
            "BASE TABLE" => TableType.table,
            "VIEW" => TableType.view,
            "TEMPORARY" => TableType.temp,
            _ => TableType.no_exist,
        };
    }

    public static Task RunAsync(this MySqlConnection connection, string script)
    {
        MySqlCommand command = new() { Connection = connection, CommandText = script };

        return command.ExecuteNonQueryAsync();
    }
}
