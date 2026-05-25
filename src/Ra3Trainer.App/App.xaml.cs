using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;

namespace Ra3Trainer.App;

public partial class App : Application
{
    public App()
    {
        ConfigureRendering();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public static void ConfigureRendering()
    {
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
        MessageBox.Show(
            e.Exception.Message,
            "RA3 Trainer 错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            WriteCrashLog(exception);
        }
    }

    private static void WriteCrashLog(Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ra3Trainer");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "crash.log"),
                $"[{DateTimeOffset.Now:O}] {exception}\n\n");
        }
        catch
        {
            // Last-resort error logging must never crash the UI.
        }
    }
}
