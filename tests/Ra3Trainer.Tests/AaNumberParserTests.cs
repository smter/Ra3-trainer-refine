using Ra3Trainer.Core.Codegen;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class AaNumberParserTests
{
    [Theory]
    [InlineData("#100000", 100000)]
    [InlineData("000186A0", 0x186A0)]
    [InlineData("A", 0xA)]
    [InlineData("d", 0xD)]
    [InlineData("0x10", 0x10)]
    public void ParseUsesCheatEngineNumberSemantics(string input, int expected)
    {
        Assert.Equal(expected, AaNumberParser.ParseInt32(input));
    }

    [Fact]
    public void ParseUInt32KeepsHighBitConstants()
    {
        Assert.Equal(0xAF4C0DA5u, AaNumberParser.ParseUInt32("AF4C0DA5"));
        Assert.Equal(99_999_999u, AaNumberParser.ParseUInt32("#99999999"));
    }
}
