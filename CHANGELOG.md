# Costing App release notes

## 0.2.3 - 2026-08-11

- Embeds the transparent square ATAG icon in the Windows executable so the
  taskbar, Desktop shortcut, and Start menu shortcut no longer fall back to the
  generic application-window icon.
- Keeps the same icon as a packaged loose asset for WinUI windows and continues
  to pass it to the Velopack installer/shortcut builder.
- Adds a release-gate pixel check that rejects a published executable whose
  embedded icon does not match the supplied square icon.
- Makes no costing, saved-document schema, database-link, or reporting change.

## 0.2.2 - 2026-08-11

- Provides the small follow-up release used to verify the v0.2.1 on-launch
  update pop-up, verified download, install, and automatic restart end to end.
- Retains the installed transparent ATAG logos, transparent square app icon,
  package-asset integrity checks, and all v0.2.1 behaviour.
- Makes no costing, saved-document schema, database-link, or reporting change.

## 0.2.1 - 2026-08-11

- Fixes the conditional ATAG Design long logos in installed builds by carrying
  both transparent theme variants as explicit publish and update-package files.
- Shows a clear launch pop-up when an installed build finds a newer release,
  including its download size and cumulative release notes.
- Lets the user install directly from that pop-up or defer the update while the
  existing Settings update controls remain available.

## 0.2.0 - 2026-08-11

- Adds the complete guided dual-insulation costing editor with separate Copper,
  Compound, and Masterbatch selection for both insulation layers.
- Adds schema-v3 dual-insulation save, open, recalculate, approve, and duplicate
  workflows while preserving schema-v1 and schema-v2 single-core documents.
- Shows the two production scopes, independent extrusion profiles, material and
  labour results, commercial comparisons, and full calculation trace.
- Persists optional Tape, Chalk, Foil, Braid, Lapscreen, and Drain wire modules
  in physical construction order without inventing unapproved material rules.
- Enables transparent light/dark ATAG Design long-logo variants automatically
  when the current Windows user has an `atagcables.com` OneDrive business
  account. The address is checked only on the device and is not retained,
  displayed, logged, or transmitted.
- Fixes Dual workspace navigation so selecting the construction no longer falls
  back to the single-core page.
- Keeps dual-specific quotation and contract-review documents staged until
  their wording and reporting payload are approved.

## 0.1.0 - 2026-08-09

- Adds the first one-file, per-user Costing App installer.
- Adds anonymous update checks from public GitHub Releases.
- Adds Stable and Beta update choices, cumulative release notes for every
  version since the installed one, download progress, and explicit
  install-and-restart confirmation in Settings.
- Keeps settings, linked database details, retained offline tables, and saved
  costings outside the replaceable application directory.
- Starts a clean installation with no embedded database links, retained rows,
  customer data, operator data, or values previously entered on another PC.
