namespace Ra3Trainer.Core.Patching;

public static class X86AssemblyLength
{
    public static byte[] EstimateOriginalBytes(IReadOnlyList<string> assembly)
    {
        var length = assembly.Sum(EstimateLength);
        return new byte[length];
    }

    public static int EstimateLength(string instruction)
    {
        var normalized = instruction.Trim().ToLowerInvariant();
        if (normalized == "nop")
        {
            return 1;
        }
        if (normalized.StartsWith("movss "))
        {
            return normalized.Contains("0000") ? 8 : 5;
        }
        if (normalized.StartsWith("fld dword ptr "))
        {
            return normalized.Contains("0000") ? 6 : 3;
        }
        if (normalized.StartsWith("cvttss2si "))
        {
            return 4;
        }
        if (normalized.StartsWith("add ") || normalized.StartsWith("sub ") || normalized.StartsWith("cmp "))
        {
            return 3;
        }
        if (normalized.StartsWith("mov "))
        {
            return normalized.Contains("0000") ? 6 : 3;
        }
        if (normalized.StartsWith("test "))
        {
            return 2;
        }
        return 5;
    }
}
