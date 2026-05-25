namespace Ra3Trainer.App.ViewModels;

public sealed record FeatureGroupViewModel(string Name, IReadOnlyList<FeatureItemViewModel> Features);
