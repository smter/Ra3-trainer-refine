using Ra3Trainer.Core.Manifest;

namespace Ra3Trainer.Core.Patching;

public static class PatchHookPlanner
{
    public static IReadOnlyList<PatchHookPlan> CreatePlans(PatchManifest manifest)
    {
        return manifest.Hooks
            .Where(hook => hook.TrampolineTarget is not null)
            .Select(hook =>
            {
                var originalBytes = OriginalByteParser.Parse(hook.OriginalAssembly);
                return new PatchHookPlan(
                    hook.Address,
                    hook.TrampolineTarget!,
                    Math.Max(5, originalBytes.Length),
                    originalBytes)
                {
                    SectionTitle = hook.SectionTitle
                };
            })
            .ToArray();
    }
}
