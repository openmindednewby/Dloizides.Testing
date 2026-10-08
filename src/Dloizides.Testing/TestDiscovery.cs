using System.Reflection;

namespace Dloizides.Testing;

internal static class TestDiscovery
{
    private const string FactAttributeName = "FactAttribute";

    public static IEnumerable<Type> TestClasses(Assembly assembly) =>
        LoadableTypes(assembly).Where(type => IsConcreteOrStaticClass(type) && TestMethods(type).Any());

    public static IEnumerable<MethodInfo> TestMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(IsFact);

    private static bool IsConcreteOrStaticClass(Type type) => type.IsClass && (!type.IsAbstract || type.IsSealed);

    public static IEnumerable<Type> LoadableTypes(Assembly assembly)
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

    private static bool IsFact(MethodInfo method) =>
        method.GetCustomAttributes(inherit: true).Any(attribute => IsFactAttributeType(attribute.GetType()));

    private static bool IsFactAttributeType(Type? type)
    {
        for (; type is not null; type = type.BaseType)
            if (type.Name == FactAttributeName)
                return true;

        return false;
    }
}
