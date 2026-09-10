using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using SharpGen.Runtime;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Event-driven D3D11 renderer for the shared LIVE Preview session. It owns
/// presentation resources only; costing rules stay in the view models and
/// renderer-agnostic scene builder.
/// </summary>
internal sealed class Preview3DRenderer : IDisposable
{
    private static readonly Guid SwapChainPanelNativeGuid =
        new("63aad0b8-7c24-40ff-85a8-640d944cc325");

    private static readonly FeatureLevel[] SupportedFeatureLevels =
    [
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0,
        FeatureLevel.Level_10_1,
        FeatureLevel.Level_10_0,
    ];

    private readonly SwapChainPanel _panel;
    private readonly Action<Preview3DStatistics> _statisticsChanged;
    private LivePreview3DScene _scene;
    private PreviewGeometry? _geometry;
    private readonly Action<string>? _renderFailed;
    private readonly DispatcherQueueTimer _renderTimer;
    private bool _isRecovering;
    private bool _renderUnavailable;
    private IDXGIFactory2? _factory;
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private IDXGISwapChain1? _swapChain;
    private ID3D11Texture2D? _backBuffer;
    private ID3D11RenderTargetView? _renderTargetView;
    private ID3D11Texture2D? _depthTexture;
    private ID3D11DepthStencilView? _depthStencilView;
    private ID3D11Buffer? _vertexBuffer;
    private ID3D11Buffer? _indexBuffer;
    private ID3D11Buffer? _sceneConstantBuffer;
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;
    private ID3D11InputLayout? _inputLayout;
    private ID3D11RasterizerState? _rasterizerState;
    private IntPtr _panelNative;
    private SetSwapChainDelegate? _setSwapChain;
    private uint _indexCount;
    private uint _pixelWidth;
    private uint _pixelHeight;
    private bool _isWarp;
    private bool _isDisposed;
    private long _frameCount;
    // Keep the default camera predominantly side-on. A large negative yaw
    // places the eye close to the cable's cut end, which makes that end fill
    // the viewport and hides the construction that the proof is meant to show.
    private float _yaw;
    private float _pitch;
    private float _distance;
    private Vector3 _target;

    public Preview3DRenderer(
        SwapChainPanel panel,
        Action<Preview3DStatistics> statisticsChanged,
        LivePreview3DScene scene,
        LivePreviewCameraState camera,
        PreviewGeometry? geometry = null,
        Action<string>? renderFailed = null)
    {
        _panel = panel;
        _statisticsChanged = statisticsChanged;
        _scene = scene;
        _geometry = geometry;
        _renderFailed = renderFailed;
        _renderTimer = panel.DispatcherQueue.CreateTimer();
        _renderTimer.Interval = TimeSpan.FromMilliseconds(33);
        _renderTimer.IsRepeating = false;
        _renderTimer.Tick += RenderTimer_Tick;
        ApplyCamera(camera);
    }

    public LivePreviewCameraState CameraState => new(
        _yaw,
        _pitch,
        _distance,
        _target);

    public void Initialize(bool forceWarp)
    {
        ThrowIfDisposed();
        DisposeDeviceResources(detachPanel: true);
        CreateDevice(forceWarp);
        CreatePipelineResources();
        Resize(
            _panel.ActualWidth,
            _panel.ActualHeight,
            _panel.XamlRoot?.RasterizationScale ?? 1d);
    }

    public void Resize(double logicalWidth, double logicalHeight, double scale)
    {
        ThrowIfDisposed();
        if (_device is null || _context is null)
        {
            return;
        }

        var width = (uint)Math.Max(1, Math.Round(logicalWidth * scale));
        var height = (uint)Math.Max(1, Math.Round(logicalHeight * scale));
        if (width == _pixelWidth && height == _pixelHeight && _swapChain is not null)
        {
            Render();
            return;
        }

        _context.OMSetRenderTargets(
            Array.Empty<ID3D11RenderTargetView>(),
            null);
        DisposeSurfaceResources();

        _pixelWidth = width;
        _pixelHeight = height;
        if (_swapChain is null)
        {
            CreateSwapChain();
        }
        else
        {
            _swapChain.ResizeBuffers(
                2,
                width,
                height,
                Format.B8G8R8A8_UNorm,
                SwapChainFlags.None);
        }

        ApplyCompositionScale(scale);
        CreateSurfaceResources();
        Render();
    }

    public void UpdateScene(LivePreview3DScene scene)
    {
        ThrowIfDisposed();
        _scene = scene;
        if (_device is null)
        {
            return;
        }

        CreateGeometryBuffers();
        Render();
    }

    public void UpdateGeometry(PreviewGeometry geometry)
    {
        ThrowIfDisposed();
        _geometry = geometry;
        if (_device is null) return;
        CreateGeometryBuffers();
        Render();
    }

    private void ApplyCompositionScale(double scale)
    {
        if (_swapChain is null)
        {
            return;
        }

        // SwapChainPanel is measured in DIPs while its composition swap chain
        // is deliberately allocated in physical pixels. Without the inverse
        // transform, Windows treats each physical buffer pixel as one DIP and
        // clips the centred scene into the lower-right of a scaled display.
        var safeScale = (float)Math.Max(0.1d, scale);
        using var swapChain2 = _swapChain.QueryInterface<IDXGISwapChain2>();
        swapChain2.MatrixTransform = Matrix3x2.CreateScale(1f / safeScale);
    }

    public void Orbit(float deltaX, float deltaY)
    {
        _yaw += deltaX * 0.012f;
        _pitch = Math.Clamp(_pitch + (deltaY * 0.012f), -1.55f, 1.55f);
        Render();
    }

    public void Pan(float deltaX, float deltaY)
    {
        var viewScale = _distance * 0.0018f;
        var right = new Vector3(MathF.Cos(_yaw), 0f, -MathF.Sin(_yaw));
        var up = Vector3.UnitY;
        _target += (-right * deltaX * viewScale) + (up * deltaY * viewScale);
        Render();
    }

    public void Zoom(float wheelDelta)
    {
        var factor = MathF.Exp(-wheelDelta * 0.0011f);
        _distance = Math.Clamp(_distance * factor, 0.01f, 100f);
        Render();
    }

    public void ResetCamera()
    {
        ApplyCamera(LivePreviewCameraState.Default);
        Render();
    }

    public void InspectSurface()
    {
        var camera = LivePreviewCameraState.Default with
        {
            Yaw = 0,
            Pitch = 0,
            Target = _geometry?.InspectionTarget ?? Vector3.Zero,
            Distance = _geometry?.InspectionDistance ?? 1f,
        };
        ApplyCamera(camera);
        Render();
    }

    private void ApplyCamera(LivePreviewCameraState camera)
    {
        _yaw = camera.Yaw;
        _pitch = camera.Pitch;
        _distance = camera.Distance;
        _target = camera.Target;
    }

    public void SimulateDeviceLoss()
    {
        var useWarp = _isWarp;
        Initialize(useWarp);
    }

    public void Render()
    {
        // Pointer, resize and scene changes share a capped one-shot frame.
        // There is no recurring/idle rendering loop.
        if (!_isDisposed && !_renderUnavailable && !_renderTimer.IsRunning)
            _renderTimer.Start();
    }

    private void RenderTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            RenderFrame();
        }
        catch (Exception exception)
        {
            _renderUnavailable = true;
            _renderFailed?.Invoke(exception.Message);
            Program.Log($"LIVE Preview rendering stopped: {exception}");
        }
    }

    private void RenderFrame()
    {
        if (_isDisposed ||
            _context is null ||
            _swapChain is null ||
            _renderTargetView is null ||
            _depthStencilView is null ||
            _vertexBuffer is null ||
            _indexBuffer is null ||
            _sceneConstantBuffer is null ||
            _vertexShader is null ||
            _pixelShader is null ||
            _inputLayout is null ||
            _rasterizerState is null ||
            _pixelHeight == 0)
        {
            return;
        }

        var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        var aspect = _pixelWidth / (float)_pixelHeight;
        var horizontal = MathF.Cos(_pitch);
        var eye = _target + new Vector3(
            horizontal * MathF.Sin(_yaw),
            MathF.Sin(_pitch),
            horizontal * MathF.Cos(_yaw)) * _distance;
        var world = Matrix4x4.Identity;
        var view = Matrix4x4.CreateLookAt(eye, _target, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4.2f,
            Math.Max(0.1f, aspect),
            Math.Clamp(_distance * 0.005f, 0.0001f, 0.1f),
            Math.Max(100f, _distance * 4));
        var constants = new SceneConstants
        {
            WorldViewProjection = Matrix4x4.Transpose(world * view * projection),
            // Stable studio-style key light. The pixel shader combines this
            // with a soft fill, a narrow highlight and view-dependent contour
            // bands so adjacent strands remain legible while orbiting.
            LightDirection = new Vector4(-0.38f, -0.78f, -0.50f, 0f),
            CameraPosition = new Vector4(eye, 1f),
        };

        _context.UpdateSubresource(in constants, _sceneConstantBuffer);
        _context.ClearRenderTargetView(
            _renderTargetView,
            new Color4(0.035f, 0.055f, 0.075f, 1f));
        _context.ClearDepthStencilView(
            _depthStencilView,
            DepthStencilClearFlags.Depth,
            1f,
            0);
        _context.OMSetRenderTargets(_renderTargetView, _depthStencilView);
        _context.RSSetViewport(new Viewport(0, 0, _pixelWidth, _pixelHeight));
        _context.RSSetState(_rasterizerState);
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.IASetInputLayout(_inputLayout);
        _context.IASetVertexBuffer(
            0,
            _vertexBuffer,
            (uint)Marshal.SizeOf<PreviewVertex>());
        _context.IASetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
        _context.VSSetShader(_vertexShader);
        _context.VSSetConstantBuffer(0, _sceneConstantBuffer);
        _context.PSSetShader(_pixelShader);
        _context.DrawIndexed(_indexCount, 0, 0);

        var presentResult = _swapChain.Present(1, PresentFlags.None);
        if (presentResult.Failure)
        {
            if (_isRecovering)
                throw new InvalidOperationException("The graphics device could not recover. Select Simple 2D or retry software rendering.");
            _isRecovering = true;
            Initialize(forceWarp: true);
            return;
        }

        _isRecovering = false;

        var elapsed = Stopwatch.GetElapsedTime(started);
        var allocated = Math.Max(
            0,
            GC.GetAllocatedBytesForCurrentThread() - beforeAllocations);
        _frameCount++;
        _statisticsChanged(new Preview3DStatistics(
            _isWarp ? "WARP software" : "Hardware",
            _frameCount,
            elapsed.TotalMilliseconds,
            _pixelWidth,
            _pixelHeight,
            allocated,
            GC.GetTotalMemory(false)));
    }

    private void CreateDevice(bool forceWarp)
    {
        var flags = DeviceCreationFlags.BgraSupport;
        var result = D3D11CreateDevice(
            IntPtr.Zero,
            forceWarp ? DriverType.Warp : DriverType.Hardware,
            flags,
            SupportedFeatureLevels,
            out _device,
            out _,
            out _context);

        if (result.Failure && !forceWarp)
        {
            _device?.Dispose();
            _context?.Dispose();
            result = D3D11CreateDevice(
                IntPtr.Zero,
                DriverType.Warp,
                flags,
                SupportedFeatureLevels,
                out _device,
                out _,
                out _context);
            _isWarp = true;
        }
        else
        {
            _isWarp = forceWarp;
        }

        result.CheckError();
        _factory = CreateDXGIFactory1<IDXGIFactory2>();
    }

    private void CreateSwapChain()
    {
        if (_factory is null || _device is null)
        {
            throw new InvalidOperationException("The D3D11 device is not ready.");
        }

        var description = new SwapChainDescription1
        {
            Width = _pixelWidth,
            Height = _pixelHeight,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.None,
        };
        _swapChain = _factory.CreateSwapChainForComposition(
            _device,
            description,
            null);
        // Vortice's ISwapChainPanelNative binding carries the Windows.UI.Xaml
        // interface IID. WinUI 3 exposes the Microsoft.UI.Xaml DX interop IID,
        // so query that interface explicitly and call its single native method.
        var panelInspectable = WinRT.MarshalInspectable<SwapChainPanel>
            .FromManaged(_panel);
        try
        {
            var nativeGuid = SwapChainPanelNativeGuid;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(
                panelInspectable,
                in nativeGuid,
                out _panelNative));
        }
        finally
        {
            Marshal.Release(panelInspectable);
        }

        var vtable = Marshal.ReadIntPtr(_panelNative);
        var setSwapChainPointer = Marshal.ReadIntPtr(
            vtable,
            3 * IntPtr.Size);
        _setSwapChain = Marshal.GetDelegateForFunctionPointer<SetSwapChainDelegate>(
            setSwapChainPointer);
        Marshal.ThrowExceptionForHR(_setSwapChain(
            _panelNative,
            _swapChain.NativePointer));
    }

    private void CreateSurfaceResources()
    {
        if (_device is null || _swapChain is null)
        {
            return;
        }

        _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTargetView = _device.CreateRenderTargetView(_backBuffer);
        var depthDescription = new Texture2DDescription(
            Format.D32_Float,
            _pixelWidth,
            _pixelHeight,
            1,
            1,
            BindFlags.DepthStencil);
        _depthTexture = _device.CreateTexture2D(depthDescription);
        _depthStencilView = _device.CreateDepthStencilView(_depthTexture);
    }

    private void CreatePipelineResources()
    {
        if (_device is null)
        {
            throw new InvalidOperationException("The D3D11 device is not ready.");
        }

        var shaderPath = Path.Combine(
            AppContext.BaseDirectory,
            "Shaders",
            "LivePreview3D.hlsl");
        ReadOnlyMemory<byte> vertexByteCode = Compiler.CompileFromFile(
            shaderPath,
            "VSMain",
            "vs_5_0");
        ReadOnlyMemory<byte> pixelByteCode = Compiler.CompileFromFile(
            shaderPath,
            "PSMain",
            "ps_5_0");
        _vertexShader = _device.CreateVertexShader(vertexByteCode.Span);
        _pixelShader = _device.CreatePixelShader(pixelByteCode.Span);
        _inputLayout = _device.CreateInputLayout(
        [
            new InputElementDescription(
                "POSITION",
                0,
                Format.R32G32B32_Float,
                0,
                0),
            new InputElementDescription(
                "NORMAL",
                0,
                Format.R32G32B32_Float,
                12,
                0),
            new InputElementDescription(
                "COLOR",
                0,
                Format.R32G32B32A32_Float,
                24,
                0),
        ],
            vertexByteCode.Span);

        CreateGeometryBuffers();
        _sceneConstantBuffer = _device.CreateBuffer(
            (uint)Marshal.SizeOf<SceneConstants>(),
            BindFlags.ConstantBuffer);
        _rasterizerState = _device.CreateRasterizerState(
            new RasterizerDescription(CullMode.None, FillMode.Solid)
            {
                DepthClipEnable = true,
            });
    }

    private void CreateGeometryBuffers()
    {
        if (_device is null)
        {
            return;
        }

        _indexBuffer?.Dispose();
        _indexBuffer = null;
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        var geometry = _geometry ?? Preview3DGeometry.Create(_scene);
        if (geometry.Vertices.Length == 0 || geometry.Indices.Length == 0)
        {
            _indexCount = 0;
            return;
        }
        _vertexBuffer = _device.CreateBuffer(
            geometry.Vertices,
            BindFlags.VertexBuffer);
        _indexBuffer = _device.CreateBuffer(
            geometry.Indices,
            BindFlags.IndexBuffer);
        _indexCount = (uint)geometry.Indices.Length;
    }

    private void DisposeSurfaceResources()
    {
        _depthStencilView?.Dispose();
        _depthStencilView = null;
        _depthTexture?.Dispose();
        _depthTexture = null;
        _renderTargetView?.Dispose();
        _renderTargetView = null;
        _backBuffer?.Dispose();
        _backBuffer = null;
    }

    private void DisposeDeviceResources(bool detachPanel)
    {
        if (detachPanel)
        {
            try
            {
                if (_panelNative != IntPtr.Zero && _setSwapChain is not null)
                {
                    Marshal.ThrowExceptionForHR(_setSwapChain(
                        _panelNative,
                        IntPtr.Zero));
                }
            }
            catch
            {
                // The panel can already be disconnected during window teardown.
            }
        }

        DisposeSurfaceResources();
        _rasterizerState?.Dispose();
        _rasterizerState = null;
        _inputLayout?.Dispose();
        _inputLayout = null;
        _pixelShader?.Dispose();
        _pixelShader = null;
        _vertexShader?.Dispose();
        _vertexShader = null;
        _sceneConstantBuffer?.Dispose();
        _sceneConstantBuffer = null;
        _indexBuffer?.Dispose();
        _indexBuffer = null;
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        _swapChain?.Dispose();
        _swapChain = null;
        if (_panelNative != IntPtr.Zero)
        {
            Marshal.Release(_panelNative);
            _panelNative = IntPtr.Zero;
        }

        _setSwapChain = null;
        _context?.ClearState();
        _context?.Flush();
        _context?.Dispose();
        _context = null;
        _device?.Dispose();
        _device = null;
        _factory?.Dispose();
        _factory = null;
        _pixelWidth = 0;
        _pixelHeight = 0;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _renderTimer.Stop();
        _renderTimer.Tick -= RenderTimer_Tick;
        DisposeDeviceResources(detachPanel: true);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetSwapChainDelegate(
        IntPtr panel,
        IntPtr swapChain);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SceneConstants
    {
        public Matrix4x4 WorldViewProjection;
        public Vector4 LightDirection;
        public Vector4 CameraPosition;
    }
}

internal readonly record struct Preview3DStatistics(
    string Driver,
    long FrameCount,
    double RenderMilliseconds,
    uint PixelWidth,
    uint PixelHeight,
    long AllocatedBytes,
    long ManagedMemoryBytes);

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct PreviewVertex(
    Vector3 Position,
    Vector3 Normal,
    Vector4 Colour);

internal sealed record PreviewGeometry(
    PreviewVertex[] Vertices,
    uint[] Indices,
    Vector3? InspectionTarget = null,
    float InspectionDistance = 1f);

internal static class Preview3DGeometry
{
    private const int Segments = 24;

    public static PreviewGeometry Create(LivePreview3DScene scene)
    {
        var vertices = new List<PreviewVertex>();
        var indices = new List<uint>();

        AddCylinder(
            vertices,
            indices,
            -2.45f,
            0.25f,
            scene.InsulationRadius,
            Vector2.Zero,
            scene.InsulationColour,
            capStart: true,
            capEnd: false);

        if (!scene.IsDetailed || scene.Strands.Count == 0)
        {
            AddCylinder(
                vertices,
                indices,
                -0.08f,
                2.50f,
                scene.ConductorRadius,
                Vector2.Zero,
                scene.ConductorColour,
                capStart: false,
                capEnd: true);
        }
        else
        {
            foreach (var strand in scene.Strands)
            {
                AddCylinder(
                    vertices,
                    indices,
                    -0.08f,
                    2.50f,
                    strand.Radius,
                    new Vector2(strand.Y, strand.Z),
                    scene.ConductorColour,
                    capStart: false,
                    capEnd: true);
            }
        }

        return new PreviewGeometry(vertices.ToArray(), indices.ToArray());
    }

    private static void AddCylinder(
        List<PreviewVertex> vertices,
        List<uint> indices,
        float startX,
        float endX,
        float radius,
        Vector2 centre,
        Vector4 colour,
        bool capStart,
        bool capEnd)
    {
        var sideBase = (uint)vertices.Count;
        for (var segment = 0; segment <= Segments; segment++)
        {
            var angle = (MathF.PI * 2f * segment) / Segments;
            var normal = new Vector3(0f, MathF.Cos(angle), MathF.Sin(angle));
            var y = centre.X + (normal.Y * radius);
            var z = centre.Y + (normal.Z * radius);
            vertices.Add(new PreviewVertex(
                new Vector3(startX, y, z),
                normal,
                colour));
            vertices.Add(new PreviewVertex(
                new Vector3(endX, y, z),
                normal,
                colour));
        }

        for (uint segment = 0; segment < Segments; segment++)
        {
            var a = sideBase + (segment * 2);
            var b = a + 1;
            var c = a + 2;
            var d = a + 3;
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
            indices.Add(b);
            indices.Add(c);
            indices.Add(d);
        }

        if (capStart)
        {
            AddCap(vertices, indices, startX, radius, centre, colour, isStart: true);
        }

        if (capEnd)
        {
            AddCap(vertices, indices, endX, radius, centre, colour, isStart: false);
        }
    }

    private static void AddCap(
        List<PreviewVertex> vertices,
        List<uint> indices,
        float x,
        float radius,
        Vector2 centre,
        Vector4 colour,
        bool isStart)
    {
        var normal = isStart ? -Vector3.UnitX : Vector3.UnitX;
        var centreIndex = (uint)vertices.Count;
        vertices.Add(new PreviewVertex(
            new Vector3(x, centre.X, centre.Y),
            normal,
            colour));
        var rimBase = (uint)vertices.Count;
        for (var segment = 0; segment < Segments; segment++)
        {
            var angle = (MathF.PI * 2f * segment) / Segments;
            vertices.Add(new PreviewVertex(
                new Vector3(
                    x,
                    centre.X + (MathF.Cos(angle) * radius),
                    centre.Y + (MathF.Sin(angle) * radius)),
                normal,
                colour));
        }

        for (uint segment = 0; segment < Segments; segment++)
        {
            var current = rimBase + segment;
            var next = rimBase + ((segment + 1) % Segments);
            indices.Add(centreIndex);
            indices.Add(isStart ? next : current);
            indices.Add(isStart ? current : next);
        }
    }
}
