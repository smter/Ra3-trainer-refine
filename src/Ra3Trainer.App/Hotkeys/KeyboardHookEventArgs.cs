using Ra3Trainer.Core.Hotkeys;

namespace Ra3Trainer.App.Hotkeys;

public sealed class KeyboardHookEventArgs : EventArgs
{
    public KeyboardHookEventArgs(int virtualKey, HotkeyModifiers modifiers)
    {
        VirtualKey = virtualKey;
        Modifiers = modifiers;
    }

    public int VirtualKey { get; }

    public HotkeyModifiers Modifiers { get; }

    public bool Handled { get; set; }
}
