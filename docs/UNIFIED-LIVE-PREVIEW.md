# Unified LIVE Preview

The shared preview consumes an immutable `CablePreviewScene` from the
Application layer. A costing or engineering page supplies a scene adapter;
`UnifiedLivePreview` supplies the presentation and lifecycle. The preview is
presentation only: all material quantities, dimensions used in costing,
production settings and quoted prices remain owned by the existing modules.

## Modes and consumers

Every connected page offers Simple 2D and Detailed 2D, each with a cross-section
and side view, plus interactive orbital 3D. The common control supplies mode
selection, camera reset, keyboard/pointer camera controls, software rendering,
and an owned pop-out window. The existing responsive workspace rail and its
preview On/Off switch remain the entry point.

Current consumers are COR, Dual Insulation, Braid Coverage, Buncher Lay and
Coil. Braid can select the 16- or 24-carrier result. Physical construction
components describe conductor/strand bundles, insulation, equal cable cores,
braid, tape, foil, drain/screen elements, winding bar and coiled cable.
Round, flat and D-shaped sections use the same sweep representation.

Optional Dual layer checkboxes do not yet supply physical thickness, width or
lay information. The adapter explains which selected layers need those inputs
before they can be drawn. A future layer module can supply its approved
dimensions through the same component interface. The preview does not guess
missing material dimensions.

## Code map

- `Application/Visualisation/CablePreviewScene.cs`: immutable, unit-labelled
  component, section, drawing, mesh and budget contracts.
- `CablePreviewPaths.cs`: straight, helical and coiled presentation paths.
- `CablePreviewGeometry.cs`: validation, bounded section/sweep geometry and
  2D projection. No WinUI, database or file dependency.
- `ModulePreviewSceneFactory.cs`: construction-specific adapters over physical
  inputs and existing calculation results.
- `WinUI/PreviewSceneAdapters.cs`: maps visible view-model state into scenes.
- `WinUI/UnifiedLivePreview.cs`: mode, activity, source observation, debounce,
  cancellation, prepared-view cache and presentation transfer.
- `UnifiedPreviewDrawing.cs`: serializes ordered 2D paths off the UI thread
  and displays one native SVG image per view, preserving depth order.
- `LivePreview3DHost.cs` / `Preview3DRenderer.cs`: hardware-first Direct3D 11,
  WARP fallback, on-demand frames and camera input.
- `UnifiedPreviewWindow.cs`: an owned window for the same presentation.
- `CablePrintLayout.cs` / `CablePrintedSceneBuilder.cs`: bounded dot raster,
  two complete impressions, cylindrical ink placement and sample length.

Paths above are relative to their existing project directories under `src/`.

## Connect another page

Create a scene adapter from approved input/result state. Give components stable
IDs, explanatory labels, physical dimensions in millimetres and material colours.
Use `DetailOnly` and `SimpleOnly` when a strand construction has a simplified
envelope. Describe illustrative or bounded representations in scene notes.

Place `UnifiedLivePreview` in `ModuleWorkspaceShell.PreviewContent`, then bind
the input source once with `BindSource(viewModel, sceneFactory)`. A scene can
also be supplied directly through `Scene`. Navigation must call
`SetWorkspaceActive` (or the page's `SetPreviewActive` wrapper), because a
collapsed WinUI page is still loaded. A page outside the shell must explicitly
call `SetActive` with its page visibility and preview On/Off state.

## Performance and validity

Capture a coherent input snapshot after a short trailing debounce. Generate
geometry off the UI thread, cancel superseded generations, and publish only
the current revision. Reuse prepared views for unchanged scenes. Never create
a XAML control per strand or mesh face. Expensive surfaces are released while
preview is off or its page is inactive.

Scene validation rejects non-finite coordinates, invalid sections, duplicate
IDs and exhausted budgets. Budget handling must preserve whole cores/layers;
it must not quietly drop primary geometry. Representative lengths, reduced
strand detail and bounded turns must be described visibly. The renderer is
idle when input, size and camera are unchanged; camera changes are rate-limited.

Pop-out and redock transfer one presentation with its mode and camera. There
is at most one detached preview. This implementation follows the active page;
cross-page pinned/follow selection and persistent multi-monitor window placement
remain separate work. The current window is clamped to an available display.

## Print and saved settings

COR print now uses the same scene as the cable. Simple/Detailed side views and
orbital 3D consume the exact same dot positions. The printable jacket length is
the repeat distance plus one complete text width and margins, so two full
impressions fit. A short conductor cutaway remains visible beyond the jacket.
The costing's quoted length is unaffected.

Dot diameter, whole-number dots high (5–64), horizontal and vertical
centre-to-centre pitches, text, colour and repeat are visible production inputs.
The interface explains the derived ink height:
`(dots high - 1) × vertical pitch + dot diameter`. New nullable document fields
retain dot diameter and dot count while older documents preserve their existing
requested height and pitches. Differences are shown explicitly rather than
silently changing a saved setting.

The included reference bitmap alphabet is illustrative. Unsupported characters
are visibly reported and displayed as question marks without altering saved
text. Two impressions are generated completely or rejected at the 20,000-dot
budget; dots are never silently discarded. Overlapping dots/impressions are
shown at the specified spacing with a note. Ink taller than the supplied cable
circumference is rejected. Invalid print settings leave the cable available and
explain why its print was not drawn.

Use **Fit cable** to see both repeats, **Inspect surface** to target the first
impression, and the wheel or Page Up/Down to zoom. Camera clipping adjusts for
close inspection of small ink dots. The old separate print-strip drawing has
been removed from the page.

## Verification record

Implementation verification and remaining acceptance limits are recorded in
`UNIFIED-LIVE-PREVIEW-ACCEPTANCE-2026-09-10.md` after the build/test/native checks.
