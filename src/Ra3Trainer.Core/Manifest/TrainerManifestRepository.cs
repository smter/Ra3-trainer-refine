using System.Text.Json;

namespace Ra3Trainer.Core.Manifest;

public static class TrainerManifestRepository
{
    private static readonly IReadOnlyDictionary<string, string[]> KnownActionFlags =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Select Unit Ammo MAX"] = ["iEnable+12"],
            ["Select Unit Ammo MAX 2"] = ["iEnable+12"],
            ["Danger Level MAX"] = ["iEnable+13"],
            ["Danger Level MIN"] = ["iEnable+13"],
            ["Restore Danger Level Normal"] = ["iEnable+13"],
            ["Restore Select Ore Mine"] = ["iEnable+14"]
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static TrainerManifest Load(string analysisDirectory)
    {
        var reportPath = Path.Combine(analysisDirectory, "trainer_report.json");
        using var stream = File.OpenRead(reportPath);
        return Load(stream, reportPath);
    }

    public static TrainerManifest Load(Stream stream, string sourceName)
    {
        var report = JsonSerializer.Deserialize<TrainerReportDto>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Unable to read trainer manifest from {sourceName}.");

        return new TrainerManifest(
            report.TrainerMetadata.TargetProcess,
            report.Features.Select(ToFeature).ToArray(),
            new PatchManifest(report.PatchManifest.Hooks.Select(ToHook).ToArray()),
            report.ActionDispatch.Select(item => new ActionDispatchEntry(item.Value, item.Target, item.Description)).ToArray());
    }

    private static TrainerFeature ToFeature(FeatureDto feature)
    {
        return new TrainerFeature(
            feature.Name,
            DisplayName(feature.Name),
            NormalizeHotkey(feature.Hotkey),
            ResolveEnableFlags(feature),
            feature.DispatchTarget,
            feature.ValueHint);
    }

    private static IReadOnlyList<string> ResolveEnableFlags(FeatureDto feature)
    {
        if (feature.EnableFlags is { Count: > 0 })
        {
            return feature.EnableFlags.ToArray();
        }

        return KnownActionFlags.TryGetValue(feature.Name, out var flags)
            ? flags
            : Array.Empty<string>();
    }

    private static PatchHook ToHook(PatchHookDto hook)
    {
        return new PatchHook(
            hook.Address,
            hook.SectionTitle,
            hook.PatchAssembly.ToArray(),
            hook.TrampolineTarget,
            hook.ReturnLabel,
            hook.EnableFlags?.ToArray() ?? Array.Empty<string>(),
            hook.OriginalAssembly.ToArray());
    }

    private static string DisplayName(string rawName)
    {
        return rawName
            .Replace("Moeny", "Money", StringComparison.Ordinal)
            .Replace("Destory", "Destroy", StringComparison.Ordinal);
    }

    private static string? NormalizeHotkey(string? hotkey)
    {
        return hotkey switch
        {
            "elete." => "Delete",
            "Page Up!" => "Page Up",
            _ => hotkey
        };
    }
}
