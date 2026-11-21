namespace Dloizides.Testing.Security.Tests;

public sealed class LeakyHostFailures : IAsyncLifetime
{
    public IReadOnlyList<SecurityProbeFailure> All { get; private set; } = [];

    public async Task InitializeAsync()
    {
        await using var host = await ProbeHost.StartLeakyAsync();
        All = await SecurityProbe.FindFailuresAsync(host.Target);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public IReadOnlyList<string> Of(SecurityProbeKind kind) =>
        All.Where(failure => failure.Kind == kind).Select(failure => $"{failure.Request} => {failure.Actual}").ToList();

    public IReadOnlyList<string> ExpectedOf(SecurityProbeKind kind) =>
        All.Where(failure => failure.Kind == kind).Select(failure => failure.Expected).ToList();
}
