using Ra3Trainer.Core.Runtime;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class TrainerRuntimeAssetsTests
{
    [Fact]
    public void LoadManifestReadsEmbeddedTrainerReport()
    {
        var manifest = TrainerRuntimeAssets.LoadManifest();

        Assert.Equal("ra3_1.12.game", manifest.TargetProcess);
        Assert.NotEmpty(manifest.Features);
        Assert.NotEmpty(manifest.PatchManifest.Hooks);
    }

    [Fact]
    public void ReadBootstrapLinesReadsEmbeddedBootstrapScript()
    {
        var lines = TrainerRuntimeAssets.ReadBootstrapLines();

        Assert.Contains(lines, line => line.Contains("Game Version : 1.12.3444.25830", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Equals("iEnable+30:", StringComparison.Ordinal));
    }
}
