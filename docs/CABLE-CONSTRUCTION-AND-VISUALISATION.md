# Cable construction and live visualisation

This document records the target construction model and visual behaviour. It
separates implemented calculation/domain work from staged UI and future cable
families so later development does not have to recover the requirements from
chat or workbook screens.

## Start-of-costing construction chooser

The Home page starts with four descriptive construction tiles:

| Tile | Construction | Current state |
|---|---|---|
| COR | one conductor and one insulation layer | working single-core V1 |
| Dual insulated | one conductor and two extrusion layers | working guided costing and schema-v3 revision flow; shared renderer staged |
| Flat cable | up to ten cores in a line | planned |
| D-shape cable | up to ten cores in a line with a D-shaped finish | planned |

The chooser is construction-first. A generic **Start a costing** action must not
hide which physical cable model the user is about to create.

## Inside-to-outside dual build

The dual construction is ordered as:

1. copper conductor;
2. first insulation compound and its masterbatch;
3. zero or more optional add-on modules;
4. second insulation compound and its masterbatch.

The optional-module row contains:

- Tape;
- Chalk;
- Foil;
- Braid;
- Lapscreen;
- Drain wire.

Selected modules are inserted after the first insulation layer and before the
second layer. The displayed costing flow must use the same order as the saved
construction model. A module must not exist twice unless a later construction
model explicitly supports multiple separately identified instances.

Module meaning:

- **Tape** wraps the layer beneath it and has its own material/coverage inputs.
- **Chalk** is a material/process addition at the selected boundary.
- **Foil** records winding direction and overlap so its side-profile direction
  is visible.
- **Braid** uses opposing carrier directions around the layer it covers.
- **Lapscreen** uses the braid material family but only one carrier direction,
  rather than the other 8 or 12 carriers returning in the opposite direction.
- **Drain wire** adds another conductor to the construction and complete
  material calculation.

## Confirmed costing and production boundaries

- The conductor occurs once.
- For the mapped dual case, the conductor and first insulation process use the
  10,000 m customer quote plus 200 m internal core start-up.
- Only the 10,000 m finished length receives the second insulation.
- The 3% general waste/start-up allowance applies once to every conductor,
  compound, and masterbatch stream.
- Every calculated material cost enters the final material subtotal once.
- Both compounds enter the final cost: first-layer compound over the
  core/start-up production length and second-layer compound over finished
  length.
- Masterbatch is calculated from allowance-adjusted compound mass multiplied
  by its addition-rate fraction. It adds material amount and cost only.
- Masterbatch does not own line speed, production time, set-up time, operators,
  or labour.
- Each extrusion process owns an independent size-to-speed profile, set-up
  time, operator count, and labour rate because different lines have different
  capabilities.

The domain types `CableConstructionPlan`, `ExtrusionLineSpeedProfile`, and
`DualInsulationProductionCalculator` preserve these boundaries without placing
formulas in WinUI. `DualInsulationCostingApplicationService` now coordinates
material, both extrusion and commercial results for the guided editor.

## Opt-in live cable visualisation

The costing page will provide two linked views:

- a cable cross-section;
- a side profile.

Rendering is off by default. No geometry should be generated or refreshed until
the user turns the view on. Turning it off should release any expensive drawing
state while leaving the costing unchanged.

### Implemented COR preview

The working single-core page now provides an off-by-default responsive
inspection dock. On wide windows it stays outside the costing scroll area as a
right-hand rail whose divider can be dragged to resize it. On compact windows it
moves beneath the editor so it does not compress the costing form. Its
cross-section and side-profile drawing surfaces scale uniformly with the dock
width; the potentially long print-repeat cylinder keeps its own horizontal
scroll. It places the cross-section above the side profile and includes:

- a cross-section containing the selected insulation and conductor;
- a straight cylindrical side profile using the same
  conductor-to-finished-OD ratio, with parallel tube edges and the lower
  conductor extending beyond the insulation;
- narrow insulation and conductor end faces representing a view about 20
  degrees off side-on, rather than a pill-shaped cable silhouette;
- retained conductor OD and nominal finished core OD;
- a positive-tolerance boundary around the nominal cross-section;
- selected masterbatch colour;
- conductor colour derived from retained TCW/PCW/titanium/silver/stainless,
  bronze, or tinsel type.
- a simplified envelope mode and an opt-in detailed mode;
- every parsed strand in detailed cross-section mode;
- retained rope-lay grouping, so `7x19/0.32` is seven visible 19-strand groups
  rather than 133 unrelated points;
- a close-packed conductor side profile. Only strands or rope groups on the
  physical outside surface are visible along the side; each one is continuous
  from the insulation opening to its corresponding end-face position rather
  than being split into rear/front runs or depth-sliced fragments;
- fine longitudinal strokes within rope bundles that suggest their internal
  strands without drawing high-frequency noodle-like paths;
- a stable subtle shade per visible strand or rope-group that continues into
  its corresponding compressed end face, making its route visually traceable;
- a true hollow insulation annulus whose base colour and vertical
  highlight/shadow gradient form the rear cut face. The conductor is painted in
  front of that face in simplified and detailed modes, without a separate
  conductor-coloured plug;
- only the exposed outside rim on the closed insulation end, with no ellipse
  stroke crossing through the coloured cable body, and with the same vertical
  gradient as the tube body;
- a compressed conductor end face derived from the helix's final rotation, so
  it agrees with the visible strand endpoints instead of reverting to the
  unrotated cross-section coordinates;
- labelled conductor OD, finished OD, calculated radial wall, and a separately
  labelled published wall comparator;
- a saved core-print definition and side-profile rendering. The entire scaled
  print block is collapsed when print is disabled.

The shared renderer for Dual, Flat, and D-shape remains a future visual slice.

### Radial-wall reference boundary

`single-core-wall-guidance/v1` calculates the selected radial wall as:

```text
(finished OD - conductor OD) / 2
```

It then compares that geometry with published manufacturer data for comparable
H05/H07 flexible single-core products. The current sources are:

- Dee Cables H05V-K PVC data, which records a radial thickness of at least
  0.60 mm for the published 0.5, 0.75, and 1.0 mm² sizes:
  <https://www.deecables.co.uk/wp-content/uploads/2025/04/Dee-Cables-PVC-insulated-single-core-cable-H05V-K-2025.pdf>
- Clynder Cables 2491X H05V-K/H07V-K PVC dimensional data:
  <https://clyndercables.co.uk/wp-content/uploads/2022/01/2491X-Spec-Sheet.pdf>
- Eland Cables H05Z-K/H07Z-K LS0H/LSZH nominal insulation data:
  <https://www.elandcables.com/media/eland/media/assets/product-pdf/h05v-k-h07z-k-bs-en-50525-3-41-cable.pdf>

These values are not a universal minimum-wall rule. Direct nominal-size matches
and nearest out-of-range comparators are labelled differently. The comparison
does not change costing, assert certification, or replace selection of the
applicable voltage/product standard and customer specification.

### Core print

The COR working document now retains:

- print enabled/disabled;
- print detail/text;
- print colour;
- printed character height in millimetres;
- inter-print distance measured start-to-start;
- horizontal dot pitch in millimetres;
- vertical dot pitch in millimetres.

Print is previewed on a separate smaller cylinder below the construction side
profile. Character height is scaled against the selected finished OD. The
second copy begins at the configured start-to-start position on an axial scale;
the canvas grows and scrolls horizontally for long repeats. Axial and radial
scales are labelled separately because showing a 1,000 mm repeat at the same
pixel-per-millimetre scale as an 8 mm cable would make the cable unreadably
small. Repeats above 5,000 mm use a labelled reduced axial scale while
preserving proportional start positions. Exact repeat and dot-pitch values
remain labelled saved production inputs. No print cost or line-speed
assumption is introduced until its audited costing/process slice is defined.

### Shared geometry rules

- Geometry consumes the calculated construction result; it must not calculate
  cost, mass, yield, labour, or commercial pricing.
- Dimensions are represented in millimetres and share a declared scale. Zoom
  and pan are preferred to visually inflating a thin layer.
- Every visible layer uses the selected or retained material colour, including
  copper finish.
- The view provides a scale legend and clearly labels any missing authoritative
  dimension that prevents a fully scaled drawing.
- Selection of a stage in the costing flow highlights the same stage in both
  drawings.

### Conductor detail

The implemented COR detailed cross-section draws every strand from the selected
construction. Rope-like entries such as `7x19/0.32` retain their group structure
rather than being flattened to 133 unrelated strands. In the side profile,
each rope group is a single continuous, gently twisting bundle. Its centreline
uses the group's packed radial position, its shade is retained into the
compressed end face, and three fine longitudinal strokes hint at the 19 strands
within the group. The 7x19 reference reviewed on 29 July 2026 shows seven
coherent rods/bundles emerging from the insulation, not multiple
high-frequency sine waves. A hollow annular insulation end is painted after the
conductor so the opening masks the bundle starts cleanly; simplified mode uses
the same annulus and substitutes one solid conductor envelope. For
all parsed constructions, the side view draws only the physically exposed
outer strands or top-level rope bundles while the compressed end face and
cross-section retain every strand.

`conductor-construction/v2` and the shared preview layout now use a
close-packed triangular lattice. Complete shells form the familiar compact
hexagonal pattern; incomplete counts such as 16 are selected as a connected,
approximately hexagonal cluster instead of being forced onto circular
concentric rings with artificial gaps. Recursive source descriptions such as
`130 x 7 x 7`, `104 x 3 x 7 x 7`, and `7x19/0.32` retain every declared packing
level. Where a numeric hierarchy omits the strand diameter, the preview may
infer it from a positive retained nominal area, labels that inference, and does
not overwrite the retained source row. Supplier-defined text that contains no
reliable strand count remains a labelled simplified envelope rather than
inventing strand geometry.

The same pure layout builder is used for cached and LIVE imported Copper rows.
Its audit covers every numeric row in the built-in Copper snapshot: all parsed
strands must be present, touching a neighbour, non-overlapping, and contained
inside the selected conductor envelope. Rope sub-bundles keep a small rendering
clearance but no longer leave the large circular-ring voids of the earlier
preview. Strand, group, and end-face outline widths scale down with the actual
rendered strand size so high-count conductors remain dense instead of turning
into oversized outlined noodles.

The reviewed external BAC specification is internally inconsistent in one
annotation. Its title identifies the complete conductor as **#7 AWG**, and its
construction table specifies **7 bundles of 19 strands, #28 AWG**, with a
0.321 mm individual-strand diameter and 5.00 mm bunch diameter. Those values
agree with the application's retained `133/0.32` construction: 133 circular
0.32 mm strands calculate to approximately 10.696 mm², whose nearest overall
gauge is AWG 7. The separate **24 AWG** arrow callout does not agree with the
strand diameter or total area and must be treated as a source-document conflict,
not imported into central data without business confirmation.

This geometry is intended to support strand, bunch and cable lay-length work in
a later slice. Presentation-only normalisation such as displaying `7/0.196` as
`7/0.20` must not silently change the retained source dimension.

### Add-on appearance

- Braid is drawn around the layer it covers, with opposing carriers evident in
  the side profile.
- Lapscreen shows only its one winding direction.
- Foil direction and overlap are visible from the selected orientation.
- Tape follows its selected wrap direction and overlap.
- Drain wire is drawn as its own conductor. On a simple dual-insulated cable,
  the enclosing construction is centred around the combined core-and-drain
  envelope rather than remaining centred on the primary core alone.

## Future Flat and D-shape constructions

Flat and D-shape documents allow one to ten in-line cores. They reuse the same
typed conductor, insulation, add-on-module, trace, production, and saved-revision
boundaries. Their geometry and costing rules must be implemented and tested
before the Home tiles stop being labelled planned.

## Persistence and acceptance

Schema version 3 now stores:

- a construction discriminator;
- locked dual material/quote/dimension inputs and exact approved results;
- both production scopes explicitly;
- ordered optional-module selections;
- independent extrusion production profiles;
- visualisation preference, conductor detail mode, zoom, and view state as
  presentation settings rather than costing inputs remain part of the later
  shared-renderer slice.

Domain ordering/duplicate tests, schema-v3 round-trip and legacy-reader tests,
immutable dual-save tests, Application orchestration tests, and focused
reference-search/module-order state tests are implemented. Golden workbook
approval remains gated. Visual acceptance using asymmetric layers, rope-lay
conductor, foil direction, braid, lapscreen, and an off-centre drain
construction remains part of the shared-renderer slice.
