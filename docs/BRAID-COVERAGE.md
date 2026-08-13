# Braid Coverage and Buncher Lay

## Scope

Version 0.4.0 migrates the first-direction engineering calculation from the
workbook's `Braid Coverage Calculator` sheet. It is an independent module over
domain-owned rules, ready for later reuse inside compatible costing types.

The interface accepts:

- target braid coverage;
- single-core OD;
- a core count from 1 to 45, including its workbook lay-up and combined-OD
  factor;
- 1 to 10 ends per carrier (the workbook originally listed 4 and 6);
- effective wire diameter of 0.1 or 0.2 mm;
- target cable length.

It compares 16- and 24-carrier results for total strands, base fill,
recommended pitch, longitudinal angle, workbook perpendicular angle, coverage
at the workbook's fixed 55 mm comparison pitch, and length per strand/bobbin.
Every result has a visible formula, substituted values, units, rounding rule,
and calculation-rule version.

## Buncher Lay selector

The selector retains the workbook's large- and small-buncher reference rows.
Choosing an available target lay length performs an exact table match and
shows the required buncher size and Gear A/Gear B pair. It does not interpolate
or silently choose a nearby lay.

## Preserved workbook behaviour

The perpendicular-angle result deliberately retains the existing workbook
formula, which uses the calculated longitudinal angle as its denominator. The
trace labels this as the workbook perpendicular angle so the behaviour is
visible and can be reviewed before any future rule correction.

## Deferred work

- reverse calculation from an existing braid construction;
- saving braid scenarios into costing project revisions;
- inserting this domain module into Dual, Flat, D-shape, or later constructions;
- business approval of any formula changes from the workbook reference.
