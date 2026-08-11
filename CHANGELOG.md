# Costing App release notes

## 0.3.4 - 2026-08-11

- Centres and slightly widens the complete Settings workspace so it sits
  naturally in the available content area instead of hugging the navigation
  edge on large windows.
- Gives the selected launch-mode tile one readable foreground colour chosen for
  its accent surface, removing the mixed black-and-white text seen on the ATAG
  choice.
- Applies accent changes immediately while the theme follows Windows as well as
  in explicit Light or Dark mode.
- Keeps the default accent surface at the exact selected RGB value, including a
  validated custom hex, and shows that active hex beside the accent preview.
- Chooses black or white text automatically for useful contrast against each
  accent without changing the requested accent colour.
- Makes no costing, saved-document schema, database-link, production-speed, or
  reporting change.

## 0.3.3 - 2026-08-11

- Rebuilds the Appearance settings as a visual, app-owned workspace with large
  System, Light, and Dark preview cards instead of a compact mode list.
- Adds six ready-to-use accent colours plus a validated custom RGB hex colour;
  the chosen accent is applied only inside Costing App and never changes the
  user's Windows colour settings.
- Saves the theme, accent, custom colour, and Mica or Acrylic window material
  per Windows profile, while the isolated blank-review session remains unable
  to read or write the installed app's preferences.
- Keeps Windows text-size and contrast links clearly separated as accessibility
  settings, and continues to respect system contrast and transparency choices.
- Preserves older settings files by safely adding the existing coral accent as
  the default when the new appearance fields are absent.
- Makes no costing, saved-document schema, database-link, production-speed, or
  reporting change.

## 0.3.2 - 2026-08-11

- Makes the optional ATAG/blank launch chooser genuinely resizable and
  maximisable, including a working title-bar maximise button.
- Increases the chooser's initial size so both launch choices are visible on a
  normal display instead of clipping the blank test option below the window.
- Adds an independent vertical scrollbar so both choices remain reachable when
  the chooser is made smaller or opened on a compact display.
- Makes no costing, saved-document schema, database-link, production-speed, or
  reporting change.

## 0.3.1 - 2026-08-11

- Tightens the ATAG Design long logos in the navigation pane, Home banner, and
  Settings so the approved artwork sits naturally in the available space.
- Removes only fully transparent outer padding from the light and dark logo
  assets; every visible artwork pixel and colour remains unchanged.
- Makes no costing, saved-document schema, database-link, production-speed, or
  reporting change.

## 0.3.0 - 2026-08-11

- Adds a private Production Speed Library where each user can define production
  lines, finished-OD speed bands, and known cable runs with their measured
  speeds, dimensions, tolerances, and machine dial settings.
- Estimates production speed from up to three sufficiently similar measured
  runs, then falls back to the selected line's explicit OD bands without
  inventing a value when no usable evidence exists.
- Lets users copy geometry from COR or either dual-insulation extrusion and
  explicitly apply the accepted estimate as that costing's visible manual line
  speed.
- Keeps clean installations free of production data. The general insulation
  bands are added only through a confirmed, editable starter-profile action;
  all user-entered line data remains in LocalAppData and outside Git and update
  packages.
- Presents cumulative update notes as compact per-version cards inside an
  independent scroll area, keeping update actions visible even when several
  releases have been missed.
- Adds an owned, resizable full-screen changelog reader on the Costing App
  display and removes duplicate markdown version headings from release notes.
- Adds an optional per-Windows-profile launch chooser for the designated
  tester. It can open either the normal ATAG session or the isolated blank
  interface without compiling or publishing the tester's identity.

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
