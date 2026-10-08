using System.Diagnostics;
using System.Reflection;

namespace Dloizides.Testing;

internal static class CurrentTest
{
    private const string StateMachineStep = "MoveNext";
    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly HashSet<string> TestAttributes = new(StringComparer.Ordinal)
    {
        "FactAttribute", "TheoryAttribute", "TestAttribute", "TestCaseAttribute", "TestMethodAttribute", "DataTestMethodAttribute",
    };

    public static string Name() =>
        new StackTrace(false).GetFrames().Select(frame => TestMethodOf(frame.GetMethod())).FirstOrDefault(m => m is not null) is { DeclaringType: { } type } method
            ? $"{type.FullName}.{method.Name}"
            : string.Empty;

    private static MethodBase? TestMethodOf(MethodBase? frame)
    {
        if (frame?.DeclaringType is not { } type)
            return null;
        var method = StateMachineOwner(frame, type) ?? frame;
        return IsTest(method) ? method : null;
    }

    private static MethodBase? StateMachineOwner(MethodBase frame, Type type)
    {
        var close = type.Name.IndexOf('>', StringComparison.Ordinal);
        var isStateMachine = frame.Name == StateMachineStep && type.Name.StartsWith('<') && close > 1;
        if (!isStateMachine || type.DeclaringType is not { } owner)
            return null;
        var name = type.Name[1..close];
        return owner.GetMethods(Declared).FirstOrDefault(m => m.Name == name && IsTest(m));
    }

    private static bool IsTest(MethodBase method) => method.GetCustomAttributes(true).Any(a => IsTestAttribute(a.GetType()));

    private static bool IsTestAttribute(Type? type) => type is not null && (TestAttributes.Contains(type.Name) || IsTestAttribute(type.BaseType));
}
