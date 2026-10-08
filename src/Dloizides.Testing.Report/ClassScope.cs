namespace Dloizides.Testing.Report;

internal sealed class ClassScope
{
    private readonly Stack<(string Name, int Depth)> open = new();
    private string? declared;
    private int depth;

    public int Depth => depth;

    public string Current => open.Count > 0 ? open.Peek().Name : string.Empty;

    public void Declare(string name) => declared = name;

    public void Step(string symbol)
    {
        switch (symbol)
        {
            case "{":
                depth++;
                if (declared is not null)
                    open.Push((declared, depth));
                declared = null;
                break;
            case "}":
                if (open.Count > 0 && open.Peek().Depth == depth)
                    open.Pop();
                depth--;
                break;
            case ";":
                declared = null;
                break;
        }
    }
}
