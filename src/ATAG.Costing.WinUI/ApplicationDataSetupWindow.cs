using System.Runtime.InteropServices;
using ATAG.Costing.Application.Preferences;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.Storage.Pickers;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Resolves application-owned business data before MainPage constructs its
/// stores. Costing documents remain a separate, user-selected location.
/// </summary>
internal sealed class ApplicationDataSetupWindow : Window
{
    private readonly bool _isOrganisationManaged;
    private readonly Action<string> _selected;
    private readonly TextBlock _status;
    private bool _choiceMade;

    public ApplicationDataSetupWindow(
        bool isOrganisationManaged,
        Action<string> selected)
    {
        _isOrganisationManaged = isOrganisationManaged;
        _selected = selected;
        Title = "Costing App - Application data";
        AppWindow.SetIcon(Path.Combine(
            AppContext.BaseDirectory,
            AppRuntimeMode.AppIconRelativePath));

        ExtendsContentIntoTitleBar = true;
        var root = new Grid
        {
            Background = ResourceBrush("SolidBackgroundFillColorBaseBrush"),
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star),
        });

        var titleBar = new TitleBar
        {
            Title = AppRuntimeMode.ProductName,
            Subtitle = "Application-data location",
        };
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        var content = new StackPanel
        {
            Margin = new Thickness(32),
            Spacing = 16,
        };
        content.Children.Add(new TextBlock
        {
            Text = isOrganisationManaged
                ? "Connect to ATAG shared data"
                : "Choose where application data is stored",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(new TextBlock
        {
            Text = isOrganisationManaged
                ? "ATAG-detected sessions use the managed network folder for retained central tables and production-speed runs."
                : "This stores retained reference tables and production-speed runs. You will choose where generated costings, quotes and reports are saved separately.",
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(new TextBlock
        {
            Text = isOrganisationManaged
                ? ApplicationDataLocationPolicy.OrganisationSharedRoot
                : "No application-data folder is currently available.",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });

        _status = new TextBlock
        {
            Text = isOrganisationManaged
                ? "The network folder is unavailable. Connect to the ATAG network, then retry."
                : "Choose a shared folder, or keep the application data on this PC.",
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        };
        content.Children.Add(_status);

        if (isOrganisationManaged)
        {
            content.Children.Add(CreateButton(
                "Retry connection",
                RetryOrganisationConnection,
                isPrimary: true));
        }
        else
        {
            content.Children.Add(CreateButton(
                "Choose application-data folder",
                ChooseFolder,
                isPrimary: true));
            content.Children.Add(CreateButton(
                "Use this PC",
                UseLocalFolder,
                isPrimary: false));
        }

        var scroller = new ScrollViewer
        {
            Content = content,
            HorizontalScrollMode = ScrollMode.Disabled,
            VerticalScrollMode = ScrollMode.Auto,
        };
        Grid.SetRow(scroller, 1);
        root.Children.Add(scroller);
        Content = root;
        SizeAndCentreOnPointerDisplay();
    }

    private Button CreateButton(
        string label,
        RoutedEventHandler handler,
        bool isPrimary)
    {
        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(16, 12, 16, 12),
        };
        if (isPrimary)
        {
            button.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[
                "AccentButtonStyle"];
        }

        AutomationProperties.SetName(button, label);
        button.Click += handler;
        return button;
    }

    private void RetryOrganisationConnection(object sender, RoutedEventArgs e) =>
        TryComplete(ApplicationDataLocationPolicy.OrganisationSharedRoot);

    private async void ChooseFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = "Use for application data",
            };
            picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(this));
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                TryComplete(folder.Path);
            }
        }
        catch (Exception exception)
        {
            Program.Log($"Application-data folder selection failed: {exception}");
            _status.Text = "That folder could not be used. Please choose another location.";
        }
    }

    private void UseLocalFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(ApplicationDataLocationPolicy.LocalDefaultRoot);
            TryComplete(ApplicationDataLocationPolicy.LocalDefaultRoot);
        }
        catch (Exception exception)
        {
            Program.Log($"Local application-data setup failed: {exception}");
            _status.Text = "The local folder could not be prepared.";
        }
    }

    private void TryComplete(string root)
    {
        if (_choiceMade)
        {
            return;
        }

        if (!StorageLocationPolicy.IsAvailable(root))
        {
            _status.Text = _isOrganisationManaged
                ? "The ATAG network folder is still unavailable. Check the network connection and retry."
                : "That folder is no longer available. Please choose it again.";
            return;
        }

        _choiceMade = true;
        _selected(root);
    }

    private void SizeAndCentreOnPointerDisplay()
    {
        var displayArea = GetCursorPos(out var cursor)
            ? DisplayArea.GetFromPoint(
                new PointInt32(cursor.X, cursor.Y),
                DisplayAreaFallback.Primary)
            : DisplayArea.GetFromWindowId(
                AppWindow.Id,
                DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var width = Math.Min(720, Math.Max(420, workArea.Width - 80));
        var height = Math.Min(600, Math.Max(500, workArea.Height - 80));
        AppWindow.MoveAndResize(new RectInt32(
            workArea.X + ((workArea.Width - width) / 2),
            workArea.Y + ((workArea.Height - height) / 2),
            Math.Min(width, workArea.Width),
            Math.Min(height, workArea.Height)));
    }

    private static Brush ResourceBrush(string key) =>
        (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out CursorPoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint
    {
        public int X;
        public int Y;
    }
}
