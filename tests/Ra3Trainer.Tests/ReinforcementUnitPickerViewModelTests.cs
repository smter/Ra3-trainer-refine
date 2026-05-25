using Ra3Trainer.Core.Features;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class ReinforcementUnitPickerSearchTests
{
    [Fact]
    public void FilterSearchesByNameOrCodeCaseInsensitively()
    {
        var filtered = ReinforcementUnitCatalog.Filter(new[]
        {
            new ReinforcementUnitEntry(0xDDFC28DE, "警犬"),
            new ReinforcementUnitEntry(0x6586A5A0, "奥米茄百合子"),
            new ReinforcementUnitEntry(0xAF4C0DA5, "MCV")
        }, "mcv");

        Assert.Single(filtered);
        Assert.Equal(0xAF4C0DA5u, filtered[0].Code);

        filtered = ReinforcementUnitCatalog.Filter(new[]
        {
            new ReinforcementUnitEntry(0xDDFC28DE, "警犬"),
            new ReinforcementUnitEntry(0x6586A5A0, "奥米茄百合子"),
            new ReinforcementUnitEntry(0xAF4C0DA5, "MCV")
        }, "6586a5a0");

        Assert.Single(filtered);
        Assert.Equal("奥米茄百合子", filtered[0].Name);

        filtered = ReinforcementUnitCatalog.Filter(new[]
        {
            new ReinforcementUnitEntry(0xDDFC28DE, "警犬"),
            new ReinforcementUnitEntry(0x6586A5A0, "奥米茄百合子"),
            new ReinforcementUnitEntry(0xAF4C0DA5, "MCV")
        }, "0xDDFC28DE");

        Assert.Single(filtered);
        Assert.Equal("警犬", filtered[0].Name);
    }

    [Fact]
    public void FilterReturnsAllUnitsForBlankSearch()
    {
        var filtered = ReinforcementUnitCatalog.Filter(new[]
        {
            new ReinforcementUnitEntry(0xDDFC28DE, "警犬"),
            new ReinforcementUnitEntry(0xAF4C0DA5, "MCV")
        }, "  ");

        Assert.Equal(2, filtered.Count);
    }
}
