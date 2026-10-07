namespace Dloizides.Testing;

/// <summary>Describes, in business terms, one method a test class exercises.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class MethodUnderTestAttribute(string method, string description) : Attribute
{
    /// <summary>The test-name prefix this description covers.</summary>
    public string Method { get; } = method;

    /// <summary>One business sentence saying what the method does.</summary>
    public string Description { get; } = description;
}
