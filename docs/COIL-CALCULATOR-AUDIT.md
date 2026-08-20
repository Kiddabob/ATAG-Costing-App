# Coil calculator workbook audit

## Status

The `Coils` sheet in the adjacent `Coil Calc.xlsm` workbook is **not a parity
source**. Its cached example values agree with its stored Excel formulae, but
the central cable-length formula conflicts with the sheet's own bar-diameter
formula and materially overstates the sample cable length. The user approved a
corrected physical rule on 20 August 2026; that decision is implemented as
`coil-cable-length/v1` rather than preserving the defective workbook formula.

Audited source identity on 20 August 2026:

- file: `../Coil Calc.xlsm`;
- size: 336,018 bytes;
- modified: 17 August 2026 14:53:18 local time;
- SHA-256: `AF958D7C30751665251EC5C24E47EF5BF0B6900356550A773CE8963FCB668FEB`;
- sheet/range: `Coils!A1:U45` (the calculator itself is principally
  `Coils!A1:E21`, with a separate costing scratch area in `G5:N12`).

The workbook was inspected without editing or saving it. It contains no VBA
project and no external-link parts, so the stored worksheet formulae are the
complete calculation evidence for this sheet.

## Approved corrected rule — 20 August 2026

The production interpretation is now explicit:

- provide a Round / Flat / D-shape selector;
- Round cable uses diameter both radially and as the no-gap axial pitch;
- Flat and D-shaped cable use cable height radially and cable width as the
  no-gap axial pitch;
- required bar diameter is finished outside diameter minus two radial cable
  heights;
- calculate the cable centreline as a single-layer helix;
- round up to complete 360° turns so the two tails leave parallel;
- show the actual wound axial length and any overrun created by complete-turn
  rounding;
- tails are separate finished lengths, and non-zero strip lengths are added
  separately;
- exclude every price, markup, margin, plug and coiling-labour value.

The workbook's +5/-5 field still has no approved dimensional meaning and is
therefore not presented as a functioning tolerance input.

Implemented boundaries:

- Domain: `src/ATAG.Costing.Domain/Coiling/CoilCableLength.cs`;
- Domain tests:
  `tests/ATAG.Costing.Domain.Tests/Coiling/CoilCableLengthCalculatorTests.cs`;
- WinUI view model and page: `CoilCalculatorViewModel` and
  `CoilCalculatorView`;
- shared-shell preview mode: the complete preview rail is removed because no
  coil visual is approved and the performance-first renderer pass is pending.

Coiling time, machine selection, labour and price remain future fully dynamic
costing modules after the easy-build preset costing pages. They must consume the
approved physical result instead of reproducing the formula.

With the workbook sample interpreted in the approved Flat orientation
(2.5 mm radial height, 4.8 mm axial width, 13 mm finished OD, 90 mm requested
axial length, two 50 mm tails, and 1,100 coils), the corrected rule produces an
8 mm bar, 19 complete turns, 91.2 mm actual wound width, 733.348 mm cable per
coil, and 806.683 m total cable.

## Stored workbook calculation

The visible sample inputs are:

| Meaning | Cell | Value |
|---|---:|---:|
| Tail 1 | `B4` | 50 mm |
| Tail 2 | `B5` | 50 mm |
| Cable height | `E4` | 4.8 mm |
| Cable width | `E5` | 2.5 mm |
| Required coil outside height/diameter | `E7` | 13 mm |
| Required coil axial length | `E10` | 90 mm |
| Quantity | `D13` | 1,100 coils |

The key stored formulae are:

```text
A15 = ((E7-(E5*2)/2)*3.14)*(E10/E5)+B4+B5
A17 = A15/1000
A19 = E7-(E4*2)
A21 = D13*A17
```

They produce the cached outputs 1,286.92 mm per coil, 1.28692 m per coil,
3.4 mm bar diameter, and 1,415.612 m total.

## Material correctness issue

`A19` treats cable **height** (`E4`) as the radial dimension: the bar diameter
is finished outside diameter minus two cable heights. That makes the cable
centreline diameter for a single layer:

```text
bar diameter + cable height = E7 - E4 = 8.2 mm
```

`A15` instead uses `E7-E5 = 10.5 mm`, subtracting the cable **width** from the
finished outside diameter. The two formulae therefore cannot describe the same
physical orientation.

For the visible sample, using the internally consistent 8.2 mm centreline
diameter gives:

| Model | Per-coil cable including 100 mm tails |
|---|---:|
| Workbook `A15` | 1,286.920 mm |
| Circular turns using `PI()` | 1,027.398 mm |
| Single-layer helical turns using 2.5 mm axial pitch | 1,031.755 mm |

The workbook result is about **25.26% higher** than the circular-centreline
result. This is too large to treat as a harmless rounding difference. The
helical model is about 0.42% higher than the circular model for this example.

## Other issues exposed by the audit

- `B7:B8` (Strip length 1/2) are not referenced by any workbook formula. The
  approved app rule instead treats them as optional additions outside the two
  tail lengths.
- `C10` and `C12` (the displayed +5/-5 tolerance) are not referenced by any
  formula. Confirm which dimension the tolerance belongs to and whether it is
  merely a manufacturing instruction or should produce minimum/maximum
  results.
- The sheet uses fractional turns (`E10/E5`). The approved app rule rounds up
  to a whole turn and exposes the resulting axial overrun.
- The sheet does not reject zero/negative cable width, a required outside
  diameter no larger than two cable heights, negative tails, fractional or
  negative quantities, or other physically impossible inputs.
- `A15` hard-codes `3.14` rather than using `PI()`. This is a small independent
  accuracy loss and should not be copied into a new rule.
- The costing scratch area labels `I11` as **Mark-up**, but `I12=I10/(1-I11)`
  is target-gross-margin pricing. At 30% it returns £15,172.99; a true 30%
  markup would return £13,807.42. Its helper formula also makes a 0% entry divide
  by zero. Plug and coil-labour inputs have no declared per-unit/total units and
  are not multiplied by coil quantity.

## Implemented approval boundary

Only the physical coil-planning calculation is implemented. The ambiguous
commercial scratch area remains excluded. The shared-shell workflow presents
**Cable shape and orientation**, **Finished coil**, **Ends and quantity**, and
**Production coil plan**. Its dominant outputs are **bar diameter**, **complete
turns**, **cable per coil**, and **total cable**, followed by the full trace.
Any future diagram remains visual-only and consumes this approved result.
