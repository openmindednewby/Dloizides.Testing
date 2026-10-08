namespace Dloizides.Testing;

/// <summary>Groups a test class or test under a named business feature in the report.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public sealed class FeatureAttribute(string name) : Attribute
{
    /// <summary>The business feature name.</summary>
    public string Name { get; } = name;
}
