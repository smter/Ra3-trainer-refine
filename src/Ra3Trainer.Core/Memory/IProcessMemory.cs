namespace Ra3Trainer.Core.Memory;

public interface IProcessMemory
{
    byte[] ReadBytes(nint address, int count);

    void WriteBytes(nint address, ReadOnlySpan<byte> bytes);
}
