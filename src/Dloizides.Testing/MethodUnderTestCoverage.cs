using System.Reflection;

namespace Dloizides.Testing;

/// <summary>Lists test-name prefixes with no MethodUnderTest description, and descriptions that explain nothing.</summary>
public static class MethodUnderTestCoverage
{
    /// <summary>The shortest description <see cref="Invalid"/> accepts by default.</summary>
    public const int DefaultMinimumDescriptionLength = 20;

    private const string FactAttributeName = "FactAttribute";
    private const char NameSeparator = '_';

    /// <summary>Returns "FullClassName: Prefix" for every test-name prefix its class does not describe.</summary>
    public static IReadOnlyList<string> Missing(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return TestClasses(assembly)
            .SelectMany(type => TestedMethods(type)
                .Except(Descriptions(type).Select(attribute => attribute.Method))
                .Select(method => Entry(type, method)))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Returns "FullClassName: Method" for every description naming no tested method or shorter than minLength.</summary>
    public static IReadOnlyList<string> Invalid(Assembly assembly, int minLength = DefaultMinimumDescriptionLength)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);

        return TestClasses(assembly)
            .SelectMany(type => InvalidDescriptions(type, minLength).Select(attribute => Entry(type, attribute.Method)))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<MethodUnderTestAttribute> InvalidDescriptions(Type type, int minLength)
    {
        var tested = TestedMethods(type);
        return Descriptions(type).Where(attribute =>
        {
            var namesNoTestedMethod = !tested.Contains(attribute.Method);
            var tooShort = attribute.Description.Trim().Length < minLength;
            return namesNoTestedMethod || tooShort;
        });
    }

    private static string Entry(Type type, string method) => $"{type.FullName}: {method}";

    private static IEnumerable<Type> TestClasses(Assembly assembly) =>
        LoadableTypes(assembly).Where(type => IsConcreteOrStaticClass(type) && TestMethods(type).Any());

    private static bool IsConcreteOrStaticClass(Type type) => type.IsClass && (!type.IsAbstract || type.IsSealed);

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static IEnumerable<MethodInfo> TestMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(IsFact);

    private static bool IsFact(MethodInfo method) =>
        method.GetCustomAttributes(inherit: true).Any(attribute => IsFactAttributeType(attribute.GetType()));

    private static bool IsFactAttributeType(Type? type)
    {
        for (; type is not null; type = type.BaseType)
            if (type.Name == FactAttributeName)
                return true;

        return false;
    }

    private static HashSet<string> TestedMethods(Type type) =>
        TestMethods(type).Select(method => method.Name.Split(NameSeparator)[0]).ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<MethodUnderTestAttribute> Descriptions(Type type) =>
        type.GetCustomAttributes<MethodUnderTestAttribute>(inherit: true);
}
