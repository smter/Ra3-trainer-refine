using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Runtime;

namespace Ra3Trainer.Core.Features;

public sealed class FeatureController
{
    public static readonly TimeSpan DefaultDispatchTimeout = TimeSpan.FromMilliseconds(1500);
    public static readonly TimeSpan DefaultDispatchPollInterval = TimeSpan.FromMilliseconds(50);

    private readonly IProcessMemory _memory;
    private readonly AddressResolver _resolver;

    public FeatureController(IProcessMemory memory, AddressResolver resolver)
    {
        _memory = memory;
        _resolver = resolver;
    }

    public static bool IsToggleFeature(TrainerFeature feature)
    {
        return (feature.EnableFlags.Count > 0 || feature.ToggleBytePatches is { Count: > 0 }) &&
            feature.ValueHint is null;
    }

    public static bool IsActionFeature(TrainerFeature feature)
    {
        return feature.ValueHint is not null;
    }

    public void SetToggle(TrainerFeature feature, bool enabled)
    {
        if (!IsToggleFeature(feature))
        {
            throw new InvalidOperationException($"{feature.DisplayName} is not a toggle feature.");
        }

        var value = enabled ? (byte)1 : (byte)0;
        foreach (var flag in feature.EnableFlags)
        {
            _memory.WriteBytes(_resolver.Resolve(flag), stackalloc[] { value });
        }

        if (feature.ToggleBytePatches is null)
        {
            return;
        }

        foreach (var patch in feature.ToggleBytePatches)
        {
            _memory.WriteBytes(
                _resolver.Resolve(patch.Address),
                enabled ? patch.EnabledBytes : patch.DisabledBytes);
        }
    }

    public void TriggerAction(TrainerFeature feature)
    {
        TriggerAction(feature, reinforcementSettings: null);
    }

    public void TriggerAction(TrainerFeature feature, ReinforcementSettings? reinforcementSettings)
    {
        var valueHint = feature.ValueHint;
        if (valueHint is null)
        {
            throw new InvalidOperationException($"{feature.DisplayName} is not an action feature.");
        }

        var value = Convert.ToByte(valueHint[2..], 16);
        if (feature.DispatchTarget is null)
        {
            if (feature.EnableFlags.Count == 0)
            {
                throw new InvalidOperationException($"{feature.DisplayName} has no dispatch target or enable flag.");
            }

            foreach (var flag in feature.EnableFlags)
            {
                _memory.WriteBytes(_resolver.Resolve(flag), stackalloc[] { value });
            }
            return;
        }

        if (IsReinforcementFeature(feature))
        {
            WriteReinforcementSettings(reinforcementSettings ?? ReinforcementSettings.Default);
        }

        _memory.WriteBytes(_resolver.Resolve(RemoteStateLayout.ActionDispatch), stackalloc[] { value });
    }

    public async Task<ActionDispatchResult> TriggerActionAndWaitForConsumptionAsync(
        TrainerFeature feature,
        ReinforcementSettings? reinforcementSettings = null,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        Action? onDispatched = null,
        CancellationToken cancellationToken = default)
    {
        TriggerAction(feature, reinforcementSettings);
        onDispatched?.Invoke();

        if (feature.DispatchTarget is null)
        {
            return ActionDispatchResult.NotRequired;
        }

        var effectiveTimeout = timeout ?? DefaultDispatchTimeout;
        var effectivePollInterval = pollInterval ?? DefaultDispatchPollInterval;
        var deadline = DateTimeOffset.UtcNow + effectiveTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ReadActionDispatch() == 0)
            {
                return ActionDispatchResult.Consumed;
            }

            await Task.Delay(effectivePollInterval, cancellationToken).ConfigureAwait(false);
        }

        return ReadActionDispatch() == 0
            ? ActionDispatchResult.Consumed
            : ActionDispatchResult.TimedOut;
    }

    public void WriteReinforcementSettings(ReinforcementSettings settings)
    {
        WriteUInt32(RemoteStateLayout.ReinforcementUnitId, settings.UnitId);
        WriteUInt32(RemoteStateLayout.ReinforcementCount, unchecked((uint)settings.Count));
        WriteUInt32(RemoteStateLayout.ReinforcementRank, unchecked((uint)settings.Rank));
    }

    public void WriteResourceValues(ResourceValueSettings settings)
    {
        WriteUInt32(RemoteStateLayout.MoneyAmount, unchecked((uint)settings.MoneyAmount));
        WriteUInt32(RemoteStateLayout.PowerValue, unchecked((uint)settings.PowerValue));
        WriteUInt32(RemoteStateLayout.ScPointValue, unchecked((uint)settings.ScPointValue));
    }

    public uint ReadSelectedUnitCode()
    {
        var bytes = _memory.ReadBytes(_resolver.Resolve(RemoteStateLayout.SelectedUnitCode), 4);
        return BitConverter.ToUInt32(bytes);
    }

    public byte ReadActionDispatch()
    {
        return _memory.ReadBytes(_resolver.Resolve(RemoteStateLayout.ActionDispatch), 1)[0];
    }

    public void Reset(TrainerFeature feature)
    {
        if (IsToggleFeature(feature))
        {
            SetToggle(feature, false);
            return;
        }

        if (!IsActionFeature(feature))
        {
            return;
        }

        if (feature.DispatchTarget is not null)
        {
            _memory.WriteBytes(_resolver.Resolve(RemoteStateLayout.ActionDispatch), stackalloc[] { (byte)0 });
        }

        foreach (var flag in feature.EnableFlags)
        {
            _memory.WriteBytes(_resolver.Resolve(flag), stackalloc[] { (byte)0 });
        }
    }

    private static bool IsReinforcementFeature(TrainerFeature feature)
    {
        return feature.RawName.Equals("We Need Back", StringComparison.Ordinal) ||
            string.Equals(feature.DispatchTarget, "MustCode2+B00", StringComparison.OrdinalIgnoreCase);
    }

    private void WriteUInt32(string address, uint value)
    {
        _memory.WriteBytes(_resolver.Resolve(address), BitConverter.GetBytes(value));
    }
}
