using System.Globalization;
using System.Text.RegularExpressions;

namespace Ra3Trainer.Core.Codegen;

public static partial class AaScriptReader
{
    public static AaDocument Read(IReadOnlyList<string> lines)
    {
        var blocks = new List<AaBlock>();
        var initializers = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        AaBlock? currentBlock = null;
        string? pendingDataLabel = null;
        var inEnable = false;

        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.Equals("[ENABLE]", StringComparison.OrdinalIgnoreCase))
            {
                inEnable = true;
                continue;
            }
            if (line.Equals("[disable]", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            if (!inEnable)
            {
                continue;
            }

            var label = LabelRegex().Match(line);
            if (label.Success)
            {
                var symbol = label.Groups["symbol"].Value;
                var offset = label.Groups["offset"].Success
                    ? int.Parse(label.Groups["offset"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                    : 0;

                if (symbol.Equals("MustCode", StringComparison.OrdinalIgnoreCase) ||
                    symbol.Equals("MustCode2", StringComparison.OrdinalIgnoreCase))
                {
                    currentBlock = null;
                    pendingDataLabel = null;
                    currentBlock = new AaBlock(symbol, offset, []);
                    blocks.Add(currentBlock);
                }
                else if (symbol.Equals("iEnable", StringComparison.OrdinalIgnoreCase))
                {
                    currentBlock = null;
                    pendingDataLabel = null;
                    pendingDataLabel = offset == 0 ? symbol : $"{symbol}+{offset:X}";
                }
                else if (symbol.Equals("ra3_1.12.game", StringComparison.OrdinalIgnoreCase))
                {
                    currentBlock = null;
                    pendingDataLabel = null;
                }
                else if (currentBlock is not null)
                {
                    currentBlock.Lines.Add(line);
                }
                else
                {
                    pendingDataLabel = null;
                }
                continue;
            }

            if (pendingDataLabel is not null && line.StartsWith("db ", StringComparison.OrdinalIgnoreCase))
            {
                initializers[pendingDataLabel] = ParseDb(line);
                pendingDataLabel = null;
                continue;
            }

            if (currentBlock is not null && !IsDirective(line))
            {
                currentBlock.Lines.Add(line);
            }
        }

        return new AaDocument(blocks, initializers);
    }

    private static bool IsDirective(string line)
    {
        return line.StartsWith("fullaccess", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("globalalloc", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("define", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("label", StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] ParseDb(string line)
    {
        return line[3..]
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static string StripComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index < 0 ? line : line[..index];
    }

    [GeneratedRegex(@"^(?<symbol>[A-Za-z_][\w\.]*)(\+(?<offset>[0-9A-Fa-f]+))?:\s*$")]
    private static partial Regex LabelRegex();
}

public sealed record AaDocument(
    IReadOnlyList<AaBlock> Blocks,
    IReadOnlyDictionary<string, byte[]> IEnableInitializers);

public sealed record AaBlock(string Symbol, int Offset, List<string> Lines);
