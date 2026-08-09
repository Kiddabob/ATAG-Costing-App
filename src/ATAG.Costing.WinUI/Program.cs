using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ATAG.Costing.WinUI;

internal static class Program
{
    private static readonly string DiagnosticLogPath = Path.Combine(
        Path.GetTempPath(),
        "ATAG-Costing-startup.log");

    [STAThread]
    private static void Main(string[] args)
    {
        Log("Process entry point reached.");

        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Log("COM wrappers initialized.");

            Microsoft.UI.Xaml.Application.Start(initializationParameters =>
            {
                Log("WinUI application callback entered.");

                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                _ = new App();
                Log("App instance constructed.");
            });

            Log("WinUI application loop exited.");
        }
        catch (Exception exception)
        {
            Log(exception.ToString());
            throw;
        }
    }

    internal static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                DiagnosticLogPath,
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Startup diagnostics must never prevent the application from opening.
        }
    }
}
