using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dloizides.Testing.Postgres;

/// <summary>A throwaway PostgreSQL Testcontainer for xUnit v2, with stall-tolerant connect and command timeouts.</summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    /// <summary>The image started when a subclass names none.</summary>
    public const string DefaultImage = "postgres:17";

    /// <summary>The database created when a subclass names none.</summary>
    public const string DefaultDatabase = "test";

    /// <summary>Seconds a connection waits to open.</summary>
    public const int ConnectTimeoutSeconds = 60;

    /// <summary>Seconds a command waits to finish.</summary>
    public const int CommandTimeoutSeconds = 120;

    private PostgreSqlContainer? _container;

    /// <summary>Uses <see cref="DefaultImage"/> and <see cref="DefaultDatabase"/>.</summary>
    public PostgresContainerFixture()
        : this(DefaultImage, DefaultDatabase)
    {
    }

    /// <summary>Uses the given image and database name.</summary>
    protected PostgresContainerFixture(string image, string database)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        Image = image;
        Database = database;
    }

    /// <summary>The PostgreSQL image this fixture starts.</summary>
    public string Image { get; }

    /// <summary>The database created inside the container.</summary>
    public string Database { get; }

    /// <summary>The started container's connection string, with the stall-tolerant timeouts applied.</summary>
    public string ConnectionString => WithTimeouts(StartedContainer().GetConnectionString());

    /// <summary>Builds and starts the container.</summary>
    public virtual async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder(Image).WithDatabase(Database).Build();
        await _container.StartAsync();
    }

    /// <summary>Stops and removes the container if it was started.</summary>
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
