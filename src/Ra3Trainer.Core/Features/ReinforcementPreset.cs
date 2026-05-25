namespace Ra3Trainer.Core.Features;

public sealed record ReinforcementPreset
{
    public ReinforcementPreset(string name, uint unitId, int count, int rank)
    {
        var settings = new ReinforcementSettings(unitId, count, rank);
        Name = string.IsNullOrWhiteSpace(name) ? $"0x{unitId:X8}" : name.Trim();
        UnitId = settings.UnitId;
        Count = settings.Count;
        Rank = settings.Rank;
    }

    public string Name { get; }

    public uint UnitId { get; }

    public int Count { get; }

    public int Rank { get; }

    public ReinforcementSettings ToSettings()
    {
        return new ReinforcementSettings(UnitId, Count, Rank);
    }

    public ReinforcementQueueEntry ToQueueEntry()
    {
        return new ReinforcementQueueEntry(Name, $"0x{UnitId:X8}", Count.ToString(), Rank.ToString());
    }
}
