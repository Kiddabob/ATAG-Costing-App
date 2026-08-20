# Interactive 3D LIVE Preview feasibility

Status: feasibility confirmed and a measured prototype recommended; no 3D
renderer is implemented by this document.

## Decision

An interactive, rotatable 3D LIVE Preview is feasible in the existing WinUI 3
desktop application. The preferred implementation is one reusable Direct3D 11
renderer hosted by a WinUI 3 `SwapChainPanel`, exposed to the C# application
through a small C++/WinRT Windows Runtime component.

This is a successor mode inside the shared LIVE Preview contract, not a new
calculation system. The existing Domain and Application results remain the
only source of engineering and costing truth. The renderer consumes an
immutable, versioned scene and may never infer, repair or overwrite a costing
value.

A browser renderer embedded through WebView2 is not the preferred route. It
would add another runtime, input/focus boundary, packaging surface and scene
implementation to a native app that already has a suitable DirectX host.

## User experience

Every construction or physical module that materially benefits from geometry
may offer these preview modes:

- **Off**: no renderer, graphics device work, animation or retained scene;
- **Simple**: the existing lightweight 2D/schematic representation;
- **Detailed 2D**: bounded strand/layer detail where it is already useful;
- **Interactive 3D**: orbit, pan and zoom around the same approved scene.

Interactive 3D remains opt-in. It must use the existing responsive preview
dock, including its visible resize divider, wide right-hand placement and
compact bottom placement. Camera reset and fit-to-construction actions are
required. Pointer drag or touch rotates, the wheel or pinch zooms, and a
modified drag pans. Keyboard-accessible equivalents and a reduced-motion
experience are required.

The renderer should pause high detail while the camera is moving and restore
the requested level only after interaction settles. A user must always be able
to return to Simple or Off without losing costing state.

## Docking, external preview and module tool windows

The LIVE Preview is one app-wide capability, not a separate preview owned by
each page. A shared `PreviewSessionCoordinator` should hold the selected
document/session identifier, preview target, immutable scene revision, camera,
detail mode and follow/pin state. Costing pages and module windows publish an
explicit selected construction or module result to that coordinator.

The user can display the same preview session in one of three placements:

- **Docked right** on a sufficiently wide workspace;
- **Docked below** on a compact workspace;
- **Pop-out window** that is freely movable, resizable and maximisable on any
  connected display.

Only one live renderer host is attached at a time. Popping out detaches the
swap chain from the in-page host and attaches the shared renderer to the
external preview window; the old position shows a clear **Return preview here**
action rather than running a second scene or render loop. Closing the preview
window redocks it without losing its target, camera or mode.

The external window follows the main app's explicit preview selection by
default. **Pin this preview** holds the current construction while the user
edits or inspects other parts of the costing, and **Follow selection** resumes
app-wide following. Mere pointer hover must not change the selected preview.
The title and accessible description always identify the source costing,
module and revision so a detached view cannot be mistaken for another result.

Engineering modules may also be opened as secondary tool windows from either
the module directory or their position in a costing. A tool window is another
view over the same Application-layer document/module session, not a copied
calculator or a second saved document. Edits made in a linked tool window use
the same commands, validation, revision and undo boundary as the main
workspace. A module opened as a standalone scratch tool owns an explicitly
labelled draft and changes the costing only through an **Apply to costing**
action.

Tool windows are owned by the Costing App main window so they remain above that
app, but they are not globally always-on-top over unrelated applications. They
close with the main application. Closing or redocking a tool view does not
discard its module state. Each tool window has its own XAML view instance; no
XAML element is parented into two windows.

Use WinUI 3 secondary `Window`/`AppWindow` instances and keep strong references
to every open window. First use opens on the main app's current display. Later
uses restore the user's last size, state and display from LocalAppData, while
validating the placement against connected `DisplayArea` work areas so a
removed monitor cannot strand a window off-screen. Window placement is private
presentation state and contains no database link or costing data.

## Shared scene boundary

Create one immutable `cable-scene/v1` model in Application code. It should be
usable by COR, Dual Insulation and later Tape, Chalk, Foil, Braid, Lapscreen,
Drain, Flat and D-shape modules without page-specific rendering rules.

The scene carries only already-approved physical presentation data, including:

- construction order and stable component identifiers;
- conductor strand/group hierarchy and dimensions;
- layer inner/outer dimensions, colours and material appearance hints;
- wrap, braid and lay direction, pitch and overlap when supplied by an
  authoritative result;
- visibility, selection and explanation labels;
- whether a dimension is measured, calculated, retained, estimated or merely
  illustrative.

The scene builder owns validation and finite-value checks. Invalid or
incomplete geometry produces a labelled unavailable state rather than an
invented shape. The renderer owns camera, level of detail, meshes, shading and
hit testing only.

## Rendering architecture

Recommended layers:

1. existing Domain/Application calculations;
2. pure scene builder and bounded scene tests;
3. small C++/WinRT renderer component using Direct3D 11;
4. WinUI 3 `SwapChainPanel` host inside `ModuleWorkspaceShell`;
5. shared preview-session and secondary-window coordinators;
6. XAML overlay for labels, mode controls, status and accessible alternatives.

Use one graphics device and one active preview surface, not a device or render
loop per module. Cache reusable tube, strand, ring and wrap meshes. Rebuild
only the affected scene node after a coherent calculation revision. Resize the
swap chain using the panel's composition scale, and call `SetSwapChain(null)`
when the surface is detached or the device graph must be released.

The swap-chain surface is opaque. Mica/Acrylic styling stays in the surrounding
XAML dock rather than being expected to show through the DirectX content.

## Performance and compatibility contract

The target laptop is an older Windows 11-capable machine with integrated
graphics, not the development PC. The prototype must establish measured
budgets before it becomes a product feature:

- zero continuous frames and negligible CPU/GPU work while the scene and
  camera are unchanged;
- render on coherent data, size, camera or mode changes only;
- cap interactive orbit redraw rate and dynamically reduce detail;
- enforce explicit strand, vertex, index, draw-call and memory budgets;
- keep costly mesh generation off the UI thread;
- detach the surface and release scene resources while Off or unloaded;
- support a tested Direct3D WARP software fallback;
- recover from `DXGI_ERROR_DEVICE_REMOVED` and
  `DXGI_ERROR_DEVICE_RESET` without losing the costing;
- retain Simple mode when hardware, driver or performance acceptance fails.

The renderer must never create one XAML element per strand, face or braid
crossing. The bounded Braid optimisation is the CPU-side precedent: coherent
revision invalidation and immutable geometry happen before GPU presentation.

## Prototype and delivery slices

### Phase 1 - development-only proof

Build an isolated canned single-core scene. Prove orbit, pan, zoom, reset,
resize, DPI changes, hardware and WARP creation, device-loss recovery, no idle
rendering and clean surface teardown. Record frame time, UI-thread time,
allocations and memory on both development and representative low-spec
hardware. Do not connect live costing state yet.

### Phase 2 - shared host and scene

Introduce `cable-scene/v1`, a renderer-agnostic host contract and the shared
mode/camera controls. Add dock/pop-out transfer, follow/pin selection and
display-safe window placement without allowing two live surfaces. Preserve the
existing 2D modes as fallbacks and add automated scene budgets and
finite-geometry tests.

### Phase 3 - COR

Render the accepted single-core conductor and insulation geometry. Compare the
same input in 2D and 3D, including rope lay and simplified/detailed strands.

### Phase 4 - Dual and add-on modules

Add the second insulation layer and then Tape, Chalk, Foil, Braid, Lapscreen
and Drain in physical inside-to-outside order. Each module consumes its
approved results and carries independent visual acceptance fixtures.

### Phase 5 - Flat and D-shape

Reuse the scene and renderer after those construction and costing models are
approved. Do not make 3D geometry the reason to invent their engineering rules.

### Phase 6 - document imagery

If required later, generate a deterministic static snapshot from the same
scene for datasheets or quotations. Interactive state is not embedded in PDF.

## Prototype exit gate

The prototype can proceed into COR integration only when all of these are
demonstrated:

- stable launch, detach, reattach and app shutdown;
- one renderer transfers between docked and pop-out hosts without duplicate
  redraws or stale selection;
- preview and module tool windows restore safely across multiple displays and
  recover onto an available display after monitor removal;
- camera interaction remains responsive on representative low-spec hardware;
- idle preview performs no continuous redraw;
- bounded Simple and Detailed scenes remain within recorded budgets;
- WARP fallback and device-loss recovery work;
- 2D fallback remains immediately available;
- no engineering or costing result changes;
- the new native component is included in clean build, publish, installer and
  updater verification.

## Primary Microsoft references

- [WinUI 3 SwapChainPanel](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.swapchainpanel)
- [ISwapChainPanelNative::SetSwapChain](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain)
- [DirectX and XAML interop](https://learn.microsoft.com/en-us/windows/uwp/gaming/directx-and-xaml-interop)
- [Direct3D 11 WARP device](https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-devices-create-warp)
- [Handling device-lost scenarios](https://learn.microsoft.com/en-us/windows/uwp/gaming/handling-device-lost-scenarios)
- [ID3D11Device::GetDeviceRemovedReason](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-getdeviceremovedreason)
- [Show multiple windows for an app](https://learn.microsoft.com/en-us/windows/apps/develop/ui/multiple-windows)
- [AppWindow](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow)
