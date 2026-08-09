# ATAG Costing

ATAG Costing is a modular WinUI 3 replacement for the workbook
`(WIP Mitchell) Costing Sheet.xlsm`.

The first-openable foundation milestone and the working single-core costing V1
are complete. The app calculates conductor, insulation compound, masterbatch,
and production labour for one core using workbook-derived starter data.
Each material card now shows its own formula summary, quote usage, and cost per
metre, followed by a collapsed-on-entry dependency flow. Related values are
grouped into source, derived, and result stages; every derived tile names the
earlier values it consumes. The stages use responsive columns, recalculate as
inputs change, and remain available for audit without dominating the working
page. Users enter a
supplier's total quoted price and quoted kilograms; the app derives £/kg.
Yield, conductor diameter, and specific gravity are locked to the selected
central-data record. The 3% usage boost is modelled once as a general
waste/start-up allowance, followed by separate risk and markup steps.
Masterbatch is included once in the material subtotal and is not added again
after markup.

Clean builds contain no cached customer, operator, supplier, or material data.
On first run the app requires a save folder and guides the user through linking
and importing the five LIVE Access/SQL areas. Successful imports are retained
only in that Windows user's LocalAppData so a later connection outage does not
remove the last-known working tables.

Single-core production time follows the workbook's core-OD line-speed bands,
with an explicit manual-speed override, setup time, operator count, and hourly
labour rate. Dual insulation now supports an independent size-to-speed profile
for each extrusion; masterbatch remains material amount/cost only and never
creates a separate process time. A generated core name can be replaced by a
clearly marked custom/customer name. The same live result feeds a first
contract-review page covering customer scope, material and labour totals,
approval, order acceptance, and proposed contract amendments. Commercial
values are shown as three separately labelled methods: sequential risk then
markup, additive risk plus markup, and target gross margin.

The Home page now starts with descriptive **COR**, **Dual insulated**,
**Flat cable**, and **D-shape cable** construction tiles instead of one generic
start action. Each tile uses a cable cross-section silhouette: two concentric
circles for COR, three for Dual, in-line cores in a rounded rectangular Flat
profile, and in-line cores in a flat-bottomed domed D profile. **Single
insulated core** is the working V1 costing UI. The
dual-insulation material and production engines are implemented and tested:
conductor and first insulation cover finished length plus core start-up, the
second insulation covers finished length only, every stream receives the
general 3% allowance once, and each material enters the subtotal once. The
first dual construction planner inserts selected Tape, Chalk, Foil, Braid,
Lapscreen, and Drain wire modules between its two insulation layers. Flat and
D-shape remain clearly labelled future constructions for up to ten in-line
cores. The opt-in scaled cross-section and side-profile requirements are in
`docs/CABLE-CONSTRUCTION-AND-VISUALISATION.md`.

The COR page includes an off-by-default live cable preview in a responsive
inspection dock. On wide windows it remains visible to the right of the
scrolling costing sections and the user can drag its divider to resize it. On
compact windows it moves beneath the editor instead of squeezing the costing
form. The cross-section and side profile scale with the dock width, while the
long print-repeat strip keeps its own horizontal scroll. Its visualisations
use the retained conductor OD, nominal finished OD, positive tolerance,
conductor finish colour, and selected masterbatch colour. Simplified mode shows
the conductor envelope; detailed cross-section mode draws every parsed strand
on a connected close-packed layout. Complete shells are hexagonal, while the
confirmed 16-end construction uses five centre strands and an eleven-strand
outer layer. Rope-lay groupings such as 7 groups of 19 strands retain their
full hierarchy. The same layout is used for retained cached and LIVE imported
Copper rows. Supplier descriptions without reliable numeric stranding remain a
clearly labelled simplified envelope rather than guessed strands. The side
profile uses a straight tube with parallel edges and narrow end faces viewed
about 20 degrees off side-on. Only the exposed outside rim is drawn on the
closed insulation end, so no ellipse line crosses the cable body. At the cut
end, the insulation annulus masks the conductor edges while a centred conductor
face, or one opening cap per detailed strand, remains visible through the hole.
The annulus and its inner rim remain visible around the conductor entry in
simplified and detailed modes. For a rope lay, each visible group is one continuous, gently twisting bundle from the
insulation opening to its matching compressed end face; fine longitudinal
strokes suggest the strands inside each bundle without turning them into
scalloped or noodle-like layers. End-face positions use the bundle's final
rotation and retain every parsed strand, while the side length shows only the
physically exposed outer strands or rope groups. Strand and outline widths
scale with the actual rendered strand size. The closed left
cap uses the same vertical highlight/shadow gradient as the tube body. When core
print is enabled it is shown separately on a smaller
horizontally scrollable cylinder, with character height scaled against finished
OD and start-to-start spacing scaled along its length. The complete print
preview block is absent while print is disabled.

The preview labels conductor and finished diameters, calculated radial wall,
and a separately identified published comparator. Comparable H05/H07
manufacturer data is guidance only: an out-of-range nearest size is labelled as
such and never presented as automatic standards compliance. Core-print inputs
now retain wording, colour, character height, start-to-start repeat distance,
and horizontal/vertical dot pitch in the portable costing document. The
cable-type dropdown is no longer repeated in the costing command strip because
construction choice happens on Home.

Central data is offline-safe: the app always retains a local last-successful
snapshot. Each Copper, Compounds, Masterbatch, Contacts, or Operators link can
use Microsoft Access or SQL Server. A WinUI 3 Navigator searches real tables
and views while showing a 200-row preview; its transform editor then shows
applied steps, Access physical-name/caption/description metadata, keep/remove
and rename controls, and automatic ATAG field matches before an atomic table
import. Every kept column and every row from the transformed database object is
retained locally; the costing references are validated, unit-normalised typed
projections of that same source table, so a field which is not yet used by a
costing is no longer discarded. Live Data labels the collapsed direct linked
table preview separately from the costing-ready views used by calculations.
Copper's lower view keeps nominal OD and calculated area but omits its redundant
blank nominal-area column. Tabs cannot unlink data; the explicit **Remove link**
action stops refresh while retaining the last transformed table and validated
snapshot offline.
Missing Copper values can be completed in the costing projection when retained
cells provide a defensible relationship: component costs can supply £/kg, reel
length and net weight can supply yield, stranding can supply metallic area, and
source volume can supply OD. Exact results are labelled **calculated**; an OD
available only from close-packed strand geometry is labelled **estimated** and
requires review. Each value retains its formula and source summary, cached tables
are re-projected offline, and no original linked-table cell is overwritten.
The setup, Navigator, Transform data, and import-result pages use movable,
resizable WinUI windows with app-owned WinUI title bars on the same monitor as
the main app. Each is owned by the ATAG window, so it stays above that app
during the workflow without becoming system-wide always-on-top. Navigator and
Transform data open at workspace scale
so the source list, full-width preview, rename/remove controls, and ATAG matches
are visible together. The main window restores its previous monitor, position,
size, and maximised state. The data-area chooser always shows Copper,
Compounds, Masterbatch, Contacts, and Operators with individual link status.
**Edit existing link** reopens the saved table directly in Transform data
without discarding the last successful copy. Connection options are above the
data previews. **Remove link** always asks which linked area is being removed
before its named confirmation, even when only one link exists. The navigation
footer expands to show all five areas independently and reports the exact live
count plus LIVE, checking, ready, offline-cached, or cached-only state for each.
`#DIV/0!` cells are reported and imported as blanks without blocking valid
rows. The unobtrusive **Refresh link** control reuses the saved query and never
discards a retained table after a failed update. Real ATAG schema acceptance is
still required before business rollout.

The conductor workflow separates TCW, PCW, titanium, tinsel, and other
supplier-defined material types before the construction/mm²/AWG choice.
Imported conductors with an identifiable construction and positive yield remain
selectable when price or nominal OD is blank; the page then identifies the
missing locked mapping and blocks only the result that needs it. Supplier quote
total and quoted mass remain the COR price source. **Costing Prepared by**
defaults to Laura from the imported Office operator list when no saved operator
has already been chosen.
Masterbatch selection includes all cached colours, an in-list and selected
colour swatch, compound-family compatibility, recorded temperature limits, and
the workbook notes. OD tolerance is entered as a linked ± value by default, with
an explicit option to enter different positive and negative tolerances.

The live result is the first card in the costing workspace. It can be pinned
while the page scrolls or opened in a movable, resizable, always-on-top live
window. The costing command strip stays visible above the scrollable content.
The left navigation submenu and the compact section menu both jump directly to
each material, labour, quotation, result, or trace block. Contacts use a
readable account/short-name/postcode suggestion layout. Masterbatch search
supports retained text fields, small typing errors, combined HSL descriptions
such as `dark blue` or `warm pastel`, colour-family/tone filters, colour-type
filters, and retained swatches.
Quotation totals use leading currency symbols and thousands separators. GBP
remains the calculation currency; retained European Central Bank reference
rates can produce an explicitly labelled converted quotation total. Quotation
inputs include reel count and metres per reel, concise conductor wording, simple
insulation family, generic or exact customer colour wording, packaging, delivery,
special notes, and terms. The reporting project generates a self-contained,
single-page A4 PDF quotation from approved application values without copying
costing formulas into the template. A page-accurate editable preview and final
approved workbook-matched branding remain planned.

Single-core documents now use schema-v2 project and revision identities.
Working copies save beneath the folder selected during storage setup and appear
through a portable relative-path project index. Approval stores the exact
outputs and recursive calculation trace and makes that saved revision
immutable. Editing an approved item begins the next working revision; duplicate
creates revision 1 of a new project. Older portable `.atagcosting` files remain
readable and can be added to the selected-folder index. See
`docs/PROJECT-REVISIONS.md` for the lifecycle and storage contract.

## Open in Visual Studio

Open `ATAG.Costing.sln` in Visual Studio 2026 and choose the x64 platform. The
WinUI project is `src/ATAG.Costing.WinUI`.

The development build remains unpackaged and self-contained so it can run from
the portable workspace. Normal users install the same WinUI application through
the versioned per-user installer described below; the installer does not depend
on the workspace drive letter.

## Build and run

```powershell
dotnet build "ATAG.Costing.sln" -c Debug -p:Platform=x64
dotnet run --project "src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj" -c Debug -p:Platform=x64
dotnet test "ATAG.Costing.sln" -c Debug -p:Platform=x64
```

After building, the USB/workspace root also contains `Open ATAG Costing.lnk` and
the drive-letter-independent `Open ATAG Costing.cmd` launcher.

Use that project-owned command/shortcut, or `dotnet run`, for live verification.
Do not use the generic Computer Use `launch_app` bridge for this project: on this
PC it has started the separately indexed Hudl Device Console as well as the
requested ATAG process even though the maintained ATAG source and launchers
contain no Hudl reference.

## Install and update

`Costing-App-Setup.exe` is the single user-facing installer. It installs for the
current Windows user, creates Start menu and desktop shortcuts, and adds the
normal Windows uninstall entry without requiring administrator rights. A clean
installer contains application files only: it does not carry database links,
retained central-data rows, settings, saved costings, workbooks, or customer
documents.

The installed app checks the public GitHub Releases feed anonymously after its
main window is visible. Settings provides Stable/Beta selection, a cumulative
list of every release since the installed version, download progress, and an
explicit **Download and restart** action. Per-user
settings, linked-table definitions, retained offline tables, and user-selected
business storage are outside the replaceable application folder and remain in
place across an update.

The first installer is not code-signed, so Windows may identify it as an unknown
publisher until an organisation signing certificate is added. Build and release
instructions, the clean-package boundary, and verification requirements are in
`docs/INSTALL-AND-UPDATE.md`.

## Solution projects

- `ATAG.Costing.Domain` — calculation concepts and business rules.
- `ATAG.Costing.Application` — use cases, interfaces, preferences, and orchestration.
- `ATAG.Costing.Infrastructure` — local persistence and future workbook/data adapters.
- `ATAG.Costing.Reporting` — quote, contract-review, costing, and audit-trail templates.
- `ATAG.Costing.WinUI` — the Windows user interface and composition root.
- `ATAG.Costing.Domain.Tests` — Excel-free business-rule and trace tests.
- `ATAG.Costing.WorkbookParity.Tests` — hash-bound, approval-gated workbook
  reference cases.
- `ATAG.Costing.Application.Tests` — central-data refresh, retention,
  workbook import, revision lifecycle, immutable persistence, project-index,
  semantic colour-search, and compatibility-table tests.

See `docs/SCOPE.md` for the migration boundary and planned implementation
slices, `docs/WORKBOOK-SPECIFICATION.md` for workbook evidence, and
`docs/CENTRAL-DATA.md` for the offline/link model.
`docs/PROJECT-REVISIONS.md` describes portable indexed revision storage.
Decisions that must not be guessed remain in `docs/OPEN-QUESTIONS.md`.

To continue development from another PC or a different USB drive letter, start
with `CONTINUE-ATAG-COSTING.md`.

## Regenerate branded app-icon assets

The source company icon is stored beside the project folder as
`ATAG Design LTD. Icon.ico`. From the project directory, regenerate every WinUI
icon and logo size with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\tools\Generate-AppIconAssets.ps1"
```
