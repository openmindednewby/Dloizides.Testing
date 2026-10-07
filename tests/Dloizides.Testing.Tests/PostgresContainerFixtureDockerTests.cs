using Npgsql;
using Shouldly;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("ConnectionString", "Connects to the started container and runs a query against the requested database.")]
[Trait("Category", "Docker")]
public class PostgresContainerFixtureDockerTests(PostgresContainerFixture fixture) : IClassFixture<PostgresContainerFixture>
{
    [Fact]
    public async Task ConnectionString_WhenContainerStarted_RunsAQueryInTheTestDatabase()
    {
        const string expectedDatabase = "test";
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("select current_database()", connection);

        var database = await command.ExecuteScalarAsync();

        database.ShouldBe(expectedDatabase);
    }
}
