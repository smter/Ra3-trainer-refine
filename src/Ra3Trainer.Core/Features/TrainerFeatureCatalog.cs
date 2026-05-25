using Ra3Trainer.Core.Manifest;

namespace Ra3Trainer.Core.Features;

public static class TrainerFeatureCatalog
{
    private static readonly IReadOnlyDictionary<string, FeatureOverride> SourceTrainerOverrides =
        new Dictionary<string, FeatureOverride>(StringComparer.Ordinal)
        {
            ["Moeny"] = new("增加玩家战场资金", "Ctrl+F1", null, null, null, false),
            ["Power"] = new("无限电力", "Ctrl+F2", null, null, null, false, true),
            ["SC POINT"] = new("无限秘密协议点数", "Ctrl+F3", null, null, null, false),
            ["HAVE ALL SC"] = new("解开所有秘密协议技能", "Ctrl+F4", null, null, null, false),
            ["FAST BUILD"] = new("快速建造建筑物/单位", "Ctrl+F5", null, null, null, false),
            ["SUPER POWER"] = new("秘密协议技能与超级武器快速冷却", "Ctrl+F6", null, null, null, false),
            ["Disable ALL SP"] = new("禁止使用技能", "Ctrl+F7", null, null, null, false),
            ["Zoom"] = new("无限缩放", "Ctrl+F8", null, null, null, false),
            ["MAP"] = new("消散战争迷雾", "Ctrl+F9", null, null, null, false),
            ["Enemy Can't Build"] = new("禁止电脑建造建筑物/单位", "Ctrl+F10", null, null, null, false),
            ["Player God Mode"] = new("玩家全建筑物/单位无敌", "Ctrl+F11", null, null, null, false),
            ["Player One Kill Mode"] = new("一击必杀对方建筑物/单位", "Ctrl+F12", null, null, null, false),
            ["Select Unit Level UP"] = new("选择的单位快速升级", "P", null, null, null, false),
            ["Select Unit Super Speed"] = new("选择的单位高速移动", "-", null, null, null, false),
            ["Select Unit Slow Speed"] = new("选择的单位缓慢移动", "=", null, null, null, false),
            ["Select Unit Freeze"] = new("选择的单位暂停", "Page Up", null, null, null, false),
            ["Restore Select Unit Speed"] = new("选择的单位恢复速度", "Page Down", null, null, null, false),
            ["Select Unit HP MAX"] = new("选择的建筑物/单位无限生命值", "[", null, null, null, false),
            ["Select Unit HP MIN"] = new("选择的建筑物/单位生命值变为1", "]", null, null, null, false),
            ["Restore Select Unit Normal HP"] = new("选择的建筑物/单位恢复原本的生命值", "\\", null, null, null, false),
            ["Select Unit Ammo MAX"] = new("选择的单位无限弹药/炸弹", ";", ["iEnable+12"], null, null, false, true),
            ["Select Unit Ammo MAX 2"] = new(null, null, null, null, null, true),
            ["Select Unit Change ID"] = new("选择的建筑物/单位ID", "/", null, null, null, false),
            ["Destory Select Unit"] = new("摧毁选择的建筑物/单位", "Delete", null, null, null, false),
            ["Danger Level MAX"] = new("威胁等级最大", ",", null, null, null, false),
            ["Danger Level MIN"] = new("威胁等级最高", ".", null, null, null, false),
            ["Restore Danger Level Normal"] = new("威胁等级恢复原状", "/", null, null, null, false),
            ["Restore Select Ore Mine"] = new("选择的矿脉恢复采集矿量", "/", null, null, null, false),
            ["Free Build"] = new(
                "建筑物可随地建造",
                "L",
                [],
                null,
                null,
                false,
                true,
                [new TrainerFeatureBytePatch("MustCode+1201", [0xEB, 0x0C], [0x75, 0x0C])]),
            ["Get Me Base"] = new("给玩家基地车", "X", null, null, null, false),
            ["We Need Back"] = new("呼叫战场增援", "J", null, null, null, false),
            ["Select Unit Copy For Me"] = new("复制选择的建筑物/单位给玩家", "I", null, null, null, false)
        };

    public static IReadOnlyList<TrainerFeature> CreateUiFeatures(IEnumerable<TrainerFeature> features)
    {
        return features
            .Select(ApplySourceTrainerOverride)
            .Where(feature => feature is not null)
            .Cast<TrainerFeature>()
            .ToArray();
    }

    private static TrainerFeature? ApplySourceTrainerOverride(TrainerFeature feature)
    {
        if (!SourceTrainerOverrides.TryGetValue(feature.RawName, out var featureOverride))
        {
            return feature;
        }

        if (featureOverride.Hide)
        {
            return null;
        }

        return feature with
        {
            DisplayName = featureOverride.DisplayName ?? feature.DisplayName,
            Hotkey = featureOverride.Hotkey ?? feature.Hotkey,
            EnableFlags = featureOverride.EnableFlags ?? feature.EnableFlags,
            DispatchTarget = featureOverride.DispatchTarget ?? feature.DispatchTarget,
            ValueHint = featureOverride.HasValueHintOverride ? featureOverride.ValueHint : feature.ValueHint,
            ToggleBytePatches = featureOverride.ToggleBytePatches
        };
    }

    private sealed record FeatureOverride(
        string? DisplayName,
        string? Hotkey,
        IReadOnlyList<string>? EnableFlags,
        string? DispatchTarget,
        string? ValueHint,
        bool Hide,
        bool HasValueHintOverride = false,
        IReadOnlyList<TrainerFeatureBytePatch>? ToggleBytePatches = null);
}
