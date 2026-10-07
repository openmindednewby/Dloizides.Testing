using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dloizides.Testing;

/// <summary>A throwaway PostgreSQL Testcontainer for xUnit, with stall-tolerant connect and command timeouts.</summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    public const string DefaultImage = "postgres:17";
    public const string DefaultDatabase = "test";
    public const int ConnectTimeoutSeconds = 60;
    public const int CommandTimeoutSeconds = 120;

    private PostgreSqlContainer? _container;

    public PostgresContainerFixture()
        : this(DefaultImage, DefaultDatabase)
    {
    }

    protected PostgresContainerFixture(string image, string database)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        Image = image;
        Database = database;
    }

    public string Image { get; }

    public string Database { get; }

    public string ConnectionString => WithTimeouts(StartedContainer().GetConnectionString());

    public virtual async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder(Image).WithDatabase(Database).Build();
        await _container.StartAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    internal static string WithTimeouts(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = ConnectTimeoutSeconds,
            CommandTimeout = CommandTimeoutSeconds,
        }.ConnectionString;

    private PostgreSqlContainer StartedContainer() =>
        _container ?? throw new InvalidOperationException(
            $"{nameof(PostgresContainerFixture)} is not initialized; call {nameof(InitializeAsync)} first or use it as an xUnit fixture.");
}
