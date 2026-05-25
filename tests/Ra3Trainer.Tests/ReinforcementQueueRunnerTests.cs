using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class ReinforcementQueueRunnerTests
{
    private static readonly TrainerFeature ReinforcementFeature =
        new("We Need Back", "呼叫战场增援", "J", [], "MustCode2+B00", "0x0C");

    [Fact]
    public async Task ExecuteAsyncRunsValidEntriesInOrder()
    {
        var memory = new DispatchConsumingMemory(0x5020);
        var controller = new FeatureController(
            memory,
            new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 }));
        var entries = new[]
        {
            new ReinforcementQueueEntry("first", "0x11111111", "2", "1"),
            new ReinforcementQueueEntry("second", "0x22222222", "3", "2")
        };

        var results = await ReinforcementQueueRunner.ExecuteAsync(
            entries,
            controller,
            ReinforcementFeature,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal([ReinforcementQueueItemStatus.Executed, ReinforcementQueueItemStatus.Executed], results.Select(result => result.Status));
        Assert.Equal(new uint[] { 0x11111111, 0x22222222 }, memory.UnitWrites);
    }

    [Fact]
    public async Task ExecuteAsyncSkipsInvalidEntriesAndContinues()
    {
        var memory = new DispatchConsumingMemory(0x5020);
        var controller = new FeatureController(
            memory,
            new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 }));
        var entries = new[]
        {
            new ReinforcementQueueEntry("bad", "0x0", "2", "1"),
            new ReinforcementQueueEntry("good", "0x22222222", "3", "2")
        };

        var results = await ReinforcementQueueRunner.ExecuteAsync(
            entries,
            controller,
            ReinforcementFeature,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal(ReinforcementQueueItemStatus.Skipped, results[0].Status);
        Assert.Contains("Unit id", results[0].Message);
        Assert.Equal(ReinforcementQueueItemStatus.Executed, results[1].Status);
        Assert.Equal(new uint[] { 0x22222222 }, memory.UnitWrites);
    }

    [Fact]
    public async Task ExecuteAsyncMarksTimedOutEntriesAndContinues()
    {
        var memory = new FakeProcessMemory();
        var controller = new FeatureController(
            memory,
            new AddressResolver(0, new Dictionary<string, nint> { ["iEnable"] = 0x5000 }));
        var entries = new[]
        {
            new ReinforcementQueueEntry("slow", "0x11111111", "2", "1"),
            new ReinforcementQueueEntry("also-slow", "0x22222222", "3", "2")
        };

        var results = await ReinforcementQueueRunner.ExecuteAsync(
            entries,
            controller,
            ReinforcementFeature,
            TimeSpan.FromMilliseconds(5),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal([ReinforcementQueueItemStatus.TimedOut, ReinforcementQueueItemStatus.TimedOut], results.Select(result => result.Status));
    }

    private sealed class DispatchConsumingMemory : IProcessMemory
    {
        private readonly FakeProcessMemory _inner = new();
        private readonly nint _dispatchAddress;

        public DispatchConsumingMemory(nint dispatchAddress)
        {
            _dispatchAddress = dispatchAddress;
        }

        public List<uint> UnitWrites { get; } = [];

        public byte[] ReadBytes(nint address, int count)
        {
            return _inner.ReadBytes(address, count);
        }

        public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
        {
            _inner.WriteBytes(address, bytes);
            if (address == 0x5024 && bytes.Length == 4)
            {
                UnitWrites.Add(BitConverter.ToUInt32(bytes));
            }

            if (address == _dispatchAddress && bytes.Length == 1 && bytes[0] != 0)
            {
                _inner.WriteBytes(address, [0x00]);
            }
        }
    }
}
