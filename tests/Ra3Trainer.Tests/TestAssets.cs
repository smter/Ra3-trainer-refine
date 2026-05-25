using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Runtime;

namespace Ra3Trainer.Tests;

internal static class TestAssets
{
    public static TrainerManifest LoadManifest()
    {
        return TrainerRuntimeAssets.LoadManifest();
    }

    public static string[] ReadBootstrapLines()
    {
        return TrainerRuntimeAssets.ReadBootstrapLines().ToArray();
    }
}
