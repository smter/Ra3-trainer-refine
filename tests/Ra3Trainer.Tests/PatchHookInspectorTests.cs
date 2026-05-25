using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Patching;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class PatchHookInspectorTests
{
    [Fact]
    public void CaptureReadsExpectedAndActualHookBytes()
    {
        var memory = new FakeProcessMemory();
        memory.WriteBytes(0x70F530, new byte[] { 0xE9, 0x11, 0x22, 0x33, 0x44 });
        var resolver = new AddressResolver(0x400000, new Dictionary<string, nint>());
        var manifest = new PatchManifest([
            new PatchHook(
                Address: "ra3_1.12.game+30F530",
                SectionTitle: "Enemy Can't Build Code",
                PatchAssembly: ["jmp MustCode+3d0"],
                TrampolineTarget: "MustCode+3d0",
                ReturnLabel: "_BackEnemyCantBuild",
                EnableFlags: ["iEnable+15"],
                OriginalAssembly: ["add edx,[eax+04]", "cmp edx,edi"])
        ]);

        var snapshot = Assert.Single(PatchHookInspector.Capture(manifest, memory, resolver));

        Assert.Equal("ra3_1.12.game+30F530", snapshot.Address);
        Assert.Equal("Enemy Can't Build Code", snapshot.SectionTitle);
        Assert.Equal((nint)0x70F530, snapshot.AbsoluteAddress);
        Assert.Equal(new byte[] { 0x03, 0x50, 0x04, 0x3B, 0xD7 }, snapshot.ExpectedBytes);
        Assert.Equal(new byte[] { 0xE9, 0x11, 0x22, 0x33, 0x44 }, snapshot.ActualBytes);
        Assert.False(snapshot.Matches);
    }
}
