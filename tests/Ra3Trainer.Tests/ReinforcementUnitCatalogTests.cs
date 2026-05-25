using Ra3Trainer.Core.Features;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class ReinforcementUnitCatalogTests
{
    [Fact]
    public void ParseReadsCodesAndNamesFromCodeListLines()
    {
        var units = ReinforcementUnitCatalog.Parse(new[]
        {
            "DDFC28DE 警犬",
            "",
            "0x6586A5A0 奥米茄百合子",
            "d741d327 恐怖机器人"
        });

        Assert.Collection(
            units,
            unit =>
            {
                Assert.Equal(0xDDFC28DEu, unit.Code);
                Assert.Equal("警犬", unit.Name);
                Assert.Equal("0xDDFC28DE", unit.CodeText);
            },
            unit =>
            {
                Assert.Equal(0x6586A5A0u, unit.Code);
                Assert.Equal("奥米茄百合子", unit.Name);
                Assert.Equal("0x6586A5A0", unit.CodeText);
            },
            unit =>
            {
                Assert.Equal(0xD741D327u, unit.Code);
                Assert.Equal("恐怖机器人", unit.Name);
                Assert.Equal("0xD741D327", unit.CodeText);
            });
    }

    [Fact]
    public void ParseSkipsMalformedLines()
    {
        var units = ReinforcementUnitCatalog.Parse(new[]
        {
            "not-a-code",
            "12345",
            "  ",
            "AF4C0DA5 MCV"
        });

        Assert.Single(units);
        Assert.Equal(0xAF4C0DA5u, units[0].Code);
        Assert.Equal("MCV", units[0].Name);
    }

    [Fact]
    public void LoadBuiltInContainsRepresentativeEntries()
    {
        var units = ReinforcementUnitCatalog.LoadBuiltIn();

        Assert.NotEmpty(units);
        Assert.Contains(units, unit => unit.Code == 0x6586A5A0 && unit.Name == "奥米茄百合子");
        Assert.Contains(units, unit => unit.Code == 0x28DA574E && unit.Name == "MCV");
    }

    [Fact]
    public void MergeKeepsBuiltInEntriesAndAppendsOnlyNewCodes()
    {
        var merged = ReinforcementUnitCatalog.Merge(
            new[]
            {
                new ReinforcementUnitEntry(0x11111111, "A"),
                new ReinforcementUnitEntry(0x22222222, "B")
            },
            new[]
            {
                new ReinforcementUnitEntry(0x22222222, "B-override"),
                new ReinforcementUnitEntry(0x33333333, "C")
            });

        Assert.Collection(
            merged,
            unit => Assert.Equal((0x11111111u, "A"), (unit.Code, unit.Name)),
            unit => Assert.Equal((0x22222222u, "B"), (unit.Code, unit.Name)),
            unit => Assert.Equal((0x33333333u, "C"), (unit.Code, unit.Name)));
    }
}
