using ATAG.Costing.Application.CentralData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ATAG.Costing.WinUI;

public sealed partial class MainPage
{
    private async Task NavigateAndTransformDatabaseTableAsync(
        DatabaseLinkDraft source,
        CentralDataTableLink? existingLink)
    {
        if (!_databaseNavigators.TryGetValue(source.SourceKind, out var navigator))
        {
            await ShowCentralDataMessageAsync(
                "Database provider unavailable",
                $"No {source.SourceKind} Navigator is installed. The retained data has not changed.");
            return;
        }

        IReadOnlyList<CentralDataSourceObject> objects;
        try
        {
            CostingViewModel.CentralDataStatus =
                $"Connecting to {source.ToConnection().DisplayName} and reading its table catalogue…";
            objects = await navigator.DiscoverAsync(source.ToConnection());
        }
        catch (Exception exception)
        {
            Program.Log($"Central-data Navigator discovery failed: {exception}");
            await ShowCentralDataMessageAsync(
                "Database could not be opened",
                $"{exception.Message}\n\nThe retained central-data tables are unchanged.");
            return;
        }

        if (objects.Count == 0)
        {
            await ShowCentralDataMessageAsync(
                "No tables found",
                "The connection succeeded, but no user tables or views were available. The retained data has not changed.");
            return;
        }

        var search = new TextBox
        {
            PlaceholderText = "Search tables and views",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var objectList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        var previewTitle = new TextBlock
        {
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Text = "Choose a table or view",
        };
        var previewStatus = new InfoBar
        {
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Title = "Navigator",
            Message = "Select an object on the left to inspect its rows before any data is imported.",
        };
        var previewProgress = new ProgressRing
        {
            Width = 28,
            Height = 28,
            IsActive = false,
            Visibility = Visibility.Collapsed,
        };
        var previewHost = PreviewScroller();

        var navigatorLayout = new Grid
        {
            MinWidth = 900,
            MinHeight = 520,
            ColumnSpacing = 18,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        navigatorLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        navigatorLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var leftPanel = new Grid { RowSpacing = 10 };
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        leftPanel.Children.Add(search);
        Grid.SetRow(objectList, 1);
        leftPanel.Children.Add(objectList);
        navigatorLayout.Children.Add(leftPanel);

        var rightPanel = new Grid { RowSpacing = 10 };
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var titleRow = new Grid();
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleRow.Children.Add(previewTitle);
        Grid.SetColumn(previewProgress, 1);
        titleRow.Children.Add(previewProgress);
        rightPanel.Children.Add(titleRow);
        Grid.SetRow(previewStatus, 1);
        rightPanel.Children.Add(previewStatus);
        Grid.SetRow(previewHost, 2);
        rightPanel.Children.Add(previewHost);
        Grid.SetColumn(rightPanel, 1);
        navigatorLayout.Children.Add(rightPanel);

        var dialog = new CentralDataWorkflowWindow(
            $"Navigator · {AreaName(source.Area)}",
            navigatorLayout,
            primaryButtonText: "Transform data",
            closeButtonText: "Cancel",
            requestedTheme: ActualTheme)
        {
            IsPrimaryButtonEnabled = false,
        };

        CentralDataTablePreview? selectedPreview = null;
        CentralDataSourceObject? selectedObject = null;
        var previewRequest = 0;

        void RebuildObjectList()
        {
            var selectedName = (objectList.SelectedItem as ListViewItem)?.Tag is CentralDataSourceObject selected
                ? selected.QualifiedName
                : existingLink?.TableName;
            var filter = search.Text.Trim();
            objectList.Items.Clear();
            foreach (var item in objects.Where(item =>
                         string.IsNullOrWhiteSpace(filter) ||
                         item.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                         item.Kind.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)))
            {
                var panel = new StackPanel { Spacing = 2 };
                panel.Children.Add(new TextBlock
                {
                    Text = item.DisplayName,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                });
                panel.Children.Add(new TextBlock
                {
                    Text = item.Kind.ToString(),
                    FontSize = 12,
                    Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
                });
                var listItem = new ListViewItem
                {
                    Content = panel,
                    Tag = item,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                };
                objectList.Items.Add(listItem);
                if (string.Equals(item.QualifiedName, selectedName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Name, selectedName, StringComparison.OrdinalIgnoreCase))
                {
                    objectList.SelectedItem = listItem;
                }
            }

            if (objectList.SelectedItem is null && objectList.Items.Count > 0)
            {
                objectList.SelectedIndex = 0;
            }
        }

        search.TextChanged += (_, _) => RebuildObjectList();
        objectList.SelectionChanged += async (_, _) =>
        {
            if ((objectList.SelectedItem as ListViewItem)?.Tag is not CentralDataSourceObject item)
            {
                return;
            }

            var request = ++previewRequest;
            selectedObject = item;
            selectedPreview = null;
            dialog.IsPrimaryButtonEnabled = false;
            previewTitle.Text = item.DisplayName;
            previewProgress.IsActive = true;
            previewProgress.Visibility = Visibility.Visible;
            previewStatus.Severity = InfoBarSeverity.Informational;
            previewStatus.Title = "Reading preview";
            previewStatus.Message = "Reading up to 200 rows. Nothing is imported at this stage.";
            previewHost.Content = null;
            try
            {
                var preview = await navigator.PreviewAsync(source.ToConnection(), item, rowLimit: 200);
                if (request != previewRequest)
                {
                    return;
                }

                selectedPreview = preview;
                previewHost.Content = BuildDatabasePreviewTable(preview);
                var blockingIssue = preview.Issues.FirstOrDefault(issue => issue.IsBlocking);
                previewStatus.Severity = blockingIssue is not null
                    ? InfoBarSeverity.Error
                    : preview.IgnoredErrorCount > 0
                    ? InfoBarSeverity.Warning
                    : InfoBarSeverity.Success;
                previewStatus.Title = $"{preview.Rows.Count:N0} preview row(s) · {preview.Columns.Count:N0} column(s)";
                previewStatus.Message = blockingIssue is not null
                    ? blockingIssue.Message
                    : preview.IgnoredErrorCount > 0
                    ? $"{preview.IgnoredErrorCount:N0} division-by-zero/source-error cell(s) are shown as ignored blanks; valid records remain available."
                    : "The preview loaded successfully. Continue to inspect transformations and automatic costing-field matches.";
                dialog.IsPrimaryButtonEnabled = preview.Columns.Count > 0 && blockingIssue is null;
            }
            catch (Exception exception)
            {
                if (request != previewRequest)
                {
                    return;
                }

                Program.Log($"Central-data table preview failed: {exception}");
                previewStatus.Severity = InfoBarSeverity.Error;
                previewStatus.Title = "Preview could not be read";
                previewStatus.Message = exception.Message;
            }
            finally
            {
                if (request == previewRequest)
                {
                    previewProgress.IsActive = false;
                    previewProgress.Visibility = Visibility.Collapsed;
                }
            }
        };

        RebuildObjectList();
        var navigationResult = await dialog.ShowAsync();
        if (navigationResult != ContentDialogResult.Primary ||
            selectedPreview is null || selectedObject is null)
        {
            return;
        }

        var link = await ShowTransformEditorAsync(source, selectedObject, selectedPreview, existingLink);
        if (link is null)
        {
            return;
        }

        await ImportDatabaseTableAsync(source, selectedObject, link, navigator);
    }

    private async Task EditExistingDatabaseTableAsync(
        DatabaseLinkDraft source,
        CentralDataTableLink existingLink)
    {
        if (!_databaseNavigators.TryGetValue(source.SourceKind, out var navigator))
        {
            await ShowCentralDataMessageAsync(
                "Database provider unavailable",
                $"No {source.SourceKind} Navigator is installed. The retained data has not changed.");
            return;
        }

        var sourceObject = SourceObjectFromLink(existingLink);
        CentralDataTablePreview preview;
        try
        {
            CostingViewModel.CentralDataStatus =
                $"Reading the saved {AreaName(existingLink.Area)} table and its current transform settings…";
            preview = await navigator.PreviewAsync(
                source.ToConnection(),
                sourceObject,
                rowLimit: 200);
        }
        catch (Exception exception)
        {
            Program.Log($"Central-data transform edit preview failed: {exception}");
            await ShowCentralDataMessageAsync(
                "Saved link could not be opened",
                $"{exception.Message}\n\nThe last successful {AreaName(existingLink.Area)} table is unchanged and remains available.");
            return;
        }

        var blockingIssue = preview.Issues.FirstOrDefault(issue => issue.IsBlocking);
        if (blockingIssue is not null)
        {
            await ShowCentralDataMessageAsync(
                "Saved link needs attention",
                $"{blockingIssue.Message}\n\nThe retained table has not changed.");
            return;
        }

        var updatedLink = await ShowTransformEditorAsync(
            source,
            sourceObject,
            preview,
            existingLink);
        if (updatedLink is null)
        {
            return;
        }

        await ImportDatabaseTableAsync(source, sourceObject, updatedLink, navigator);
    }

    private async Task ImportDatabaseTableAsync(
        DatabaseLinkDraft source,
        CentralDataSourceObject sourceObject,
        CentralDataTableLink link,
        ICentralDataDatabaseNavigator navigator)
    {
        try
        {
            CostingViewModel.CentralDataStatus =
                $"Importing all available {sourceObject.DisplayName} rows and retaining the previous table until validation completes…";
            var fullTable = await navigator.PreviewAsync(
                source.ToConnection(),
                sourceObject,
                rowLimit: 0);
            var import = CostingViewModel.ImportDatabaseTable(link, fullTable);
            await ShowCentralDataMessageAsync(
                import.Succeeded ? "Import complete" : "Import not applied",
                string.Join("\n", new[] { import.Message }.Concat(import.Warnings)));
        }
        catch (Exception exception)
        {
            Program.Log($"Central-data import failed: {exception}");
            await ShowCentralDataMessageAsync(
                "Import not applied",
                $"{exception.Message}\n\nThe last successful {AreaName(source.Area)} table is still available.");
        }
    }

    private static CentralDataSourceObject SourceObjectFromLink(
        CentralDataTableLink link)
    {
        var objectName = link.TableName;
        if (!string.IsNullOrWhiteSpace(link.SchemaName) &&
            objectName.StartsWith(link.SchemaName + ".", StringComparison.OrdinalIgnoreCase))
        {
            objectName = objectName[(link.SchemaName.Length + 1)..];
        }

        return new CentralDataSourceObject(
            objectName,
            link.SchemaName,
            link.ObjectKind,
            link.TableName);
    }

    private async Task<CentralDataTableLink?> ShowTransformEditorAsync(
        DatabaseLinkDraft source,
        CentralDataSourceObject sourceObject,
        CentralDataTablePreview preview,
        CentralDataTableLink? existingLink)
    {
        var trimText = new ToggleSwitch
        {
            Header = "Trim text values",
            IsOn = ExistingStepEnabled(
                existingLink,
                CentralDataQueryStepKind.TrimText,
                fallback: true),
            OnContent = "On",
            OffContent = "Off",
        };
        var removeBlankRows = new ToggleSwitch
        {
            Header = "Remove completely blank rows",
            IsOn = ExistingStepEnabled(
                existingLink,
                CentralDataQueryStepKind.RemoveBlankRows,
                fallback: true),
            OnContent = "On",
            OffContent = "Off",
        };
        var appliedSteps = new StackPanel { Spacing = 6 };
        var filterEditors = new List<DatabaseFilterEditor>();
        var filterEditorsPanel = new StackPanel { Spacing = 8 };
        var addFilterButton = new Button
        {
            Content = "Add filter",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var columnEditorsPanel = new StackPanel { Spacing = 8 };
        var matchesPanel = new StackPanel { Spacing = 8 };
        var previewHost = PreviewScroller();
        var validation = new InfoBar { IsClosable = false, IsOpen = true };
        var mappingEditors = new Dictionary<string, ComboBox>(StringComparer.OrdinalIgnoreCase);
        var existingTransformed = preview.Apply(existingLink?.EffectiveQuerySteps ?? []);
        var columnEditors = preview.Columns
            .Select(column =>
            {
                var transformed = existingTransformed.Columns.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.EffectiveSourceName,
                        column.EffectiveSourceName,
                        StringComparison.OrdinalIgnoreCase));
                var keep = new CheckBox
                {
                    Content = "Keep",
                    IsChecked = transformed is not null,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var name = new TextBox
                {
                    Text = transformed?.Name ?? column.Name,
                    IsEnabled = transformed is not null,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                return new DatabaseColumnTransformEditor(column, keep, name);
            })
            .ToArray();

        var editorLayout = new Grid
        {
            MinWidth = 900,
            MinHeight = 540,
            ColumnSpacing = 14,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        editorLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        editorLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editorLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });

        var queryPanel = new StackPanel { Spacing = 10 };
        queryPanel.Children.Add(SectionCaption("QUERY"));
        var queryCard = new Border
        {
            Padding = new Thickness(12),
            Background = ResourceBrush("AccentFillColorSecondaryBrush"),
            CornerRadius = new CornerRadius(7),
        };
        var queryText = new StackPanel { Spacing = 3 };
        queryText.Children.Add(new TextBlock
        {
            Text = AreaName(source.Area),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        queryText.Children.Add(new TextBlock { Text = sourceObject.DisplayName, FontSize = 12 });
        queryCard.Child = queryText;
        queryPanel.Children.Add(queryCard);
        editorLayout.Children.Add(queryPanel);

        var centre = new Grid { RowSpacing = 10 };
        centre.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        centre.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        centre.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        centre.Children.Add(new TextBlock
        {
            Text = $"{sourceObject.DisplayName} · transform preview",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        Grid.SetRow(validation, 1);
        centre.Children.Add(validation);
        Grid.SetRow(previewHost, 2);
        centre.Children.Add(previewHost);
        Grid.SetColumn(centre, 1);
        editorLayout.Children.Add(centre);

        var settingsPanel = new StackPanel { Spacing = 14 };
        settingsPanel.Children.Add(SectionCaption("QUERY SETTINGS"));
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Applied steps",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        settingsPanel.Children.Add(appliedSteps);
        settingsPanel.Children.Add(trimText);
        settingsPanel.Children.Add(removeBlankRows);
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Row filters",
            Margin = new Thickness(0, 8, 0, 0),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Keep only the records that match every filter. Filters are saved with the link and applied again on refresh.",
            FontSize = 12,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        settingsPanel.Children.Add(filterEditorsPanel);
        settingsPanel.Children.Add(addFilterButton);
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Source columns",
            Margin = new Thickness(0, 8, 0, 0),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Rename or remove source columns here. The complete table is retained by default; row values remain controlled by the linked database.",
            FontSize = 12,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        settingsPanel.Children.Add(columnEditorsPanel);
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Costing import matches",
            Margin = new Thickness(0, 8, 0, 0),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        settingsPanel.Children.Add(new TextBlock
        {
            Text = "Costing fields are projected from the transformed full table. Access physical names, captions, and descriptions all take part in automatic matching.",
            FontSize = 12,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        settingsPanel.Children.Add(matchesPanel);
        var settingsScroll = new ScrollViewer
        {
            Content = settingsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        Grid.SetColumn(settingsScroll, 2);
        editorLayout.Children.Add(settingsScroll);

        var dialog = new CentralDataWorkflowWindow(
            "Transform data",
            editorLayout,
            primaryButtonText: "Import data",
            closeButtonText: "Cancel",
            requestedTheme: ActualTheme);

        foreach (var editor in columnEditors)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(editor.Keep);
            Grid.SetColumn(editor.Name, 1);
            row.Children.Add(editor.Name);
            var metadata = new TextBlock
            {
                Text = editor.Column.MetadataDisplay,
                FontSize = 11,
                Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(72, 0, 0, 0),
            };
            var group = new StackPanel { Spacing = 3 };
            group.Children.Add(row);
            group.Children.Add(metadata);
            columnEditorsPanel.Children.Add(group);
        }

        foreach (var field in CentralDataImportSchema.Fields(source.Area))
        {
            matchesPanel.Children.Add(new TextBlock
            {
                Text = field.IsRequired ? $"{field.Label} *" : field.Label,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
            var selector = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Not imported",
            };
            mappingEditors[field.Key] = selector;
            matchesPanel.Children.Add(selector);
        }

        IReadOnlyList<CentralDataQueryStep> CurrentSteps()
        {
            var steps = new List<CentralDataQueryStep>
            {
                new(CentralDataQueryStepKind.Source, "Source", source.ToConnection().DisplayName),
                new(CentralDataQueryStepKind.Navigation, "Navigation", sourceObject.QualifiedName),
                new(
                    CentralDataQueryStepKind.ReplaceDivisionByZeroWithNull,
                    "Ignore division-by-zero errors",
                    "#DIV/0! cells become blank; valid cells and rows continue."),
            };
            steps.AddRange(filterEditors.Select(editor =>
                new CentralDataQueryStep(
                    CentralDataQueryStepKind.FilterRows,
                    $"Filter {editor.SelectedColumn}",
                    FilterDescription(
                        editor.SelectedColumn,
                        editor.SelectedOperator,
                        editor.Value.Text),
                    SourceColumn: editor.SelectedColumn,
                    FilterOperator: editor.SelectedOperator,
                    FilterValue: editor.Value.Text)));
            steps.AddRange(columnEditors
                .Where(editor => editor.Keep.IsChecked != true)
                .Select(editor => new CentralDataQueryStep(
                    CentralDataQueryStepKind.RemoveColumn,
                    $"Remove {editor.Column.Name}",
                    $"Exclude source column {editor.Column.Name} from the retained transformed table.",
                    SourceColumn: editor.Column.Name)));
            steps.AddRange(columnEditors
                .Where(editor =>
                    editor.Keep.IsChecked == true &&
                    !string.Equals(
                        editor.Column.Name,
                        editor.Name.Text.Trim(),
                        StringComparison.Ordinal))
                .Select(editor => new CentralDataQueryStep(
                    CentralDataQueryStepKind.RenameColumn,
                    $"Rename {editor.Column.Name}",
                    $"Rename {editor.Column.Name} to {editor.Name.Text.Trim()}.",
                    SourceColumn: editor.Column.Name,
                    TargetColumn: editor.Name.Text.Trim())));
            steps.Add(new CentralDataQueryStep(
                CentralDataQueryStepKind.TrimText,
                "Trim text",
                "Remove leading and trailing spaces.",
                trimText.IsOn,
                CanDisable: true));
            steps.Add(new CentralDataQueryStep(
                CentralDataQueryStepKind.RemoveBlankRows,
                "Remove blank rows",
                "Discard rows in which every value is blank.",
                removeBlankRows.IsOn,
                CanDisable: true));
            return steps;
        }

        IReadOnlyDictionary<string, string> CurrentMappings() =>
            mappingEditors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.SelectedIndex > 0
                    ? pair.Value.SelectedItem?.ToString() ?? string.Empty
                    : string.Empty,
                StringComparer.OrdinalIgnoreCase);

        var suppressEditorEvents = false;

        void RebuildMappingEditors(
            IReadOnlyDictionary<string, string>? preferredMappings)
        {
            var transformed = preview.Apply(CurrentSteps());
            var matches = CentralDataImportSchema.Match(
                source.Area,
                transformed.Columns,
                preferredMappings);
            suppressEditorEvents = true;
            try
            {
                foreach (var match in matches)
                {
                    var selector = mappingEditors[match.Field.Key];
                    selector.Items.Clear();
                    selector.Items.Add("Not imported");
                    foreach (var column in transformed.Columns)
                    {
                        selector.Items.Add(column.Name);
                    }

                    selector.SelectedIndex = match.SourceColumn is null
                        ? 0
                        : transformed.Columns
                            .Select((column, index) => new { column.Name, Index = index + 1 })
                            .FirstOrDefault(item => string.Equals(
                                item.Name,
                                match.SourceColumn,
                                StringComparison.OrdinalIgnoreCase))?.Index ?? 0;
                }
            }
            finally
            {
                suppressEditorEvents = false;
            }
        }

        void RefreshEditor(bool rebuildMappings = false)
        {
            if (suppressEditorEvents)
            {
                return;
            }

            if (rebuildMappings)
            {
                RebuildMappingEditors(CurrentMappings());
            }

            var steps = CurrentSteps();
            var transformed = preview.Apply(steps);
            appliedSteps.Children.Clear();
            foreach (var step in steps.Where(step => step.IsEnabled))
            {
                var stepText = new StackPanel { Spacing = 2 };
                stepText.Children.Add(new TextBlock
                {
                    Text = step.Name,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                });
                stepText.Children.Add(new TextBlock
                {
                    Text = step.Description,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
                });
                appliedSteps.Children.Add(new Border
                {
                    Padding = new Thickness(10, 7, 10, 7),
                    Background = ResourceBrush("ControlFillColorSecondaryBrush"),
                    CornerRadius = new CornerRadius(6),
                    Child = stepText,
                });
            }

            var mappings = CurrentMappings();
            var missing = CentralDataImportSchema.Fields(source.Area)
                .Where(field => field.IsRequired &&
                                (!mappings.TryGetValue(field.Key, out var value) || string.IsNullOrWhiteSpace(value)))
                .Select(field => field.Label)
                .ToArray();
            var blockingIssue = transformed.Issues.FirstOrDefault(issue => issue.IsBlocking);
            dialog.IsPrimaryButtonEnabled =
                transformed.Columns.Count > 0 &&
                blockingIssue is null &&
                missing.Length == 0;
            validation.Severity = blockingIssue is not null || missing.Length > 0
                ? InfoBarSeverity.Error
                : transformed.IgnoredErrorCount > 0
                    ? InfoBarSeverity.Warning
                    : InfoBarSeverity.Success;
            validation.Title = blockingIssue is not null
                ? "Column transformations need attention"
                : missing.Length > 0
                    ? "Required matches need attention"
                    : "Ready to import the full table";
            validation.Message = blockingIssue is not null
                ? blockingIssue.Message
                : missing.Length > 0
                    ? $"Choose a source column for: {string.Join(", ", missing)}."
                    : transformed.IgnoredErrorCount > 0
                        ? $"{transformed.IgnoredErrorCount:N0} error cell(s) are treated as blank. Every retained transformed column and all available rows will be saved after you confirm."
                        : "Every retained transformed column and all available rows will be saved. The matched fields below form the costing projection.";
            previewHost.Content = BuildDatabasePreviewTable(transformed);
        }

        void AddFilterEditor(CentralDataQueryStep? existingStep = null)
        {
            var column = new ComboBox
            {
                Header = "Column",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = preview.Columns.Select(item => item.Name).ToArray(),
                SelectedItem = existingStep?.SourceColumn,
            };
            if (column.SelectedItem is null && column.Items.Count > 0)
            {
                column.SelectedIndex = 0;
            }

            var comparison = new ComboBox
            {
                Header = "Condition",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = FilterOperatorOptions,
                DisplayMemberPath = nameof(DatabaseFilterOperatorOption.Display),
                SelectedValuePath = nameof(DatabaseFilterOperatorOption.Value),
                SelectedValue = existingStep?.FilterOperator ??
                                CentralDataFilterOperator.Equals,
            };
            var value = new TextBox
            {
                Header = "Value",
                Text = existingStep?.FilterValue ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            var remove = new Button
            {
                Content = "Remove",
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(column);
            Grid.SetColumn(comparison, 1);
            row.Children.Add(comparison);
            Grid.SetColumn(value, 2);
            row.Children.Add(value);
            Grid.SetColumn(remove, 3);
            row.Children.Add(remove);
            var card = new Border
            {
                Padding = new Thickness(8),
                Background = ResourceBrush("ControlFillColorSecondaryBrush"),
                CornerRadius = new CornerRadius(7),
                Child = row,
            };
            var editor = new DatabaseFilterEditor(
                card,
                column,
                comparison,
                value);
            filterEditors.Add(editor);
            filterEditorsPanel.Children.Add(card);

            void UpdateValueState()
            {
                value.IsEnabled = editor.SelectedOperator is not
                    (CentralDataFilterOperator.IsBlank or
                     CentralDataFilterOperator.IsNotBlank);
            }

            column.SelectionChanged += (_, _) => RefreshEditor();
            comparison.SelectionChanged += (_, _) =>
            {
                UpdateValueState();
                RefreshEditor();
            };
            value.TextChanged += (_, _) => RefreshEditor();
            remove.Click += (_, _) =>
            {
                filterEditors.Remove(editor);
                filterEditorsPanel.Children.Remove(card);
                RefreshEditor();
            };
            UpdateValueState();
        }

        trimText.Toggled += (_, _) => RefreshEditor();
        removeBlankRows.Toggled += (_, _) => RefreshEditor();
        foreach (var editor in columnEditors)
        {
            editor.Keep.Checked += (_, _) =>
            {
                editor.Name.IsEnabled = true;
                RefreshEditor(rebuildMappings: true);
            };
            editor.Keep.Unchecked += (_, _) =>
            {
                editor.Name.IsEnabled = false;
                RefreshEditor(rebuildMappings: true);
            };
            editor.Name.TextChanged += (_, _) => RefreshEditor(rebuildMappings: true);
        }
        foreach (var editor in mappingEditors.Values)
        {
            editor.SelectionChanged += (_, _) => RefreshEditor();
        }
        foreach (var existingFilter in existingLink?.EffectiveQuerySteps.Where(
                     step => step.Kind == CentralDataQueryStepKind.FilterRows) ?? [])
        {
            AddFilterEditor(existingFilter);
        }
        addFilterButton.Click += (_, _) =>
        {
            AddFilterEditor();
            RefreshEditor();
        };
        RebuildMappingEditors(existingLink?.ColumnMappings);
        RefreshEditor();

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        var displayName = source.SourceKind == CentralDataSourceKind.AccessDatabase
            ? $"{AreaName(source.Area)} · Access · {Path.GetFileName(source.AccessDatabasePath)}"
            : $"{AreaName(source.Area)} · SQL · {source.SqlServer}/{source.SqlDatabase}";
        return new CentralDataTableLink(
            source.Area,
            source.SourceKind,
            displayName,
            sourceObject.QualifiedName,
            CurrentMappings(),
            source.AccessDatabasePath,
            source.SqlServer,
            source.SqlDatabase,
            source.UseWindowsAuthentication,
            sourceObject.SchemaName,
            sourceObject.Kind,
            CurrentSteps());
    }

    private FrameworkElement BuildDatabasePreviewTable(CentralDataTablePreview preview)
    {
        const int maximumVisibleRows = 75;
        const int maximumVisibleColumns = 30;
        var columns = preview.Columns.Take(maximumVisibleColumns).ToArray();
        var rows = preview.Rows.Take(maximumVisibleRows).ToArray();
        var panel = new StackPanel { Spacing = 8 };
        var summaryParts = new List<string>
        {
            $"Showing {rows.Length:N0} of {preview.Rows.Count:N0} preview row(s)",
            $"{columns.Length:N0} of {preview.Columns.Count:N0} column(s)",
        };
        if (preview.IgnoredErrorCount > 0)
        {
            summaryParts.Add($"{preview.IgnoredErrorCount:N0} ignored error cell(s)");
        }
        panel.Children.Add(new TextBlock
        {
            Text = string.Join(" · ", summaryParts),
            FontSize = 12,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
        });

        if (columns.Length == 0)
        {
            panel.Children.Add(new TextBlock { Text = "No columns were returned." });
            return panel;
        }

        var table = new Grid
        {
            BorderBrush = ResourceBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
        };
        foreach (var _ in columns)
        {
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        }
        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var index = 0; index < rows.Length; index++)
        {
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
        {
            AddDatabasePreviewCell(
                table,
                0,
                columnIndex,
                columns[columnIndex].MetadataDisplay,
                isHeader: true,
                hasError: false);
        }
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                var cell = rows[rowIndex].Cell(columns[columnIndex].Name);
                AddDatabasePreviewCell(
                    table,
                    rowIndex + 1,
                    columnIndex,
                    cell.DisplayValue,
                    isHeader: false,
                    hasError: cell.HasError);
            }
        }
        panel.Children.Add(table);
        return panel;
    }

    private void RefreshRetainedSourceTablesView()
    {
        if (RetainedSourceTablesHost is null)
        {
            return;
        }

        RetainedSourceTablesHost.Children.Clear();
        if (CostingViewModel.RetainedSourceTables.Count == 0)
        {
            RetainedSourceTablesHost.Children.Add(new TextBlock
            {
                Text = "No full live database object has been imported yet. A clean installation contains no customer or material rows; use Set up data link above to import the required tables.",
                Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }

        RetainedSourceTablesHost.Children.Add(new TextBlock
        {
            Text = "These are the complete transformed tables saved by the importer. The on-screen grid is a bounded preview; every transformed row and every kept column remains retained. Closing a tab is intentionally disabled—use Remove link below to stop refresh without deleting retained data.",
            FontSize = 12,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        var tabs = new TabView
        {
            IsAddTabButtonVisible = false,
            TabWidthMode = TabViewWidthMode.SizeToContent,
        };
        foreach (var retained in CostingViewModel.RetainedSourceTables)
        {
            var isLinked = CostingViewModel.GetDatabaseTableLink(retained.Area) is not null;
            var preview = new CentralDataTablePreview(
                new CentralDataSourceObject(
                    retained.TableName,
                    retained.SchemaName,
                    retained.ObjectKind,
                    retained.DisplayName),
                retained.Columns,
                retained.Rows,
                retained.EffectiveIssues,
                PreviewLimit: 75);
            var scroller = PreviewScroller();
            scroller.MaxHeight = 520;
            scroller.Content = BuildDatabasePreviewTable(preview);
            tabs.TabItems.Add(new TabViewItem
            {
                Header = $"{AreaName(retained.Area)} · {retained.Rows.Count:N0} × {retained.Columns.Count:N0} · {(isLinked ? "linked" : "cached")}",
                Content = scroller,
                IsClosable = false,
            });
        }

        RetainedSourceTablesHost.Children.Add(tabs);
    }

    private static void AddDatabasePreviewCell(
        Grid table,
        int row,
        int column,
        string text,
        bool isHeader,
        bool hasError)
    {
        var cell = new Border
        {
            Padding = new Thickness(9, 7, 9, 7),
            BorderBrush = ResourceBrush("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Background = hasError
                ? new SolidColorBrush(Color.FromArgb(45, 255, 185, 0))
                : isHeader ? ResourceBrush("ControlFillColorSecondaryBrush") : null,
            Child = new TextBlock
            {
                Text = string.IsNullOrEmpty(text) ? "—" : text,
                FontSize = isHeader ? 12 : 13,
                FontWeight = isHeader
                    ? Microsoft.UI.Text.FontWeights.SemiBold
                    : Microsoft.UI.Text.FontWeights.Normal,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = isHeader ? TextWrapping.Wrap : TextWrapping.NoWrap,
            },
        };
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        table.Children.Add(cell);
    }

    private async Task ShowCentralDataMessageAsync(string title, string message)
    {
        var content = new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
            },
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        var window = new CentralDataWorkflowWindow(
            title,
            content,
            primaryButtonText: string.Empty,
            closeButtonText: "Close",
            requestedTheme: ActualTheme,
            size: CentralDataWorkflowWindowSize.Message,
            showPrimaryButton: false);
        await window.ShowAsync();
    }

    private static ScrollViewer PreviewScroller() => new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollMode = ScrollMode.Enabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollMode = ScrollMode.Enabled,
    };

    private static TextBlock SectionCaption(string text) => new()
    {
        Text = text,
        FontSize = 12,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
    };

    private static Brush ResourceBrush(string key) =>
        (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];

    private static bool ExistingStepEnabled(
        CentralDataTableLink? link,
        CentralDataQueryStepKind kind,
        bool fallback) =>
        link?.EffectiveQuerySteps.FirstOrDefault(step => step.Kind == kind)?.IsEnabled
        ?? fallback;

    private static readonly IReadOnlyList<DatabaseFilterOperatorOption>
        FilterOperatorOptions =
        [
            new("Equals", CentralDataFilterOperator.Equals),
            new("Does not equal", CentralDataFilterOperator.DoesNotEqual),
            new("Contains", CentralDataFilterOperator.Contains),
            new("Does not contain", CentralDataFilterOperator.DoesNotContain),
            new("Starts with", CentralDataFilterOperator.StartsWith),
            new("Ends with", CentralDataFilterOperator.EndsWith),
            new("Is blank", CentralDataFilterOperator.IsBlank),
            new("Is not blank", CentralDataFilterOperator.IsNotBlank),
        ];

    private static string FilterDescription(
        string column,
        CentralDataFilterOperator filterOperator,
        string value) =>
        filterOperator switch
        {
            CentralDataFilterOperator.IsBlank => $"Keep rows where {column} is blank.",
            CentralDataFilterOperator.IsNotBlank => $"Keep rows where {column} is not blank.",
            _ => $"Keep rows where {column} {FilterOperatorOptions.First(item => item.Value == filterOperator).Display.ToLowerInvariant()} '{value}'.",
        };

    private sealed record DatabaseColumnTransformEditor(
        CentralDataPreviewColumn Column,
        CheckBox Keep,
        TextBox Name);

    private sealed record DatabaseFilterOperatorOption(
        string Display,
        CentralDataFilterOperator Value);

    private sealed record DatabaseFilterEditor(
        Border Container,
        ComboBox Column,
        ComboBox Operator,
        TextBox Value)
    {
        public string SelectedColumn => Column.SelectedItem?.ToString() ?? string.Empty;

        public CentralDataFilterOperator SelectedOperator =>
            Operator.SelectedValue is CentralDataFilterOperator value
                ? value
                : CentralDataFilterOperator.Equals;
    }
}
