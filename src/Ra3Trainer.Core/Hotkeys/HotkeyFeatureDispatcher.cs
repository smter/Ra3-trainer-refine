using Ra3Trainer.Core.Manifest;

namespace Ra3Trainer.Core.Hotkeys;

public sealed record HotkeyFeatureBinding(
    HotkeyGesture Gesture,
    TrainerFeature Feature,
    Action Execute);

public sealed class HotkeyFeatureDispatcher
{
    private readonly HashSet<int> _pressedKeys = [];
    private IReadOnlyDictionary<HotkeyGesture, HotkeyFeatureBinding[]> _bindings =
        new Dictionary<HotkeyGesture, HotkeyFeatureBinding[]>();

    public bool Enabled { get; private set; }

    public void Update(IEnumerable<HotkeyFeatureBinding> bindings, bool enabled)
    {
        _bindings = bindings
            .GroupBy(binding => binding.Gesture)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray());
        Enabled = enabled;
        _pressedKeys.Clear();
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
        {
            _pressedKeys.Clear();
        }
    }

    public bool TryDispatch(int virtualKey, HotkeyModifiers modifiers)
    {
        if (!Enabled)
        {
            return false;
        }

        var gesture = _bindings.Keys.FirstOrDefault(key =>
            key.VirtualKey == virtualKey && key.Modifiers == modifiers);
        if (gesture is null || !_bindings.TryGetValue(gesture, out var bindings))
        {
            return false;
        }

        if (!_pressedKeys.Add(virtualKey))
        {
            return true;
        }

        foreach (var binding in bindings)
        {
            binding.Execute();
        }

        return true;
    }

    public void Release(int virtualKey)
    {
        _pressedKeys.Remove(virtualKey);
    }
}
