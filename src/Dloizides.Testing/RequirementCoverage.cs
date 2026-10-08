using System.Reflection;

namespace Dloizides.Testing;

/// <summary>Lists declared requirements their own class never covers, and covered ids their own class never declares.</summary>
public static class RequirementCoverage
{
    /// <summary>Returns "FullClassName: Id" for every [Requirement] that no [Covers] on the same class or its tests names.</summary>
    public static IReadOnlyList<string> Uncovered(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Sorted(TestDiscovery.LoadableTypes(assembly).Where(DeclaresRequirements).SelectMany(type =>
        {
            var covered = Covers(type).SelectMany(link => link.Ids).ToHashSet(StringComparer.Ordinal);
            return DeclaredIds(type).Where(id => !covered.Contains(id)).Select(id => $"{type.FullName}: {id}");
        }));
    }

    /// <summary>Returns "FullClassName[.Method]: Id" for every [Covers] id that no [Requirement] on the same class declares.</summary>
    public static IReadOnlyList<string> Undeclared(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Sorted(TestDiscovery.TestClasses(assembly).SelectMany(type =>
        {
            var declared = DeclaredIds(type).ToHashSet(StringComparer.Ordinal);
            return Covers(type).SelectMany(link => link.Ids
                .Where(id => !declared.Contains(id))
                .Select(id => $"{link.Owner}: {id}"));
        }));
    }

    private static IReadOnlyList<string> Sorted(IEnumerable<string> entries) =>
        entries.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    private static bool DeclaresRequirements(Type type) => type.IsClass && DeclaredIds(type).Any();

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
