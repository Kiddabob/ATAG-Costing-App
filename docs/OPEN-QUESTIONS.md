# Open questions

This file records workbook intent that cannot be safely inferred from formulas
alone. Resolved items should remain in a dated resolution section so later
migration work can see why a rule changed.

## Approval required

### OQ-001 — first golden parity case

Can the discovered `COR1MBPrice` case in
`tests/ATAG.Costing.WorkbookParity.Tests/Fixtures/masterbatch-usage-cor1.json`
be approved as the first golden workbook-parity case?

Approval means confirming:

- the source workbook hash;
- `COR1CompPrice!B26` and `B30` as the pre-allowance usage inputs;
- 3% as the intended waste/start-up allowance;
- `COR1MBPrice!B14` as a fraction, where `0.01` means 1%;
- `COR1MBPrice!B16` as £/kg;
- the cached outputs at `V26`, `V28`, `B28`, and `B32`;
- the recorded absolute tolerance of `0.000000000000001`.

Until approval, the parity test remains skipped and the fixture must not be
described as an accepted golden costing.

### OQ-003 — global display midpoint rule

Most workbook formulas retain raw values and rely on number formats for display.
The first domain rule makes display rounding explicit with
midpoint-away-from-zero, matching Excel-style business rounding.

Confirm whether this is the global display policy, or whether any calculation
families require a different midpoint rule. Explicit workbook `ROUND` formulas
remain separate named calculation boundaries.

### OQ-004 — Power Query authority and refresh

Which system or files are authoritative for the `Compounds`, `Contacts`,
`Copper`, `MasterbatchCodeList`, and `Operators` queries, and who is permitted to
refresh or approve those values?

Do not build a two-way data update workflow until this is decided.

### OQ-005 — first braid acceptance cases

Which braid examples should be approved for:

- forward optical-coverage calculation;
- reverse calculation from target coverage;
- boundary behaviour when geometry is infeasible?

The `Braid Calculator` formulas are mapped but are not part of the first
implemented slice.

## Resolved

### 2026-07-28 — meaning of 3%

The user confirmed that 3% is a general waste/start-up allowance that boosts
usage to cover expected material waste and process start-up. It is not risk,
markup, or margin.

The domain rule identifier is:

```text
usage-allowance/v1
```

### 2026-07-28 — OQ-002 repeated 3% in masterbatch quote price

The user clarified that the 3% is one general waste/start-up amount whose
purpose is to boost material usage. It is not an additional price multiplier.

The single-core V1 therefore applies the general 3% once to each material usage
stream. It does not migrate the extra `*1.03` in `COR1MBPrice!B34`, because the
upstream compound/masterbatch usage already includes the allowance and applying
it again to price would duplicate the confirmed general allowance.

### 2026-07-28 — repeated masterbatch in the core summary

The user explicitly confirmed that adding masterbatch a second time is wrong.
The workbook expression in `CORT1Summary!X12` applies markup to the material
subtotal and then adds the masterbatch quote value again. Single-core V1 does
not reproduce that defect: conductor, compound, and masterbatch are summed once,
risk is applied to that subtotal, and markup is applied to the risk-adjusted
subtotal.

### 2026-07-29 — dual-insulation length scopes and repeated additions

The user confirmed that the 200 m additional start-up length belongs only to
the conductor and first insulation layer. For the mapped reference case:

- conductor and first insulation cover 10,000 m finished length plus 200 m
  core start-up;
- only the 10,000 m finished core receives the second insulation layer;
- the general 3% allowance applies once to every material stream in both
  layers;
- any second `*1.03` price multiplier is accidental;
- any material or masterbatch price added a second time is accidental.

The dual domain therefore keeps the two production lengths separate, applies
the allowance once inside each stream, and includes each resulting price once
in the production-run subtotal.

### 2026-07-29 — OQ-006 second-layer masterbatch and extrusion time ownership

The user confirmed that the second-layer workbook path is wrong where
`SBS1DualInsMBPrice2!V30` takes a compound-litre value and presents it as
masterbatch kilograms. Both insulation layers use the shared mass-based rule:

```text
allowance-adjusted compound mass × masterbatch addition-rate fraction
```

The user also confirmed:

- masterbatch contributes only its calculated material amount and cost;
- masterbatch does not own production time, line speed, set-up time, operators,
  or labour;
- the first and second extrusion processes each own an independent
  size-to-speed profile because different production lines have different
  capabilities;
- the first extrusion uses the conductor/first-layer production length,
  including the internal start-up;
- the second extrusion uses finished quote length only.

The corrected formula decisions are approved. The complete dual fixture remains
`PendingBusinessApproval` until its workbook identity, complete source map, and
corrected expected case are approved together; it is no longer blocked by
OQ-006.
