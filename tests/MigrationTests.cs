using MySqlConnector;
using TessBox.DotNet.MySql;

namespace TessBox.MySql.Extensions.Tests;

public class MigrationsTests
{
    [Fact]
    public async Task ProgressMigrationAsync_First()
    {
        // arrange
        var Connection = new MySqlConnection(
            "server=localhost;port=3308;uid=root;pwd=123456;database=mysql_test"
        );

        // act
        var version = await Connection.ProgressMigrationAsync(typeof(MigrationsTests).Assembly);

        // assert
        Assert.Equal(1, version);
    }
}
