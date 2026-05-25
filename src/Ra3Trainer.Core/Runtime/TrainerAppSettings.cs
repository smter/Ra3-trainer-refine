using System.Text.Json;
using Ra3Trainer.Core.Features;

namespace Ra3Trainer.Core.Runtime;

public sealed record TrainerAppSettings
{
    public TrainerAppSettings(
        string LauncherPath,
        string LauncherArguments,
        int AttachTimeoutSeconds)
        : this(
            LauncherPath,
            LauncherArguments,
            AttachTimeoutSeconds,
            ResourceValueSettings.Default,
            Array.Empty<ReinforcementPreset>())
    {
    }

    public TrainerAppSettings(
        string LauncherPath,
        string LauncherArguments,
        int AttachTimeoutSeconds,
        ResourceValueSettings ResourceValues,
        IReadOnlyList<ReinforcementPreset> ReinforcementPresets)
    {
        this.LauncherPath = LauncherPath;
        this.LauncherArguments = LauncherArguments;
        this.AttachTimeoutSeconds = AttachTimeoutSeconds;
        this.ResourceValues = ResourceValues;
        this.ReinforcementPresets = ReinforcementPresets.ToArray();
    }

    public static TrainerAppSettings Default { get; } =
        new(string.Empty, "-win -xres 512 -yres 384", 30);

    public string LauncherPath { get; }

    public string LauncherArguments { get; }

    public int AttachTimeoutSeconds { get; }

    public ResourceValueSettings ResourceValues { get; }

    public IReadOnlyList<ReinforcementPreset> ReinforcementPresets { get; }
}

public sealed class TrainerAppSettingsStore
{
    public const string SettingsFileName = "Ra3Trainer.settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public TrainerAppSettingsStore(string? path = null)
    {
        _path = path ?? DefaultPath();
    }

    public TrainerAppSettings Load()
    {
        if (!File.Exists(_path))
        {
            TrySave(TrainerAppSettings.Default);
            return TrainerAppSettings.Default;
        }

        try
        {
            using var stream = File.OpenRead(_path);
            using var document = JsonDocument.Parse(stream);
            return Normalize(document.RootElement);
        }
        catch (JsonException)
        {
            return TrainerAppSettings.Default;
        }
        catch (IOException)
        {
            return TrainerAppSettings.Default;
        }
    }

    public void Save(TrainerAppSettings settings)
    {
        SaveToPath(_path, settings);
    }

    public static string DefaultPath()
    {
        return DefaultPath(AppContext.BaseDirectory);
    }

    public static string DefaultPath(string baseDirectory)
    {
        return Path.Combine(baseDirectory, SettingsFileName);
    }

    private void TrySave(TrainerAppSettings settings)
    {
        try
        {
            Save(settings);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void SaveToPath(string path, TrainerAppSettings settings)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, settings, JsonOptions);
    }

    private static TrainerAppSettings Normalize(JsonElement root)
    {
        var defaults = TrainerAppSettings.Default;
        var launcherPath = ReadString(root, nameof(TrainerAppSettings.LauncherPath))
            ?? ReadString(root, "GameExecutablePath")
            ?? defaults.LauncherPath;
        var launcherArguments = ReadString(root, nameof(TrainerAppSettings.LauncherArguments))
            ?? defaults.LauncherArguments;
        var attachTimeoutSeconds = ReadInt32(root, nameof(TrainerAppSettings.AttachTimeoutSeconds))
            ?? defaults.AttachTimeoutSeconds;
        var resourceValues = ReadResourceValues(root) ?? defaults.ResourceValues;
        var reinforcementPresets = ReadReinforcementPresets(root);

        if (string.IsNullOrWhiteSpace(launcherArguments))
        {
            launcherArguments = defaults.LauncherArguments;
        }

        if (attachTimeoutSeconds <= 0)
        {
            attachTimeoutSeconds = defaults.AttachTimeoutSeconds;
        }

        return new TrainerAppSettings(
            launcherPath,
            launcherArguments,
            attachTimeoutSeconds,
            resourceValues,
            reinforcementPresets);
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? ReadInt32(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result)
            ? result
            : null;
    }

    private static ResourceValueSettings? ReadResourceValues(JsonElement root)
    {
        if (!root.TryGetProperty(nameof(TrainerAppSettings.ResourceValues), out var value) ||
            value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var defaults = ResourceValueSettings.Default;
        var moneyAmount = ReadInt32(value, nameof(ResourceValueSettings.MoneyAmount)) ?? defaults.MoneyAmount;
        var powerValue = ReadInt32(value, nameof(ResourceValueSettings.PowerValue)) ?? defaults.PowerValue;
        var scPointValue = ReadInt32(value, nameof(ResourceValueSettings.ScPointValue)) ?? defaults.ScPointValue;
        try
        {
            return new ResourceValueSettings(moneyAmount, powerValue, scPointValue);
        }
        catch (ArgumentOutOfRangeException)
        {
            return defaults;
        }
    }

    private static IReadOnlyList<ReinforcementPreset> ReadReinforcementPresets(JsonElement root)
    {
        if (!root.TryGetProperty(nameof(TrainerAppSettings.ReinforcementPresets), out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ReinforcementPreset>();
        }

        var presets = new List<ReinforcementPreset>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = ReadString(item, nameof(ReinforcementPreset.Name)) ?? string.Empty;
            var unitId = ReadUInt32(item, nameof(ReinforcementPreset.UnitId));
            var count = ReadInt32(item, nameof(ReinforcementPreset.Count));
            var rank = ReadInt32(item, nameof(ReinforcementPreset.Rank));
            if (unitId is null || count is null || rank is null)
            {
                continue;
            }

            try
            {
                presets.Add(new ReinforcementPreset(name, unitId.Value, count.Value, rank.Value));
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        return presets;
    }

    private static uint? ReadUInt32(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetUInt32(out var result)
            ? result
            : null;
    }

}
