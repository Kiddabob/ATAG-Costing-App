using System.Globalization;
using ATAG.Costing.Application.Production;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI;

public sealed partial class MainPage
{
    private async void AddProductionLine_Click(
        object sender,
        RoutedEventArgs e)
    {
        var result = await ShowProductionLineDialogAsync(
            "Add production line",
            "",
            null);
        if (result is null)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryAddLine(
                result.Value.Name,
                result.Value.AboveMaximumSpeed,
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Line could not be added",
                message);
        }
    }

    private async void AddGeneralProductionStarterProfile_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Add the general insulation starter profile?",
            Content =
                "This adds the general finished-OD bands (1.00, 1.20, 2.00 and 2.50 mm) as an editable local production line. It is not a substitute for the actual line's measured capability.",
            PrimaryButtonText = "Add starter profile",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryAddGeneralStarterProfile(
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Starter profile was not added",
                message);
        }
    }

    private async void EditProductionLine_Click(
        object sender,
        RoutedEventArgs e)
    {
        var line = ProductionSpeedViewModel.SelectedLine;
        if (line is null)
        {
            await ShowProductionSpeedMessageAsync(
                "Choose a production line",
                "Select an existing line or add a new one first.");
            return;
        }

        var result = await ShowProductionLineDialogAsync(
            "Edit production line",
            line.Name,
            line.AboveMaximumLineSpeedMetresPerHour);
        if (result is null)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryUpdateSelectedLine(
                result.Value.Name,
                result.Value.AboveMaximumSpeed,
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Line could not be updated",
                message);
        }
    }

    private async void DeleteProductionLine_Click(
        object sender,
        RoutedEventArgs e)
    {
        var line = ProductionSpeedViewModel.SelectedLine;
        if (line is null)
        {
            await ShowProductionSpeedMessageAsync(
                "Choose a production line",
                "Select an existing line first.");
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Delete {line.Name}?",
            Content =
                "Its OD bands and known cable runs will be removed from this user's private library. Saved costings are not changed.",
            PrimaryButtonText = "Delete line",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        ProductionSpeedViewModel.TryDeleteSelectedLine(out _);
    }

    private async void AddProductionSpeedBand_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ProductionSpeedViewModel.SelectedLine is null)
        {
            await ShowProductionSpeedMessageAsync(
                "Choose a production line",
                "Select or add a production line before adding OD bands.");
            return;
        }

        var result = await ShowProductionSpeedBandDialogAsync(
            "Add OD speed band",
            maximumOutsideDiameter: null,
            lineSpeed: null);
        if (result is null)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryAddBand(
                result.Value.MaximumOutsideDiameter,
                result.Value.LineSpeed,
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Band could not be added",
                message);
        }
    }

    private async void EditProductionSpeedBand_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement
            {
                Tag: ProductionSpeedBandDefinition band,
            })
        {
            return;
        }

        var result = await ShowProductionSpeedBandDialogAsync(
            "Edit OD speed band",
            band.MaximumFinishedOutsideDiameterMillimetres,
            band.LineSpeedMetresPerHour);
        if (result is null)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryUpdateBand(
                band.Id,
                result.Value.MaximumOutsideDiameter,
                result.Value.LineSpeed,
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Band could not be updated",
                message);
        }
    }

    private async void DeleteProductionSpeedBand_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement
            {
                Tag: ProductionSpeedBandDefinition band,
            })
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Delete this OD band?",
            Content =
                $"Finished OD ≤ {band.MaximumFinishedOutsideDiameterMillimetres:0.###} mm at {band.LineSpeedMetresPerHour:N0} m/h will be removed.",
            PrimaryButtonText = "Delete band",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ProductionSpeedViewModel.TryDeleteBand(band.Id, out _);
        }
    }

    private async void AddProductionRunObservation_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ProductionSpeedViewModel.SelectedLine is null)
        {
            await ShowProductionSpeedMessageAsync(
                "Choose a production line",
                "Select or add a production line before adding a known cable run.");
            return;
        }

        var observation = await ShowProductionRunDialogAsync(
            "Add known cable run",
            existing: null);
        if (observation is null)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryAddObservation(
                observation,
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Known run could not be added",
                message);
        }
    }

    private async void EditProductionRunObservation_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement
            {
                Tag: ProductionRunObservation existing,
            })
        {
            return;
        }

        var observation = await ShowProductionRunDialogAsync(
            "Edit known cable run",
            existing);
        if (observation is null)
        {
            return;
        }

        if (!ProductionSpeedViewModel.TryUpdateObservation(
                observation,
                out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Known run could not be updated",
                message);
        }
    }

    private async void DeleteProductionRunObservation_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement
            {
                Tag: ProductionRunObservation observation,
            })
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Delete {observation.CableReference}?",
            Content =
                "The machine settings and measured-run evidence will be removed from this production line.",
            PrimaryButtonText = "Delete known run",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ProductionSpeedViewModel.TryDeleteObservation(observation.Id, out _);
        }
    }

    private async void CalculateProductionSpeedEstimate_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ProductionSpeedViewModel.TryCalculateEstimate(out var message))
        {
            await ShowProductionSpeedMessageAsync(
                "Estimate unavailable",
                message);
        }
    }

    private void UseCurrentSingleCoreForSpeedEstimate_Click(
        object sender,
        RoutedEventArgs e) =>
        ProductionSpeedViewModel.SetEstimateInputs(
            "Insulation",
            CostingViewModel.ConductorOutsideDiameterMillimetres,
            CostingViewModel.NominalFinishedCoreOutsideDiameterMillimetres,
            CostingViewModel.QuoteLengthMetres,
            "the current single-core costing");

    private void UseDualFirstForSpeedEstimate_Click(
        object sender,
        RoutedEventArgs e) =>
        ProductionSpeedViewModel.SetEstimateInputs(
            "Insulation",
            DualCostingViewModel.ConductorOutsideDiameterMillimetres,
            DualCostingViewModel.FirstFinishedOutsideDiameterMillimetres,
            DualCostingViewModel.FinishedQuoteLengthMetres +
            DualCostingViewModel.CoreStartupLengthMetres,
            "the dual costing's first extrusion");

    private void UseDualSecondForSpeedEstimate_Click(
        object sender,
        RoutedEventArgs e) =>
        ProductionSpeedViewModel.SetEstimateInputs(
            "Insulation",
            DualCostingViewModel.FirstFinishedOutsideDiameterMillimetres,
            DualCostingViewModel.SecondFinishedOutsideDiameterMillimetres,
            DualCostingViewModel.FinishedQuoteLengthMetres,
            "the dual costing's second extrusion");

    private void ApplyProductionSpeedToSingleCore_Click(
        object sender,
        RoutedEventArgs e)
    {
        var estimate = ProductionSpeedViewModel.LatestEstimate;
        var line = ProductionSpeedViewModel.SelectedLine;
        if (estimate is null || line is null)
        {
            return;
        }

        CostingViewModel.ManualLineSpeedMetresPerHour =
            (double)estimate.RecommendedLineSpeedMetresPerHour;
        CostingViewModel.UseManualLineSpeed = true;
        CostingViewModel.CalculationStatus =
            $"Applied {estimate.RecommendedLineSpeedMetresPerHour:N0} m/h from " +
            $"{line.Name} ({estimate.Source}) as the visible manual line-speed input.";
        ShowSection("costing");
        DispatcherQueue.TryEnqueue(
            () => BringCostingSectionIntoView("labour"));
    }

    private void ApplyProductionSpeedToDualFirst_Click(
        object sender,
        RoutedEventArgs e)
    {
        var estimate = ProductionSpeedViewModel.LatestEstimate;
        var line = ProductionSpeedViewModel.SelectedLine;
        if (estimate is null || line is null)
        {
            return;
        }

        DualCostingViewModel.FirstManualLineSpeedMetresPerHour =
            (double)estimate.RecommendedLineSpeedMetresPerHour;
        DualCostingViewModel.UseFirstManualLineSpeed = true;
        DualCostingViewModel.CalculationStatus =
            $"Applied {estimate.RecommendedLineSpeedMetresPerHour:N0} m/h from " +
            $"{line.Name} ({estimate.Source}) to the first extrusion's visible manual-speed input.";
        ShowSection("costing-dual");
    }

    private void ApplyProductionSpeedToDualSecond_Click(
        object sender,
        RoutedEventArgs e)
    {
        var estimate = ProductionSpeedViewModel.LatestEstimate;
        var line = ProductionSpeedViewModel.SelectedLine;
        if (estimate is null || line is null)
        {
            return;
        }

        DualCostingViewModel.SecondManualLineSpeedMetresPerHour =
            (double)estimate.RecommendedLineSpeedMetresPerHour;
        DualCostingViewModel.UseSecondManualLineSpeed = true;
        DualCostingViewModel.CalculationStatus =
            $"Applied {estimate.RecommendedLineSpeedMetresPerHour:N0} m/h from " +
            $"{line.Name} ({estimate.Source}) to the second extrusion's visible manual-speed input.";
        ShowSection("costing-dual");
    }

    private async Task<(string Name, decimal AboveMaximumSpeed)?>
        ShowProductionLineDialogAsync(
            string title,
            string name,
            decimal? aboveMaximumSpeed)
    {
        var nameBox = new TextBox
        {
            Header = "Production-line name",
            Text = name,
        };
        var aboveMaximumBox = NumberInput(
            "Speed above the largest OD band (m/h)",
            aboveMaximumSpeed);
        var content = new StackPanel
        {
            MinWidth = 520,
            Spacing = 14,
        };
        content.Children.Add(new TextBlock
        {
            Text =
                "A new line starts with no size bands or known runs. Its above-maximum speed is used only when configured bands exist but the cable is larger than all of them.",
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(nameBox);
        content.Children.Add(aboveMaximumBox);

        var dialog = EditorDialog(title, content, "Save line");
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        return (nameBox.Text, DecimalValue(aboveMaximumBox));
    }

    private async Task<(decimal MaximumOutsideDiameter, decimal LineSpeed)?>
        ShowProductionSpeedBandDialogAsync(
            string title,
            decimal? maximumOutsideDiameter,
            decimal? lineSpeed)
    {
        var maximumBox = NumberInput(
            "Maximum finished OD (mm)",
            maximumOutsideDiameter);
        var speedBox = NumberInput("Line speed (m/h)", lineSpeed);
        var content = new StackPanel
        {
            MinWidth = 480,
            Spacing = 14,
        };
        content.Children.Add(new TextBlock
        {
            Text =
                "Bands are sorted automatically. The first maximum OD that contains the cable supplies its fallback speed.",
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(maximumBox);
        content.Children.Add(speedBox);

        var dialog = EditorDialog(title, content, "Save band");
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        return (DecimalValue(maximumBox), DecimalValue(speedBox));
    }

    private async Task<ProductionRunObservation?> ShowProductionRunDialogAsync(
        string title,
        ProductionRunObservation? existing)
    {
        var cableReference = new TextBox
        {
            Header = "Cable reference or description",
            Text = existing?.CableReference ?? "",
        };
        var process = new TextBox
        {
            Header = "Process",
            Text = existing?.ProcessName ?? "",
        };
        var coreOd = NumberInput(
            "Core OD (mm)",
            existing?.CoreOutsideDiameterMillimetres);
        var coreTolerance = NumberInput(
            "Core OD tolerance ± (mm)",
            existing?.CoreOutsideDiameterToleranceMillimetres);
        var finishedOd = NumberInput(
            "Finished OD (mm)",
            existing?.FinishedOutsideDiameterMillimetres);
        var finishedTolerance = NumberInput(
            "Finished OD tolerance ± (mm)",
            existing?.FinishedOutsideDiameterToleranceMillimetres);
        var capstan = NumberInput(
            "Capstan setting (machine dial value)",
            existing?.CapstanSetting);
        var extruder = NumberInput(
            "Extruder setting (machine dial value)",
            existing?.ExtruderSetting);
        var measuredSpeed = NumberInput(
            "Measured line speed (m/h, optional)",
            existing?.MeasuredLineSpeedMetresPerHour);
        var producedLength = NumberInput(
            "Produced length (m, optional)",
            existing?.ProducedLengthMetres);
        var runningMinutes = NumberInput(
            "Running time (minutes, optional)",
            existing?.RunningTimeMinutes);
        var notes = new TextBox
        {
            Header = "Notes",
            Text = existing?.Notes ?? "",
            AcceptsReturn = true,
            MinHeight = 90,
            TextWrapping = TextWrapping.Wrap,
        };
        var fields = new StackPanel
        {
            Spacing = 12,
        };
        fields.Children.Add(new InfoBar
        {
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Title = "Settings are calibration values",
            Message =
                "Capstan and extruder settings are not assumed to be m/h. Enter measured speed, or both produced length and running minutes, for this run to influence estimates.",
        });
        fields.Children.Add(cableReference);
        fields.Children.Add(process);
        fields.Children.Add(coreOd);
        fields.Children.Add(coreTolerance);
        fields.Children.Add(finishedOd);
        fields.Children.Add(finishedTolerance);
        fields.Children.Add(capstan);
        fields.Children.Add(extruder);
        fields.Children.Add(measuredSpeed);
        fields.Children.Add(producedLength);
        fields.Children.Add(runningMinutes);
        fields.Children.Add(notes);
        var content = new ScrollViewer
        {
            MinWidth = 560,
            MaxHeight = 620,
            Content = fields,
        };

        var dialog = EditorDialog(title, content, "Save known run");
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        return new ProductionRunObservation
        {
            Id = existing?.Id ?? "",
            CableReference = cableReference.Text,
            ProcessName = process.Text,
            CoreOutsideDiameterMillimetres = DecimalValue(coreOd),
            CoreOutsideDiameterToleranceMillimetres = DecimalValue(coreTolerance),
            FinishedOutsideDiameterMillimetres = DecimalValue(finishedOd),
            FinishedOutsideDiameterToleranceMillimetres = DecimalValue(finishedTolerance),
            CapstanSetting = OptionalDecimalValue(capstan),
            ExtruderSetting = OptionalDecimalValue(extruder),
            MeasuredLineSpeedMetresPerHour = OptionalDecimalValue(measuredSpeed),
            ProducedLengthMetres = OptionalDecimalValue(producedLength),
            RunningTimeMinutes = OptionalDecimalValue(runningMinutes),
            Notes = notes.Text,
        };
    }

    private ContentDialog EditorDialog(
        string title,
        object content,
        string primaryButtonText) =>
        new()
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };

    private async Task ShowProductionSpeedMessageAsync(
        string title,
        string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "Close",
        };
        await dialog.ShowAsync();
    }

    private static NumberBox NumberInput(string header, decimal? value) =>
        new()
        {
            Header = header,
            Minimum = 0,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Value = value is null ? double.NaN : (double)value.Value,
        };

    private static decimal DecimalValue(NumberBox numberBox) =>
        double.IsNaN(numberBox.Value) || double.IsInfinity(numberBox.Value)
            ? 0m
            : Convert.ToDecimal(numberBox.Value, CultureInfo.InvariantCulture);

    private static decimal? OptionalDecimalValue(NumberBox numberBox) =>
        double.IsNaN(numberBox.Value) || double.IsInfinity(numberBox.Value)
            ? null
            : Convert.ToDecimal(numberBox.Value, CultureInfo.InvariantCulture);
}
