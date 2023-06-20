using TessBox.DotNet.MySql;

namespace TessBox.MySql.Extensions.Tests;

public class MigrationItemProviderTests
{
    [Fact]
    public void Migrations_Single()
    {
        // act
        var provider = new MigrationItemProvider(GetType().Assembly);

        // assert
        Assert.Single(provider.Migrations);
        Assert.Equal(1, provider.Version);
    }

    [Fact]
    public async Task Migrations_Custom()
    {
        // arrange
        var provider = new MigrationItemProvider(GetType().Assembly);

        // act
        var content = await provider.GetScriptAsync("fake.sql");

        // assert
        Assert.Single(provider.Migrations);
        Assert.Equal("I M A CUSTOM SQL SCRIPT ", content);
    }
}
