namespace Ra3Trainer.Core.Memory;

public sealed class AddressResolver
{
    private readonly nint _moduleBase;
    private readonly IReadOnlyDictionary<string, nint> _symbols;

    public AddressResolver(nint moduleBase, IReadOnlyDictionary<string, nint> symbols)
    {
        _moduleBase = moduleBase;
        _symbols = new Dictionary<string, nint>(symbols, StringComparer.OrdinalIgnoreCase);
    }

    public nint Resolve(string expression)
    {
        var parts = expression.Split('+', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            if (_symbols.TryGetValue(parts[0], out var symbolAddress))
            {
                return symbolAddress;
            }
            throw new InvalidOperationException($"Unknown address expression '{expression}'.");
        }

        var offset = Convert.ToInt32(parts[1], 16);
        if (parts[0].Equals("ra3_1.12.game", StringComparison.OrdinalIgnoreCase))
        {
            return _moduleBase + offset;
        }

        if (_symbols.TryGetValue(parts[0], out var baseAddress))
        {
            return baseAddress + offset;
        }

        throw new InvalidOperationException($"Unknown symbol '{parts[0]}'.");
    }
}
