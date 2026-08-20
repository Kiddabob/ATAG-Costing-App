# Shared module UI contract

This contract keeps new Costing App feature modules visually consistent without moving calculation rules into the interface.

## Standard module shell

New interactive engineering and costing modules should use `ModuleWorkspaceShell` unless their approved workflow cannot fit it.

The shell provides:

- a header area that remains visible while the module editor scrolls;
- a full-width scrolling workspace for inputs, results and calculation trace;
- an optional LIVE Preview rail on wide windows;
- a visible horizontal resize cursor and accent divider while the preview rail is hovered or dragged;
- an automatic bottom-docked preview on narrower windows;
- guidance that remains visible while LIVE Preview is off;
- preview content that is detached while disabled, so expensive geometry is not constructed or refreshed unnecessarily.

LIVE Preview is off by default. Module geometry consumes already-calculated results and visible physical inputs. It must never become a second calculation engine or silently correct a business result.

If no useful visual has been approved, set the shared shell's preview as
unavailable so the entire dock, divider, toggle and compact preview row are
removed. Do not reserve an empty rail merely for visual consistency. The Coil
calculator is the first reference consumer of this no-preview mode.

## Performance contract

The current XAML preview implementations are an accepted functional baseline,
not a permanent rendering requirement. Before adding more construction
previews, profile navigation, input recalculation, layout, binding, allocation,
geometry creation and invalidation on both a development PC and representative
older integrated-graphics laptop hardware.

The shared preview successor should use one immutable cable-scene/result model
and one reusable renderer across costing pages. It should render only when its
inputs, size, camera or mode change; cache reusable meshes/paths, brushes and
materials; cap interactive redraws; suspend completely while hidden; and avoid
large per-strand XAML visual trees. Evaluate a hardware-accelerated
Win2D/Direct3D or Windows Composition surface with device-loss recovery and a
tested software/WARP fallback. Retain **Off** and lightweight **Simple** modes
even when the accelerated renderer is available.

Do not call this hardware decoding: the requirement is hardware-accelerated
rendering. GPU use cannot compensate for excessive UI-thread object creation,
layout passes, bindings or calculation churn, so measurement and those CPU-side
repairs come before a renderer rewrite.

### Interactive 3D successor

Interactive, rotatable 3D is an optional future preview mode inside this same
shell. It must consume the same immutable scene as the bounded 2D modes, remain
Off by default, preserve an immediate Simple fallback and never own an
engineering or costing formula. Use a single shared WinUI 3 Direct3D surface,
render on change rather than continuously, lower detail while the camera moves,
and recover cleanly from device loss or software fallback. The measured
prototype and acceptance gates are recorded in
[`INTERACTIVE-3D-PREVIEW-FEASIBILITY.md`](INTERACTIVE-3D-PREVIEW-FEASIBILITY.md).

### Detachable preview and module tools

The shell must expose consistent **Pop out** and **Return to workspace** actions
for the shared LIVE Preview. The external preview is a movable, resizable,
maximisable app-owned tool window that may be placed on any connected display.
It follows the user's explicit app-wide preview selection or can pin one target.
There is one selected scene, one camera state and at most one attached live
renderer; a pop-out must not duplicate calculation or rendering work.

Modules may use the same secondary-window infrastructure to open as side tools.
A costing-linked module window edits the same document/session through shared
Application commands. A standalone scratch module is clearly labelled and
must use an explicit **Apply to costing** action. Closing a tool window changes
presentation only and never silently deletes module state.

Owned tool windows stay above Costing App but are not globally always-on-top.
Remember size, state and display as presentation-only LocalAppData, validate
restored placement against connected displays, and return an off-screen window
to a visible work area. All tools must remain fully usable when redocked.

## Shared visual hierarchy

The reusable resources in `App.xaml` establish the hierarchy:

- `ModuleHeaderCardStyle` identifies the current module with a restrained, stable engineering-blue edge;
- `ModuleSectionCardStyle` contains a complete workflow stage;
- `ModuleBackgroundCardStyle` holds supporting inputs, source values and explanations;
- `ModulePrimaryResultCardStyle` highlights authoritative outputs with the stable engineering-result palette and clear border;
- the user-selected accent is reserved for focus, selection, active navigation and user-invoked actions; it must not communicate success, warning, error, approval or calculated authority;
- `ModuleMetricLabelStyle` and `ModuleMetricValueStyle` keep values scannable;
- `ModuleStatusInfoBarStyle` uses WinUI's accessible Information, Success, Warning and Error semantics.

Colour is never the only carrier of meaning. Every state also needs a label, value, icon or explanatory message. User-selected material colours belong to the cable or material preview and must not be mistaken for success, warning or error status.

Do not use a strong recommendation colour unless an approved, tested recommendation rule exists. Parallel valid alternatives receive equal visual weight.

## Calculation flow

Where one set of values feeds another, group the source values first, then show a directional connector before the derived results. Parallel outputs sit together and use matching presentation. Calculation traces remain collapsed by default but retain every formula, substituted value, unit and rounding rule.

## First reference implementation

The Braid Coverage module is the first consumer of this shared shell. Its LIVE Preview is deliberately schematic and visually subordinate to the labelled workbook-derived pitch, coverage, strand-length and gear outputs.

The accepted COR preview remains unchanged in this slice. Migrating COR and Dual Insulation to the shell is a separate, controlled refactor because their saved-costing, revision and detailed conductor-preview behaviour must remain identical.

## New-module checklist

Before a new module is considered structurally ready:

1. Use the shared shell and semantic styles.
2. Keep actions and current status in the non-scrolling header area.
3. Keep supporting data neutral and visually quieter than authoritative results.
4. Provide an optional responsive LIVE Preview when geometry materially helps the user.
5. Keep guidance available with the preview disabled.
6. Defer preview construction while disabled.
7. Label whether a diagram is scaled, schematic or illustrative.
8. Keep calculation rules in Domain/Application services and expose a complete trace.
9. Test the module at wide and compact window widths.
10. Verify System, Light and Dark modes plus at least one custom app accent.
11. Remove the entire preview dock when the module has no approved visual.
12. Verify dock, pop-out, redock, pin/follow and multi-monitor recovery where
    the module exposes a preview or detachable tool view.
