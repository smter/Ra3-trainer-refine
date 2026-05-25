namespace Ra3Trainer.Core.Features;

using System.IO;
using System.Reflection;

public static class ReinforcementUnitCatalog
{
    private static readonly Lazy<IReadOnlyList<ReinforcementUnitEntry>> BuiltInUnits =
        new(LoadBuiltInCore);

    public static IReadOnlyList<ReinforcementUnitEntry> Load(string path)
    {
        return Parse(File.ReadLines(path));
    }

    public static IReadOnlyList<ReinforcementUnitEntry> LoadBuiltIn()
    {
        return BuiltInUnits.Value;
    }

    public static IReadOnlyList<ReinforcementUnitEntry> Merge(
        IEnumerable<ReinforcementUnitEntry> primary,
        IEnumerable<ReinforcementUnitEntry> secondary)
    {
        var merged = new List<ReinforcementUnitEntry>();
        var seen = new HashSet<uint>();

        foreach (var entry in primary.Concat(secondary))
        {
            if (seen.Add(entry.Code))
            {
                merged.Add(entry);
            }
        }

        return merged;
    }

    public static IReadOnlyList<ReinforcementUnitEntry> Filter(
        IEnumerable<ReinforcementUnitEntry> units,
        string? searchText)
    {
        var normalized = (searchText ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return units.ToArray();
        }

        return units
            .Where(unit => MatchesSearch(unit, normalized))
            .ToArray();
    }

    public static IReadOnlyList<ReinforcementUnitEntry> Parse(IEnumerable<string> lines)
    {
        return lines
            .Select(ParseLine)
            .Where(entry => entry is not null)
            .Cast<ReinforcementUnitEntry>()
            .ToArray();
    }

    private static IReadOnlyList<ReinforcementUnitEntry> LoadBuiltInCore()
    {
        using var stream = OpenBuiltInResourceStream();
        using var reader = new StreamReader(stream);
        return Parse(ReadLines(reader));
    }

    private static ReinforcementUnitEntry? ParseLine(string line)
    {
        var trimmed = (line ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var firstSpace = trimmed.IndexOfAny([' ', '\t']);
        if (firstSpace <= 0 || firstSpace >= trimmed.Length - 1)
        {
            return null;
        }

        var codeText = trimmed[..firstSpace];
        var name = trimmed[(firstSpace + 1)..].Trim();
        if (name.Length == 0 || !UnitCodeParser.TryParse(codeText, out var code))
        {
            return null;
        }

        return new ReinforcementUnitEntry(code, name);
    }

    private static bool MatchesSearch(ReinforcementUnitEntry unit, string searchText)
    {
        return unit.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            unit.CodeText.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            unit.CodeText[2..].Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private static Stream OpenBuiltInResourceStream()
    {
        var assembly = typeof(ReinforcementUnitCatalog).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("code.txt", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException("Built-in unit code resource code.txt was not found.");
        }

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Unable to open embedded resource {resourceName}.");
    }

    private static IEnumerable<string> ReadLines(StreamReader reader)
    {
        while (reader.ReadLine() is string line)
        {
            yield return line;
        }
    }
}

public sealed record ReinforcementUnitEntry(uint Code, string Name)
{
    public string CodeText => UnitCodeParser.Format(Code);
}
