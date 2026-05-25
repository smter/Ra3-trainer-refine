namespace Ra3Trainer.Core.Memory;

public interface IRemoteMemoryProtector
{
    void ProtectExecuteReadWrite(nint address, int size);
}
