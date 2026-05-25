using Ra3Trainer.Core.Codegen;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class AaScriptReaderTests
{
    [Fact]
    public void ReadBootstrapScriptExtractsDataAndRemoteBlocks()
    {
        var script = TestAssets.ReadBootstrapLines();

        var document = AaScriptReader.Read(script);

        Assert.Equal(new byte[] { 0xA0, 0xA5, 0x86, 0x65 }, document.IEnableInitializers["iEnable+24"]);
        Assert.Contains(document.Blocks, block => block.Symbol.Equals("MustCode", StringComparison.OrdinalIgnoreCase) && block.Offset == 0);
        Assert.Contains(document.Blocks, block => block.Symbol.Equals("MustCode", StringComparison.OrdinalIgnoreCase) && block.Offset == 0x29);
        Assert.Contains(document.Blocks, block => block.Symbol.Equals("MustCode2", StringComparison.OrdinalIgnoreCase) && block.Offset == 0x700);
        Assert.DoesNotContain(document.Blocks, block => block.Symbol.Equals("ra3_1.12.game", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReadKeepsLocalLabelsInsideCurrentRemoteBlock()
    {
        var script = new[]
        {
            "[ENABLE]",
            "MustCode+10:",
            "push eax",
            "_ExitPlayerOneKillItMode:",
            "pop eax",
            "ret",
            "[disable]"
        };

        var document = AaScriptReader.Read(script);

        var block = Assert.Single(document.Blocks);
        Assert.Equal(["push eax", "_ExitPlayerOneKillItMode:", "pop eax", "ret"], block.Lines);
    }

    [Fact]
    public void ReadStopsRemoteBlockWhenModuleHookAnchorStarts()
    {
        var script = new[]
        {
            "[ENABLE]",
            "MustCode:",
            "ret",
            "ra3_1.12.game+1000:",
            "jmp MustCode+10",
            "_Back:",
            "MustCode+10:",
            "nop",
            "[disable]"
        };

        var document = AaScriptReader.Read(script);

        Assert.Equal(2, document.Blocks.Count);
        Assert.Equal(["ret"], document.Blocks[0].Lines);
        Assert.Equal(["nop"], document.Blocks[1].Lines);
    }
}
