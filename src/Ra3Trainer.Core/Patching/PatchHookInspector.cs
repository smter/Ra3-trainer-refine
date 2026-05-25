using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;

namespace Ra3Trainer.Core.Patching;

public sealed record PatchHookByteSnapshot(
    string Address,
    string SectionTitle,
    nint AbsoluteAddress,
    byte[] ExpectedBytes,
    byte[] ActualBytes,
    bool Matches);

public static class PatchHookInspector
{
    public static IReadOnlyList<PatchHookByteSnapshot> Capture(
        PatchManifest manifest,
        IProcessMemory memory,
        AddressResolver resolver)
    {
        return manifest.Hooks
            .Where(hook => hook.TrampolineTarget is not null)
            .Select(hook =>
            {
                var expectedBytes = OriginalByteParser.Parse(hook.OriginalAssembly);
                var address = resolver.Resolve(hook.Address);
                var actualBytes = memory.ReadBytes(address, expectedBytes.Length);
                return new PatchHookByteSnapshot(
                    hook.Address,
                    hook.SectionTitle,
                    address,
                    expectedBytes,
                    actualBytes,
                    actualBytes.SequenceEqual(expectedBytes));
            })
            .ToArray();
    }
}
