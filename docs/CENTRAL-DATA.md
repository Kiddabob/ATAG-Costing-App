# Central data and offline behaviour

## V1 objective

The app must remain usable when a configured central-data source is missing,
moved, or temporarily offline. After a successful import, material selections
therefore read from that user's local last-successful table rather than querying
the live connection for every costing action.

A clean build and clean installation contain **no customer, supplier, operator,
or material rows**. First-run setup guides the user through linking and importing
the five required areas: Copper, Compounds, Masterbatch, Contacts, and Operators.
The setup is complete only when all five have both a saved link and a validated
retained table.

Costing selectors use validated typed projections with the required price and
engineering values. A successful database import now also retains the complete
transformed source object: every row, every kept column, its physical source
name, available caption/description metadata, type, nullability, non-blocking
cell diagnostics, and saved query steps. The typed records are therefore a
costing-safe projection of the full retained table rather than the only data
kept by the app.

## Refresh model

```text
Access or SQL table -- successful import/refresh --> local retained table
                                                        |
                                                        v
                                                costing selections
```

Changing link settings does not discard the current snapshot. A configured link
is checked every 30 seconds. After a failed attempt, automatic checks pause and
the colour-coded state reports **OFFLINE** until the user presses **Refresh
link**. A successful read replaces the local
snapshot only after the complete read succeeds. A missing file, unavailable
database, invalid response, or cancelled refresh leaves the prior snapshot
untouched and reports that retained state to the user.

Indexed `.atagcosting` save/open is independent of the central-data link and
remains available while the link is offline. Approved schema-v2 revisions show
their stored outputs and recursive calculation trace rather than recalculating
against a refreshed catalogue or newer rules. Saved locked material evidence
can reconstruct a document-local reference when a row is no longer present in
the retained catalogue.

The snapshot and link configuration are stored per Windows user at:

```text
%LOCALAPPDATA%/ATAG Design Ltd/ATAG Costing/central-data-state.json
```

The retained state is machine/user-local and is excluded from source control and
release contents. Existing users keep their previously imported rows when an
upgrade is installed. Legacy states are normalised without inventing missing
tables; a clean or incomplete state remains visibly incomplete until the user
imports the required areas.

## Database Navigator and transform editor

The runtime central-data setup mirrors Excel's linked-database workflow. It
does not treat an Excel workbook as the central database and it no longer asks
the user to type a table name or choose app columns before seeing the data.

The WinUI 3 flow is:

1. choose **Copper**, **Compounds**, **Masterbatch**, **Contacts**, or
   **Operators**;
2. choose Microsoft Access or SQL Server and identify the database;
3. use **Navigator** to search the real table/view catalogue and inspect up to
   200 rows in a horizontally and vertically scrollable table preview;
4. continue to **Transform data**, which keeps the table preview visible and
   shows the query, applied steps, reusable row filters, text/blank-row options,
   source-column names and metadata, deliberate keep/remove and rename controls,
   and automatic ATAG field matches;
5. import only after all required matches validate. The complete selected
   object is read again; the linked area and its query definition are written
   to the last-successful state together in one atomic file replacement.

Every page in this flow now opens in a normal WinUI 3 window with an app-owned
WinUI title bar rather than a separate standard Windows caption. Each workflow
window is owned by the ATAG Costing main window, so it stays above that app
while the user is completing the workflow but does not become system-wide
always-on-top over unrelated applications. It opens on the same monitor as the
ATAG Costing main window. The setup pages use a generous compact default, while
Navigator and Transform data open as near-work-area workspaces so the
catalogue, table preview, and source-column settings are visible together. All
of these windows can be moved, resized, maximised, and minimised without
changing any imported data. The main window saves its last restored position,
monitor, size, and maximised state in
`%LOCALAPPDATA%/ATAG Design Ltd/ATAG Costing/window-placement.json`; a missing
monitor safely falls back to the primary display.

Navigator is for choosing a table or view, not choosing columns. Transform data
retains all source columns by default; the user can then remove a genuinely
unwanted column or rename one before mapping. The editor infers app fields from
recognised effective headers and, for Access, the physical OLE DB column name
plus available caption/description metadata. This allows a physical field such
as `Nominal` to map to the costing field `Nom OD (mm)` when the Access column
description carries that business label. It also shows the inferred matches so
the user can correct an ambiguous or missing match before import. Source,
Navigation, ignore-error, row-filter, remove-column, rename-column, trim-text,
and remove-blank-row steps are stored with the link so manual and scheduled
refreshes use the same query. Row filters support equals, not equals, contains,
does not contain, starts with, ends with, blank, and not blank. Values are
compared without case sensitivity, and boolean fields accept source values such
as `True` and `False`; for example, an Operators link can retain only rows where
`Office` equals `True`.

The first setup page shows all five independent areas together, including each
area's linked or retained-only status. Importing Copper therefore does not hide
Compounds, Masterbatch, Contacts, or Operators from the next setup.

After a table has been linked, **Edit existing link** reopens its saved table
directly in Transform data. If several areas are linked, the app first asks
which saved link to edit. The current query steps and field matches are loaded,
and the previous retained full table remains active until the edited query has
been validated and its complete source object has imported successfully.

Masterbatch compatibility is a grouped projection, not a single source field.
Transform data maps every material-family pair independently: **Use** and
**Max Temp** for PVC, PE/PP/PUR, PS, ABS, ACETAL, PBT, Nylon, and PC/PES. The
complete transformed source table remains retained, while the costing view
combines those sixteen mapped fields only for presentation. An explicit false
compatibility leaves that material family's temperature blank in the costing
view; an unmapped family remains visibly unrecorded. This preserves the Access
table's real column structure and avoids flattening unrelated compatibility and
temperature values into two ambiguous text columns.

The Office worker selector is projected directly from retained Operators rows
whose mapped **Office** value is true. It does not require a separate
workbook-only employee flag. When Laura is present in that filtered list she is
the default **Costing Prepared by** value; the user can still choose another
eligible office worker.

The complete transformed table and its validated typed costing projection are
committed atomically. A transform or required mapping error cannot replace
either the prior full table or the prior costing projection. Live Data exposes
the complete retained object in a bounded inspection grid with its full saved
row/column counts, while the working views show the validated, unit-normalised
fields used directly by calculations. The direct transformed table is the
source of truth; the lower view is not a second database link. The direct table
preview is collapsed by default but easy to open. Copper's costing-ready view
shows **Nominal OD** and calculated metallic area but omits the redundant blank
nominal-area display.

### Derived values in the typed Copper view

A blank or zero Copper field does not automatically make the retained row
unusable. The app first preserves the complete source row exactly as imported,
then applies versioned rules only to the typed view used by costings:

- price per kilogram = manufacturing cost + copper cost (using copper including
  premium only when the dedicated copper-cost source is unavailable);
- yield = reel conductor length ÷ reel net weight;
- metallic area = strand count × π × strand diameter² ÷ 4;
- outside diameter = √(4 × volume per metre ÷ (1,000 × π)).

Those are labelled **calculated** because their source values determine the
answer. If OD and source volume are both absent but the strand construction is
numeric, the live-preview close-packed envelope may supply an **estimated** OD.
That estimate is visibly labelled, opens the conductor review notice, and is
not presented as database fact.

Every filled value carries the formula, substituted source values, confidence,
and derivation-rule version. The original blank/zero/error cell is retained in
the direct table and is never overwritten. These rules are reapplied from the
cached full Copper table when the app starts, so an existing offline import can
benefit without reconnecting to Access or SQL Server. Other data areas are not
guessed: a new derivation is added only when its source relationship can be
documented and tested.

Neither direct-table nor costing-view tabs can be closed. **Remove link** is the
only unlink action. It always starts with a choice of linked area, even when
only one link currently exists, and then requires confirmation for that named
area. It stops future refresh for the chosen area but deliberately retains the
full transformed table and last validated costing snapshot for offline work
and audit.

The **Connection options** card is placed before the source and costing tables,
so setting up, editing, or removing a link does not require scrolling through
the data previews. The left **Material links** flyout opens upward so its footer
header does not move while it is expanded. It always lists Copper, Compounds,
Masterbatch, Contacts, and Operators separately. Each row reports
**LIVE**, **Checking**, **Ready to check**, **Offline · cached**, or **Cached
only**. The collapsed summary reports the exact live count, for example
**1 of 5 LIVE**, rather than allowing one successful Copper link to imply that
all five tables are connected.

The first production providers are:

- Microsoft Access through the installed 64-bit ACE OLE DB provider, accepting
  `.accdb` and `.mdb` files and enumerating user tables/views;
- SQL Server through `Microsoft.Data.SqlClient`, supporting Windows sign-in or
  a one-time SQL user name/password.

SQL passwords are never stored. A Windows-authenticated SQL link and an Access
link can refresh from the saved definition. Editing a SQL-password link asks
for the session-only sign-in again before reopening Transform data.

Each data area keeps an independent link, so the five areas may use different
databases, objects, or source types. A manual or 30-second refresh processes
each linked area independently. A successfully read and validated area replaces
only that retained table. If another link fails, its previous rows remain and
the connection state is **PARTIAL/OFFLINE** until a manual retry. Refresh
results retain a structured outcome for every configured area so a partial
refresh updates those five status rows without flattening them into one
misleading global state.

### Non-blocking source errors

`#DIV/0!`, `#DIV/0`, and provider division-by-zero values are deliberately
non-blocking during Navigator, transform preview, and import:

- the affected cell is displayed as **Ignored · division by zero**;
- that cell is treated as blank when a typed app value is required;
- other cells in the row and all other readable rows continue;
- the preview/import summary reports the ignored-cell count;
- a row is skipped only when its identifying required data is blank, not merely
  because another cell contains a division error.

If a database provider aborts the entire query rather than returning an error
cell, rows already read remain previewable and the linked retained table is not
replaced by an incomplete import.

No live ATAG database file is committed to this workspace. A user-connected
Access Copper import has now exercised the real Navigator/import route, but the
saved cache was created before full-table retention and exposed an ambiguous
typed-view heading: **Nominal** was nominal area, while the saved OD mapping was
already `Nom OD (mm)`. The heading is corrected and the next import will retain
the full object. The providers, transform pipeline, metadata matching, atomic
typed/full-table commit, and refresh orchestration are covered by synthetic
provider/table tests. Before business rollout, exercise all five authoritative
objects through the new full-table path and approve keys, units, types,
nullability, price-effective-date rules, and any source-specific
transformations.

## Reference-workbook importer

The existing Open XML reader remains an internal migration/test adapter for
checking the five workbook reference tables without launching Excel. It is not
offered as a runtime central-database link.

## Future extension

Before treating a real database as authoritative, document and approve:

- authoritative tables and keys;
- column mappings and units;
- effective-date and price-selection rules;
- authentication and secret handling;
- transaction/isolation expectations;
- validation required before committing a replacement snapshot;
- audit and approval ownership.
