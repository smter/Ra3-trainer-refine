using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Hotkeys;
using Ra3Trainer.Core.Manifest;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class HotkeyFeatureDispatcherTests
{
    [Fact]
    public void TryDispatchIgnoresKeysWhenHotkeysAreDisabled()
    {
        var dispatcher = new HotkeyFeatureDispatcher();
        var invoked = 0;
        dispatcher.Update(
            [new HotkeyFeatureBinding(
                HotkeyGesture.Parse("P"),
                new TrainerFeature("Select Unit Level UP", "选择的单位快速升级", "P", [], "MustCode2+700", "0x08"),
                () => invoked++)],
            enabled: false);

        var handled = dispatcher.TryDispatch(0x50, HotkeyModifiers.None);

        Assert.False(handled);
        Assert.Equal(0, invoked);
    }

    [Fact]
    public void TryDispatchTriggersMatchingFeatureOncePerKeyPress()
    {
        var dispatcher = new HotkeyFeatureDispatcher();
        var invoked = 0;
        dispatcher.Update(
            [new HotkeyFeatureBinding(
                HotkeyGesture.Parse("P"),
                new TrainerFeature("Select Unit Level UP", "选择的单位快速升级", "P", [], "MustCode2+700", "0x08"),
                () => invoked++)],
            enabled: true);

        Assert.True(dispatcher.TryDispatch(0x50, HotkeyModifiers.None));
        Assert.True(dispatcher.TryDispatch(0x50, HotkeyModifiers.None));
        dispatcher.Release(0x50);
        Assert.True(dispatcher.TryDispatch(0x50, HotkeyModifiers.None));

        Assert.Equal(2, invoked);
    }

    [Fact]
    public void TryDispatchCanTriggerMultipleSourceTrainerActionsOnSameHotkey()
    {
        var dispatcher = new HotkeyFeatureDispatcher();
        var invoked = new List<string>();
        dispatcher.Update(
            [
                new HotkeyFeatureBinding(
                    HotkeyGesture.Parse("/"),
                    new TrainerFeature("Select Unit Change ID", "选择的建筑物/单位ID", "/", [], "MustCode2+800", "0x09"),
                    () => invoked.Add("id")),
                new HotkeyFeatureBinding(
                    HotkeyGesture.Parse("/"),
                    new TrainerFeature("Restore Danger Level Normal", "威胁等级恢复原状", "/", ["iEnable+13"], null, "0x00"),
                    () => invoked.Add("danger")),
                new HotkeyFeatureBinding(
                    HotkeyGesture.Parse("/"),
                    new TrainerFeature("Restore Select Ore Mine", "选择的矿脉恢复采集矿量", "/", ["iEnable+14"], null, "0x01"),
                    () => invoked.Add("ore"))
            ],
            enabled: true);

        var handled = dispatcher.TryDispatch(0xBF, HotkeyModifiers.None);

        Assert.True(handled);
        Assert.Equal(["id", "danger", "ore"], invoked);
    }
}
