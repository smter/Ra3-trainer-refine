namespace Ra3Trainer.Core.Patching;

public sealed class PatchInstallException : InvalidOperationException
{
    public PatchInstallException(
        PatchHookPlan hook,
        int hookIndex,
        int hookCount,
        nint absoluteAddress,
        Exception innerException)
        : base(CreateMessage(hook, hookIndex, hookCount, absoluteAddress, innerException), innerException)
    {
        Hook = hook;
        HookIndex = hookIndex;
        HookCount = hookCount;
        AbsoluteAddress = absoluteAddress;
    }

    public PatchHookPlan Hook { get; }

    public int HookIndex { get; }

    public int HookCount { get; }

    public nint AbsoluteAddress { get; }

    private static string CreateMessage(
        PatchHookPlan hook,
        int hookIndex,
        int hookCount,
        nint absoluteAddress,
        Exception innerException)
    {
        var section = string.IsNullOrWhiteSpace(hook.SectionTitle)
            ? string.Empty
            : $" section '{hook.SectionTitle}',";
        return $"Failed to install hook {hookIndex}/{hookCount}{section} " +
            $"at {hook.Address} (absolute 0x{absoluteAddress:X}). {innerException.Message}";
    }
}
