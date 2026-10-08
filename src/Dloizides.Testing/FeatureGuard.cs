using System.Reflection;

namespace Dloizides.Testing;

/// <summary>Lists the report areas whose feature no assembly-level [Feature] explains.</summary>
public static class FeatureGuard
{
    /// <summary>Returns, sorted, every area with tests but no assembly-level [Feature] of that name carrying a Why.</summary>
    public static IReadOnlyList<string> MissingFeatures(Assembly assembly) => MissingFeatures(assembly, string.Empty);

    /// <summary>Same list, skipping a leading namespace segment equal to the report set name (Unit, Integration).</summary>
    public static IReadOnlyList<string> MissingFeatures(Assembly assembly, string setName)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var declared = assembly.GetCustomAttributes<FeatureAttribute>()
            .Where(feature => !string.IsNullOrWhiteSpace(feature.Why))
            .Select(feature => FeatureArea.Key(feature.Name))
            .ToHashSet(StringComparer.Ordinal);
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        return TestDiscovery.TestClasses(assembly)
            .SelectMany(type => TestDiscovery.TestMethods(type).Select(method => AreaOf(assemblyName, setName, type, method)))
            .Where(area => !declared.Contains(FeatureArea.Key(area)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static string AreaOf(string assemblyName, string setName, Type type, MethodInfo method) =>
        method.GetCustomAttributes<FeatureAttribute>(inherit: true).FirstOrDefault()?.Name
        ?? type.GetCustomAttributes<FeatureAttribute>(inherit: true).FirstOrDefault()?.Name
        ?? FeatureArea.Of(type.FullName ?? type.Name, assemblyName, setName);
}
