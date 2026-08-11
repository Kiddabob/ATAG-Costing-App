# Product scope

## Objective

Replace the costing workbook with a polished Windows application that preserves
the business rules while making every input, assumption, calculation step, and
output easy to inspect. Costing, reporting, and review pages must be independent
modules over one shared calculation model rather than copies of a worksheet.

The workbook remains the reference implementation until each migrated rule has
been documented, tested against representative workbook cases, and accepted.

## Non-negotiable behaviour

1. Calculations are auditable. A result can expose its source inputs, expression,
   substituted values, unrounded result, displayed result, units, dependencies,
   and warnings.
2. Business rules live in the domain layer, never in a page, report, or print
   template.
3. Rounding is explicit and occurs only at named business boundaries.
4. Risk and markup are applied sequentially and shown as separate steps.
5. Markup and margin are treated as different concepts and labelled accordingly.
6. Braid coverage can be calculated in both directions where the workbook process
   requires it.
7. Reports consume saved costing data; they do not duplicate or recalculate the
   costing logic.
8. Source workbook formulas, cached errors, hidden sheets, and VBA are treated as
   migration evidence rather than copied blindly.

## First-start storage policy

- On launch, the user is asked to choose a local or network folder.
- The selected location is the default root for costings, quote revisions,
  reports, templates, and backups.
- The setup screen continues to appear on every launch by default, even after a
  valid folder is selected.
- The recurring screen can be disabled only from **Settings > Storage and files**.
- Settings can reopen the setup screen, change the folder, or restore the startup
  prompt.
- If the saved folder becomes unavailable, setup is shown again to prevent files
  being silently written elsewhere.
- Preferences are stored per Windows user under local application data; business
  files are stored only in the selected folder.
- The setup card is content-height and centred inside the full-screen overlay;
  the surrounding view remains scrollable on windows too small for the content.

## Modules

### Costing workspace

The primary guided workflow for constructing a cable costing. Sections will cover
the product definition, conductor, insulation, cabling or laying-up, screening,
braid, sheath, packing, labour, risk, markup, and final selling price as relevant
to the selected construction.

Each section exposes:

- entered and selected inputs;
- material and process lookups;
- intermediate quantities and units;
- formula trace;
- validation and warnings;
- section subtotal and contribution to the final result.

### Material data

Maintains the shared reference data currently spread across workbook sheets,
including copper, compounds, masterbatch, colours, yields, suppliers, contacts,
operators, labour/process values, and future price histories.

### Braid calculator

An independent engineering calculator backed by the same braid domain rules used
by a costing. It supports scenario comparison and reverse calculations without
duplicating formulas.

### Reports

Separate templates over the same saved costing revision:

- customer quote;
- internal costing summary;
- contract review;
- detailed calculation appendix/audit trail;
- future production or technical sheets.

Templates choose which fields and calculation steps to display. Print preview,
page breaks, headers, footers, revision identifiers, and PDF output belong to the
reporting module rather than the costing UI.

### Administration and settings

Controls storage, reference-data import/export, report templates, calculation-rule
versions, and future backup/retention options.

### Conditional organisation branding

- On startup, the WinUI shell reads the current Windows user's local OneDrive
  account registrations. A `Business*` registration whose current or legacy
  email ends exactly in `@atagcables.com` enables ATAG organisation branding.
- The account address is used only for this in-memory decision. It is not saved,
  displayed, logged, or transmitted, and the app does not perform a Microsoft
  sign-in or call OneDrive over the network.
- When enabled, the packaged transparent ATAG Design long logo is shown without
  an artificial card/background in the navigation pane, Home welcome panel,
  and Settings. Navigation and Settings switch between dark-text and white-text
  variants with the app theme; the navy Home banner always uses white text.
  Settings also explains the detected state.
- When no matching account is registered, the logo controls remain collapsed
  and the standard Costing App branding remains active. Signing into OneDrive
  and restarting the app causes the decision to be evaluated again.
- This is shell branding only. Quotation and other report logos remain the
  responsibility of their versioned Reporting templates.

## Architecture boundaries

```text
WinUI
  -> Application
     -> Domain

Infrastructure
  -> Application + Domain

Reporting
  -> Application + Domain
```

- **Domain** has no UI, filesystem, Excel, or printing dependency.
- **Application** coordinates use cases through interfaces.
- **Infrastructure** implements JSON/database storage and workbook migration
  adapters.
- **Reporting** maps approved costing snapshots to versioned document templates.
- **WinUI** presents state and dispatches use cases; it does not own formulas.

## Core records

- `CostingProject` — customer/product context and document identity.
- `CostingRevision` — immutable version of inputs, rule versions, and results.
- `CableConstruction` — ordered construction stages and dimensions.
- `Material` and `MaterialPrice` — typed reference data with effective dates.
- `ProcessRate` and `LabourRate` — typed operational reference data.
- `CalculationStep` — auditable input/intermediate/result node.
- `ValidationFinding` — warning or blocking rule with a source.
- `ReportDefinition` — versioned layout and field-selection policy.
- `ContractReview` — review answers and approval state linked to a revision.

## Delivery slices

### 0. Foundation — complete

- WinUI 3 development environment and Developer Mode configured.
- Five-project modular solution.
- Native navigation shell and visual language.
- First-start storage selection and persistent settings.
- Self-contained unpackaged development build for the exFAT workspace.

### 1. Workbook specification and parity harness

- Catalogue visible/hidden sheets, named ranges, VBA entry points, tables, and
  external queries.
- Group formula families and document units, rounding, defaults, and error
  behaviour.
- Select representative golden costings for each construction family.
- Build automated parity tests that compare domain results with approved workbook
  results.
- Resolve the two known cached `#DIV/0!` cells before using those cases as golden
  references.

Progress as of 29 July 2026:

- immutable workbook size, timestamp, and SHA-256 evidence recorded;
- all 54 sheets, 9 tables, 6 names, 5 connections/query outputs, 1,565 formulas,
  VBA modules/entry points, duplicated formula families, print area, rounding
  cells, and cached errors mapped reproducibly;
- `ATAG.Costing.Domain.Tests` and `ATAG.Costing.WorkbookParity.Tests` added;
- the general 3% waste/start-up usage allowance implemented as
  `usage-allowance/v1`;
- the first pure masterbatch usage rule implemented as
  `masterbatch-usage-per-metre/v1`, including ordered calculation steps, units,
  substituted expressions, raw/display values, rounding policy, dependencies,
  warnings, and rule identifiers;
- the discovered `COR1MBPrice` fixture remains pending business approval and its
  golden parity test remains intentionally skipped.
- the upstream conductor and compound usage family is implemented as
  `single-core-material-costing/v1`;
- the first dual-insulation family is implemented as
  `dual-insulation-material-costing/v1`, with separate 10,200 m core/first-layer
  and 10,000 m second-layer scopes, one 3% allowance per stream, and each price
  included once;
- dual production owns two independent extrusion line-speed profiles and labour
  calculations; masterbatch contributes material amount/cost only;
- a current-workbook dual fixture records the mapped source cells and known
  duplicate allowance/addition defects; its evidence test runs while its golden
  comparison remains pending business approval;
- a usable WinUI V1 now costs one insulated core from conductor, compound, and
  masterbatch inputs and exposes the complete calculation trace;
- users enter each supplier's total quote and quoted kilograms; the application
  derives £/kg while central-data yield, OD, and specific-gravity values remain
  locked;
- each material card groups its formula summary, quote usage, and cost per metre;
- risk is a separate step before markup, and masterbatch is included exactly
  once in the material subtotal;
- production time and labour are implemented from the workbook's core-OD
  line-speed bands, with visible manual-speed, setup-time, operator-count, and
  hourly-rate inputs and a complete calculation trace;
- version 0.3.0 provides a private Production Speed Library with
  user-created production lines, per-line OD speed bands, measured cable-run
  evidence, transparent speed/runtime estimates, and explicit application to
  COR or either dual extrusion. Clean installations contain no production rows;
  the accepted general insulation profile is added only when the user requests
  the editable starter profile;
- a locally opted-in tester profile can choose the normal ATAG session or the
  isolated blank interface on launch; this uses a generic current-user registry
  flag and publishes no tester identity;
- sequential risk then markup is the recommended selling-price result;
  additive risk plus markup and target gross margin are separately labelled
  comparison methods;
- the workbook-derived core-name generator is implemented with an explicit
  custom/customer-name override;
- a first contract-review page consumes the same live costing result and
  captures customer scope, approval, order acceptance, and amendment review;
- descriptive COR, Dual insulated, Flat, and D-shape start tiles make the
  selected construction explicit before entry;
- the four start tiles use construction-specific vector silhouettes rather
  than generic application glyphs;
- the construction tiles fill their equal grid cells consistently, and the
  D-shape silhouette positions its in-line cores above the flat base;
- the guided dual-insulation workspace inserts selected Tape, Chalk, Foil,
  Braid, Lapscreen, and Drain wire modules after first insulation and before
  second insulation, and retains them in that physical order without inventing
  module material formulas;
- a typed construction plan preserves the same inside-to-outside order, rejects
  duplicates, and bounds future Flat/D-shape constructions to ten in-line
  cores;
- independent Access/SQL table links for Copper, Compounds, Masterbatch,
  Contacts, and Operators, searchable table/view Navigator previews, a bounded
  transform editor, full transformed source-table retention, Access physical
  name/caption/description matching, deliberate rename/remove-column steps,
  automatic ATAG field projection, local last-successful snapshot retention, a
  30-second connection check, visible
  online/partial/offline state, and manual refresh are implemented;
- central-data workflow windows use app-owned WinUI title bars and owner-only
  stacking above the ATAG main window; connection actions precede the data
  previews, unlink always starts with an explicit linked-area choice, and the
  navigation footer reports the exact independent state of all five links;
- clean builds contain no business-data snapshot; each Windows user imports the
  five LIVE tables during first-run setup, and only that user's successful
  imports are retained locally for offline use;
- conductor choice now supports strand construction, nominal mm², or calculated
  AWG plus class and supplier, with rope-lay parsing, exact strand-area
  calculation, presentation-only diameter normalisation, and visible
  nominal-versus-calculated warnings;
- imported conductor records with a valid description, supplier, and parsed
  construction remain visible even when a source price, yield, area, or nominal
  OD is blank. A versioned projection may calculate exact price from component
  costs, yield from reel length/net weight, metallic area from stranding, or OD
  from volume; an OD available only from close-packed geometry is explicitly
  labelled **estimated** and requires review. User-entered quote total and quoted
  mass remain the COR supplier-price source;
- every calculated/estimated Copper value retains its formula, substituted
  source values, confidence, and rule version. The full transformed source row
  is never rewritten, and cached full tables are re-projected while offline;
- the material-local workings are grouped into explicit source, derived, and
  result stages, with each tile naming the earlier values it consumes; the
  final audit trace remains a full-width responsive view;
- schema-v2 `.atagcosting` revisions save and reopen the single-core inputs,
  locked material values, rule identifiers, exact outputs, effective naming,
  recursive calculation trace, and contract-review fields independently of
  central-data availability;
- schema-v3 adds a construction discriminator, locked dual material inputs,
  explicit core/first-layer and second-layer production scopes, independent
  extrusion profiles, ordered optional modules, exact dual results and trace,
  legacy schema-v1/v2 single-core reads, and immutable dual approval saves;
- explicit project/revision identities, working-copy versus approved state,
  timestamps, immutable approved saves, next-revision-on-edit, and duplicate as
  new project are implemented;
- a portable relative-path project index is stored only in the selected
  business-data folder; an unavailable folder stops the operation instead of
  triggering a fallback;
- conductor material/finish is selected separately from construction, with TCW,
  PCW, titanium, tinsel, and other supplier-defined types kept distinct;
- masterbatch choice exposes all cached colour rows, colour swatches,
  compound-family compatibility, recorded temperatures, and source notes;
  search includes workbook-derived HSL descriptions, combined semantic terms,
  family/tone and colour-type filters, and small-error text matching;
- OD tolerance is a linked ± value by default, with an explicit asymmetric
  positive/negative option;
- material workings and the final trace are collapsed by default, while a
  compact dependency-flow view, pinned result, and section-jump menu keep the
  long-form page navigable;
- quotations retain GBP as the calculation basis, can show an explicitly
  labelled converted total from a locally retained ECB reference-rate snapshot,
  and can be generated as a single-page A4 PDF;
- Access/SQL table discovery and reads are implemented; acceptance against the
  authoritative ATAG schema, keys, units, and price-effective-date rules remains
  gated until a real database copy is available for inspection.

### 2. Reference data and costing engine

- Define typed materials, prices, rates, and effective dates.
- Import approved workbook master data through a one-way migration adapter.
- Implement pure calculation services one construction stage at a time.
- Display the complete dependency trace for every result.

V1 progress as of 29 July 2026:

- typed conductor, compound, masterbatch, contact, and operator references
  implemented;
- clean-install first-run setup requires a save folder and validated links for
  Copper, Compounds, Masterbatch, Contacts, and Operators; no workbook-derived
  customer or material rows are embedded in source or release binaries;
- read-only Open XML import retained as a migration/test adapter for the
  `Copper`, `Compounds`, `MasterbatchCodeList`, `Contacts`, and `Operators`
  workbook tables;
- successful refreshes replace the local snapshot atomically; failed refreshes
  retain the last available data;
- a WinUI Navigator selects a real Access/SQL table or view by searchable row
  preview, followed by a transform editor with saved applied steps, all columns
  kept by default, deliberate remove/rename controls, and reviewable automatic
  ATAG field matches using available source metadata;
- every central-data setup, Navigator, transform, and result surface is a
  movable/resizable WinUI window with an app-owned title bar on the main app's
  monitor; owner-only stacking keeps it above ATAG but not unrelated apps,
  data-heavy pages default to near-work-area size, the main app remembers its
  previous monitor, restored bounds, and maximised state, and **Edit existing
  link** returns a configured object directly to its saved transform and
  field-match editor;
- the complete transformed database object is saved atomically beside the
  typed costing projection, preserving currently unused columns for
  traceability and later features;
- missing Copper projection fields are completed only from defensible retained
  relationships: component costs, reel length/net weight, strand geometry, and
  source volume. Exact values and geometry estimates are labelled separately,
  and the direct retained cells remain unchanged;
- Live Data identifies the collapsed complete transformed table as the source
  of truth and the lower unit-normalised view as the data used by calculations;
  all five areas remain visible in the setup picker, data tabs cannot be closed,
  connection actions appear above the previews, **Remove link** always asks
  which linked area is intended before confirmation, and its removal retains
  both offline table forms while stopping refresh;
- the navigation footer expands into five permanent per-area status rows and
  reports the exact live count; structured per-area refresh outcomes preserve
  accurate LIVE, checking, ready, offline-cached, and cached-only states after
  partial refreshes;
- source `#DIV/0!` cells are non-blocking blanks with visible diagnostics, so
  valid rows continue through preview and import;
- a versioned, portable single-core project/revision document and relative-path
  index are persisted atomically behind Application interfaces and
  Infrastructure JSON implementations;
- approved revisions retain exact results and recursive trace evidence and
  cannot be overwritten; schema-v1 working documents remain readable;
- one-core material usage, supplier-quote unit-price derivation, quote total,
  production labour, separate risk, separate markup, additive comparison, and
  target-margin comparison implemented;
- pure two-layer annular geometry, supplier-quote derivation, layer-specific
  production lengths, allowance-once material usage, masterbatch usage, run
  subtotal, finished-metre result, and recursive trace implemented without
  adding formulas to Application or WinUI.

### 3. Guided costing workspace

- Add/edit a costing and its revisions.
- Progressive construction-stage UI with validation and section totals.
- Compare revisions and scenarios.
- Save, reopen, duplicate, archive, and search costings.

The first bounded UI is now implemented for one core, including material-local
and labour-local dependency stages, generated/custom naming, **Costing Prepared
by** with Laura selected from the database Office list by default, commercial
comparison cards, the cable-type chooser, a top-positioned result with pin and
an always-on-top resizable pop-out, a non-scrolling command strip, left-pane and
compact section navigation, readable typed Contacts search, semantic
masterbatch-colour search and filters, quotation reel and wording inputs,
quotation currency display, a single-page A4 quotation, a live contract-review
V1, and manual
save/reopen through the selected-folder project index, portable legacy-file
browse, immutable approved single-core revisions, automatic next working
revision, duplicate as new project, and clear unsaved/validation state. The Home
page also contains construction-specific start tiles. The Dual tile opens a
complete guided editor with searchable Copper, Compound, and Masterbatch
selectors for both layers, explicit production scopes, two independent line
profiles, live material/labour/commercial results, full trace, and schema-v3
save/open/duplicate/approve lifecycle. The working COR page has an
off-by-default responsive preview dock with its cross-section above its side
profile. At wide sizes it is a user-resizable right-hand rail outside the
costing scroller; at compact sizes it moves to a bottom dock. The fixed-coordinate
drawings scale with the available dock width while the long print-repeat strip
retains horizontal scrolling. It uses retained dimensions/material colours and
offers a simple envelope or all-strand cross-section detail using the same pure
layout for cached and LIVE imported Copper rows. Parsed strands occupy a
connected close-packed layout: complete shells are hexagonal, and the confirmed
16-end construction uses five centre strands with eleven strands in its outer
layer. Recursive rope-lay groups remain distinct. Supplier-only text
without reliable numeric stranding stays a labelled simplified envelope rather
than guessed geometry. The preview uses a straight cylindrical side profile
with narrow approximately 20-degree
end faces. The closed insulation end draws only its exposed outside rim and
uses the same highlight/shadow gradient as the tube. At the cut end, the
insulation annulus masks the conductor edges while a centred conductor face, or
one cap per detailed strand, remains visible through the hole.
Only physically exposed outer strands or rope groups run down the side; the
compressed end face and cross-section retain every parsed strand. Each rope
group is one continuous, gently twisting bundle with fine longitudinal strand
cues and a matching final-rotation end face, rather than split
rear/front paths or short depth fragments. It labels radial-wall
references and renders enabled saved core-print specifications in a separate,
scrollable scale strip; the entire print block is absent when print is disabled.
The redundant
in-workspace cable chooser is removed. Revision comparison, search
for every remaining data selector, the full visual colour browser, an editable
page-accurate quotation preview, dual-specific quotation/contract-review
wording, the shared Dual/Flat/D-shape renderer, Flat/D-shape calculation
engines, scenario comparison, and archive remain future work. See
`CABLE-CONSTRUCTION-AND-VISUALISATION.md`.

### 4. Quotes, contract review, and printing

- Versioned quote and contract-review models.
- Print/PDF preview with selectable modules and calculation appendix.
- Template-specific page composition without worksheet duplication.
- Revision/approval status and document history.

V1 progress as of 29 July 2026:

- a live contract-review screen consumes the current single-core result without
  copying any business formulas;
- material usage, material cost, labour cost, estimated production cost, risk,
  markup, and alternate selling-price methods are shown together;
- estimate approval, order acceptance, acknowledgement, and proposed-amendment
  fields mirror the important checkpoints identified in the reference
  workbooks;
- the contract-review fields are saved and reopened with the single-core
  `.atagcosting` document;
- a workbook-inspired single-page A4 PDF quotation is generated from application
  values without calculating inside the report template; its reel plan, concise
  conductor, simple insulation, colour wording, special notes, and terms come
  from explicit saved quotation inputs;
- GBP is the stored calculation basis and an optional retained ECB reference
  rate converts the displayed/generated customer quotation;
- revision comparison/history UI, the editable page-accurate quotation preview,
  final approved workbook-matched branding/wording, additional document
  templates, and electronic approval remain later slices.

### 5. Operational hardening

- Backup/restore and data retention.
- Import diagnostics and migration reports.
- A versioned Velopack per-user installer, desktop/Start shortcuts, uninstall
  entry, anonymous public-GitHub Stable/Beta update feed, release notes,
  progress, explicit restart, SHA-256 package verification, and repeatable
  release workflow are implemented for x64 Windows.
- The clean-package audit excludes databases, workbooks, retained central data,
  settings, saved costings, developer symbols, and machine-local environment
  files. Runtime state remains outside the replaceable application directory.
- Organisation code signing remains pending; the current installer can show an
  Unknown publisher or SmartScreen warning.
- Continue acceptance testing, performance, accessibility, backup/restore, and
  release hardening.

## Explicitly outside the single-core V1

- Remaining workbook calculation families.
- Editing the source workbook from the app.
- Multi-user database synchronisation.
- User accounts, permissions, or electronic approvals.
- Final approved quote/contract-review templates and print preview.
- Organisation-trusted code signing and enterprise deployment policy.

These items remain planned work and must not be implied by the current navigation
placeholders.

## Acceptance criteria for a migrated calculation

A calculation family is complete only when:

- its business meaning and units are documented;
- its inputs and defaults are validated;
- its trace is understandable without opening Excel;
- rounding and failure behaviour are explicit;
- representative normal, boundary, and invalid cases are tested;
- golden cases match approved workbook results;
- the costing UI, reports, and contract review all consume the same result model.
