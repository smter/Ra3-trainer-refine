using System.Diagnostics;

namespace Ra3Trainer.Core.Runtime;

public sealed class GameLauncher
{
    public Process Start(string launcherPath, string arguments = "")
    {
        if (string.IsNullOrWhiteSpace(launcherPath))
        {
            throw new ArgumentException("Launcher path is required.", nameof(launcherPath));
        }

        if (!File.Exists(launcherPath))
        {
            throw new FileNotFoundException("Launcher file was not found.", launcherPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = launcherPath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(launcherPath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start launcher process.");
    }
}
