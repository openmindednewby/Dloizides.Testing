namespace Dloizides.Testing;

/// <summary>Places a test at one step of a named business flow.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class FlowAttribute(string name, int step) : Attribute
{
    /// <summary>The business flow name.</summary>
    public string Name { get; } = name;

    /// <summary>The 1-based step of the flow this test proves.</summary>
    public int Step { get; } = step;
}
