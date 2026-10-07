# Dloizides.Testing

Shared test infrastructure for our .NET services. Two pieces in v0.1:

1. **`[MethodUnderTest]` + `MethodUnderTestCoverage`** — every test-name prefix (`Save` in
   `Save_WhenNew_Persists`) gets a one-sentence business description on its test class. The test
   report shows that sentence next to the results, and a two-line guard fails the build when a
   prefix has none or a description has gone stale.
2. **`PostgresContainerFixture`** — a throwaway PostgreSQL Testcontainer (`postgres:17` by default)
   whose connection string carries a 60 s connect and 120 s command timeout. Npgsql's defaults
   (15 s / 30 s) failed whole RENER Trading integration runs when Docker Desktop's port forward
   stalled for a few seconds; the longer timeouts made the same runs pass.

## Install

```bash
dotnet add package Dloizides.Testing
```

Targets `net8.0` and `net10.0`. The fixture implements xUnit **v2** `IAsyncLifetime`
(`xunit.extensibility.core` 2.9.x). The coverage guard has no xUnit dependency: it finds tests by
attribute type **name** (`FactAttribute` or any subclass, so `[Theory]` and custom facts count).

## Describe what each test class exercises

```csharp
[MethodUnderTest("Save", "Stores a new declaration batch and returns it with its generated id.")]
[MethodUnderTest("Load", "Reads a declaration batch back with its portfolio slots.")]
public class DeclarationRepositoryTests
{
    [Fact]
    public async Task Save_WhenNew_ReturnsGeneratedId() { /* ... */ }

    [Fact]
    public async Task Load_WithSlots_ReturnsThemInOrder() { /* ... */ }
}
```

## Add the guard (once per test project)

```csharp
[MethodUnderTest("Missing", "Fails the build when a test class leaves a test-name prefix undescribed.")]
[MethodUnderTest("Invalid", "Fails the build when a description is stale or too short to explain anything.")]
public class MethodUnderTestGuardTests
{
    [Fact]
    public void Missing_WithThisTestAssembly_IsEmpty() =>
        Assert.Empty(MethodUnderTestCoverage.Missing(typeof(MethodUnderTestGuardTests).Assembly));

    [Fact]
    public void Invalid_WithThisTestAssembly_IsEmpty() =>
        Assert.Empty(MethodUnderTestCoverage.Invalid(typeof(MethodUnderTestGuardTests).Assembly));
}
```

| Call | Reports (`"<Class>: <Method>"`, ordinal-sorted) |
|---|---|
| `Missing(assembly)` | a test-name prefix whose class has no `[MethodUnderTest]` for it |
| `Invalid(assembly, minLength = 20)` | a `[MethodUnderTest]` naming no tested method, or with a description shorter than `minLength` |

Abstract classes and classes with no tests are skipped. Attributes are read from the class itself
(`inherit: false`).

## PostgreSQL fixture

```csharp
public class OrdersRepositoryTests(PostgresContainerFixture db) : IClassFixture<PostgresContainerFixture>
{
    [Fact]
    public async Task Save_WhenNew_Persists()
    {
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        // ...
    }
}
```

Another image or database name: subclass it.

```csharp
public sealed class Postgres16Fixture() : PostgresContainerFixture("postgres:16-alpine", "orders");
```

`ConnectionString` throws `InvalidOperationException` before `InitializeAsync` has run. v0.1 has no
EF Core or Respawn coupling: run your migrations and resets on top of `ConnectionString`.

## License

MIT
