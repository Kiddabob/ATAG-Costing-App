using System.ComponentModel;
using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI;

public sealed partial class BraidCalculatorView : UserControl
{
    private INotifyPropertyChanged? _observedViewModel;

    public BraidCalculatorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ObserveViewModel(DataContext as BraidCoverageViewModel);
        UpdateRecommendationStyling();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ObserveViewModel(null);
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ObserveViewModel(args.NewValue as BraidCoverageViewModel);
        UpdateRecommendationStyling();
    }

    private void ObserveViewModel(BraidCoverageViewModel? viewModel)
    {
        if (_observedViewModel is not null)
        {
            _observedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _observedViewModel = viewModel;

        if (_observedViewModel is not null)
        {
            _observedViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BraidCoverageViewModel.IsSixteenCarrierRecommended)
            or nameof(BraidCoverageViewModel.CalculationStatus))
        {
            DispatcherQueue.TryEnqueue(UpdateRecommendationStyling);
        }
    }

    private void UpdateRecommendationStyling()
    {
        if (DataContext is not BraidCoverageViewModel viewModel)
        {
            return;
        }

        var hasResult = viewModel.CalculationStatus.StartsWith("Live", StringComparison.OrdinalIgnoreCase);
        var sixteenRecommended = hasResult && viewModel.IsSixteenCarrierRecommended;
        var twentyFourRecommended = hasResult && !viewModel.IsSixteenCarrierRecommended;

        SixteenCarrierResultCard.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[
            sixteenRecommended ? "ModuleRecommendedOptionCardStyle" : "ModuleBackgroundCardStyle"];
        TwentyFourCarrierResultCard.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[
            twentyFourRecommended ? "ModuleRecommendedOptionCardStyle" : "ModuleBackgroundCardStyle"];
        SixteenRecommendationBadge.Visibility = sixteenRecommended ? Visibility.Visible : Visibility.Collapsed;
        TwentyFourRecommendationBadge.Visibility = twentyFourRecommended ? Visibility.Visible : Visibility.Collapsed;
    }
}
