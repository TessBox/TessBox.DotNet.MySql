using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using XUnit.Extensions.IntegrationTests;
using TessBox.DotNet.MySql;

namespace TessBox.MySql.Extensions.Tests;

public class MigrationsTests : IntegrationTest<Context>
{
    private const string ConnectionString =
        "server=localhost;port=3308;uid=root;pwd=123456;database=mysql_test";

    public MigrationsTests(ITestOutputHelper testOutputHelper, Context context)
        : base(testOutputHelper, context) { }

    [Fact, TestPriority(1)]
    public async Task ProgressMigrationAsync_First()
    {
        // arrange
        var Connection = new MySqlMigration(ConnectionString);

        // act
        var version = await Connection.ProgressMigrationAsync();

        // assert
        Assert.Equal(1, version);
    }

    [Fact, TestPriority(2)]
    public async Task ProgressMigrationAsync_Again_Nothing()
    {
        // arrange
        var Connection = new MySqlMigration(ConnectionString);

        // act
        var version = await Connection.ProgressMigrationAsync();

        // assert
        Assert.Equal(1, version);
    }
}

public class Context : TestContext
{
    protected override void AddServices(
        IServiceCollection services,
        IConfiguration? configuration
    ) { }

    protected override IEnumerable<string> GetSettingsFiles()
    {
        return Array.Empty<string>();
    }
}
