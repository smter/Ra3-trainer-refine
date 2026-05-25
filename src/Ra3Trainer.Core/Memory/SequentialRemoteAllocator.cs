namespace Ra3Trainer.Core.Memory;

public sealed class SequentialRemoteAllocator : IRemoteAllocator
{
    private nint _next;

    public SequentialRemoteAllocator(nint startAddress)
    {
        _next = startAddress;
    }

    public nint Allocate(int size)
    {
        var address = _next;
        _next += size;
        return address;
    }
}
