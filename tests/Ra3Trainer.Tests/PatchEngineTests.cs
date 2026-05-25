using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Patching;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class PatchEngineTests
{
    [Fact]
    public void PlannerCreatesPlansForAllParsedHooks()
    {
        var manifest = TestAssets.LoadManifest();

        var plans = PatchHookPlanner.CreatePlans(manifest.PatchManifest);

        Assert.Equal(22, plans.Count);
        Assert.All(plans, plan =>
        {
            Assert.True(plan.PatchLength >= 5);
            Assert.NotEmpty(plan.OriginalBytes);
            Assert.Contains(plan.OriginalBytes, value => value != 0);
        });
    }

    [Fact]
    public void InstallWritesPatchesAndRestoreWritesOriginalBytes()
    {
        var memory = new FakeProcessMemory();
        memory.WriteBytes(0x1000, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        var hook = new PatchHookPlan(
            Address: "ra3_1.12.game+1000",
            Target: "MustCode+20",
            PatchLength: 5,
            OriginalBytes: new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["MustCode"] = 0x2000 });
        var engine = new PatchEngine(memory, resolver);

        engine.Install([hook]);

        Assert.Equal(new byte[] { 0xE9, 0x1B, 0x10, 0x00, 0x00 }, memory.ReadBytes(0x1000, 5));

        engine.RestoreAll();

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, memory.ReadBytes(0x1000, 5));
    }

    [Fact]
    public void InstallTreatsAlreadyInstalledPatchAsIdempotent()
    {
        var memory = new FakeProcessMemory();
        memory.WriteBytes(0x1000, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        var hook = new PatchHookPlan(
            Address: "ra3_1.12.game+1000",
            Target: "MustCode+20",
            PatchLength: 5,
            OriginalBytes: new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["MustCode"] = 0x2000 });
        var engine = new PatchEngine(memory, resolver);

        engine.Install([hook]);
        engine.Install([hook]);

        Assert.Equal(1, engine.InstalledHookCount);

        engine.RestoreAll();

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, memory.ReadBytes(0x1000, 5));
        Assert.Equal(0, engine.InstalledHookCount);
    }

    [Fact]
    public void InstallRollsBackAlreadyWrittenHooksWhenLaterHookFails()
    {
        var memory = new FakeProcessMemory();
        memory.WriteBytes(0x1000, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        memory.WriteBytes(0x1100, new byte[] { 0xFF, 0xEE, 0xDD, 0xCC, 0xBB });
        var resolver = new AddressResolver(0, new Dictionary<string, nint> { ["MustCode"] = 0x2000 });
        var engine = new PatchEngine(memory, resolver);

        var hooks = new[]
        {
            new PatchHookPlan("ra3_1.12.game+1000", "MustCode+20", 5, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }),
            new PatchHookPlan("ra3_1.12.game+1100", "MustCode+40", 5, new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE })
        };

        Assert.Throws<PatchInstallException>(() => engine.Install(hooks));
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, memory.ReadBytes(0x1000, 5));
    }

    [Fact]
    public void InstallReportsExpectedAndActualBytesWhenOriginalBytesMismatch()
    {
        var memory = new FakeProcessMemory();
        memory.WriteBytes(0x70F530, new byte[] { 0xE9, 0x11, 0x22, 0x33, 0x44 });
        var resolver = new AddressResolver(0x400000, new Dictionary<string, nint> { ["MustCode"] = 0x10000000 });
        var engine = new PatchEngine(memory, resolver);
        var hook = new PatchHookPlan(
            Address: "ra3_1.12.game+30F530",
            Target: "MustCode+3d0",
            PatchLength: 5,
            OriginalBytes: new byte[] { 0x03, 0x50, 0x04, 0x3B, 0xD7 });

        var exception = Assert.Throws<PatchInstallException>(() => engine.Install([hook]));

        Assert.Equal(1, exception.HookIndex);
        Assert.Equal(1, exception.HookCount);
        Assert.Equal((nint)0x70F530, exception.AbsoluteAddress);
        Assert.Contains("ra3_1.12.game+30F530", exception.Message);
        Assert.Contains("absolute 0x70F530", exception.Message);
        Assert.Contains("expected 03 50 04 3B D7", exception.Message);
        Assert.Contains("actual E9 11 22 33 44", exception.Message);
    }

    [Fact]
    public void InstallReportsHookContextWhenPatchWriteFails()
    {
        var originalBytes = new byte[] { 0x8B, 0x89, 0x3C, 0x03, 0x00, 0x00 };
        var memory = new WriteFailingProcessMemory(0x6E24E3);
        memory.Seed(0x6E24E3, originalBytes);
        var resolver = new AddressResolver(0x400000, new Dictionary<string, nint> { ["MustCode"] = 0x10000000 });
        var engine = new PatchEngine(memory, resolver);
        var hook = new PatchHookPlan(
            Address: "ra3_1.12.game+2E24E3",
            Target: "MustCode+506",
            PatchLength: 6,
            OriginalBytes: originalBytes)
        {
            SectionTitle = "Player One Kill It Mode Code"
        };

        var exception = Assert.Throws<PatchInstallException>(() => engine.Install([hook]));

        Assert.Equal(1, exception.HookIndex);
        Assert.Equal(1, exception.HookCount);
        Assert.Equal((nint)0x6E24E3, exception.AbsoluteAddress);
        Assert.Contains("hook 1/1", exception.Message);
        Assert.Contains("ra3_1.12.game+2E24E3", exception.Message);
        Assert.Contains("absolute 0x6E24E3", exception.Message);
        Assert.Contains("Player One Kill It Mode Code", exception.Message);
        Assert.Contains("simulated write failure", exception.Message);
    }

    private sealed class WriteFailingProcessMemory : IProcessMemory
    {
        private readonly Dictionary<nint, byte> _bytes = new();
        private readonly nint _failAddress;

        public WriteFailingProcessMemory(nint failAddress)
        {
            _failAddress = failAddress;
        }

        public byte[] ReadBytes(nint address, int count)
        {
            var bytes = new byte[count];
            for (var index = 0; index < count; index++)
            {
                bytes[index] = _bytes.GetValueOrDefault(address + index);
            }

            return bytes;
        }

        public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
        {
            if (address == _failAddress)
            {
                throw new InvalidOperationException($"simulated write failure at 0x{address:X}");
            }

            Seed(address, bytes.ToArray());
        }

        public void Seed(nint address, ReadOnlySpan<byte> bytes)
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                _bytes[address + index] = bytes[index];
            }
        }
    }
}
