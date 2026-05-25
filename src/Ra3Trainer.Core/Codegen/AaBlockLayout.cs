namespace Ra3Trainer.Core.Codegen;

public static class AaBlockLayout
{
    public static byte[] Build(string symbol, int maxSize, IEnumerable<AaEncodedBlock> blocks)
    {
        var output = new List<byte>(maxSize);
        foreach (var block in blocks
            .Where(block => block.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .OrderBy(block => block.Offset))
        {
            if (block.Offset < output.Count)
            {
                throw new InvalidOperationException(
                    $"{block.Symbol}+{block.Offset:X} overlaps previous encoded bytes ending at +{output.Count:X}.");
            }

            while (output.Count < block.Offset)
            {
                output.Add(0);
            }

            output.AddRange(block.Bytes);
            if (output.Count > maxSize)
            {
                throw new InvalidOperationException($"{symbol} exceeds allocated size 0x{maxSize:X}.");
            }
        }

        return output.ToArray();
    }
}

public sealed record AaEncodedBlock(string Symbol, int Offset, byte[] Bytes);
