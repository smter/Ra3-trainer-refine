using Ra3Trainer.Core.Features;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class TrainerFeatureCatalogTests
{
    [Fact]
    public void CreateUiFeaturesUsesSourceTrainerNamesAndHotkeys()
    {
        var manifest = TestAssets.LoadManifest();

        var features = TrainerFeatureCatalog.CreateUiFeatures(manifest.Features);

        Assert.Contains(features, feature =>
            feature.DisplayName == "增加玩家战场资金" &&
            feature.Hotkey == "Ctrl+F1");
        var power = Assert.Single(features, feature =>
            feature.DisplayName == "无限电力" &&
            feature.Hotkey == "Ctrl+F2");
        Assert.Null(power.ValueHint);
        Assert.Contains(features, feature =>
            feature.DisplayName == "选择的单位快速升级" &&
            feature.Hotkey == "P");
        Assert.Contains(features, feature =>
            feature.DisplayName == "选择的单位高速移动" &&
            feature.Hotkey == "-");
        Assert.Contains(features, feature =>
            feature.DisplayName == "选择的单位缓慢移动" &&
            feature.Hotkey == "=");
        Assert.Contains(features, feature =>
            feature.DisplayName == "选择的建筑物/单位ID" &&
            feature.Hotkey == "/");
        Assert.Contains(features, feature =>
            feature.DisplayName == "摧毁选择的建筑物/单位" &&
            feature.Hotkey == "Delete");
        Assert.Contains(features, feature =>
            feature.DisplayName == "建筑物可随地建造" &&
            feature.Hotkey == "L");
        Assert.Contains(features, feature =>
            feature.DisplayName == "给玩家基地车" &&
            feature.Hotkey == "X");
        Assert.Contains(features, feature =>
            feature.DisplayName == "呼叫战场增援" &&
            feature.Hotkey == "J");
        Assert.Contains(features, feature =>
            feature.DisplayName == "复制选择的建筑物/单位给玩家" &&
            feature.Hotkey == "I");
    }

    [Fact]
    public void CreateUiFeaturesMergesAmmoMaxPairIntoSingleToggle()
    {
        var manifest = TestAssets.LoadManifest();

        var features = TrainerFeatureCatalog.CreateUiFeatures(manifest.Features);

        var ammo = Assert.Single(features, feature => feature.RawName == "Select Unit Ammo MAX");
        Assert.Equal("选择的单位无限弹药/炸弹", ammo.DisplayName);
        Assert.Equal(";", ammo.Hotkey);
        Assert.Equal(new[] { "iEnable+12" }, ammo.EnableFlags);
        Assert.Null(ammo.ValueHint);
        Assert.Null(ammo.DispatchTarget);
        Assert.DoesNotContain(features, feature => feature.RawName == "Select Unit Ammo MAX 2");
    }

    [Fact]
    public void CreateUiFeaturesMapsFreeBuildToRemoteByteToggle()
    {
        var manifest = TestAssets.LoadManifest();

        var features = TrainerFeatureCatalog.CreateUiFeatures(manifest.Features);

        var freeBuild = Assert.Single(features, feature => feature.RawName == "Free Build");
        var patch = Assert.Single(freeBuild.ToggleBytePatches ?? []);
        Assert.Equal("MustCode+1201", patch.Address);
        Assert.Equal(new byte[] { 0xEB, 0x0C }, patch.EnabledBytes);
        Assert.Equal(new byte[] { 0x75, 0x0C }, patch.DisabledBytes);
        Assert.Null(freeBuild.ValueHint);
    }

    [Fact]
    public void CreateUiFeaturesKeepsManifestFeatureCountMinusMergedAmmoPair()
    {
        var manifest = TestAssets.LoadManifest();

        var features = TrainerFeatureCatalog.CreateUiFeatures(manifest.Features);

        Assert.Equal(31, features.Count);
    }
}
