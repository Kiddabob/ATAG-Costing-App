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

## Shared visual hierarchy

The reusable resources in `App.xaml` establish the hierarchy:

- `ModuleHeaderCardStyle` identifies the current module with a restrained accent edge;
- `ModuleSectionCardStyle` contains a complete workflow stage;
- `ModuleBackgroundCardStyle` holds supporting inputs, source values and explanations;
- `ModulePrimaryResultCardStyle` highlights authoritative outputs with a subtle app-accent tint and clear border;
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
