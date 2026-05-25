using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Runtime;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class TrainerAppSettingsStoreTests
{
    [Fact]
    public void LoadCreatesDefaultSettingsFileWhenSettingsFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Ra3Trainer.settings.json");
        var store = new TrainerAppSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(string.Empty, settings.LauncherPath);
        Assert.Equal("-win -xres 512 -yres 384", settings.LauncherArguments);
        Assert.Equal(30, settings.AttachTimeoutSeconds);
        Assert.Equal(ResourceValueSettings.Default, settings.ResourceValues);
        Assert.Empty(settings.ReinforcementPresets);
        Assert.True(File.Exists(path));

        var loadedAgain = new TrainerAppSettingsStore(path).Load();
        Assert.Equal(settings, loadedAgain);
    }

    [Fact]
    public void DefaultPathUsesApplicationDirectoryAndRa3TrainerSettingsFile()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var path = TrainerAppSettingsStore.DefaultPath(baseDirectory);

        Assert.Equal(Path.Combine(baseDirectory, "Ra3Trainer.settings.json"), path);
    }

    [Fact]
    public void SaveAndLoadRoundTripsLauncherSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new TrainerAppSettingsStore(Path.Combine(directory, "settings.json"));
        var expected = new TrainerAppSettings(
            LauncherPath: @"D:\Games\RA3.exe",
            LauncherArguments: "-win",
            AttachTimeoutSeconds: 45,
            ResourceValues: new ResourceValueSettings(123456, 234567, 9),
            ReinforcementPresets:
            [
                new ReinforcementPreset("Yuriko", 0x6586A5A0, 8, 3),
                new ReinforcementPreset("MCV", 0xAF4C0DA5, 2, 0)
            ]);

        store.Save(expected);
        var loaded = store.Load();

        Assert.Equal(expected.LauncherPath, loaded.LauncherPath);
        Assert.Equal(expected.LauncherArguments, loaded.LauncherArguments);
        Assert.Equal(expected.AttachTimeoutSeconds, loaded.AttachTimeoutSeconds);
        Assert.Equal(expected.ResourceValues, loaded.ResourceValues);
        Assert.Equal(expected.ReinforcementPresets, loaded.ReinforcementPresets);
    }

    [Fact]
    public void LoadMigratesLegacyGameExecutablePathAndDefaultsMissingArguments()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, """
            {
              "GameExecutablePath": "D:\\Games\\RA3\\RA3.exe",
              "WorkingDirectory": "D:\\Games\\RA3"
            }
            """);
        var store = new TrainerAppSettingsStore(path);

        var loaded = store.Load();

        Assert.Equal(@"D:\Games\RA3\RA3.exe", loaded.LauncherPath);
        Assert.Equal("-win -xres 512 -yres 384", loaded.LauncherArguments);
        Assert.Equal(30, loaded.AttachTimeoutSeconds);
        Assert.Equal(ResourceValueSettings.Default, loaded.ResourceValues);
        Assert.Empty(loaded.ReinforcementPresets);
    }
}
