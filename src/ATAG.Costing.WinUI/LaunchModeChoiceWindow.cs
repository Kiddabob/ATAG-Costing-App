using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;

namespace ATAG.Costing.WinUI;

/// <summary>
/// A small first-window choice shown only for an explicitly opted-in ATAG
/// Windows profile. The selected mode exists in memory for this launch only.
/// </summary>
internal sealed class LaunchModeChoiceWindow : Window
{
    private readonly Action<AppSessionMode> _selected;
    private bool _choiceMade;

    public LaunchModeChoiceWindow(Action<AppSessionMode> selected)
    {
        _selected = selected;
        Title = "Costing App - Choose launch mode";
        AppWindow.SetIcon(Path.Combine(
            AppContext.BaseDirectory,
            AppRuntimeMode.AppIconRelativePath));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
        }

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
            Title = "Costing App",
            Subtitle = "Choose this launch only",
            IconSource = new ImageIconSource
            {
                ImageSource = new BitmapImage(
                    new Uri("ms-appx:///Assets/AppIcon.ico")),
            },
        };
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        var content = new StackPanel
        {
            Margin = new Thickness(32, 26, 32, 32),
            Spacing = 18,
        };
        content.Children.Add(new TextBlock
        {
            Text = "How would you like to open Costing App?",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(new TextBlock
        {
            Text =
                "This testing choice appears only for the opted-in Windows profile. " +
                "It does not change saved data or future launches.",
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(CreateChoiceButton(
            "ATAG version",
            "Open normally with ATAG branding, saved links, retained tables, settings, and costings.",
            AppSessionMode.Organisation,
            isPrimary: true));
        content.Children.Add(CreateChoiceButton(
            "Blank test version",
            "Open the isolated interface-only mode with no database links, cached rows, saved settings, or business defaults.",
            AppSessionMode.BlankReview,
            isPrimary: false));
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        Content = root;
        SizeAndCentreOnPointerDisplay();
    }

    private Button CreateChoiceButton(
        string title,
        string description,
        AppSessionMode mode,
        bool isPrimary)
    {
        var text = new StackPanel
        {
            Spacing = 4,
        };
        text.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        text.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });

        var button = new Button
        {
            Padding = new Thickness(18, 15, 18, 15),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = text,
        };
        if (isPrimary)
        {
            button.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[
                "AccentButtonStyle"];
        }

        AutomationProperties.SetName(button, title);
        AutomationProperties.SetHelpText(button, description);
        button.Click += (_, _) => Select(mode);
        return button;
    }

    private void Select(AppSessionMode mode)
    {
        if (_choiceMade)
        {
            return;
        }

        _choiceMade = true;
        _selected(mode);
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
        var width = Math.Min(720, workArea.Width);
        var height = Math.Min(510, workArea.Height);
        AppWindow.MoveAndResize(new RectInt32(
            workArea.X + ((workArea.Width - width) / 2),
            workArea.Y + ((workArea.Height - height) / 2),
            width,
            height));
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
