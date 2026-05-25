using Ra3Trainer.Core.Memory;

namespace Ra3Trainer.Core.Patching;

public sealed class PatchEngine
{
    private readonly IProcessMemory _memory;
    private readonly AddressResolver _resolver;
    private readonly List<(nint Address, byte[] OriginalBytes)> _installed = new();

    public PatchEngine(IProcessMemory memory, AddressResolver resolver)
    {
        _memory = memory;
        _resolver = resolver;
    }

    public int InstalledHookCount => _installed.Count;

    public void Install(IEnumerable<PatchHookPlan> hooks)
    {
        var hookList = hooks.ToArray();
        PatchInstallException? installException = null;
        try
        {
            for (var index = 0; index < hookList.Length; index++)
            {
                var hook = hookList[index];
                var address = _resolver.Resolve(hook.Address);
                try
                {
                    var currentBytes = _memory.ReadBytes(address, hook.OriginalBytes.Length);
                    var target = _resolver.Resolve(hook.Target);
                    var patchBytes = X86PatchEncoder.EncodeNearJumpWithNops(address, target, hook.PatchLength);
                    if (!currentBytes.SequenceEqual(hook.OriginalBytes))
                    {
                        if (currentBytes.SequenceEqual(patchBytes))
                        {
                            AddInstalled(address, hook.OriginalBytes);
                            continue;
                        }

                        throw new InvalidOperationException(
                            $"Original bytes do not match at {hook.Address} (absolute 0x{address:X}); " +
                            $"expected {FormatBytes(hook.OriginalBytes)}, actual {FormatBytes(currentBytes)}.");
                    }

                    _memory.WriteBytes(address, patchBytes);
                    AddInstalled(address, hook.OriginalBytes);
                }
                catch (Exception ex)
                {
                    installException = new PatchInstallException(hook, index + 1, hookList.Length, address, ex);
                    throw installException;
                }
            }
        }
        catch
        {
            try
            {
                RestoreAll();
            }
            catch (Exception restoreException) when (installException is not null)
            {
                throw new InvalidOperationException(
                    $"{installException.Message} Rollback also failed: {restoreException.Message}",
                    installException);
            }

            throw;
        }
    }

    public void RestoreAll()
    {
        for (var index = _installed.Count - 1; index >= 0; index--)
        {
            var installed = _installed[index];
            _memory.WriteBytes(installed.Address, installed.OriginalBytes);
        }
        _installed.Clear();
    }

    private void AddInstalled(nint address, byte[] originalBytes)
    {
        if (_installed.Any(installed => installed.Address == address))
        {
            return;
        }

        _installed.Add((address, originalBytes));
    }

    private static string FormatBytes(IEnumerable<byte> bytes)
    {
        return string.Join(" ", bytes.Select(value => value.ToString("X2")));
    }
}
