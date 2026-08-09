import fs from "node:fs";
import path from "node:path";

if (process.argv.length !== 4) {
  throw new Error(
    "Usage: node tools/generate-central-data-snapshot.mjs <artifact-table-json> <output-json>",
  );
}

const [, , inputPath, outputPath] = process.argv;
const artifact = JSON.parse(fs.readFileSync(inputPath, "utf8"));

function table(name) {
  const source = artifact.tables.find((candidate) => candidate.name === name);
  if (!source) {
    throw new Error(`Workbook table ${name} was not found.`);
  }

  return source.rows.map((row) =>
    Object.fromEntries(source.headers.map((header, index) => [header, row[index]])),
  );
}

function text(value) {
  return value === null || value === undefined ? "" : String(value).trim();
}

function number(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function positive(...values) {
  return values.map(number).find((value) => value > 0) ?? 0;
}

function flag(value) {
  return value === true || number(value) > 0;
}

function compatibility(row) {
  const fields = [
    ["PVC Use", "PVC"],
    ["PE/PP/PUR Use", "PE/PP/PUR"],
    ["PS Use", "PS"],
    ["ABS Use", "ABS"],
    ["ACETAL Use", "ACETAL"],
    ["PBT Use", "PBT"],
    ["Nylon Use", "Nylon"],
    ["PC/PES Use", "PC/PES"],
  ];
  const values = fields
    .filter(([field]) => flag(row[field]))
    .map(([, label]) => label);
  return values.length ? values.join(", ") : "Compatibility not recorded";
}

function temperatureLimits(row) {
  const fields = [
    ["PVC Use", "PVC Max Temp", "PVC"],
    ["PE/PP/PUR Use", "PE/PP/PUR Max Temp", "PE/PP/PUR"],
    ["PS Use", "PS Max Temp", "PS"],
    ["ABS Use", "ABS Max Temp", "ABS"],
    ["ACETAL Use", "ACETAL Max Temp", "ACETAL"],
    ["PBT Use", "PBT Max Temp", "PBT"],
    ["Nylon Use", "Nylon Max Temp", "Nylon"],
    ["PC/PES Use", "PC/PES Max Temp", "PC/PES"],
  ];
  return fields
    .filter(([use]) => flag(row[use]))
    .map(([, limit, label]) => {
      const value = text(row[limit]);
      return value ? `${label} ${value} °C` : `${label} limit not recorded`;
    })
    .join(" · ");
}

const copper = table("Copper")
  .filter((row) => text(row.Description))
  .map((row) => ({
    Id: text(row.ID) || text(row.Description),
    Description: text(row.Description),
    Supplier: text(row.Company) || "Unknown supplier",
    PricePerKilogram: positive(
      row["Total Cost 2 (£/kg)"],
      row["Total Cost"],
      row["Copper Cost (£/kg)"],
      row["Manufature Cost"],
    ),
    YieldMetresPerKilogram: positive(
      row["Yield (m/kg) Manual"],
      row["Yield (m/kg)"],
    ),
    NominalOutsideDiameterMillimetres: positive(row["Nom OD (mm)"]),
    NominalAreaSquareMillimetres: positive(row["mm²"]),
    WorkbookAwg: text(row.AWG) || null,
  }));

const compounds = table("Compounds")
  .filter((row) => text(row.Compound))
  .map((row) => ({
    Id: text(row.ID) || text(row.Compound),
    CompoundName: text(row.Compound),
    Supplier: text(row.Company) || "Unknown supplier",
    PricePerKilogram: positive(row["Cost (£/kg)"]),
    SpecificGravity: positive(row["Specific Gravity"]),
    MaterialType: text(row.Type),
    Description: text(row["Material Description"]),
    HasDataSheet: flag(row["Data Sheet"]),
  }));

const masterbatches = table("MasterbatchCodeList")
  .filter((row) => text(row["Colour Code"]) || text(row.Colour))
  .map((row) => ({
    ColourCode: text(row["Colour Code"]),
    ColourName: text(row.Colour),
    Supplier: text(row["Colour Supplier"]) || "Unknown supplier",
    PricePerKilogram: positive(row["£/kg"]),
    Compatibility: compatibility(row),
    ColourHex: text(row["Colour Hex"]) || null,
    ColourType: text(row["Colour Type"]),
    RalEquivalent: text(row["RAL Number Equivalent"]) || null,
    TemperatureLimits: temperatureLimits(row),
  }));

const contacts = table("Contacts")
  .filter((row) => text(row["Account Name"]))
  .map((row) => ({
    Id: text(row.UniqueCusRef) || text(row["Account Name"]),
    AccountName: text(row["Account Name"]),
    ShortName: text(row["Short Name"]),
    AddressLine1: text(row["Address Line 1"]),
    AddressLine2: text(row["Address Line 2"]),
    AddressLine3: text(row["Address Line 3"]),
    AddressLine4: text(row["Address Line 4"]),
    PostCode: text(row["Post/Zip Code"]),
    PhoneNumber: text(row["Phone Number"]),
    PersonalEmail: text(row.PersonalEmail),
    SalesEmail: text(row.SalesEmail),
    AccountsEmail: text(row.AccountsEmail),
    IsAssemblyCustomer: flag(row.AccTypeAssemblyCust),
    IsCableCustomer: flag(row.AccTypeCableCust),
    IsCompoundSupplier: flag(row.AccTypeCompSupp),
    IsConductorSupplier: flag(row.AccTypeCondSupp),
    IsPartSupplier: flag(row.AccTypePartSupp),
    IsOtherSupplier: flag(row.AccTypeOtherSupp),
    IsOtherCustomer: flag(row.AccTypeOtherCust),
  }));

const operators = table("Operators")
  .filter((row) => text(row["First Name"]) || text(row["Last Name"]))
  .map((row) => ({
    Id: text(row.ID),
    LastName: text(row["Last Name"]),
    MiddleNames: text(row["Middle Name(s)"]),
    FirstName: text(row["First Name"]),
    Initials: text(row.Initials),
    Assembly: flag(row.Assembly),
    Production: flag(row.Production),
    Office: flag(row.Office),
    Other: flag(row.Other),
    QualityControl: flag(row["Quality Control"]),
    Grn: flag(row.GRN),
    Employee: flag(row.Employee),
  }));

const snapshot = {
  SchemaVersion: 2,
  Revision: "reference-workbook-full-tables-2026-07-28",
  CapturedAt: "2026-07-28T13:17:46+00:00",
  SourceLabel: "Built-in full workbook table snapshot",
  Copper: copper,
  Compounds: compounds,
  Masterbatches: masterbatches,
  Contacts: contacts,
  Operators: operators,
};

const expected = {
  Copper: 322,
  Compounds: 74,
  Masterbatches: 203,
  Contacts: 567,
  Operators: 5,
};
for (const [name, count] of Object.entries(expected)) {
  if (snapshot[name].length !== count) {
    throw new Error(
      `${name} expected ${count} records but generated ${snapshot[name].length}.`,
    );
  }
}

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, `${JSON.stringify(snapshot, null, 2)}\n`, "utf8");
console.log(
  Object.entries(expected)
    .map(([name, count]) => `${name}=${count}`)
    .join(" "),
);
