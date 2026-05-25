namespace Ra3Trainer.Core.Memory;

public interface IRemoteAllocator
{
    nint Allocate(int size);
}
