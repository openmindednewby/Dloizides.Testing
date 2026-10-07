namespace Dloizides.Testing;

/// <summary>Describes, in business terms, one method a test class exercises.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MethodUnderTestAttribute(string method, string description) : Attribute
{
    public string Method { get; } = method;

    public string Description { get; } = description;
}
