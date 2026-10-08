namespace Dloizides.Testing;

/// <summary>Declares, on a test class, one requirement its tests prove, in one business sentence.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequirementAttribute(string id, string text) : Attribute
{
    /// <summary>The requirement id, for example "AC-01".</summary>
    public string Id { get; } = id;

    /// <summary>One business sentence saying what must hold.</summary>
    public string Text { get; } = text;
}
