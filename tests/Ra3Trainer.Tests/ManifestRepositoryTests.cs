using Ra3Trainer.Core.Patching;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class ManifestRepositoryTests
{
    [Fact]
    public void LoadReadsParsedTrainerArtifacts()
    {
        var manifest = TestAssets.LoadManifest();

        Assert.Equal("ra3_1.12.game", manifest.TargetProcess);
        Assert.Equal(32, manifest.Features.Count);
        Assert.Equal(22, manifest.PatchManifest.Hooks.Count);
        Assert.Equal(13, manifest.ActionDispatch.Count);

        var money = Assert.Single(manifest.Features, feature => feature.RawName == "Moeny");
        Assert.Equal("Money", money.DisplayName);
        Assert.Equal("Ctrl+F1", money.Hotkey);
        Assert.Equal(new[] { "iEnable+8" }, money.EnableFlags);

        var destroy = Assert.Single(manifest.Features, feature => feature.RawName == "Destory Select Unit");
        Assert.Equal("Destroy Select Unit", destroy.DisplayName);
        Assert.Equal("MustCode2+900", destroy.DispatchTarget);

        Assert.Equal(
            new[] { "iEnable+13" },
            Assert.Single(manifest.Features, feature => feature.RawName == "Danger Level MIN").EnableFlags);
        Assert.Equal(
            new[] { "iEnable+14" },
            Assert.Single(manifest.Features, feature => feature.RawName == "Restore Select Ore Mine").EnableFlags);
    }

    [Fact]
    public void AllPatchRestoreAssemblyEncodesToConcreteBytes()
    {
        var manifest = TestAssets.LoadManifest();

        foreach (var hook in manifest.PatchManifest.Hooks)
        {
            var bytes = OriginalByteParser.Parse(hook.OriginalAssembly);

            Assert.True(bytes.Length >= 5, $"{hook.Address} restore bytes are too short.");
            Assert.Contains(bytes, value => value != 0);
        }
    }
}
