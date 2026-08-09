using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;

namespace ATAG.Costing.WinUI;

/// <summary>
/// A movable and resizable live view over the shared costing result. The window
/// displays existing view-model values and owns no costing calculations.
/// </summary>
public sealed class ResultWindow : Window
{
    private static readonly SolidColorBrush Navy =
        new(Color.FromArgb(255, 16, 42, 67));
    private static readonly SolidColorBrush Cyan =
        new(Color.FromArgb(255, 78, 207, 238));
    private static readonly SolidColorBrush Muted =
        new(Color.FromArgb(255, 175, 199, 216));

    public ResultWindow(SingleCoreCostingViewModel viewModel)
    {
        Title = $"{AppRuntimeMode.ProductName} - Live Result";
        AppWindow.Resize(new SizeInt32(760, 430));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
        }

        var root = new Grid
        {
            Padding = new Thickness(24),
            Background = Navy,
            RowSpacing = 18,
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = "ONE-CORE LIVE RESULT · ALWAYS ON TOP",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Cyan,
        };
        root.Children.Add(heading);

        var totals = new Grid
        {
            ColumnSpacing = 28,
        };
        totals.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        totals.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        totals.Children.Add(ResultStack(
            "PRICE PER METRE",
            nameof(viewModel.MarkedUpCostPerMetreDisplay),
            viewModel,
            36));
        var quote = ResultStack(
            "CUSTOMER QUOTATION",
            nameof(viewModel.ConvertedQuoteDisplay),
            viewModel,
            30);
        Grid.SetColumn(quote, 1);
        totals.Children.Add(quote);
        Grid.SetRow(totals, 1);
        root.Children.Add(totals);

        var divider = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(
                Color.FromArgb(255, 65, 97, 119)),
        };
        Grid.SetRow(divider, 2);
        root.Children.Add(divider);

        var details = new Grid
        {
            ColumnSpacing = 24,
            RowSpacing = 16,
        };
        details.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        details.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        details.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        details.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddDetail(
            details,
            0,
            0,
            "Material quote",
            nameof(viewModel.CoreMaterialQuoteDisplay),
            viewModel);
        AddDetail(
            details,
            0,
            1,
            "Labour quote",
            nameof(viewModel.LabourCostDisplay),
            viewModel);
        AddDetail(
            details,
            1,
            0,
            "Estimated cost",
            nameof(viewModel.TotalEstimatedCostDisplay),
            viewModel);
        AddDetail(
            details,
            1,
            1,
            "Cost including risk",
            nameof(viewModel.RiskAdjustedQuoteDisplay),
            viewModel);
        Grid.SetRow(details, 3);
        root.Children.Add(details);

        Content = root;
    }

    private static StackPanel ResultStack(
        string label,
        string valuePath,
        object source,
        double fontSize)
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = Muted,
        });
        stack.Children.Add(BoundValue(valuePath, source, fontSize));
        return stack;
    }

    private static void AddDetail(
        Grid grid,
        int row,
        int column,
        string label,
        string valuePath,
        object source)
    {
        var stack = ResultStack(label, valuePath, source, 20);
        Grid.SetRow(stack, row);
        Grid.SetColumn(stack, column);
        grid.Children.Add(stack);
    }

    private static TextBlock BoundValue(
        string path,
        object source,
        double fontSize)
    {
        var text = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetBinding(
            TextBlock.TextProperty,
            new Binding
            {
                Path = new PropertyPath(path),
                Source = source,
                Mode = BindingMode.OneWay,
            });
        return text;
    }
}
