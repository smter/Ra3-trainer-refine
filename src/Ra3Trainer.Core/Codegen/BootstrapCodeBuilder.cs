namespace Ra3Trainer.Core.Codegen;

public class BootstrapCodeBuilder
{
    public virtual BootstrapCode Build(IReadOnlyList<string> autoAssemblerLines)
    {
        if (autoAssemblerLines.Count == 0)
        {
            return BootstrapCode.Empty;
        }

        throw new NotSupportedException(
            "Bootstrap build context is required to encode 00_bootstrap.aa remote code safely.");
    }

    public virtual BootstrapCode Build(IReadOnlyList<string> autoAssemblerLines, BootstrapBuildContext context)
    {
        if (autoAssemblerLines.Count == 0)
        {
            return BootstrapCode.Empty;
        }

        var document = AaScriptReader.Read(autoAssemblerLines);
        var definitions = AaDefineReader.Read(autoAssemblerLines);
        var encodedBlocks = document.Blocks
            .Select(block =>
            {
                var expandedLines = ExpandDefinitions(block.Lines, definitions);
                var origin = context.Resolve(block.Offset == 0 ? block.Symbol : $"{block.Symbol}+{block.Offset:X}");
                var bytes = AaInstructionEmitter.Encode(expandedLines, origin, context);
                return new AaEncodedBlock(block.Symbol, block.Offset, bytes);
            })
            .ToArray();

        return new BootstrapCode(
            AaBlockLayout.Build("MustCode", 0x3000, encodedBlocks),
            AaBlockLayout.Build("MustCode2", 0x1000, encodedBlocks),
            document.IEnableInitializers);
    }

    private static IReadOnlyList<string> ExpandDefinitions(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, string> definitions)
    {
        return lines
            .Select(line => definitions.TryGetValue(line.Trim(), out var replacement) ? replacement : line)
            .ToArray();
    }
}

public sealed record BootstrapCode(
    byte[] MustCode,
    byte[] MustCode2,
    IReadOnlyDictionary<string, byte[]> Initializers)
{
    public static BootstrapCode Empty { get; } =
        new([], [], new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase));
}

public sealed record BootstrapBuildContext(
    Func<string, nint> ResolveAddress,
    IReadOnlyDictionary<string, nint> Labels)
{
    public nint Resolve(string expression)
    {
        return Labels.TryGetValue(expression, out var address)
            ? address
            : ResolveAddress(expression);
    }
}

internal static partial class AaDefineReader
{
    public static IReadOnlyDictionary<string, string> Read(IReadOnlyList<string> lines)
    {
        var definitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine).Trim();
            var match = DefineRegex().Match(line);
            if (match.Success)
            {
                definitions[match.Groups["name"].Value] = match.Groups["value"].Value.Trim();
            }
        }

        return definitions;
    }

    private static string StripComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index < 0 ? line : line[..index];
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^define\((?<name>[^,]+),(?<value>.+)\)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex DefineRegex();
}
