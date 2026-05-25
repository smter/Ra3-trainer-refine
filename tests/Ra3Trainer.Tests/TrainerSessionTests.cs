using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Patching;
using Ra3Trainer.Core.Runtime;
using Ra3Trainer.Core.Codegen;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class TrainerSessionTests
{
    [Fact]
    public void AttachRejectsUnsupportedTarget()
    {
        var manifest = MinimalManifest();
        var target = new TrainerTarget(
            ProcessName: "ra3_1.12.game",
            ModuleBase: 0x400000,
            Is32Bit: true,
            VersionSupported: false);

        var session = new TrainerSession(manifest, new FakeProcessMemory(), target);

        var result = session.Attach();

        Assert.False(result.Success);
        Assert.Contains("版本不支持", result.Message);
        Assert.False(session.CanUseFeatures);
    }

    [Fact]
    public void AttachAllocatesExpectedRemoteSymbols()
    {
        var session = new TrainerSession(
            MinimalManifest(),
            new FakeProcessMemory(),
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true),
            new SequentialRemoteAllocator(0x700000));

        var result = session.Attach();

        Assert.True(result.Success);
        Assert.Equal((nint)0x700000, session.Symbols["ID"]);
        Assert.Equal((nint)0x700100, session.Symbols["iEnable"]);
        Assert.Equal((nint)0x700200, session.Symbols["MustCode"]);
        Assert.Equal((nint)0x703200, session.Symbols["MustCode2"]);
        Assert.Contains("iEnable=0x700100", session.RemoteSymbolSummary);
    }

    [Fact]
    public void AttachAcceptsProcessNameWithoutGameExtension()
    {
        var session = new TrainerSession(
            MinimalManifest(),
            new FakeProcessMemory(),
            new TrainerTarget("ra3_1.12", 0x400000, true, true),
            new SequentialRemoteAllocator(0x700000));

        var result = session.Attach();

        Assert.True(result.Success);
    }

    [Fact]
    public void InstallPatchesWritesBootstrapCodeAndHooks()
    {
        var memory = new FakeProcessMemory();
        memory.WriteBytes(0x401000, new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 });
        var manifest = MinimalManifest(new PatchHook(
            "ra3_1.12.game+1000",
            "Test Hook",
            ["jmp MustCode"],
            "MustCode",
            "_Back",
            [],
            ["mov edx,[eax+28]", "mov eax,[edx+20]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder());

        session.Attach();
        session.InstallPatches();

        Assert.Equal(1, session.InstalledHookCount);
        Assert.Equal(new byte[] { 0xCC }, memory.ReadBytes(0x700200, 1));
        Assert.Equal(new byte[] { 0x90 }, memory.ReadBytes(0x703200, 1));
        Assert.Equal(new byte[] { 0xE9, 0xFB, 0xF1, 0x2F, 0x00, 0x90 }, memory.ReadBytes(0x401000, 6));
    }

    [Fact]
    public void InstallPatchesSuspendsTargetProcessWhileWritingRemoteCodeAndHooks()
    {
        var memory = new RecordingProcessMemory();
        memory.WriteBytes(0x401000, new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 });
        memory.Events.Clear();
        var suspender = new RecordingProcessSuspender(memory.Events);
        var manifest = MinimalManifest(new PatchHook(
            "ra3_1.12.game+1000",
            "Test Hook",
            ["jmp MustCode"],
            "MustCode",
            "_Back",
            [],
            ["mov edx,[eax+28]", "mov eax,[edx+20]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true, ProcessId: 1234),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder(),
            processSuspender: suspender);

        session.Attach();
        session.InstallPatches();

        Assert.Equal(
            [
                "suspend 1234",
                "write 0x700124",
                "write 0x700200",
                "write 0x703200",
                "write 0x401000",
                "resume 1234"
            ],
            memory.Events);
    }

    [Fact]
    public void InstallPatchesDoesNotMakeWholeImageExecuteReadWrite()
    {
        var memory = new RecordingProtectedProcessMemory();
        memory.WriteBytes(0x401000, new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 });
        memory.Events.Clear();
        var manifest = MinimalManifest(new PatchHook(
            "ra3_1.12.game+1000",
            "Test Hook",
            ["jmp MustCode"],
            "MustCode",
            "_Back",
            [],
            ["mov edx,[eax+28]", "mov eax,[edx+20]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder());

        session.Attach();
        session.InstallPatches();

        Assert.DoesNotContain(memory.Events, item => item.StartsWith("protect ", StringComparison.Ordinal));
    }

    [Fact]
    public void InstallPatchesCanLimitInstalledHookCountForDiagnostics()
    {
        var memory = new FakeProcessMemory();
        var firstOriginal = new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 };
        var secondOriginal = new byte[] { 0x8B, 0x40, 0x04, 0x8B, 0x8E, 0xB0, 0x03, 0x00, 0x00 };
        memory.WriteBytes(0x401000, firstOriginal);
        memory.WriteBytes(0x401100, secondOriginal);
        var manifest = MinimalManifest(
            new PatchHook(
                "ra3_1.12.game+1000",
                "First Hook",
                ["jmp MustCode"],
                "MustCode",
                "_BackFirst",
                [],
                ["mov edx,[eax+28]", "mov eax,[edx+20]"]),
            new PatchHook(
                "ra3_1.12.game+1100",
                "Second Hook",
                ["jmp MustCode2"],
                "MustCode2",
                "_BackSecond",
                [],
                ["mov eax,[eax+04]", "mov ecx,[esi+000003b0]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder());

        session.Attach();
        session.InstallPatches(maxHookCount: 1);

        Assert.Equal(1, session.InstalledHookCount);
        Assert.NotEqual(firstOriginal, memory.ReadBytes(0x401000, firstOriginal.Length));
        Assert.Equal(secondOriginal, memory.ReadBytes(0x401100, secondOriginal.Length));
    }

    [Fact]
    public void DisposeRestoresInstalledHooks()
    {
        var memory = new FakeProcessMemory();
        var originalBytes = new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 };
        memory.WriteBytes(0x401000, originalBytes);
        var manifest = MinimalManifest(new PatchHook(
            "ra3_1.12.game+1000",
            "Test Hook",
            ["jmp MustCode"],
            "MustCode",
            "_Back",
            [],
            ["mov edx,[eax+28]", "mov eax,[edx+20]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder());

        session.Attach();
        session.InstallPatches();
        session.Dispose();

        Assert.Equal(originalBytes, memory.ReadBytes(0x401000, originalBytes.Length));
    }

    [Fact]
    public void DisposeDoesNotThrowWhenExitedTargetMakesRestoreMemoryUnavailable()
    {
        var originalBytes = new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 };
        var memory = new ExitAfterInstallMemory();
        memory.Seed(0x401000, originalBytes);
        var manifest = MinimalManifest(new PatchHook(
            "ra3_1.12.game+1000",
            "Test Hook",
            ["jmp MustCode"],
            "MustCode",
            "_Back",
            [],
            ["mov edx,[eax+28]", "mov eax,[edx+20]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true, ProcessId: null),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder(),
            targetProcessState: new FixedTargetProcessState(isRunning: false));

        session.Attach();
        session.InstallPatches();

        var exception = Record.Exception(session.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public void DisposeStillThrowsRestoreFailureWhenTargetIsRunning()
    {
        var originalBytes = new byte[] { 0x8B, 0x50, 0x28, 0x8B, 0x42, 0x20 };
        var memory = new ExitAfterInstallMemory();
        memory.Seed(0x401000, originalBytes);
        var manifest = MinimalManifest(new PatchHook(
            "ra3_1.12.game+1000",
            "Test Hook",
            ["jmp MustCode"],
            "MustCode",
            "_Back",
            [],
            ["mov edx,[eax+28]", "mov eax,[edx+20]"]));
        var session = new TrainerSession(
            manifest,
            memory,
            new TrainerTarget("ra3_1.12.game", 0x400000, true, true, ProcessId: 1234),
            new SequentialRemoteAllocator(0x700000),
            new FakeBootstrapCodeBuilder(),
            processSuspender: NoopProcessSuspender.Instance,
            targetProcessState: new FixedTargetProcessState(isRunning: true));

        session.Attach();
        session.InstallPatches();

        var exception = Assert.Throws<InvalidOperationException>(session.Dispose);

        Assert.Contains("memory unavailable", exception.Message);
    }

    private static TrainerManifest MinimalManifest(params PatchHook[] hooks)
    {
        return new TrainerManifest(
            "ra3_1.12.game",
            [],
            new PatchManifest(hooks),
            []);
    }

    private sealed class FakeBootstrapCodeBuilder : BootstrapCodeBuilder
    {
        public override BootstrapCode Build(IReadOnlyList<string> autoAssemblerLines)
        {
            return BuildFake();
        }

        public override BootstrapCode Build(IReadOnlyList<string> autoAssemblerLines, BootstrapBuildContext context)
        {
            return BuildFake();
        }

        private static BootstrapCode BuildFake()
        {
            return new BootstrapCode([0xCC], [0x90], new Dictionary<string, byte[]>
            {
                ["iEnable+24"] = [0xA0, 0xA5, 0x86, 0x65]
            });
        }
    }

    private sealed class RecordingProcessMemory : IProcessMemory
    {
        private readonly FakeProcessMemory _inner = new();

        public List<string> Events { get; } = [];

        public byte[] ReadBytes(nint address, int count)
        {
            return _inner.ReadBytes(address, count);
        }

        public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
        {
            _inner.WriteBytes(address, bytes);
            Events.Add($"write 0x{address:X}");
        }
    }

    private sealed class RecordingProtectedProcessMemory : IProcessMemory, IRemoteMemoryProtector
    {
        private readonly FakeProcessMemory _inner = new();

        public List<string> Events { get; } = [];

        public byte[] ReadBytes(nint address, int count)
        {
            return _inner.ReadBytes(address, count);
        }

        public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
        {
            _inner.WriteBytes(address, bytes);
            Events.Add($"write 0x{address:X}");
        }

        public void ProtectExecuteReadWrite(nint address, int size)
        {
            Events.Add($"protect 0x{address:X} 0x{size:X}");
        }
    }

    private sealed class RecordingProcessSuspender : IProcessSuspender
    {
        private readonly List<string> _events;

        public RecordingProcessSuspender(List<string> events)
        {
            _events = events;
        }

        public IDisposable Suspend(int processId)
        {
            _events.Add($"suspend {processId}");
            return new ResumeScope(_events, processId);
        }

        private sealed class ResumeScope : IDisposable
        {
            private readonly List<string> _events;
            private readonly int _processId;

            public ResumeScope(List<string> events, int processId)
            {
                _events = events;
                _processId = processId;
            }

            public void Dispose()
            {
                _events.Add($"resume {_processId}");
            }
        }
    }

    private sealed class ExitAfterInstallMemory : IProcessMemory
    {
        private readonly FakeProcessMemory _inner = new();
        private int _writes;

        public byte[] ReadBytes(nint address, int count)
        {
            return _inner.ReadBytes(address, count);
        }

        public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
        {
            _writes++;
            if (_writes > 4)
            {
                throw new InvalidOperationException("simulated process memory unavailable");
            }

            _inner.WriteBytes(address, bytes);
        }

        public void Seed(nint address, ReadOnlySpan<byte> bytes)
        {
            _inner.WriteBytes(address, bytes);
        }
    }

    private sealed class FixedTargetProcessState : ITargetProcessState
    {
        private readonly bool _isRunning;

        public FixedTargetProcessState(bool isRunning)
        {
            _isRunning = isRunning;
        }

        public bool IsRunning(int? processId)
        {
            return _isRunning;
        }
    }

    private sealed class NoopProcessSuspender : IProcessSuspender
    {
        public static NoopProcessSuspender Instance { get; } = new();

        public IDisposable Suspend(int processId)
        {
            return NoopDisposable.Instance;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
