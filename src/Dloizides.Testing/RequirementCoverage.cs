using System.Reflection;

namespace Dloizides.Testing;

/// <summary>Lists declared requirements no test covers, and covered ids no requirement declares.</summary>
public static class RequirementCoverage
{
    /// <summary>Returns "FullClassName: Id" for every [Requirement] that no [Covers] in the assembly names.</summary>
    public static IReadOnlyList<string> Uncovered(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var classes = TestDiscovery.TestClasses(assembly).ToList();
        var covered = classes.SelectMany(Covers).SelectMany(link => link.Ids).ToHashSet(StringComparer.Ordinal);
        return Sorted(classes.SelectMany(type => DeclaredIds(type)
            .Where(id => !covered.Contains(id))
            .Select(id => $"{type.FullName}: {id}")));
    }

    /// <summary>Returns "FullClassName[.Method]: Id" for every [Covers] id that no [Requirement] in the assembly declares.</summary>
    public static IReadOnlyList<string> Undeclared(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var classes = TestDiscovery.TestClasses(assembly).ToList();
        var declared = classes.SelectMany(DeclaredIds).ToHashSet(StringComparer.Ordinal);
        return Sorted(classes.SelectMany(Covers).SelectMany(link => link.Ids
            .Where(id => !declared.Contains(id))
            .Select(id => $"{link.Owner}: {id}")));
    }

    private static IReadOnlyList<string> Sorted(IEnumerable<string> entries) =>
        entries.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    private static IEnumerable<string> DeclaredIds(Type type) =>
        type.GetCustomAttributes<RequirementAttribute>(inherit: true).Select(requirement => requirement.Id);

    private static IEnumerable<(string Owner, IReadOnlyList<string> Ids)> Covers(Type type)
    {
        var onClass = type.GetCustomAttributes<CoversAttribute>(inherit: true)
            .Select(link => (type.FullName ?? type.Name, link.Ids));
        var onMethods = TestDiscovery.TestMethods(type).SelectMany(method => method
            .GetCustomAttributes<CoversAttribute>(inherit: true)
            .Select(link => ($"{type.FullName}.{method.Name}", link.Ids)));
        return onClass.Concat(onMethods);
    }
}
