namespace Dloizides.Testing;

/// <summary>States one use case a test class or test proves, as the goal of the actor who has it.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class UseCaseAttribute(string text) : Attribute
{
    /// <summary>The goal in a few words, for example "Get buy and sell capacity per slot".</summary>
    public string Text { get; } = text;

    /// <summary>Who has the goal, for example "Signed-in API caller".</summary>
    public string Actor { get; set; } = string.Empty;
}
