using System.Globalization;
using System.Text.RegularExpressions;
using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace Ra3Trainer.Core.Codegen;

public static partial class AaInstructionEmitter
{
    private static readonly HashSet<string> Register32Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "eax", "ebx", "ecx", "edx", "esi", "edi", "esp", "ebp"
    };

    private static readonly HashSet<string> Register8Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "al", "bl", "cl", "dl", "ah", "bh", "ch", "dh"
    };

    private static readonly HashSet<string> XmmRegisterNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "xmm0", "xmm1"
    };

    private static readonly IReadOnlyDictionary<string, byte> Register32Codes =
        new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            ["eax"] = 0,
            ["ecx"] = 1,
            ["edx"] = 2,
            ["ebx"] = 3,
            ["esp"] = 4,
            ["ebp"] = 5,
            ["esi"] = 6,
            ["edi"] = 7
        };

    public static byte[] Encode(IReadOnlyList<string> lines, nint origin, BootstrapBuildContext context)
    {
        var assembler = new Assembler(32);
        var labels = CollectLabels(lines, assembler);

        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (IsLabel(line))
            {
                var label = labels[line[..^1]];
                assembler.Label(ref label);
                continue;
            }

            Emit(assembler, line, context, labels);
        }

        using var stream = new MemoryStream();
        assembler.Assemble(new StreamCodeWriter(stream), unchecked((ulong)origin));
        return stream.ToArray();
    }

    private static Dictionary<string, Label> CollectLabels(IReadOnlyList<string> lines, Assembler assembler)
    {
        var labels = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine).Trim();
            if (IsLabel(line))
            {
                var name = line[..^1];
                labels[name] = assembler.CreateLabel(name);
            }
        }

        return labels;
    }

    private static void Emit(
        Assembler assembler,
        string instruction,
        BootstrapBuildContext context,
        IReadOnlyDictionary<string, Label> localLabels)
    {
        var normalized = instruction.ToLowerInvariant();
        switch (normalized)
        {
            case "nop":
                assembler.nop();
                return;
            case "pushad":
                assembler.pushad();
                return;
            case "pushfd":
                assembler.pushfd();
                return;
            case "popad":
                assembler.popad();
                return;
            case "popfd":
                assembler.popfd();
                return;
            case "ret":
                assembler.db(0xC3);
                return;
        }

        var parts = SplitInstruction(instruction);
        switch (parts.Mnemonic)
        {
            case "add":
                EmitAdd(assembler, parts.Operands, context);
                return;
            case "call":
                assembler.call(ResolveBranchTarget(parts.Operands[0], context, localLabels));
                return;
            case "cmp":
                EmitCmp(assembler, parts.Operands, context);
                return;
            case "cvttss2si":
                assembler.cvttss2si(Register32(parts.Operands[0]), Memory(parts.Operands[1], context));
                return;
            case "db":
                assembler.db(ParseDb(parts.Operands));
                return;
            case "dec":
                EmitDec(assembler, parts.Operands[0], context);
                return;
            case "fadd":
                assembler.fadd(DwordMemory(parts.Operands[0], context));
                return;
            case "fld":
                assembler.fld(DwordMemory(parts.Operands[0], context));
                return;
            case "fstp":
                assembler.fstp(DwordMemory(parts.Operands[0], context));
                return;
            case "fsub":
                assembler.fsub(DwordMemory(parts.Operands[0], context));
                return;
            case "inc":
                assembler.inc(Register32(parts.Operands[0]));
                return;
            case "je":
                assembler.je(ResolveBranchTarget(parts.Operands[0], context, localLabels));
                return;
            case "jmp":
                assembler.jmp(ResolveBranchTarget(parts.Operands[0], context, localLabels));
                return;
            case "jne":
                assembler.jne(ResolveBranchTarget(parts.Operands[0], context, localLabels));
                return;
            case "jz":
                assembler.jz(ResolveBranchTarget(parts.Operands[0], context, localLabels));
                return;
            case "lea":
                assembler.lea(Register32(parts.Operands[0]), Memory(parts.Operands[1], context));
                return;
            case "mov":
                EmitMov(assembler, parts.Operands, context);
                return;
            case "movss":
                EmitMovss(assembler, parts.Operands, context);
                return;
            case "or":
                EmitOr(assembler, parts.Operands, context);
                return;
            case "pop":
                assembler.pop(Register32(parts.Operands[0]));
                return;
            case "push":
                EmitPush(assembler, parts.Operands[0], context);
                return;
            case "sub":
                EmitSub(assembler, parts.Operands, context);
                return;
            case "test":
                EmitTest(assembler, parts.Operands, context);
                return;
            case "xor":
                assembler.xor(Register32(parts.Operands[0]), Register32(parts.Operands[1]));
                return;
            default:
                throw new NotSupportedException($"Unsupported bootstrap instruction '{instruction}'.");
        }
    }

    private static void EmitAdd(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        if (IsMemory(operands[0]))
        {
            if (IsRegister32(operands[1]))
            {
                assembler.add(Memory(operands[0], context), Register32(operands[1]));
                return;
            }

            if (!IsByteMemory(operands[0]))
            {
                EmitGroup1DwordMemoryImmediate(assembler, group: 0, operands[0], operands[1], context);
                return;
            }

            assembler.add(ImpliedDwordMemory(operands[0], context), AaNumberParser.ParseUInt32(operands[1]));
            return;
        }

        if (IsMemory(operands[1]))
        {
            assembler.add(Register32(operands[0]), Memory(operands[1], context));
            return;
        }

        assembler.add(Register32(operands[0]), AaNumberParser.ParseUInt32(operands[1]));
    }

    private static void EmitCmp(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        if (IsMemory(operands[0]))
        {
            if (IsRegister32(operands[1]))
            {
                assembler.cmp(Memory(operands[0], context), Register32(operands[1]));
                return;
            }

            if (!IsByteMemory(operands[0]))
            {
                EmitGroup1DwordMemoryImmediate(assembler, group: 7, operands[0], operands[1], context);
                return;
            }

            assembler.cmp(ImpliedDwordMemory(operands[0], context), AaNumberParser.ParseUInt32(operands[1]));
            return;
        }

        if (IsMemory(operands[1]))
        {
            assembler.cmp(Register32(operands[0]), Memory(operands[1], context));
            return;
        }

        if (IsRegister32(operands[1]))
        {
            assembler.cmp(Register32(operands[0]), Register32(operands[1]));
            return;
        }

        assembler.cmp(Register32(operands[0]), AaNumberParser.ParseUInt32(operands[1]));
    }

    private static void EmitDec(Assembler assembler, string operand, BootstrapBuildContext context)
    {
        if (IsMemory(operand))
        {
            assembler.dec(ImpliedDwordMemory(operand, context));
            return;
        }

        assembler.dec(Register32(operand));
    }

    private static void EmitMov(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        var destination = operands[0];
        var source = operands[1];
        if (IsMemory(destination))
        {
            if (IsRegister8(source))
            {
                assembler.mov(ByteMemory(destination, context), Register8(source));
                return;
            }

            if (IsRegister32(source))
            {
                assembler.mov(Memory(destination, context), Register32(source));
                return;
            }

            assembler.mov(ImpliedDwordMemory(destination, context), AaNumberParser.ParseUInt32(source));
            return;
        }

        if (IsMemory(source))
        {
            assembler.mov(Register32(destination), Memory(source, context));
            return;
        }

        if (IsRegister32(source))
        {
            assembler.mov(Register32(destination), Register32(source));
            return;
        }

        assembler.mov(Register32(destination), AaNumberParser.ParseUInt32(source));
    }

    private static void EmitMovss(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        if (IsMemory(operands[0]))
        {
            assembler.movss(Memory(operands[0], context), RegisterXmm(operands[1]));
            return;
        }

        assembler.movss(RegisterXmm(operands[0]), Memory(operands[1], context));
    }

    private static void EmitOr(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        if (IsMemory(operands[0]))
        {
            if (IsRegister32(operands[1]))
            {
                assembler.or(Memory(operands[0], context), Register32(operands[1]));
                return;
            }

            if (!IsByteMemory(operands[0]))
            {
                EmitGroup1DwordMemoryImmediate(assembler, group: 1, operands[0], operands[1], context);
                return;
            }

            assembler.or(ImpliedDwordMemory(operands[0], context), AaNumberParser.ParseUInt32(operands[1]));
            return;
        }

        if (IsMemory(operands[1]))
        {
            assembler.or(Register32(operands[0]), Memory(operands[1], context));
            return;
        }

        assembler.or(Register32(operands[0]), Register32(operands[1]));
    }

    private static void EmitPush(Assembler assembler, string operand, BootstrapBuildContext context)
    {
        if (IsMemory(operand))
        {
            assembler.push(DwordMemory(operand, context));
            return;
        }

        if (IsRegister32(operand))
        {
            assembler.push(Register32(operand));
            return;
        }

        assembler.push(AaNumberParser.ParseInt32(operand));
    }

    private static void EmitSub(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        if (IsMemory(operands[1]))
        {
            assembler.sub(Register32(operands[0]), Memory(operands[1], context));
            return;
        }

        assembler.sub(Register32(operands[0]), Register32(operands[1]));
    }

    private static void EmitTest(Assembler assembler, IReadOnlyList<string> operands, BootstrapBuildContext context)
    {
        if (IsMemory(operands[1]))
        {
            assembler.test(Memory(operands[1], context), Register32(operands[0]));
            return;
        }

        assembler.test(Register32(operands[0]), Register32(operands[1]));
    }

    private static ulong ResolveBranchTarget(
        string operand,
        BootstrapBuildContext context,
        IReadOnlyDictionary<string, Label> localLabels)
    {
        if (localLabels.TryGetValue(operand, out var label))
        {
            return label.Id;
        }

        return unchecked((uint)(int)context.Resolve(operand));
    }

    private static AssemblerMemoryOperand Memory(string operand, BootstrapBuildContext context)
    {
        var body = NormalizeMemoryBody(operand);
        if (TryResolveAbsoluteMemory(body, context, out var absolute))
        {
            return __[unchecked((uint)(int)absolute)];
        }

        var baseMatch = BaseMemoryRegex().Match(body);
        if (!baseMatch.Success)
        {
            throw new NotSupportedException($"Unsupported memory operand '{operand}'.");
        }

        var register = Register32(baseMatch.Groups["base"].Value);
        var offsetText = baseMatch.Groups["offset"].Value;
        var offset = offsetText.Length == 0 ? 0 : AaNumberParser.ParseInt32(offsetText);
        if (baseMatch.Groups["sign"].Value == "-")
        {
            offset = -offset;
        }

        if (baseMatch.Groups["index"].Success)
        {
            var index = Register32(baseMatch.Groups["index"].Value);
            var scale = int.Parse(baseMatch.Groups["scale"].Value, CultureInfo.InvariantCulture);
            return register + index * scale + offset;
        }

        return register + offset;
    }

    private static AssemblerMemoryOperand ByteMemory(string operand, BootstrapBuildContext context)
    {
        return __byte_ptr[Memory(operand, context)];
    }

    private static AssemblerMemoryOperand DwordMemory(string operand, BootstrapBuildContext context)
    {
        return __dword_ptr[Memory(operand, context)];
    }

    private static AssemblerMemoryOperand ImpliedMemory(string operand, BootstrapBuildContext context)
    {
        if (operand.TrimStart().StartsWith("byte ptr", StringComparison.OrdinalIgnoreCase))
        {
            return ByteMemory(operand, context);
        }

        if (operand.TrimStart().StartsWith("dword ptr", StringComparison.OrdinalIgnoreCase))
        {
            return DwordMemory(operand, context);
        }

        return Memory(operand, context);
    }

    private static AssemblerMemoryOperand ImpliedDwordMemory(string operand, BootstrapBuildContext context)
    {
        return operand.TrimStart().StartsWith("byte ptr", StringComparison.OrdinalIgnoreCase)
            ? ByteMemory(operand, context)
            : DwordMemory(operand, context);
    }

    private static bool IsByteMemory(string operand)
    {
        return operand.TrimStart().StartsWith("byte ptr", StringComparison.OrdinalIgnoreCase);
    }

    private static void EmitGroup1DwordMemoryImmediate(
        Assembler assembler,
        byte group,
        string memoryOperand,
        string immediateOperand,
        BootstrapBuildContext context)
    {
        var body = NormalizeMemoryBody(memoryOperand);
        var immediate = AaNumberParser.ParseUInt32(immediateOperand);
        assembler.db(EncodeGroup1DwordMemoryImmediate(group, body, immediate, context));
    }

    private static byte[] EncodeGroup1DwordMemoryImmediate(
        byte group,
        string memoryBody,
        uint immediate,
        BootstrapBuildContext context)
    {
        var bytes = new List<byte> { 0x81 };
        if (TryResolveAbsoluteMemory(memoryBody, context, out var absolute))
        {
            bytes.Add((byte)((group << 3) | 0x05));
            AddUInt32(bytes, unchecked((uint)(int)absolute));
            AddUInt32(bytes, immediate);
            return bytes.ToArray();
        }

        var baseMatch = BaseMemoryRegex().Match(memoryBody);
        if (!baseMatch.Success || baseMatch.Groups["index"].Success)
        {
            throw new NotSupportedException($"Unsupported dword immediate memory operand '[{memoryBody}]'.");
        }

        var baseName = baseMatch.Groups["base"].Value;
        var baseCode = Register32Codes[baseName];
        var offsetText = baseMatch.Groups["offset"].Value;
        var offset = offsetText.Length == 0 ? 0 : AaNumberParser.ParseInt32(offsetText);
        if (baseMatch.Groups["sign"].Value == "-")
        {
            offset = -offset;
        }

        var mod = offset == 0 && baseCode != 5
            ? 0
            : offset is >= sbyte.MinValue and <= sbyte.MaxValue ? 1 : 2;
        bytes.Add((byte)((mod << 6) | (group << 3) | (baseCode == 4 ? 4 : baseCode)));
        if (baseCode == 4)
        {
            bytes.Add(0x24);
        }

        if (mod == 1)
        {
            bytes.Add(unchecked((byte)offset));
        }
        else if (mod == 2 || baseCode == 5)
        {
            AddUInt32(bytes, unchecked((uint)offset));
        }

        AddUInt32(bytes, immediate);
        return bytes.ToArray();
    }

    private static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 24));
    }

    private static bool TryResolveAbsoluteMemory(string body, BootstrapBuildContext context, out nint address)
    {
        address = 0;
        if (Register32Names.Any(name => body.Contains(name, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        address = context.Resolve(body);
        return true;
    }

    private static string NormalizeMemoryBody(string operand)
    {
        var body = operand.Trim();
        body = body.Replace("byte ptr ", "", StringComparison.OrdinalIgnoreCase);
        body = body.Replace("dword ptr ", "", StringComparison.OrdinalIgnoreCase);
        if (!body.StartsWith("[", StringComparison.Ordinal) || !body.EndsWith("]", StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Unsupported memory operand '{operand}'.");
        }

        return body[1..^1].Trim();
    }

    private static byte[] ParseDb(IReadOnlyList<string> operands)
    {
        return operands
            .SelectMany(value => value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(value => unchecked((byte)AaNumberParser.ParseInt32(value)))
            .ToArray();
    }

    private static (string Mnemonic, IReadOnlyList<string> Operands) SplitInstruction(string instruction)
    {
        var firstSpace = instruction.IndexOf(' ');
        if (firstSpace < 0)
        {
            return (instruction.ToLowerInvariant(), Array.Empty<string>());
        }

        var mnemonic = instruction[..firstSpace].ToLowerInvariant();
        var operands = instruction[(firstSpace + 1)..]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (mnemonic, operands);
    }

    private static bool IsLabel(string line)
    {
        return LocalLabelRegex().IsMatch(line);
    }

    private static bool IsMemory(string operand) => operand.TrimStart().Contains('[', StringComparison.Ordinal);

    private static bool IsRegister32(string operand) => Register32Names.Contains(operand.Trim());

    private static bool IsRegister8(string operand) => Register8Names.Contains(operand.Trim());

    private static string StripComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index < 0 ? line : line[..index];
    }

    private static AssemblerRegister8 Register8(string name)
    {
        return name.Trim().ToLowerInvariant() switch
        {
            "al" => al,
            "bl" => bl,
            "cl" => cl,
            "dl" => dl,
            "ah" => ah,
            "bh" => bh,
            "ch" => ch,
            "dh" => dh,
            _ => throw new NotSupportedException($"Unsupported 8-bit register '{name}'.")
        };
    }

    private static AssemblerRegister32 Register32(string name)
    {
        return name.Trim().ToLowerInvariant() switch
        {
            "eax" => eax,
            "ebx" => ebx,
            "ecx" => ecx,
            "edx" => edx,
            "esi" => esi,
            "edi" => edi,
            "esp" => esp,
            "ebp" => ebp,
            _ => throw new NotSupportedException($"Unsupported 32-bit register '{name}'.")
        };
    }

    private static AssemblerRegisterXMM RegisterXmm(string name)
    {
        if (!XmmRegisterNames.Contains(name.Trim()))
        {
            throw new NotSupportedException($"Unsupported XMM register '{name}'.");
        }

        return name.Trim().ToLowerInvariant() switch
        {
            "xmm0" => xmm0,
            "xmm1" => xmm1,
            _ => throw new NotSupportedException($"Unsupported XMM register '{name}'.")
        };
    }

    [GeneratedRegex(@"^[A-Za-z_][\w]*:\s*$")]
    private static partial Regex LocalLabelRegex();

    [GeneratedRegex(@"^(?<base>e[abcd]x|esi|edi|esp|ebp)(\+(?<index>e[abcd]x|esi|edi|esp|ebp)\*(?<scale>[1248]))?((?<sign>[+-])(?<offset>[0-9A-Fa-fx#]+))?$", RegexOptions.IgnoreCase)]
    private static partial Regex BaseMemoryRegex();
}
