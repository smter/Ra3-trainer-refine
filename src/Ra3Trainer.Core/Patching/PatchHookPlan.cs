namespace Ra3Trainer.Core.Patching;

public sealed record PatchHookPlan(
    string Address,
    string Target,
    int PatchLength,
    byte[] OriginalBytes)
{
    public string SectionTitle { get; init; } = string.Empty;
}
