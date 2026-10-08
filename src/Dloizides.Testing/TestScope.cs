using System.Reflection;

namespace Dloizides.Testing;

/// <summary>Carries the running test across awaits and into the system under test, so its recorded calls land on it.</summary>
public static class TestScope
{
    private static readonly AsyncLocal<CallScope?> Current = new();
    private static int cases;

    /// <summary>Marks <paramref name="testMethod"/> as the running test; call it from a before-test hook.</summary>
    public static void Enter(MethodInfo testMethod)
    {
        ArgumentNullException.ThrowIfNull(testMethod);
        var type = testMethod.ReflectedType ?? testMethod.DeclaringType;
        Current.Value = new CallScope($"{type?.FullName}.{testMethod.Name}", Interlocked.Increment(ref cases));
    }

    /// <summary>Clears the running test; call it from the matching after-test hook.</summary>
    public static void Exit() => Current.Value = null;

    internal static CallScope Resolve() =>
        Current.Value ?? new CallScope(CurrentTest.Name() is { Length: > 0 } name ? name : CallLog.Unattributed, 0);
}

internal sealed record CallScope(string Test, int Case);
