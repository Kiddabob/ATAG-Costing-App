# Workbook specification

## Status

This document records the first reproducible audit of the reference workbook for
Slice 1. It is migration evidence, not a claim that every workbook behaviour has
already been approved as a permanent business rule.

The audit was performed without opening, recalculating, saving, or modifying the
workbook. `tools/inspect_reference_workbook.py` reads the Open XML package and
the embedded VBA project directly. Re-run it from the project directory with:

```powershell
python .\tools\inspect_reference_workbook.py --section summary
```

The script finds the workbook relative to the project and contains no drive
letter.

## Immutable source evidence

| Field | Value |
|---|---|
| Relative path | `../(WIP Mitchell) Costing Sheet.xlsm` |
| Filename | `(WIP Mitchell) Costing Sheet.xlsm` |
| Size | 1,311,202 bytes |
| Last modified | 2026-07-28 14:17:46 +01:00 |
| SHA-256 | `6A9DBE53DF2A403BDB92A23FDC2C4AD55702B6ADF089ED02FA327F3E504851D3` |

That SHA-256 remains the immutable parity baseline. A read-only verification on
29 July 2026 found that the current workbook file had drifted to SHA-256
`823FCE28815A9420E87A9FA119790243C8A4E9961B26B976A26EBE79BE9FA0ED`,
size 1,322,759 bytes, and modified time 28 July 2026 23:36:28. The full five-table
embedded snapshot was generated from this current workbook. Do not replace the
parity baseline or approve new golden results until the workbook change has been
reviewed.

Any later cleaned or approved workbook must be retained as a separate file and
recorded with its own size, timestamp, and hash. Do not overwrite this evidence
file.

## Workbook inventory

- 54 worksheets: 21 visible, 32 hidden, and 1 very hidden.
- 1,565 formula cells.
- 9 real Excel tables.
- 6 defined names.
- 5 workbook connections and 5 Power Query output/query-table parts.
- An embedded VBA project with 68 declared and extracted modules.
- No Open XML external-link parts.
- 2 cached formula errors, both `#DIV/0!`.

### Visible worksheets

`MasterbatchCodeList`, `Copper`, `Compounds`, `Contacts`, `Operators`,
`COR1CopperPrice`, `CORT1Summary`, `CORT1Additional`,
`CableSheathSummary`, `Menu`, `Braid Calculator`,
`SBS1DualInsCopperPrice`, `SBS1DualInsContractReview`,
`SBS2FlatCopperPrice`, `SBS2FlatCompPrice1`, `SBS2FlatMB1Price1`,
`SBS2FlatMB2Price1`, `SBS2FlatCompPrice2`, `SBS2FlatMBPrice2`,
`SBS2FlatSummary`, and `SBS2FlatContractReview`.

### Hidden worksheets

`Masterbatch`, `CableSheath1`, `COR1CompPrice`, `COR1MBPrice`,
`SBS1CorCopperPrice`, `SBS1CorCompPrice`, `SBS1CorMBPrice`,
`SBS1CorSummary`, `SBS1CorContractReview`, `SBS1DualInsCompPrice1`,
`SBS1DualInsMBPrice1`, `SBS1DualInsCompPrice2`,
`SBS1DualInsMBPrice2`, `SBS1DualInsSummary`, `Sheath1MBPrice`, and
`COR2MBPrice` through `COR18MBPrice`.

### Very-hidden worksheet

`_LastClearBackup` is a small six-row backup used by the clear/restore VBA
workflow. It is operational workbook state, not a costing formula family.

## Structured tables and reference data

| Worksheet | Table | Range | Purpose |
|---|---|---:|---|
| MasterbatchCodeList | `MasterbatchCodeList` | `A1:W204` | Power Query output containing colour codes, suppliers, compatibility, temperatures, price, and colour hex |
| Masterbatch | `Masterbatch` | `A1:AB349` | Hidden expanded masterbatch reference table used by lookups and the colour picker |
| Copper | `Copper` | `A1:R323` | Conductor descriptions, supplier/manufacturing costs, yields, nominal OD, section, and AWG |
| Compounds | `Compounds` | `A1:H75` | Compound price, specific gravity, material type, description, and data-sheet flag |
| Contacts | `Contacts` | `A1:W568` | Customer/supplier identity, addresses, contact data, and account-type flags |
| Operators | `Operators` | `A1:N6` | Operator identity and role flags |
| SBS1CorSummary | `SBS1CorTypeMap` | `AQ1:AR26` | Raw material type to cable-code abbreviation |
| SBS1DualInsSummary | `SBS1CorTypeMap9` | `AQ1:AR27` | Dual-insulation material type to cable-code abbreviation |
| SBS2FlatSummary | `SBS1CorTypeMap910` | `AQ1:AR27` | Flat-cable material type to cable-code abbreviation |

The five Power Query outputs are `Compounds`, `Contacts`, `Copper`,
`MasterbatchCodeList`, and `Operators`. Their connection names are:

- `Query - Compounds`
- `Query - Contacts`
- `Query - Copper`
- `Query - MasterbatchCodeList`
- `Query - Operators`

Each connection uses the embedded `Microsoft.Mashup.OleDb.1` provider with
`Data Source=$Workbook$` and selects from the same-named query. The workbook
contains the corresponding query-table and custom XML parts. The upstream
authoritative data sources and refresh ownership still require confirmation.

## Defined names

Five hidden, sheet-scoped names called `ExternalData_1` cover the Power Query
output ranges on `MasterbatchCodeList`, `Copper`, `Compounds`, `Contacts`, and
`Operators`.

The only explicit print-area name is:

```text
SBS1DualInsContractReview!$A$2:$AE$47
```

No other worksheet has a defined print area in the current file. Report and
print composition must therefore remain a separate application concern rather
than treating current worksheet print settings as final templates.

## VBA inventory

The VBA project is named `VBAProject`. All 68 declared modules were extracted:
54 document modules and 14 standard/class/form modules. `ThisWorkbook` contains
the private `Workbook_Open` event.

The non-document modules and public entry points are:

| Module | Type | Public entry points |
|---|---|---|
| `clearSheet` | Standard | `ClearInputs_OnThisSheet`, `RestoreLastClearedInputs_OnThisSheet` |
| `clsColourPickerLabel` | Class | `Init` |
| `ContractReviewAddDate` | Standard | `SBS1CorEstimateApproveDate`, `SBS1CorReviewApproveDate`, `SBS1CorOrderAcknowledgementDate` |
| `frmChooseMasterbatchColour` | UserForm | `targetSheetName`, `SelectColourByIndex`, `ScrollColourFrame` |
| `GoToPriceSheet` | Standard | `GoToPriceSheet_FromImage` |
| `GoToSummaryFromMB` | Standard | `BackToSummary_HideCurrentMBPriceSheet` |
| `modCableCodeBuilder` | Standard | `BuildDualInsCableCode`, `BuildCableCode` |
| `modUserFormMouseWheel` | Standard | `HookColourPickerMouseWheel`, `UnhookColourPickerMouseWheel` |
| `SBS1CoreClear` | Standard | `SBSClearInputs_OnThisSheet` |
| `SBS1CoreNav` | Standard | core build, navigation, next/previous, summary, contract-review, and menu entry points |
| `SBS1DualInsNav` | Standard | dual-insulation build and navigation entry points |
| `SBSFlatNav` | Standard | 2/4/6/8/10-core flat build and navigation entry points |
| `ShowColPicker` | Standard | `ShowMasterbatchColourPicker`, `PositionFormOverExcel` |
| `showWorking` | Standard | `ToggleWorkingRows_OnThisSheet` |

VBA is therefore part of the behavioural specification. It covers clearing and
restoring inputs, defaulting, navigation, sheet visibility, cable-code building,
contract-review dates, masterbatch colour selection, and showing hidden
workings. Formula-cell extraction alone would omit those behaviours.

## Formula map

### High-level families

| Family | Representative evidence | Inputs and outputs |
|---|---|---|
| Copper price and usage | `COR1CopperPrice`, `SBS1CorCopperPrice`, `SBS1DualInsCopperPrice`, `SBS2FlatCopperPrice` | conductor selection, supplier price by kg or metre, yield, quote length, price per metre, quote price, and quote mass |
| Compound geometry and usage | `COR1CompPrice`, `SBS1CorCompPrice`, dual-insulation and flat compound sheets | copper OD, insulation OD/tolerance, cross-sectional areas, compound density, kg/m, quote kg, and price |
| Masterbatch compatibility and usage | standard, dual-insulation, and flat masterbatch sheets | colour compatibility, addition rate, masterbatch kg per quote, kg/m, g/m, and price |
| Core summaries | `CORT1Summary`, `SBS1CorSummary`, `SBS1DualInsSummary`, `SBS2FlatSummary` | per-metre and per-quote copper/compound/masterbatch totals, material descriptions, core count, allowance, and markup |
| Sheath geometry and usage | `CableSheath1`, `CableSheathSummary` | core count/OD, sheath OD/tolerance, material area, compound density, kg/m, quote usage, and summary totals |
| Braid engineering | `Braid Calculator` | bobbins, ends, strand diameter, total strand count, combined width, coverage factors, lay-up factors, and gear/lay reference data |
| Contract review | `SBS1CorContractReview`, dual-insulation and flat variants | customer/address lookup, material quantities, review answers, risk/markup inputs, approval dates, and revision-facing values |
| Cable codes | summary formulas plus `modCableCodeBuilder` | conductor construction, wire type, material type map, customer suffix, and construction-specific code |

Lookup and reference expressions dominate the workbook: references, `IFERROR`
with `VLOOKUP`, plain `IFERROR`, `IF`, and table lookups account for most formula
cells. Geometry and costing formulas then combine those selected values.

### Repeated worksheet families

Formula signatures identify these exact repeated families:

- 19 standard masterbatch sheets, each with 31 formula cells:
  `COR1MBPrice`, `Sheath1MBPrice`, and `COR2MBPrice` through
  `COR18MBPrice`.
- 3 first-layer masterbatch sheets, each with 32 formula cells:
  `SBS1DualInsMBPrice1`, `SBS2FlatMB1Price1`, and
  `SBS2FlatMB2Price1`.
- 2 first-layer compound sheets, each with 37 formula cells:
  `SBS1DualInsCompPrice1` and `SBS2FlatCompPrice1`.
- 2 second-layer masterbatch sheets, each with 38 formula cells:
  `SBS1DualInsMBPrice2` and `SBS2FlatMBPrice2`.

These are migration families, not separate application calculations. One domain
rule must serve every occurrence.

### Allowance, risk, markup, and margin

The workbook uses `1.03` as a general material-usage boost. The user confirmed
that this is a 3% waste/start-up allowance. It must be labelled and traced as
such; it is not risk, markup, or margin.

The first domain implementation gives this rule its own identifier:

```text
usage-allowance/v1
```

Summary sheets hold separate markup inputs and create their multipliers with
`1 + rate`. Risk, markup, and margin remain distinct concepts and must not absorb
the waste/start-up allowance.

The `COR1` summary also contains a confirmed workbook defect:

- `CORT1Summary!X11` already includes the masterbatch quote value in the core
  material subtotal;
- `CORT1Summary!X12` calculates `X11*AH4+X15`, adding that same masterbatch value
  again after markup.

The user confirmed that the second addition is wrong. Single-core V1 therefore
sums conductor, compound, and masterbatch once, applies risk, and then applies
markup. The contract-review evidence at `SBS1CorContractReview!Y39` shows the
intended commercial order as `(Total × (1 + Risk)) × (1 + Markup)`.

### Production time and labour

The working workbook derives insulation line speed from finished core outside
diameter:

- up to 1.00 mm: 15,000 m/h;
- up to 1.20 mm: 13,000 m/h;
- up to 2.00 mm: 8,000 m/h;
- up to 2.50 mm: 6,000 m/h;
- above 2.50 mm: 700 m/h.

The evidence is the line-speed lookup beside
`SBS1CorCompPrice!AW19:AX27`, selected by `SBS1CorCompPrice!AX24`.
`SBS1CorCompPrice!AX25` divides quote length by line speed to obtain production
hours. The contract review then carries that time through
`SBS1CorContractReview!R33` and calculates labour at
`SBS1CorContractReview!Y33`.

The separately supplied original-creator workbook, `GB - 16-2-2-C.xlsm`, has the
same broad model in `Costing!G34:G37` and
`Costing Calculations!N33`: select speed from OD, calculate running time, and
apply an hourly labour/overhead rate. Its formulas contain several broken
defined-name and `#REF!` dependencies, so it is retained as feature-coverage
evidence rather than an approved numerical oracle.

The application implementation keeps line speed, setup time, operator count,
and hourly labour rate as distinct traced inputs. Automatic speed is the
default; a manual override is visible when a job must run differently.

### Commercial price comparisons

The working workbook exposes more than one commercial-price expression:

- `SBS1CorContractReview!Y39` and `AP39` apply risk and markup sequentially;
- `SBS1CorContractReview!AP40` adds the risk and markup rates to the estimate as
  a comparison;
- the original-creator workbook uses `Contract Review!G40` for a
  divide-by-remaining-percentage expression, which is a target-margin method
  even though the nearby wording calls the input markup.

The application therefore makes the sequential expression the recommended
result and shows the additive and target-gross-margin expressions as clearly
labelled alternatives. It does not treat markup and margin as synonyms.

### Generated and custom core names

`SBS1CorSummary!B23` generates a core name by parsing conductor strand count and
strand diameter, mapping conductor type and compound family to short codes, and
optionally appending a customer short name for a special product. The working
example resolves to the pattern `COR 0720 T T2`.

`SBS1CorContractReview!A30` selects a manually entered name when its override is
enabled and otherwise uses the generated summary name. The application
preserves this distinction as a visible generated name, a deliberate custom
name toggle, and one effective name carried into contract review.

### Rounding and display precision

Only five formula cells explicitly call a rounding/text function:

- `COR1CopperPrice!B14`: `ROUND(B16/B12,2)`
- `SBS1CorCopperPrice!B16`: quote price divided by quote length, rounded to 4 decimals
- `SBS1DualInsCopperPrice!B20`: the same 4-decimal boundary
- `SBS2FlatCopperPrice!B20`: the same 4-decimal boundary
- `SBS1CorSummary!B23`: cable-code strand diameter rounded to a two-digit code

Most other workbook formulas retain unrounded cached values and rely on cell
number formats for display. Domain calculations must keep the raw value and
state display rounding separately. The initial compatibility policy uses
midpoint-away-from-zero at named display boundaries; business approval is still
required before that becomes a global application policy.

## Error and default behaviour

The two cached errors are:

```text
SBS1DualInsMBPrice2!AV31 = #DIV/0!
SBS2FlatMBPrice2!AV31    = #DIV/0!
```

Neither cell is used by the first parity candidate.

Workbook error/default patterns include:

- `IFERROR(...,"")` for missing or invalid intermediate values;
- user-facing fallback labels such as `Supplier`, `Description`, `Max Temp`,
  `Nom OD (mm)`, `£ Per Core`, and `Missing Yield`;
- zero fallbacks on some summary and contract-review outputs;
- `LET` validation that rejects simultaneous kg and metre/bag quote entries with
  `Enter kg OR m, not both` or `Enter kg OR bags, not both`;
- the very-hidden `_LastClearBackup` plus VBA defaults for clearing/restoring
  user inputs.

The application domain should return typed validation findings instead of
turning invalid values into empty strings. The current first rule rejects
negative quantities/rates/prices and zero or negative quote length, permits a
zero-usage boundary, and warns when an addition-rate fraction exceeds 1.

## First vertical slice: masterbatch usage per metre

### Selection reason

This slice is small, unit-stable, repeated across 19 standard masterbatch
worksheets, and independent of the two cached errors. It produces a complete
trace without requiring the costing UI or copying spreadsheet lookups into a
view model.

The reusable rule is:

```text
masterbatch-usage-per-metre/v1
```

### Source-cell map

| Meaning | Source | Formula/value | Unit/display |
|---|---|---|---|
| Base compound usage per metre before allowance | `COR1CompPrice!B26` | cached `0.0011704066262801845` | kg/m |
| Quote length | `COR1CompPrice!B30` | cached `5000` | m |
| Waste/start-up allowance | `COR1CompPrice!B28` | `(B26*B30)*1.03` | 3% usage boost; 6-decimal kg display |
| Masterbatch addition rate | `COR1MBPrice!B14` | input `0.01` | fraction |
| Masterbatch supplier price | `COR1MBPrice!B16` | input `14.83` | £/kg, 2 decimals |
| Masterbatch mass for quote | `COR1MBPrice!V26` | `COR1CompPrice!B28*B14` | kg, 6 decimals |
| Masterbatch mass per metre | `COR1MBPrice!V28` | `V26/B30` | kg/m, 9 decimals |
| Masterbatch mass per metre | `COR1MBPrice!B28` | `V26/B30*1000` | g/m, 6 decimals |
| Masterbatch price per metre | `COR1MBPrice!B32` | `V28*B16` | £/m, 2 decimals |

The domain trace makes these ordered operations explicit:

1. capture base compound mass before allowance;
2. capture the waste/start-up allowance rate;
3. form `1 + allowance rate`;
4. apply the allowance to the base usage;
5. apply the masterbatch addition rate;
6. divide quote mass by quote length;
7. convert kg/m to g/m;
8. multiply kg/m by £/kg.

No calculation-stage rounding occurs in this chain. Display values are separate
from raw values.

### Discovered parity case

The discovered `COR1MBPrice` values are recorded in:

```text
tests/ATAG.Costing.WorkbookParity.Tests/Fixtures/
  masterbatch-usage-cor1.json
```

Its status is `PendingBusinessApproval`. The corresponding golden parity test is
intentionally skipped until an ATAG reviewer approves the case. Domain tests use
independent values and already cover normal, zero, invalid, warning, trace, and
rounding-boundary behaviour.

## Dual-insulation V1.3a source map

The first pure two-layer construction rule is:

```text
dual-insulation-material-costing/v1
```

The mapped case uses `SBS1DualInsCopperPrice`,
`SBS1DualInsCompPrice1`, `SBS1DualInsMBPrice1`,
`SBS1DualInsCompPrice2`, `SBS1DualInsMBPrice2`, and
`SBS1DualInsSummary`.

### Confirmed production and allowance scopes

The 200 m additional start-up belongs to the core and first insulation process,
not the second layer. The rule therefore carries two explicit lengths:

| Scope | Formula | Mapped value |
|---|---|---:|
| Core and first layer | finished quote length + core start-up | 10,200 m |
| Second layer | finished quote length only | 10,000 m |

The user confirmed that the general 3% waste/start-up allowance still applies
once to every conductor, compound, and masterbatch stream in both layers. A
second `*1.03` in a downstream price is a workbook defect, not a separate
allowance. Each resulting material price enters its subtotal exactly once.

### Geometry and material evidence

| Meaning | Source | Formula/value |
|---|---|---|
| Core production length | `SBS1DualInsCopperPrice!B18` | `B14+B16` = 10,200 m |
| First finished OD | `SBS1DualInsCompPrice1!V14` | 2.2 mm |
| First positive tolerance | `SBS1DualInsCompPrice1!AF14` | 0.025 mm |
| First compound quote mass | `SBS1DualInsCompPrice1!B28` | `(B26*B32)*1.03` |
| Second minimum inner diameter | `SBS1DualInsCompPrice2!B18` | `(B14-L14)/2`, stored as radius |
| Second finished OD | `SBS1DualInsCompPrice2!V14` | 3.2 mm |
| Second positive tolerance | `SBS1DualInsCompPrice2!AF14` | 0.1 mm |
| Second annular area | `SBS1DualInsCompPrice2!B22` | outer circle minus inner circle |
| Second quote length | `SBS1DualInsCompPrice2!B32` | 10,000 m |
| Second compound quote mass | `SBS1DualInsCompPrice2!B30` | `(B28*B32)*1.03` |
| Second masterbatch addition rate | `SBS1DualInsMBPrice2!T14` | 0.01 |

The domain retains unrounded decimal values. The workbook's
`SBS1DualInsCopperPrice!B20` is an explicit four-decimal display/intermediate
rounding boundary and remains trace metadata rather than a hidden domain round.

### Recorded workbook defects

- `SBS1DualInsMBPrice1!B36` applies `1.03` to a masterbatch quantity already
  derived from allowance-adjusted first compound mass. Its zero price hides the
  numerical effect in this case.
- `SBS1DualInsMBPrice2!B40` applies `1.03` a second time.
- `SBS1DualInsSummary!X16` already contains combined masterbatch `X24`, while
  `X26` adds `X24` again after markup.
- `SBS1DualInsMBPrice2!V30` takes the addition rate from compound litres but
  labels the same value as masterbatch kilograms. The user confirmed this is a
  defect. The domain uses allowance-adjusted compound mass multiplied by the
  masterbatch addition-rate fraction for both layers.

Masterbatch is a material addition only. It has no separate line speed,
production time, set-up time, operator count, or labour charge. The first and
second extrusion processes instead own independent size-to-speed profiles and
labour inputs. The first process uses 10,200 m in this mapped case; the second
uses 10,000 m.

The evidence and corrected expected values are stored in:

```text
tests/ATAG.Costing.WorkbookParity.Tests/Fixtures/
  dual-insulation-sbs1.json
```

The formula corrections above have been confirmed, resolving OQ-006. The
fixture remains `PendingBusinessApproval` for approval of the complete mapped
case and is bound to the current observed workbook hash
`823FCE28815A9420E87A9FA119790243C8A4E9961B26B976A26EBE79BE9FA0ED`.
It does not replace the earlier immutable single-core baseline. Its evidence
test runs, while its golden comparison remains intentionally skipped.

## Reproducibility and integrity checks

Before accepting future workbook findings:

1. re-run the inspector against the relative workbook path;
2. compare the workbook hash with this document;
3. do not recalculate or save the reference workbook;
4. record ambiguous intent in `docs/OPEN-QUESTIONS.md`;
5. promote a parity fixture to `Approved` only after business review;
6. keep source cells in fixture/specification metadata, never in reusable domain
   formulas.
