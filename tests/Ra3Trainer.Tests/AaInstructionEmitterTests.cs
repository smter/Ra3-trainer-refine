using Ra3Trainer.Core.Codegen;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class AaInstructionEmitterTests
{
    [Fact]
    public void EncodeWritesRawDbBytes()
    {
        var context = TestContext();

        var bytes = AaInstructionEmitter.Encode(["db 75 0e"], 0x710000, context);

        Assert.Equal(new byte[] { 0x75, 0x0E }, bytes);
    }

    [Fact]
    public void EncodeResolvesAbsoluteSymbolMemoryOperands()
    {
        var context = TestContext();

        var bytes = AaInstructionEmitter.Encode([
            "mov eax,[ID]",
            "mov [iEnable+20],eax"
        ], 0x710000, context);

        Assert.Equal(new byte[]
        {
            0xA1, 0x00, 0x00, 0x70, 0x00,
            0xA3, 0x20, 0x01, 0x70, 0x00
        }, bytes);
    }

    [Fact]
    public void EncodeResolvesBranchesAndCalls()
    {
        var context = TestContext();

        var bytes = AaInstructionEmitter.Encode([
            "call MustCode+700",
            "jmp _BackPlayerMoney"
        ], 0x710600, context);

        Assert.Equal(new byte[]
        {
            0xE8, 0xFB, 0x00, 0x00, 0x00,
            0xE9, 0xF9, 0xF7, 0x3B, 0x00
        }, bytes);
    }

    [Fact]
    public void EncodeKeepsLocalLabelsAsJumpTargets()
    {
        var context = TestContext();

        var bytes = AaInstructionEmitter.Encode([
            "jne exit",
            "xor eax,eax",
            "exit:",
            "ret"
        ], 0x710000, context);

        Assert.Equal(new byte[] { 0x75, 0x02, 0x31, 0xC0, 0xC3 }, bytes);
    }

    [Fact]
    public void EncodeUsesDwordImmediateForDwordMemoryAdd()
    {
        var context = TestContext();

        var bytes = AaInstructionEmitter.Encode([
            "add [esi+20],00000001"
        ], 0x710000, context);

        Assert.Equal(new byte[] { 0x81, 0x46, 0x20, 0x01, 0x00, 0x00, 0x00 }, bytes);
    }

    [Fact]
    public void EncodeUsesDwordImmediateForDwordMemoryCmp()
    {
        var context = TestContext();

        var bytes = AaInstructionEmitter.Encode([
            "cmp [ebx+8],00000000"
        ], 0x710000, context);

        Assert.Equal(new byte[] { 0x81, 0x7B, 0x08, 0x00, 0x00, 0x00, 0x00 }, bytes);
    }

    [Fact]
    public void EncodeDisableAllSuperPower2BlockKeepsRawJumpAligned()
    {
        var context = TestContext(new Dictionary<string, nint>(StringComparer.OrdinalIgnoreCase)
        {
            ["_BackDisableAllSuperPower2"] = 0x70A5FC
        });

        var bytes = AaInstructionEmitter.Encode([
            "mov ecx,[eax+50]",
            "cmp ecx,[esi+20]",
            "pushfd",
            "pushad",
            "cmp byte ptr [iEnable+E],01",
            "db 75 07",
            "add [esi+20],00000001",
            "popad",
            "popfd",
            "jmp _BackDisableAllSuperPower2"
        ], 0x710294, context);

        Assert.Equal(0x81, bytes[17]);
        Assert.Equal(0x61, bytes[24]);
        Assert.Equal(0x9D, bytes[25]);
        Assert.Equal(0xE9, bytes[26]);
    }

    private static BootstrapBuildContext TestContext(
        IReadOnlyDictionary<string, nint>? labels = null)
    {
        return new BootstrapBuildContext(
            expression => expression.ToLowerInvariant() switch
            {
                "id" => 0x700000,
                "ienable" => 0x700100,
                "ienable+e" => 0x70010E,
                "ienable+20" => 0x700120,
                "mustcode" => 0x710000,
                "mustcode+700" => 0x710700,
                "mustcode2" => 0x720000,
                "ra3_1.12.game+6cfdfE" => 0xACFDFE,
                _ => throw new InvalidOperationException($"Unexpected expression '{expression}'.")
            },
            labels ?? new Dictionary<string, nint>(StringComparer.OrdinalIgnoreCase)
            {
                ["_BackPlayerMoney"] = 0xACFE03
            });
    }
}
