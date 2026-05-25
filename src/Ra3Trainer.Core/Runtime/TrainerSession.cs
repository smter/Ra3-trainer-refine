using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Patching;
using Ra3Trainer.Core.Codegen;

namespace Ra3Trainer.Core.Runtime;

public sealed class TrainerSession : IDisposable
{
    private readonly TrainerManifest _manifest;
    private readonly IProcessMemory _memory;
    private readonly TrainerTarget _target;
    private readonly IRemoteAllocator _allocator;
    private readonly BootstrapCodeBuilder _codeBuilder;
    private readonly IReadOnlyList<string> _bootstrapLines;
    private readonly IProcessSuspender _processSuspender;
    private readonly ITargetProcessState _targetProcessState;
    private PatchEngine? _patchEngine;

    public TrainerSession(
        TrainerManifest manifest,
        IProcessMemory memory,
        TrainerTarget target,
        IRemoteAllocator? allocator = null,
        BootstrapCodeBuilder? codeBuilder = null,
        IReadOnlyList<string>? bootstrapLines = null,
        IProcessSuspender? processSuspender = null,
        ITargetProcessState? targetProcessState = null)
    {
        _manifest = manifest;
        _memory = memory;
        _target = target;
        _allocator = allocator ?? new SequentialRemoteAllocator(0);
        _codeBuilder = codeBuilder ?? new BootstrapCodeBuilder();
        _bootstrapLines = bootstrapLines ?? Array.Empty<string>();
        _processSuspender = processSuspender ?? Win32ProcessSuspender.Instance;
        _targetProcessState = targetProcessState ?? TargetProcessState.Instance;
    }

    public bool CanUseFeatures { get; private set; }

    public IReadOnlyDictionary<string, nint> Symbols { get; private set; } =
        new Dictionary<string, nint>();

    public AddressResolver? Resolver { get; private set; }

    public int InstalledHookCount => _patchEngine?.InstalledHookCount ?? 0;

    public string RemoteSymbolSummary =>
        Symbols.Count == 0
            ? "远程符号未分配。"
            : string.Join(", ", Symbols.Select(symbol => $"{symbol.Key}=0x{symbol.Value:X}"));

    public AttachResult Attach()
    {
        if (!TrainerProcessName.Matches(_target.ProcessName, _manifest.TargetProcess))
        {
            return Reject("目标进程不匹配。");
        }

        if (!_target.Is32Bit)
        {
            return Reject("目标进程不是 32 位。");
        }

        if (!_target.VersionSupported)
        {
            return Reject("版本不支持，仅支持 RA3 1.12.3444.25830。");
        }

        var symbols = new Dictionary<string, nint>
        {
            ["ID"] = _allocator.Allocate(0x100),
            ["iEnable"] = _allocator.Allocate(0x100),
            ["MustCode"] = _allocator.Allocate(0x3000),
            ["MustCode2"] = _allocator.Allocate(0x1000)
        };
        Symbols = symbols;
        Resolver = new AddressResolver(_target.ModuleBase, symbols);
        _patchEngine = new PatchEngine(_memory, Resolver);
        CanUseFeatures = true;
        return new AttachResult(true, "已连接 RA3 1.12.3444.25830。");
    }

    public void InstallPatches(int? maxHookCount = null)
    {
        if (!CanUseFeatures || Resolver is null || _patchEngine is null)
        {
            throw new InvalidOperationException("Attach must succeed before installing patches.");
        }

        var suspension = _target.ProcessId is int processId
            ? _processSuspender.Suspend(processId)
            : null;

        try
        {
            var plans = PatchHookPlanner.CreatePlans(_manifest.PatchManifest);
            if (maxHookCount is < 0 || maxHookCount > plans.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHookCount),
                    $"Hook count must be between 0 and {plans.Count}.");
            }

            var bootstrapCode = _codeBuilder.Build(_bootstrapLines, CreateBootstrapContext(plans));
            foreach (var initializer in bootstrapCode.Initializers)
            {
                _memory.WriteBytes(Resolver.Resolve(initializer.Key), initializer.Value);
            }
            if (bootstrapCode.MustCode.Length > 0)
            {
                _memory.WriteBytes(Resolver.Resolve("MustCode"), bootstrapCode.MustCode);
            }
            if (bootstrapCode.MustCode2.Length > 0)
            {
                _memory.WriteBytes(Resolver.Resolve("MustCode2"), bootstrapCode.MustCode2);
            }

            _patchEngine.Install(maxHookCount is int count ? plans.Take(count) : plans);
        }
        finally
        {
            suspension?.Dispose();
        }
    }

    public void Dispose()
    {
        try
        {
            _patchEngine?.RestoreAll();
        }
        catch when (!_targetProcessState.IsRunning(_target.ProcessId))
        {
            // The game has already exited, so its address space is gone and hooks cannot be restored.
        }
    }

    private AttachResult Reject(string message)
    {
        CanUseFeatures = false;
        return new AttachResult(false, message);
    }

    private BootstrapBuildContext CreateBootstrapContext(IReadOnlyList<PatchHookPlan> plans)
    {
        var labels = new Dictionary<string, nint>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < _manifest.PatchManifest.Hooks.Count; index++)
        {
            var label = _manifest.PatchManifest.Hooks[index].ReturnLabel;
            if (label is null)
            {
                continue;
            }

            var plan = plans[index];
            labels[label] = Resolver!.Resolve(plan.Address) + plan.PatchLength;
        }

        return new BootstrapBuildContext(expression => Resolver!.Resolve(expression), labels);
    }
}
