namespace Dloizides.Testing;

/// <summary>Links a test to the requirement ids it proves.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CoversAttribute(params string[] ids) : Attribute
{
    /// <summary>The requirement ids this test proves.</summary>
    public IReadOnlyList<string> Ids { get; } = ids;
}
