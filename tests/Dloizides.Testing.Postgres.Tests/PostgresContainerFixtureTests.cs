using Npgsql;
using Shouldly;

namespace Dloizides.Testing.Postgres.Tests;

[MethodUnderTest("Image", "Starts the PostgreSQL image a service asks for, postgres:17 when it asks for none.")]
[MethodUnderTest("ConnectionString", "Refuses to hand out a connection string before the container has started.")]
[MethodUnderTest("WithTimeouts", "Gives every connection a 60 s connect and 120 s command timeout so Docker Desktop port-forward stalls do not fail tests.")]
public class PostgresContainerFixtureTests
{
    [Fact]
    public void Image_WithDefaultConstructor_IsPostgres17()
    {
        const string expected = "postgres:17";

        var fixture = new PostgresContainerFixture();

        fixture.Image.ShouldBe(expected);
    }

    [Fact]
    public void Image_WithSubclassOverride_UsesGivenImageAndDatabase()
    {
        const string image = "postgres:16-alpine";
        const string database = "orders";

        var fixture = new OrdersFixture(image, database);

        fixture.ShouldSatisfyAllConditions(
            () => fixture.Image.ShouldBe(image),
            () => fixture.Database.ShouldBe(database));
    }

    [Fact]
    public void ConnectionString_WithoutInitialize_ThrowsInvalidOperation()
    {
        var fixture = new PostgresContainerFixture();

        var act = () => fixture.ConnectionString;

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void WithTimeouts_WithPlainConnectionString_SetsConnectAndCommandTimeouts()
    {
        const string plain = "Host=localhost;Port=5432;Database=test;Username=postgres";
        const int connectSeconds = 60;
        const int commandSeconds = 120;

        var withTimeouts = new NpgsqlConnectionStringBuilder(PostgresContainerFixture.WithTimeouts(plain));

        withTimeouts.ShouldSatisfyAllConditions(
            () => withTimeouts.Timeout.ShouldBe(connectSeconds),
            () => withTimeouts.CommandTimeout.ShouldBe(commandSeconds),
            () => withTimeouts.Database.ShouldBe("test"));
    }

    private sealed class OrdersFixture(string image, string database) : PostgresContainerFixture(image, database);
}
