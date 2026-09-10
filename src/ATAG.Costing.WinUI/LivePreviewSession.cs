using System.Globalization;
using System.Numerics;

namespace ATAG.Costing.WinUI;

internal enum LivePreviewMode
{
    Simple = 0,
    Detailed = 1,
    Interactive3D = 2,
}

internal readonly record struct LivePreview3DStrand(
    float Y,
    float Z,
    float Radius);

internal sealed record LivePreview3DScene(
    float InsulationRadius,
    float ConductorRadius,
    Vector4 InsulationColour,
    Vector4 ConductorColour,
    IReadOnlyList<LivePreview3DStrand> Strands,
    string Description,
    bool IsDetailed)
{
    public static LivePreview3DScene Empty { get; } = new(
        InsulationRadius: 1.10f,
        ConductorRadius: 0.42f,
        InsulationColour: FromHex("#6C7A89"),
        ConductorColour: FromHex("#C7782E"),
        Strands: [],
        Description: "Choose a conductor and finished core size.",
        IsDetailed: false);

    public static Vector4 FromHex(string? value)
    {
        var normalized = value?.Trim().TrimStart('#');
        if (normalized?.Length != 6 ||
            !uint.TryParse(
                normalized,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var colour))
        {
            return new Vector4(0.42f, 0.48f, 0.54f, 1f);
        }

        return new Vector4(
            ((colour >> 16) & 0xff) / 255f,
            ((colour >> 8) & 0xff) / 255f,
            (colour & 0xff) / 255f,
            1f);
    }
}

internal readonly record struct LivePreviewCameraState(
    float Yaw,
    float Pitch,
    float Distance,
    Vector3 Target)
{
    public static LivePreviewCameraState Default { get; } = new(
        Yaw: 0.28f,
        Pitch: 0.08f,
        Distance: 10.5f,
        Target: new Vector3(0.025f, 0f, 0f));
}

/// <summary>
/// App-owned preview state. Docked and detached surfaces consume the same
/// scene, mode and camera state, so moving the preview never creates a second
/// source of truth or a second active renderer.
/// </summary>
internal sealed class LivePreviewSession
{
    private LivePreviewMode _mode = LivePreviewMode.Simple;
    private LivePreview3DScene _scene = LivePreview3DScene.Empty;

    public event EventHandler? ModeChanged;

    public event EventHandler? SceneChanged;

    public event EventHandler? DriverChanged;

    public PreviewGeometry? Geometry { get; private set; }

    public string GeometryDescription { get; private set; } = string.Empty;

    private bool _preferSoftware;

    public bool PreferSoftware
    {
        get => _preferSoftware;
        set
        {
            if (_preferSoftware == value) return;
            _preferSoftware = value;
            DriverChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetGeometry(PreviewGeometry geometry, string description)
    {
        Geometry = geometry;
        GeometryDescription = description;
        SceneChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearGeometry()
    {
        Geometry = null;
        GeometryDescription = string.Empty;
    }

    public LivePreviewMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
            {
                return;
            }

            _mode = value;
            ModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public LivePreview3DScene Scene
    {
        get => _scene;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _scene = value;
            SceneChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public LivePreviewCameraState Camera { get; set; } =
        LivePreviewCameraState.Default;

    public bool ForceWarp => PreferSoftware || string.Equals(
        Environment.GetEnvironmentVariable("ATAG_COSTING_3D_FORCE_WARP"),
        "1",
        StringComparison.Ordinal);
}
