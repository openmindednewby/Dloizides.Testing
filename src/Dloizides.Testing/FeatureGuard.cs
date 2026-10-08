using System.Reflection;

namespace Dloizides.Testing;

/// <summary>Lists the report areas whose feature no assembly-level [Feature] explains.</summary>
public static class FeatureGuard
{
    private const string TestsSuffix = "Tests";

    /// <summary>Returns, sorted, every area with tests but no assembly-level [Feature] of that name carrying a Why.</summary>
    public static IReadOnlyList<string> MissingFeatures(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var declared = assembly.GetCustomAttributes<FeatureAttribute>()
            .Where(feature => !string.IsNullOrWhiteSpace(feature.Why))
            .Select(feature => Key(feature.Name))
            .ToHashSet(StringComparer.Ordinal);
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        return TestDiscovery.TestClasses(assembly)
            .SelectMany(type => TestDiscovery.TestMethods(type).Select(method => AreaOf(assemblyName, type, method)))
            .Where(area => !declared.Contains(Key(area)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static string Key(string name) => new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string AreaOf(string assemblyName, Type type, MethodInfo method) =>
        method.GetCustomAttributes<FeatureAttribute>(inherit: true).FirstOrDefault()?.Name
        ?? type.GetCustomAttributes<FeatureAttribute>(inherit: true).FirstOrDefault()?.Name
        ?? FolderArea(assemblyName, type);

    private static string FolderArea(string assemblyName, Type type)
    {
        var full = type.FullName ?? type.Name;
        var rest = full.StartsWith(assemblyName + ".", StringComparison.Ordinal) ? full[(assemblyName.Length + 1)..] : full;
        var segments = rest.Split('.');
        if (segments.Length > 1)
            return segments[0];
        var head = segments[0].Split('+')[0];
        return head.Length > TestsSuffix.Length && head.EndsWith(TestsSuffix, StringComparison.Ordinal) ? head[..^TestsSuffix.Length] : head;
    }
}
