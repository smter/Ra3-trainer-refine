using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Runtime;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class FeatureControllerTests
{
    [Fact]
    public void IsToggleFeatureRequiresEnableFlagsWithoutValueHint()
    {
        var zoom = new TrainerFeature("Zoom", "Zoom", "Ctrl+F8", ["iEnable+F", "iEnable+10"], null, null);
        var danger = new TrainerFeature("Danger Level MIN", "Danger Level MIN", "#190", ["iEnable+13"], null, "0x02");

        Assert.True(FeatureController.IsToggleFeature(zoom));
        Assert.False(FeatureController.IsToggleFeature(danger));
        Assert.True(FeatureController.IsActionFeature(danger));
    }

    [Fact]
    public void ToggleWritesAllEnableFlags()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Zoom", "Zoom", "Ctrl+F8", ["iEnable+F", "iEnable+10"], null, null);

        controller.SetToggle(feature, true);

        Assert.Equal(1, memory.ReadByte(0x500F));
        Assert.Equal(1, memory.ReadByte(0x5010));

        controller.SetToggle(feature, false);

        Assert.Equal(0, memory.ReadByte(0x500F));
        Assert.Equal(0, memory.ReadByte(0x5010));
    }

    [Fact]
    public void ToggleWritesRemoteBytePatches()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["MustCode"] = 0x7000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature(
            "Free Build",
            "建筑物可随地建造",
            "L",
            [],
            null,
            null,
            [new TrainerFeatureBytePatch("MustCode+1201", [0xEB, 0x0C], [0x75, 0x0C])]);

        controller.SetToggle(feature, true);

        Assert.Equal(0xEB, memory.ReadByte(0x8201));
        Assert.Equal(0x0C, memory.ReadByte(0x8202));

        controller.SetToggle(feature, false);

        Assert.Equal(0x75, memory.ReadByte(0x8201));
        Assert.Equal(0x0C, memory.ReadByte(0x8202));
    }

    [Fact]
    public void TriggerActionWritesDispatchValueToIEnable20()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Select Unit HP MAX", "Select Unit HP MAX", "#219", [], "MustCode2+400", "0x05");

        controller.TriggerAction(feature);

        Assert.Equal(0x05, memory.ReadByte(0x5020));
    }

    [Fact]
    public void RemoteStateLayoutMatchesBootstrapOffsets()
    {
        Assert.Equal("iEnable+20", RemoteStateLayout.ActionDispatch);
        Assert.Equal("iEnable+24", RemoteStateLayout.ReinforcementUnitId);
        Assert.Equal("iEnable+28", RemoteStateLayout.ReinforcementCount);
        Assert.Equal("iEnable+2C", RemoteStateLayout.ReinforcementRank);
        Assert.Equal("iEnable+30", RemoteStateLayout.MoneyAmount);
        Assert.Equal("iEnable+34", RemoteStateLayout.PowerValue);
        Assert.Equal("iEnable+38", RemoteStateLayout.ScPointValue);
        Assert.Equal("ra3_1.12.game+8E9838", RemoteStateLayout.SelectedUnitCode);
    }

    [Fact]
    public void ReadActionDispatchReadsIEnable20()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        memory.WriteBytes(0x5020, [0x0C]);

        var value = controller.ReadActionDispatch();

        Assert.Equal(0x0C, value);
    }

    [Fact]
    public async Task TriggerActionAndWaitForConsumptionReturnsConsumedWhenDispatchIsCleared()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Select Unit HP MAX", "Select Unit HP MAX", "#219", [], "MustCode2+400", "0x05");

        var result = await controller.TriggerActionAndWaitForConsumptionAsync(
            feature,
            reinforcementSettings: null,
            timeout: TimeSpan.FromMilliseconds(500),
            pollInterval: TimeSpan.FromMilliseconds(10),
            onDispatched: () => memory.WriteBytes(0x5020, [0x00]));

        Assert.Equal(ActionDispatchResult.Consumed, result);
        Assert.Equal(0x00, memory.ReadByte(0x5020));
    }

    [Fact]
    public async Task TriggerActionAndWaitForConsumptionReturnsTimedOutWhenDispatchStaysPending()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Select Unit HP MAX", "Select Unit HP MAX", "#219", [], "MustCode2+400", "0x05");

        var result = await controller.TriggerActionAndWaitForConsumptionAsync(
            feature,
            reinforcementSettings: null,
            timeout: TimeSpan.FromMilliseconds(20),
            pollInterval: TimeSpan.FromMilliseconds(5));

        Assert.Equal(ActionDispatchResult.TimedOut, result);
        Assert.Equal(0x05, memory.ReadByte(0x5020));
    }

    [Fact]
    public async Task TriggerActionAndWaitForConsumptionReturnsNotRequiredForFlagAction()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Danger Level MIN", "Danger Level MIN", "#190", ["iEnable+13"], null, "0x02");

        var result = await controller.TriggerActionAndWaitForConsumptionAsync(
            feature,
            reinforcementSettings: null,
            timeout: TimeSpan.FromMilliseconds(20),
            pollInterval: TimeSpan.FromMilliseconds(5));

        Assert.Equal(ActionDispatchResult.NotRequired, result);
        Assert.Equal(0x02, memory.ReadByte(0x5013));
        Assert.Equal(0x00, memory.ReadByte(0x5020));
    }

    [Fact]
    public void TriggerActionWritesReinforcementSettingsBeforeDispatch()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("We Need Back", "呼叫战场增援", "J", [], "MustCode2+B00", "0x0C");
        var settings = new ReinforcementSettings(0x12345678, 12, 2);

        controller.TriggerAction(feature, settings);

        Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, memory.ReadBytes(0x5024, 4));
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x00, 0x00 }, memory.ReadBytes(0x5028, 4));
        Assert.Equal(new byte[] { 0x02, 0x00, 0x00, 0x00 }, memory.ReadBytes(0x502C, 4));
        Assert.Equal(0x0C, memory.ReadByte(0x5020));
    }

    [Fact]
    public void WriteResourceValuesWritesRuntimeParameters()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var settings = new ResourceValueSettings(123456, 234567, 9);

        controller.WriteResourceValues(settings);

        Assert.Equal(BitConverter.GetBytes(123456), memory.ReadBytes(0x5030, 4));
        Assert.Equal(BitConverter.GetBytes(234567), memory.ReadBytes(0x5034, 4));
        Assert.Equal(BitConverter.GetBytes(9), memory.ReadBytes(0x5038, 4));
    }

    [Fact]
    public void ReadSelectedUnitCodeReadsBlueprintHashFromTargetModule()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0x400000, new Dictionary<string, nint>());
        var controller = new FeatureController(memory, resolver);
        memory.WriteBytes(0xCE9838, [0xA0, 0xA5, 0x86, 0x65]);

        var code = controller.ReadSelectedUnitCode();

        Assert.Equal(0x6586A5A0u, code);
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0x6586A5A0, 0, 3)]
    [InlineData(0x6586A5A0, 51, 3)]
    [InlineData(0x6586A5A0, 8, 4)]
    public void ReinforcementSettingsRejectsInvalidValues(uint unitId, int count, int rank)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReinforcementSettings(unitId, count, rank));
    }

    [Theory]
    [InlineData("0x6586A5A0", "8", "3", 0x6586A5A0u, 8, 3)]
    [InlineData("6586A5A0", "1", "0", 0x6586A5A0u, 1, 0)]
    [InlineData(" 0x12345678 ", "50", "2", 0x12345678u, 50, 2)]
    public void ReinforcementSettingsParsesUiInput(
        string unitIdText,
        string countText,
        string rankText,
        uint unitId,
        int count,
        int rank)
    {
        var settings = ReinforcementSettings.Parse(unitIdText, countText, rankText);

        Assert.Equal(unitId, settings.UnitId);
        Assert.Equal(count, settings.Count);
        Assert.Equal(rank, settings.Rank);
    }

    [Theory]
    [InlineData("", "8", "3")]
    [InlineData("xyz", "8", "3")]
    [InlineData("0x6586A5A0", "many", "3")]
    [InlineData("0x6586A5A0", "8", "max")]
    public void ReinforcementSettingsRejectsInvalidUiInput(string unitIdText, string countText, string rankText)
    {
        Assert.Throws<FormatException>(() => ReinforcementSettings.Parse(unitIdText, countText, rankText));
    }

    [Fact]
    public void TriggerActionWritesValueHintToEnableFlagWhenNoDispatchTargetExists()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Danger Level MIN", "Danger Level MIN", "#190", ["iEnable+13"], null, "0x02");

        controller.TriggerAction(feature);

        Assert.Equal(0x02, memory.ReadByte(0x5013));
        Assert.Equal(0x00, memory.ReadByte(0x5020));
    }

    [Fact]
    public void ResetClearsToggleFlags()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var feature = new TrainerFeature("Zoom", "Zoom", "Ctrl+F8", ["iEnable+F", "iEnable+10"], null, null);
        controller.SetToggle(feature, true);

        controller.Reset(feature);

        Assert.Equal(0, memory.ReadByte(0x500F));
        Assert.Equal(0, memory.ReadByte(0x5010));
    }

    [Fact]
    public void ResetClearsDispatchAndActionFlags()
    {
        var memory = new FakeProcessMemory();
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 });
        var controller = new FeatureController(memory, resolver);
        var dispatch = new TrainerFeature("Select Unit HP MAX", "Select Unit HP MAX", "#219", [], "MustCode2+400", "0x05");
        var flagAction = new TrainerFeature("Danger Level MIN", "Danger Level MIN", "#190", ["iEnable+13"], null, "0x02");
        controller.TriggerAction(dispatch);
        controller.TriggerAction(flagAction);

        controller.Reset(dispatch);
        controller.Reset(flagAction);

        Assert.Equal(0, memory.ReadByte(0x5020));
        Assert.Equal(0, memory.ReadByte(0x5013));
    }
}
