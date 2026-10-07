# Build & Publish — Dloizides.Testing

## Build + test

```bash
dotnet build Dloizides.Testing.slnx -c Release     # net8.0;net10.0, warnings are errors
dotnet test Dloizides.Testing.slnx -c Release      # needs Docker for the [Trait("Category","Docker")] class
dotnet test Dloizides.Testing.slnx -c Release --filter "Category!=Docker"   # without Docker
```

`tests/Dloizides.Testing.Tests.Samples` is a plain class library of deliberately broken and correct
sample test classes. It is not a test project, so the runner never executes it; the unit tests point
`MethodUnderTestCoverage` at it. The test project itself carries the two-line guard, and DLZ0004
(`Dloizides.Analyzers`) is an error there.

## Publish to nuget.org

```powershell
cd NuGetPackages/Dloizides.Testing
.\publish.ps1 -NoBump          # ship the <Version> in Directory.Build.props
.\publish.ps1 -Bump minor      # or bump + ship
```

The API key auto-loads from `SaaS/.env.local` (`NUGET_API_KEY`). The NuGet publish path has no
dirty-tree gate of its own beyond `publish-guard.ps1`: commit first.
