using ATAG.Costing.Domain.Braiding;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATAG.Costing.WinUI.ViewModels;

public sealed class BuncherLayChoice
{
    public BuncherLayChoice(BuncherLaySetting setting)
    {
        Setting = setting;
    }

    public BuncherLaySetting Setting { get; }

    public string LayDisplay => $"{Setting.LayLengthMillimetres:0.##} mm";

    public string MachineDisplay => $"{Setting.BuncherSize} buncher";

    public string GearsDisplay =>
        $"Gear A {Setting.GearA} · Gear B {Setting.GearB}";

    public string TraceDisplay =>
        $"{Setting.LayLengthMillimetres:0.##} mm target lay → " +
        $"{Setting.BuncherSize} buncher → gears {Setting.GearA} and {Setting.GearB}.";
}

public partial class BuncherLayViewModel : ObservableObject
{
    public IReadOnlyList<BuncherLayChoice> LayChoices { get; } =
        BraidReferenceTables.BuncherLaySettings
            .Select(setting => new BuncherLayChoice(setting))
            .ToArray();

    [ObservableProperty]
    public partial BuncherLayChoice? SelectedChoice { get; set; }

    public string MachineDisplay => SelectedChoice?.MachineDisplay ?? "—";

    public string GearsDisplay => SelectedChoice?.GearsDisplay ?? "—";

    public string TraceDisplay =>
        SelectedChoice?.TraceDisplay ??
        "Choose a target lay length to reveal the exact workbook-table machine and gear pair.";

    public string StatusDisplay => SelectedChoice is null
        ? "Choose a target lay length."
        : "Exact retained workbook-table match.";

    public double SelectedLayLengthMillimetres =>
        SelectedChoice?.Setting.LayLengthMillimetres ?? 0d;

    public BuncherLayViewModel()
    {
        SelectedChoice = LayChoices.FirstOrDefault(choice =>
            Math.Abs(choice.Setting.LayLengthMillimetres - 19.43d) < 0.001d);
    }

    partial void OnSelectedChoiceChanged(BuncherLayChoice? value)
    {
        OnPropertyChanged(nameof(MachineDisplay));
        OnPropertyChanged(nameof(GearsDisplay));
        OnPropertyChanged(nameof(TraceDisplay));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(SelectedLayLengthMillimetres));
    }
}
