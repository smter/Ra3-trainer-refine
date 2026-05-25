using Ra3Trainer.Core.Codegen;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class AaBlockLayoutTests
{
    [Fact]
    public void LayoutPadsBlocksToFixedOffsets()
    {
        var blocks = new[]
        {
            new AaEncodedBlock("MustCode", 0, [0xAA, 0xBB]),
            new AaEncodedBlock("MustCode", 4, [0xCC])
        };

        var result = AaBlockLayout.Build("MustCode", 0x10, blocks);

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0x00, 0x00, 0xCC }, result[..5]);
    }

    [Fact]
    public void LayoutRejectsBlocksThatOverlapFixedOffsets()
    {
        var blocks = new[]
        {
            new AaEncodedBlock("MustCode", 0, [0xAA, 0xBB, 0xCC]),
            new AaEncodedBlock("MustCode", 2, [0xDD])
        };

        Assert.Throws<InvalidOperationException>(() => AaBlockLayout.Build("MustCode", 0x10, blocks));
    }
}
