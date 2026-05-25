using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Manifest;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class FeatureScanPlannerTests
{
    [Fact]
    public void CreateMarksManifestFeaturesAsScannableOrSkipped()
    {
        var manifest = TestAssets.LoadManifest();

        var items = FeatureScanPlanner.Create(manifest.Features);

        Assert.Equal(32, items.Count);
        Assert.Equal(31, items.Count(item => item.CanScan));
        var skipped = Assert.Single(items, item => !item.CanScan);
        Assert.Equal("Free Build", skipped.Feature.DisplayName);
        Assert.Contains("enable flag", skipped.SkipReason);
    }

    [Fact]
    public void CreateClassifiesToggleAndActionFeatures()
    {
        var toggle = new TrainerFeature("Zoom", "Zoom", "Ctrl+F8", ["iEnable+F", "iEnable+10"], null, null);
        var action = new TrainerFeature("Select Unit HP MAX", "Select Unit HP MAX", "#219", [], "MustCode2+400", "0x05");

        var items = FeatureScanPlanner.Create([toggle, action]);

        Assert.Equal(FeatureScanKind.Toggle, items[0].Kind);
        Assert.Equal(FeatureScanKind.Action, items[1].Kind);
    }
}
