# Costing projects and revisions

## Purpose

The `.atagcosting` format is a portable project-revision document. Schema
version 3 adds an explicit construction discriminator plus dual-insulation
inputs and result evidence while retaining the schema-v1/v2 single-core
reader. Approved revisions store the exact evidence required to reproduce what
was reviewed.

Business files are rooted only in the folder selected during storage setup. The
app does not substitute Documents, local application data, or another folder if
that location is unavailable.

## Portable storage layout

All index paths are relative to the selected business-data folder:

```text
<selected business-data folder>/
  ATAG-Costing-Index.json
  Costings/
    <project identity>/
      Revision-0001-<revision identity>.atagcosting
      Revision-0002-<revision identity>.atagcosting
```

The project and revision identities are GUIDs. The index stores project name,
customer, revision number, state, timestamps, and the relative document path.
It contains no USB drive letter, machine-specific root, or central-data
connection.

`JsonSingleCoreProjectRepository` refuses an unavailable selected root and
rejects an index path that escapes that root.

## Revision lifecycle

Each document is in one of two explicit states:

- `WorkingCopy` — editable and saveable to its stable revision path.
- `ApprovedRevision` — immutable after its first successful approval save.

Approval requires a valid calculated result and non-empty calculation trace.
The document store permits a matching working document to be replaced once by
its approved form. Any later overwrite of that approved path is rejected in
Infrastructure, independently of the UI.

The first edit after opening an approved revision starts a new working revision:

- the project identity is retained;
- a new revision identity is created;
- the revision number increments;
- approval metadata is cleared;
- the approved source document remains unchanged.

**Duplicate as new project** instead creates a new project identity, a new
revision identity, and working revision 1 while copying the current costing,
quotation, and contract-review inputs.

## Stored approval evidence

Schema version 2 added the single-core revision identity, lifecycle, result and
trace evidence. Schema version 3 retains those fields unchanged and adds:

- `SingleInsulatedCore` or `DualInsulation` construction kind;
- a locked conductor reference and two locked compound/masterbatch layers;
- explicit finished quote length, core/first-layer start-up, allowance, risk,
  markup, and comparison-margin inputs;
- first-layer production length as finished plus start-up and second-layer
  production length as finished-only;
- two independent extrusion line profiles, manual overrides, setup times,
  operator counts, and rates;
- ordered Tape, Chalk, Foil, Braid, Lapscreen, and Drain wire selections, even
  though module-specific material formulas remain staged;
- dual material, both extrusion, labour, commercial, and complete recursive
  trace evidence.

The common revision evidence includes:

- project identity, revision identity, and revision number;
- working/approved state;
- created, updated, saved, and optional approved timestamps;
- the effective and generated core names;
- every displayed material, production, labour, commercial, and quote result;
- the raw recommended quotation price used by reporting;
- calculation sections containing recursive calculation steps.

Each saved calculation step retains its identifier, label, business meaning,
expression, substituted expression, raw value, displayed value, unit, rounding
rule, warning, rule version, and recursive input steps.

When an approved revision opens, the matching WinUI view model uses the stored
result and trace rather than silently recalculating it with newer rules.
Working copies are recalculated from their locked saved material values and
current shared domain rules.

## Central-data and offline boundary

The project document copies the selected material identifiers, names,
suppliers, locked engineering values, and supplier-quote values. If a retained
catalogue no longer contains one of those rows, the app reconstructs a
document-local reference from that evidence. This lets an approved revision
reopen while the live central-data link is offline or has changed.

This behavior does not turn the workbook importer into a runtime central-data
source. Access and SQL live reads remain gated until the actual ATAG provider
and schema are available for validation.

## Legacy compatibility

Schema versions 1 and 2 remain readable as single insulated core documents.
Schema-v1 input-only files receive project/revision identity, working-copy
state, and timestamps derived from their saved time. Their outputs are
recalculated because no approved result snapshot exists. The schema-v3
construction discriminator is persisted on the next save.

The indexed Open dialog can also browse an older portable `.atagcosting` file.
The file is opened without changing it; **Save costing** adds it to the current
selected business-data folder and index.

## Key source files

```text
src/ATAG.Costing.Application/Projects/SingleCoreProjectDocument.cs
src/ATAG.Costing.Application/Projects/DualInsulationProjectPayload.cs
src/ATAG.Costing.Application/Costing/DualInsulationCostingApplicationService.cs
src/ATAG.Costing.Application/Costing/DualInsulationWorkspaceState.cs
src/ATAG.Costing.Application/Projects/SingleCoreCalculatedResultSnapshot.cs
src/ATAG.Costing.Application/Projects/SingleCoreProjectRevisionService.cs
src/ATAG.Costing.Application/Projects/ISingleCoreProjectRepository.cs
src/ATAG.Costing.Infrastructure/Projects/JsonSingleCoreProjectDocumentStore.cs
src/ATAG.Costing.Infrastructure/Projects/JsonSingleCoreProjectRepository.cs
src/ATAG.Costing.WinUI/ViewModels/SingleCoreCostingViewModel.cs
src/ATAG.Costing.WinUI/ViewModels/DualInsulationCostingViewModel.cs
src/ATAG.Costing.WinUI/MainPage.xaml.cs
tests/ATAG.Costing.Application.Tests/Projects/SingleCoreProjectDocumentTests.cs
```
