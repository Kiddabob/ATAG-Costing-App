using Microsoft.UI.Xaml;
namespace ATAG.Costing.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Microsoft.UI.Xaml.Application
{
    private LaunchModeChoiceWindow? _launchModeChoiceWindow;

    /// <summary>
    /// The main application window. Use <c>App.Window</c> from any class that needs
    /// the window reference (for dialogs, pickers, interop, etc.).
    /// </summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>
    /// The UI thread dispatcher. Use <c>App.DispatcherQueue</c> to marshal calls
    /// to the UI thread. Fully qualified to avoid CS0104 ambiguity with
    /// <see cref="Windows.System.DispatcherQueue"/>.
    /// </summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    /// <summary>
    /// The native window handle (HWND). Use for file pickers,
    /// <c>DataTransferManager</c>, and any WinRT interop that requires
    /// <c>InitializeWithWindow</c>.
    /// </summary>
    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        Program.Log("App constructor entered.");
        InitializeComponent();
        Program.Log("App XAML initialized.");

        UnhandledException += (_, eventArgs) =>
            Program.Log($"Unhandled WinUI exception: {eventArgs.Exception}");
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Program.Log("OnLaunched entered.");

        try
        {
            DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (AppRuntimeMode.ShouldOfferLaunchModeChoice)
            {
                Program.Log(
                    "Per-user launch mode chooser enabled for this Windows profile.");
                _launchModeChoiceWindow = new LaunchModeChoiceWindow(
                    CompleteModeChoice);
                _launchModeChoiceWindow.Activate();
                return;
            }

            OpenMainWindow();
        }
        catch (Exception exception)
        {
            Program.Log($"OnLaunched failed: {exception}");
            throw;
        }
    }

    private void CompleteModeChoice(AppSessionMode mode)
    {
        AppRuntimeMode.SelectSessionMode(mode);
        Program.Log(mode == AppSessionMode.BlankReview
            ? "Blank test session selected."
            : "ATAG session selected.");

        var choiceWindow = _launchModeChoiceWindow;
        _launchModeChoiceWindow = null;
        OpenMainWindow();
        choiceWindow?.Close();
    }

    private static void OpenMainWindow()
    {
        Window = new MainWindow();
        Program.Log(
            $"MainWindow constructed with HWND 0x{WindowHandle:X}.");
        Window.Activate();
        Program.Log(
            $"MainWindow activated with HWND 0x{WindowHandle:X}.");
    }
}
