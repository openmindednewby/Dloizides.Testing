using System.Xml.Linq;

namespace Dloizides.Testing.Report;

internal static class TaskTraits
{
    public const string Name = "Task";

    private static readonly string[] CategoryPrefixes = [Name + "=", Name + ":"];

    public static IReadOnlyList<string> FromTrx(XElement unitTest)
    {
        var properties = Children(unitTest, "Properties").SelectMany(p => Children(p, "Property"))
            .Where(p => Value(p, "Key") == Name)
            .Select(p => Value(p, "Value"));
        var categories = Children(unitTest, "TestCategory").SelectMany(c => Children(c, "TestCategoryItem"))
            .Select(item => FromCategory(item.Attribute("TestCategory")?.Value ?? string.Empty))
            .OfType<string>();
        return Clean(properties.Concat(categories));
    }

    public static IReadOnlyList<string> Clean(IEnumerable<string> ids) =>
        ids.Select(id => id.Trim()).Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    private static string? FromCategory(string category) =>
        CategoryPrefixes.Where(prefix => category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Select(prefix => category[prefix.Length..]).FirstOrDefault();

    private static IEnumerable<XElement> Children(XElement parent, string localName) =>
        parent.Elements().Where(e => e.Name.LocalName == localName);

    private static string Value(XElement parent, string localName) =>
        Children(parent, localName).FirstOrDefault()?.Value ?? string.Empty;
}
