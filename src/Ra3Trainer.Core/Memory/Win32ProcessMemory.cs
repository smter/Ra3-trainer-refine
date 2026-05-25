using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace Ra3Trainer.Core.Memory;

public sealed class Win32ProcessMemory : IProcessMemory, IRemoteAllocator, IRemoteMemoryProtector, IDisposable
{
    private const uint ProcessVmOperation = 0x0008;
    private const uint ProcessVmRead = 0x0010;
    private const uint ProcessVmWrite = 0x0020;
    private const uint ProcessQueryInformation = 0x0400;
    private const uint RequiredProcessAccess =
        ProcessVmOperation | ProcessVmRead | ProcessVmWrite | ProcessQueryInformation;
    private const uint MemCommitReserve = 0x3000;
    private const uint PageExecuteReadWrite = 0x40;

    private readonly IWin32MemoryApi _api;
    private readonly SafeProcessHandle _handle;
    private readonly List<(nint Address, nint End)> _executeReadWriteRanges = new();

    public Win32ProcessMemory(int processId)
        : this(processId, Kernel32MemoryApi.Instance)
    {
    }

    internal Win32ProcessMemory(int processId, IWin32MemoryApi api)
    {
        _api = api;
        _handle = _api.OpenProcess(RequiredProcessAccess, false, processId);
        if (_handle.IsInvalid)
        {
            throw CreateLastWin32Exception("OpenProcess failed.");
        }
    }

    public byte[] ReadBytes(nint address, int count)
    {
        var buffer = new byte[count];
        if (!_api.ReadProcessMemory(_handle, address, buffer, buffer.Length, out var read) || read != count)
        {
            throw CreateLastWin32Exception($"ReadProcessMemory failed at 0x{address:X}.");
        }
        return buffer;
    }

    public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        var buffer = bytes.ToArray();
        if (buffer.Length == 0)
        {
            return;
        }

        if (IsInExecuteReadWriteRange(address, buffer.Length))
        {
            WriteBytesWithoutProtect(address, buffer);
            return;
        }

        if (!_api.VirtualProtectEx(_handle, address, (nuint)buffer.Length, PageExecuteReadWrite, out var oldProtect))
        {
            throw CreateLastWin32Exception($"VirtualProtectEx failed at 0x{address:X}.");
        }

        try
        {
            if (!_api.WriteProcessMemory(_handle, address, buffer, buffer.Length, out var written) || written != buffer.Length)
            {
                throw CreateLastWin32Exception($"WriteProcessMemory failed at 0x{address:X}.");
            }

            if (!_api.FlushInstructionCache(_handle, address, (nuint)buffer.Length))
            {
                throw CreateLastWin32Exception($"FlushInstructionCache failed at 0x{address:X}.");
            }
        }
        finally
        {
            _api.VirtualProtectEx(_handle, address, (nuint)buffer.Length, oldProtect, out _);
        }
    }

    public void ProtectExecuteReadWrite(nint address, int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive.");
        }

        if (!_api.VirtualProtectEx(_handle, address, (nuint)size, PageExecuteReadWrite, out _))
        {
            throw CreateLastWin32Exception($"VirtualProtectEx failed at 0x{address:X} for 0x{size:X} bytes.");
        }

        _executeReadWriteRanges.Add((address, address + size));
    }

    public Win32MemoryRegion? Query(nint address)
    {
        var size = (nuint)System.Runtime.InteropServices.Marshal.SizeOf<Win32MemoryBasicInformation>();
        var result = _api.VirtualQueryEx(_handle, address, out var info, size);
        if (result == 0)
        {
            return null;
        }

        return new Win32MemoryRegion(
            info.BaseAddress,
            info.RegionSize,
            info.AllocationProtect,
            info.State,
            info.Protect,
            info.Type);
    }

    public nint Allocate(int size)
    {
        var address = _api.VirtualAllocEx(_handle, 0, (nuint)size, MemCommitReserve, PageExecuteReadWrite);
        if (address == 0)
        {
            throw CreateLastWin32Exception("VirtualAllocEx failed.");
        }
        return address;
    }

    public void Dispose()
    {
        _handle.Dispose();
    }

    private bool IsInExecuteReadWriteRange(nint address, int size)
    {
        var end = address + size;
        return _executeReadWriteRanges.Any(range => address >= range.Address && end <= range.End);
    }

    private void WriteBytesWithoutProtect(nint address, byte[] buffer)
    {
        if (!_api.WriteProcessMemory(_handle, address, buffer, buffer.Length, out var written) || written != buffer.Length)
        {
            throw CreateLastWin32Exception($"WriteProcessMemory failed at 0x{address:X}.");
        }

        if (!_api.FlushInstructionCache(_handle, address, (nuint)buffer.Length))
        {
            throw CreateLastWin32Exception($"FlushInstructionCache failed at 0x{address:X}.");
        }
    }

    private Win32Exception CreateLastWin32Exception(string message)
    {
        var error = _api.GetLastWin32Error();
        var region = QueryContext(message);
        return new Win32Exception(error, $"{message} Win32 error {error}: {new Win32Exception(error).Message}{region}");
    }

    private string QueryContext(string message)
    {
        var marker = message.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return string.Empty;
        }

        var end = marker + 2;
        while (end < message.Length && Uri.IsHexDigit(message[end]))
        {
            end++;
        }

        if (!nint.TryParse(message[(marker + 2)..end], System.Globalization.NumberStyles.HexNumber, null, out var address))
        {
            return string.Empty;
        }

        var region = Query(address);
        return region is null ? " Memory region: unavailable." : $" Memory region: {region}.";
    }
}
