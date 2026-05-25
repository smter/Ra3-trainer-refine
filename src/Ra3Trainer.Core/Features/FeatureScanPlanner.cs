using Ra3Trainer.Core.Manifest;

namespace Ra3Trainer.Core.Features;

public enum FeatureScanKind
{
    Toggle,
    Action,
    Unsupported
}

public sealed record FeatureScanItem(
    TrainerFeature Feature,
    FeatureScanKind Kind,
    string? SkipReason)
{
    public bool CanScan => SkipReason is null;
}

public static class FeatureScanPlanner
{
    public static IReadOnlyList<FeatureScanItem> Create(IEnumerable<TrainerFeature> features)
    {
        return features.Select(CreateItem).ToArray();
    }

    private static FeatureScanItem CreateItem(TrainerFeature feature)
    {
        if (FeatureController.IsToggleFeature(feature))
        {
            return new FeatureScanItem(feature, FeatureScanKind.Toggle, null);
        }

        if (FeatureController.IsActionFeature(feature))
        {
            return feature.DispatchTarget is not null || feature.EnableFlags.Count > 0
                ? new FeatureScanItem(feature, FeatureScanKind.Action, null)
                : new FeatureScanItem(
                    feature,
                    FeatureScanKind.Unsupported,
                    "缺少 dispatch target 或 enable flag。");
        }

        return new FeatureScanItem(
            feature,
            FeatureScanKind.Unsupported,
            "不是可触发的 toggle/action 功能。");
    }
}
