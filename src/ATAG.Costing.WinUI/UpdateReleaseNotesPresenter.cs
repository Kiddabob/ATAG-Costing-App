using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Converts the cumulative plain-text release feed into compact, readable
/// version cards without introducing a web renderer into the updater.
/// </summary>
internal static class UpdateReleaseNotesPresenter
{
    public static FrameworkElement Create(string? releaseNotes)
    {
        var sections = Parse(releaseNotes);
        var stack = new StackPanel
        {
            Spacing = 12,
        };

        foreach (var section in sections)
        {
            var content = new StackPanel
            {
                Spacing = 9,
            };
            content.Children.Add(new TextBlock
            {
                Text = section.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });

            foreach (var line in section.Lines)
            {
                content.Children.Add(CreateLine(line));
            }

            stack.Children.Add(new Border
            {
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                BorderBrush = ResourceBrush("CardStrokeColorDefaultBrush"),
                Background = ResourceBrush("CardBackgroundFillColorDefaultBrush"),
                Child = content,
            });
        }

        return stack;
    }

    private static FrameworkElement CreateLine(string rawLine)
    {
        var line = rawLine.Trim();
        if (line.StartsWith("- ", StringComparison.Ordinal))
        {
            var row = new Grid
            {
                ColumnSpacing = 10,
            };
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
            });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            row.Children.Add(new FontIcon
            {
                Glyph = "\uE73E",
                FontSize = 9,
                Margin = new Thickness(1, 6, 0, 0),
                Foreground = ResourceBrush("AccentTextFillColorPrimaryBrush"),
            });
            var text = Paragraph(line[2..]);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            return row;
        }

        if (line.StartsWith('#'))
        {
            return new TextBlock
            {
                Text = line.TrimStart('#').Trim(),
                Margin = new Thickness(0, 3, 0, 0),
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            };
        }

        return Paragraph(line);
    }

    private static TextBlock Paragraph(string text) =>
        new()
        {
            Text = text,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        };

    private static IReadOnlyList<ReleaseNotesSection> Parse(string? notes)
    {
        var normalized = string.IsNullOrWhiteSpace(notes)
            ? "No release notes were supplied for this version."
            : notes.Trim();
        var lines = MergeWrappedBulletLines(normalized);
        var sections = new List<ReleaseNotesSection>();
        string? title = null;
        var body = new List<string>();

        void AddSection()
        {
            if (title is null && body.Count == 0)
            {
                return;
            }

            sections.Add(new ReleaseNotesSection(
                title ?? "Release notes",
                body.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray()));
            body.Clear();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("Version ", StringComparison.OrdinalIgnoreCase))
            {
                AddSection();
                title = line;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                body.Add(line);
            }
        }

        AddSection();
        return sections;
    }

    private static IReadOnlyList<string> MergeWrappedBulletLines(string notes)
    {
        var physicalLines = notes
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
        var logicalLines = new List<string>();

        foreach (var physicalLine in physicalLines)
        {
            var line = physicalLine.Trim();
            var isIndentedContinuation =
                physicalLine.Length > 0 &&
                char.IsWhiteSpace(physicalLine[0]) &&
                !line.StartsWith("- ", StringComparison.Ordinal) &&
                logicalLines.Count > 0 &&
                logicalLines[^1].StartsWith("- ", StringComparison.Ordinal);
            if (isIndentedContinuation)
            {
                logicalLines[^1] += " " + line;
                continue;
            }

            logicalLines.Add(line);
        }

        return logicalLines;
    }

    private static Brush ResourceBrush(string key) =>
        (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];

    private sealed record ReleaseNotesSection(
        string Title,
        IReadOnlyList<string> Lines);
}
