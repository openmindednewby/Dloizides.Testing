namespace Dloizides.Testing;

/// <summary>Names the business feature of a test; on the assembly, also says why the feature exists.</summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class FeatureAttribute(string name) : Attribute
{
    /// <summary>The business feature name.</summary>
    public string Name { get; } = name;

    /// <summary>Why the feature exists, in one or two business sentences.</summary>
    public string Why { get; set; } = string.Empty;

    /// <summary>Where the feature is triggered from and who relies on it.</summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>Who answers questions about the feature.</summary>
    public string Owner { get; set; } = string.Empty;
}
