using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Parse", "Reads each [MethodUnderTest] description from C# source and files it under the class that carries it.")]
[MethodUnderTest("ReadDirectories", "Reads descriptions from every .cs file under the --source folders, skipping build output.")]
public sealed class MethodDescriptionReaderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "test-report-src-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    [Fact]
    public void Parse_WithAttributeBeforeClass_KeysByClassAndMethod()
    {
        const string source = """
            [MethodUnderTest("Save", "Stores a new order and returns its id.")]
            public class OrderServiceTests { }
            """;

        var descriptions = MethodDescriptionReader.Parse(source);

        descriptions["OrderServiceTests|Save"].ShouldBe("Stores a new order and returns its id.");
    }

    [Fact]
    public void Parse_WithStringConcatenation_JoinsTheLiterals()
    {
        const string source = """
            [MethodUnderTest("Save", "Stores a new order "
                + "and returns its id.")]
            public class OrderServiceTests { }
            """;

        var descriptions = MethodDescriptionReader.Parse(source);

        descriptions["OrderServiceTests|Save"].ShouldBe("Stores a new order and returns its id.");
    }

    [Fact]
    public void Parse_WithNestedClasses_FilesEachDescriptionUnderItsOwnClass()
    {
        const string source = """
            [MethodUnderTest("Save", "Outer save description text.")]
            public class Outer
            {
                [MethodUnderTest("Load", "Inner load description text.")]
                public class Inner { }
            }
            """;

        var descriptions = MethodDescriptionReader.Parse(source);

        descriptions.ShouldBe(new Dictionary<string, string>
        {
            ["Outer|Save"] = "Outer save description text.",
            ["Inner|Load"] = "Inner load description text.",
        });
    }

    [Fact]
    public void Parse_WithEscapedQuote_UnescapesIt()
    {
        const string source = """
            [MethodUnderTest("Save", "Stores the \"draft\" order.")]
            public class OrderServiceTests { }
            """;

        var descriptions = MethodDescriptionReader.Parse(source);

        descriptions["OrderServiceTests|Save"].ShouldBe("Stores the \"draft\" order.");
    }

    [Fact]
    public void ReadDirectories_WhenFolderIsMissing_ReturnsEmpty()
    {
        var missing = Path.Combine(root, "missing");

        var descriptions = MethodDescriptionReader.ReadDirectories([missing]);

        descriptions.ShouldBeEmpty();
    }

    [Fact]
    public void ReadDirectories_WithBuildOutputFolders_SkipsThem()
    {
        const string source = """
            [MethodUnderTest("Save", "Stores a new order and returns its id.")]
            public class {0} { }
            """;
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        File.WriteAllText(Path.Combine(root, "Kept.cs"), source.Replace("{0}", "KeptTests", StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(root, "obj", "Generated.cs"), source.Replace("{0}", "GeneratedTests", StringComparison.Ordinal));

        var descriptions = MethodDescriptionReader.ReadDirectories([root]);

        descriptions.Keys.ShouldBe(["KeptTests|Save"]);
    }
}
