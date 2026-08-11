# Production speed library

## Purpose

The Production speed library replaces hidden workbook lookups with private,
editable app data. A user can create any number of production lines, maintain
the OD-to-running-speed bands for each line, and record known cable runs with
their capstan and extruder settings.

The library is intentionally separate from linked Copper, Compound,
Masterbatch, Contact, and Operator tables. It is retained in the current
Windows user's LocalAppData and is never included in the installer, a public
release, or the Git repository.

## Workbook evidence reviewed on 11 August 2026

Sixteen distinct macro workbooks on the removable drive were inspected
read-only.

- Five newer modular workbooks use finished-core OD bands of 1.00, 1.20, 2.00,
  and 2.50 mm with respective speeds of 15,000, 13,000, 8,000, and 6,000 m/h,
  followed by 700 m/h above the final band.
- Eleven original/legacy workbooks use the same broad insulation rule but use
  6,500 m/h at the 2.50 mm boundary.
- The legacy sheets also contain separate round/profile sheathing tables. Those
  values are copied into a production-sheet capstan-speed field, but the
  extruder-setting field is blank.
- Workbook labels sometimes say `km/h` even though the calculation divides
  metres by the value to obtain hours. The app therefore uses the dimensionally
  correct unit `m/h`.

A clean installation starts with no production lines, speed bands, known cable
runs, or machine settings. The accepted newer insulation profile is available
only through the explicit **Add general starter profile** action. The user is
warned to rename and adjust it for the actual line. Legacy sheathing/profile
values are evidence, not an assumed machine calibration, and are never silently
seeded as production truth.

## Modular data model

Each production line owns:

1. a stable local identifier and user-editable name;
2. an ordered list of maximum finished-OD bands and line speeds in m/h;
3. an above-maximum speed;
4. any number of known cable-run observations.

Each known run can hold:

- cable reference and process name;
- nominal core OD and its plus/minus tolerance;
- nominal finished OD and its plus/minus tolerance;
- capstan setting and extruder setting as machine dial values;
- either a directly measured line speed, or produced length plus running time;
- notes.

Capstan and extruder values are not assumed to be metres per hour. A run can be
saved with settings only, but it cannot influence an estimated quote speed
until measured speed evidence is also present. When length and running minutes
are entered, measured speed is derived visibly as:

`observed speed = produced length / (running minutes / 60)`

## Estimation rule

For the selected production line and process, the estimator ranks usable known
runs by finished OD, core OD, and—when supplied on both sides—capstan and
extruder settings. It uses at most the three closest sufficiently similar runs
and reports the contributing records and confidence. If no sufficiently close
measured run exists, it falls back to the selected line's OD band.

The result always exposes:

- recommended running speed and source;
- quote length;
- calculated running time (`length / speed`);
- confidence;
- the known runs used as evidence, or the exact OD band used as fallback.

The first integration applies an accepted library estimate to the current
costing as an explicit manual line-speed input. This immediately affects
production time, labour, and quote calculations without hiding the selected
value. Persisting a complete versioned library-evidence snapshot inside each
approved project is a later revision-schema enhancement.

## User interface

`Production speeds` is a first-class navigation destination beside Live Data.
It provides:

- production-line selection plus Add, Edit, and Delete line actions;
- an editable OD speed-band list;
- an editable known cable-run list;
- a quotation estimator with optional machine settings;
- one-click input copying from the current COR, dual first-extrusion, or dual
  second-extrusion costing, with missing costing values left visibly blank;
- explicit actions to apply the latest estimate to single-core, dual first
  extrusion, or dual second extrusion costing.

No removable-drive letter is stored. The workbook scan is migration evidence
only; normal app operation has no Excel dependency. The runtime file
`production-speed-library.json` is excluded from Git and rejected by the release
package safety audit.
