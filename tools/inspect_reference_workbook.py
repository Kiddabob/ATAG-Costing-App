from __future__ import annotations

import argparse
import collections
import datetime as dt
import hashlib
import json
import math
import re
import struct
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Any, Iterable
from xml.etree import ElementTree as ET


MAIN_NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG_REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"

NS = {"x": MAIN_NS, "r": REL_NS, "pr": PKG_REL_NS}

CELL_REFERENCE = re.compile(
    r"(?<![A-Za-z0-9_])"
    r"(?:(?:'(?:[^']|'')+'|[A-Za-z_][A-Za-z0-9_.]*)!)?"
    r"\$?[A-Z]{1,3}\$?[0-9]+"
    r"(?::"
    r"(?:(?:'(?:[^']|'')+'|[A-Za-z_][A-Za-z0-9_.]*)!)?"
    r"\$?[A-Z]{1,3}\$?[0-9]+)?"
)
NUMBER_LITERAL = re.compile(r"(?<![A-Za-z0-9_.])(?:\d+(?:\.\d+)?|\.\d+)(?![A-Za-z0-9_.])")
PROCEDURE = re.compile(
    r"^\s*(?:(Public|Private|Friend|Static)\s+)?"
    r"(Sub|Function|Property\s+(?:Get|Let|Set))\s+"
    r"([A-Za-z_][A-Za-z0-9_]*)",
    re.IGNORECASE | re.MULTILINE,
)
MODULE_ATTRIBUTE = re.compile(
    r'^\s*Attribute\s+VB_Name\s*=\s*"([^"]+)"',
    re.IGNORECASE | re.MULTILINE,
)


BUILT_IN_NUMBER_FORMATS = {
    0: "General",
    1: "0",
    2: "0.00",
    3: "#,##0",
    4: "#,##0.00",
    9: "0%",
    10: "0.00%",
    11: "0.00E+00",
    12: "# ?/?",
    13: "# ??/??",
    14: "mm-dd-yy",
    15: "d-mmm-yy",
    16: "d-mmm",
    17: "mmm-yy",
    18: "h:mm AM/PM",
    19: "h:mm:ss AM/PM",
    20: "h:mm",
    21: "h:mm:ss",
    22: "m/d/yy h:mm",
    37: "#,##0 ;(#,##0)",
    38: "#,##0 ;[Red](#,##0)",
    39: "#,##0.00;(#,##0.00)",
    40: "#,##0.00;[Red](#,##0.00)",
    45: "mm:ss",
    46: "[h]:mm:ss",
    47: "mmss.0",
    48: "##0.0E+0",
    49: "@",
}


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def xml_root(archive: zipfile.ZipFile, name: str) -> ET.Element | None:
    try:
        return ET.fromstring(archive.read(name))
    except KeyError:
        return None


def relationship_map(archive: zipfile.ZipFile, name: str) -> dict[str, dict[str, str]]:
    root = xml_root(archive, name)
    if root is None:
        return {}
    relationships: dict[str, dict[str, str]] = {}
    for relation in root:
        relation_id = relation.attrib.get("Id")
        if relation_id:
            relationships[relation_id] = dict(relation.attrib)
    return relationships


def resolve_relationship(source_part: str, target: str) -> str:
    if target.startswith("/"):
        return target.lstrip("/")
    source_directory = PurePosixPath(source_part).parent
    resolved_parts: list[str] = []
    for part in (source_directory / target).parts:
        if part == "..":
            if resolved_parts:
                resolved_parts.pop()
        elif part != ".":
            resolved_parts.append(part)
    return "/".join(resolved_parts)


def read_shared_strings(archive: zipfile.ZipFile) -> list[str]:
    root = xml_root(archive, "xl/sharedStrings.xml")
    if root is None:
        return []
    strings: list[str] = []
    for item in root.findall("x:si", NS):
        strings.append("".join(node.text or "" for node in item.iter() if local_name(node.tag) == "t"))
    return strings


def read_number_formats(archive: zipfile.ZipFile) -> tuple[dict[int, str], list[int]]:
    formats = dict(BUILT_IN_NUMBER_FORMATS)
    root = xml_root(archive, "xl/styles.xml")
    if root is None:
        return formats, []
    for item in root.findall("x:numFmts/x:numFmt", NS):
        try:
            formats[int(item.attrib["numFmtId"])] = item.attrib.get("formatCode", "")
        except (KeyError, ValueError):
            continue
    cell_format_ids: list[int] = []
    for cell_format in root.findall("x:cellXfs/x:xf", NS):
        try:
            cell_format_ids.append(int(cell_format.attrib.get("numFmtId", "0")))
        except ValueError:
            cell_format_ids.append(0)
    return formats, cell_format_ids


def read_cell_value(cell: ET.Element, shared_strings: list[str]) -> Any:
    value = cell.find("x:v", NS)
    cell_type = cell.attrib.get("t")
    if cell_type == "inlineStr":
        inline = cell.find("x:is", NS)
        if inline is None:
            return ""
        return "".join(node.text or "" for node in inline.iter() if local_name(node.tag) == "t")
    if value is None or value.text is None:
        return None
    text = value.text
    if cell_type == "s":
        try:
            return shared_strings[int(text)]
        except (ValueError, IndexError):
            return text
    if cell_type in {"str", "e"}:
        return text
    if cell_type == "b":
        return text == "1"
    try:
        return int(text)
    except ValueError:
        try:
            return float(text)
        except ValueError:
            return text


def canonical_formula(formula: str) -> str:
    canonical = formula.upper().replace(" ", "")
    canonical = CELL_REFERENCE.sub("{CELL}", canonical)
    canonical = NUMBER_LITERAL.sub("{N}", canonical)
    return canonical


def formula_family_name(formula: str) -> str:
    functions = re.findall(r"\b([A-Z][A-Z0-9_.]*)\s*\(", formula.upper())
    if functions:
        return " > ".join(dict.fromkeys(functions))
    operators = [operator for operator in ("+", "-", "*", "/", "^") if operator in formula]
    return "Arithmetic " + "".join(operators) if operators else "Reference"


def sheet_relationships_path(sheet_part: str) -> str:
    path = PurePosixPath(sheet_part)
    return str(path.parent / "_rels" / f"{path.name}.rels")


def workbook_connections(archive: zipfile.ZipFile) -> list[dict[str, Any]]:
    root = xml_root(archive, "xl/connections.xml")
    if root is None:
        return []
    results: list[dict[str, Any]] = []
    for connection in root:
        record: dict[str, Any] = {
            "id": connection.attrib.get("id"),
            "name": connection.attrib.get("name"),
            "type": connection.attrib.get("type"),
            "description": connection.attrib.get("description"),
            "refreshedVersion": connection.attrib.get("refreshedVersion"),
            "saveData": connection.attrib.get("saveData"),
        }
        providers: list[dict[str, Any]] = []
        for child in connection:
            providers.append({"kind": local_name(child.tag), **dict(child.attrib)})
        record["providers"] = providers
        results.append(record)
    return results


def table_details(
    archive: zipfile.ZipFile,
    sheet_part: str,
    sheet_root: ET.Element,
) -> list[dict[str, Any]]:
    relationships = relationship_map(archive, sheet_relationships_path(sheet_part))
    tables: list[dict[str, Any]] = []
    for table_part in sheet_root.findall("x:tableParts/x:tablePart", NS):
        relation_id = table_part.attrib.get(f"{{{REL_NS}}}id")
        relation = relationships.get(relation_id or "")
        if not relation:
            continue
        table_path = resolve_relationship(sheet_part, relation["Target"])
        root = xml_root(archive, table_path)
        if root is None:
            continue
        query_table = None
        table_relationships = relationship_map(
            archive,
            sheet_relationships_path(table_path),
        )
        for table_relation in table_relationships.values():
            if not table_relation.get("Type", "").endswith("/queryTable"):
                continue
            query_path = resolve_relationship(table_path, table_relation["Target"])
            query_root = xml_root(archive, query_path)
            if query_root is not None:
                query_table = {
                    "name": query_root.attrib.get("name"),
                    "connectionId": query_root.attrib.get("connectionId"),
                    "autoFormatId": query_root.attrib.get("autoFormatId"),
                    "refreshOnLoad": query_root.attrib.get("refreshOnLoad"),
                    "part": query_path,
                }
            break
        columns = [
            column.attrib.get("name", "")
            for column in root.findall("x:tableColumns/x:tableColumn", NS)
        ]
        tables.append(
            {
                "name": root.attrib.get("name"),
                "displayName": root.attrib.get("displayName"),
                "reference": root.attrib.get("ref"),
                "totalsRowShown": root.attrib.get("totalsRowShown"),
                "columns": columns,
                "queryTable": query_table,
            }
        )
    return tables


def query_table_details(
    archive: zipfile.ZipFile,
    sheet_part: str,
) -> list[dict[str, Any]]:
    relationships = relationship_map(archive, sheet_relationships_path(sheet_part))
    results: list[dict[str, Any]] = []
    for relation in relationships.values():
        if not relation.get("Type", "").endswith("/queryTable"):
            continue
        query_path = resolve_relationship(sheet_part, relation["Target"])
        root = xml_root(archive, query_path)
        if root is not None:
            results.append(
                {
                    "name": root.attrib.get("name"),
                    "connectionId": root.attrib.get("connectionId"),
                    "autoFormatId": root.attrib.get("autoFormatId"),
                    "refreshOnLoad": root.attrib.get("refreshOnLoad"),
                }
            )
    return results


@dataclass
class DirectoryEntry:
    name: str
    object_type: int
    start_sector: int
    size: int


class CompoundFile:
    END_OF_CHAIN = 0xFFFFFFFE
    FREE_SECTOR = 0xFFFFFFFF

    def __init__(self, data: bytes):
        if data[:8] != bytes.fromhex("D0CF11E0A1B11AE1"):
            raise ValueError("Not an OLE compound file")
        self.data = data
        self.sector_size = 1 << struct.unpack_from("<H", data, 30)[0]
        self.mini_sector_size = 1 << struct.unpack_from("<H", data, 32)[0]
        self.fat_sector_count = struct.unpack_from("<I", data, 44)[0]
        self.first_directory_sector = struct.unpack_from("<I", data, 48)[0]
        self.mini_stream_cutoff = struct.unpack_from("<I", data, 56)[0]
        self.first_minifat_sector = struct.unpack_from("<I", data, 60)[0]
        self.minifat_sector_count = struct.unpack_from("<I", data, 64)[0]
        first_difat_sector = struct.unpack_from("<I", data, 68)[0]
        difat_sector_count = struct.unpack_from("<I", data, 72)[0]

        difat = [
            sector
            for sector in struct.unpack_from("<109I", data, 76)
            if sector != self.FREE_SECTOR
        ]
        current = first_difat_sector
        for _ in range(difat_sector_count):
            if current in {self.END_OF_CHAIN, self.FREE_SECTOR}:
                break
            sector_data = self._sector(current)
            values = struct.unpack(f"<{self.sector_size // 4}I", sector_data)
            difat.extend(value for value in values[:-1] if value != self.FREE_SECTOR)
            current = values[-1]

        fat_values: list[int] = []
        for sector in difat[: self.fat_sector_count]:
            fat_values.extend(struct.unpack(f"<{self.sector_size // 4}I", self._sector(sector)))
        self.fat = fat_values

        directory_bytes = self._read_regular_chain(self.first_directory_sector)
        self.entries: list[DirectoryEntry] = []
        for offset in range(0, len(directory_bytes), 128):
            entry = directory_bytes[offset : offset + 128]
            if len(entry) < 128:
                continue
            name_length = struct.unpack_from("<H", entry, 64)[0]
            if name_length < 2:
                name = ""
            else:
                name = entry[: name_length - 2].decode("utf-16le", errors="replace")
            self.entries.append(
                DirectoryEntry(
                    name=name,
                    object_type=entry[66],
                    start_sector=struct.unpack_from("<I", entry, 116)[0],
                    size=struct.unpack_from("<Q", entry, 120)[0],
                )
            )

        root_entries = [entry for entry in self.entries if entry.object_type == 5]
        self.root_entry = root_entries[0] if root_entries else None
        if (
            self.first_minifat_sector not in {self.END_OF_CHAIN, self.FREE_SECTOR}
            and self.minifat_sector_count
        ):
            minifat_bytes = self._read_regular_chain(self.first_minifat_sector)
            count = len(minifat_bytes) // 4
            self.minifat = list(struct.unpack(f"<{count}I", minifat_bytes[: count * 4]))
        else:
            self.minifat = []
        self.mini_stream = (
            self._read_regular_chain(self.root_entry.start_sector)[: self.root_entry.size]
            if self.root_entry is not None and self.root_entry.size
            else b""
        )

    def _sector(self, sector_id: int) -> bytes:
        start = (sector_id + 1) * self.sector_size
        return self.data[start : start + self.sector_size]

    @staticmethod
    def _chain(start: int, fat: list[int]) -> Iterable[int]:
        current = start
        seen: set[int] = set()
        while current not in {CompoundFile.END_OF_CHAIN, CompoundFile.FREE_SECTOR}:
            if current in seen or current >= len(fat):
                break
            seen.add(current)
            yield current
            current = fat[current]

    def _read_regular_chain(self, start: int) -> bytes:
        return b"".join(self._sector(sector) for sector in self._chain(start, self.fat))

    def _read_mini_chain(self, start: int) -> bytes:
        blocks: list[bytes] = []
        for sector in self._chain(start, self.minifat):
            offset = sector * self.mini_sector_size
            blocks.append(self.mini_stream[offset : offset + self.mini_sector_size])
        return b"".join(blocks)

    def streams(self) -> dict[str, bytes]:
        streams: dict[str, bytes] = {}
        for entry in self.entries:
            if entry.object_type != 2 or not entry.name:
                continue
            if entry.size < self.mini_stream_cutoff:
                content = self._read_mini_chain(entry.start_sector)
            else:
                content = self._read_regular_chain(entry.start_sector)
            streams[entry.name] = content[: entry.size]
        return streams


def decompress_vba_container(data: bytes) -> bytes:
    if not data or data[0] != 0x01:
        raise ValueError("Missing compressed-container signature")
    cursor = 1
    output = bytearray()
    while cursor + 2 <= len(data):
        chunk_start = cursor
        header = struct.unpack_from("<H", data, cursor)[0]
        cursor += 2
        chunk_size = (header & 0x0FFF) + 3
        signature = (header >> 12) & 0x07
        compressed = (header >> 15) & 0x01
        if signature != 0x03 or chunk_size < 3:
            raise ValueError("Invalid compressed-chunk header")
        chunk_end = min(chunk_start + chunk_size, len(data))
        decompressed_chunk_start = len(output)
        if not compressed:
            output.extend(data[cursor:chunk_end])
            cursor = chunk_end
            continue
        while cursor < chunk_end and len(output) - decompressed_chunk_start < 4096:
            flag_byte = data[cursor]
            cursor += 1
            for bit in range(8):
                if cursor >= chunk_end:
                    break
                if flag_byte & (1 << bit):
                    if cursor + 2 > chunk_end:
                        raise ValueError("Truncated copy token")
                    token = struct.unpack_from("<H", data, cursor)[0]
                    cursor += 2
                    current_position = len(output) - decompressed_chunk_start
                    offset_bits = max(4, math.ceil(math.log2(max(current_position, 1))))
                    length_bits = 16 - offset_bits
                    length_mask = (1 << length_bits) - 1
                    length = (token & length_mask) + 3
                    offset = (token >> length_bits) + 1
                    if offset > current_position:
                        raise ValueError("Invalid copy-token offset")
                    for _ in range(length):
                        output.append(output[-offset])
                else:
                    output.append(data[cursor])
                    cursor += 1
                if len(output) - decompressed_chunk_start >= 4096:
                    break
        cursor = chunk_end
    return bytes(output)


def extract_vba_sources(project_data: bytes) -> dict[str, Any]:
    compound = CompoundFile(project_data)
    streams = compound.streams()
    project_text = streams.get("PROJECT", b"").decode("cp1252", errors="replace")
    declared_types: dict[str, str] = {}
    for line in project_text.splitlines():
        if "=" not in line:
            continue
        key, value = line.split("=", 1)
        module_name = value.split("/", 1)[0].strip()
        if key in {"Module", "Class", "Document", "BaseClass"}:
            declared_types[module_name] = key

    modules: list[dict[str, Any]] = []
    ignored = {
        "PROJECT",
        "PROJECTwm",
        "_VBA_PROJECT",
        "dir",
        "\u0001CompObj",
        "\u0005SummaryInformation",
        "\u0005DocumentSummaryInformation",
    }
    for stream_name, content in streams.items():
        if stream_name in ignored:
            continue
        best_source: str | None = None
        best_offset: int | None = None
        for offset, byte in enumerate(content):
            if byte != 0x01:
                continue
            try:
                decompressed = decompress_vba_container(content[offset:])
            except (ValueError, struct.error, IndexError):
                continue
            text = decompressed.decode("cp1252", errors="replace")
            if "Attribute VB_" not in text:
                continue
            if best_source is None or len(text) > len(best_source):
                best_source = text
                best_offset = offset
        if best_source is None:
            continue
        name_match = MODULE_ATTRIBUTE.search(best_source)
        module_name = name_match.group(1) if name_match else stream_name
        procedures: list[dict[str, str]] = []
        for match in PROCEDURE.finditer(best_source):
            access = match.group(1) or "Public"
            procedure_kind = re.sub(r"\s+", " ", match.group(2).title())
            procedures.append(
                {
                    "access": access.title(),
                    "kind": procedure_kind,
                    "name": match.group(3),
                }
            )
        modules.append(
            {
                "name": module_name,
                "streamName": stream_name,
                "declaredType": declared_types.get(module_name, "Unknown"),
                "sourceOffset": best_offset,
                "lineCount": len(best_source.splitlines()),
                "procedures": procedures,
                "publicEntryPoints": [
                    procedure["name"]
                    for procedure in procedures
                    if procedure["access"] != "Private"
                ],
            }
        )
    modules.sort(key=lambda module: module["name"].lower())
    return {
        "projectName": next(
            (
                line.split("=", 1)[1].strip().strip('"')
                for line in project_text.splitlines()
                if line.startswith("Name=")
            ),
            None,
        ),
        "streamCount": len(streams),
        "declaredModuleCount": len(declared_types),
        "modules": modules,
    }


def inspect_workbook(path: Path) -> dict[str, Any]:
    file_stat = path.stat()
    with zipfile.ZipFile(path, "r") as archive:
        names = set(archive.namelist())
        workbook_root = xml_root(archive, "xl/workbook.xml")
        if workbook_root is None:
            raise ValueError("Workbook part xl/workbook.xml is missing")
        workbook_relationships = relationship_map(
            archive,
            "xl/_rels/workbook.xml.rels",
        )
        shared_strings = read_shared_strings(archive)
        number_formats, style_number_format_ids = read_number_formats(archive)

        defined_names: list[dict[str, Any]] = []
        for defined_name in workbook_root.findall("x:definedNames/x:definedName", NS):
            local_sheet_id = defined_name.attrib.get("localSheetId")
            defined_names.append(
                {
                    "name": defined_name.attrib.get("name"),
                    "localSheetId": int(local_sheet_id) if local_sheet_id is not None else None,
                    "hidden": defined_name.attrib.get("hidden"),
                    "formula": defined_name.text,
                }
            )

        sheets: list[dict[str, Any]] = []
        exact_formula_locations: dict[str, list[dict[str, str]]] = collections.defaultdict(list)
        canonical_formula_locations: dict[str, list[dict[str, str]]] = collections.defaultdict(list)
        formula_family_counts: collections.Counter[str] = collections.Counter()
        error_cells: list[dict[str, Any]] = []
        rounding_formulas: list[dict[str, str]] = []

        for sheet_index, sheet in enumerate(workbook_root.findall("x:sheets/x:sheet", NS)):
            relation_id = sheet.attrib.get(f"{{{REL_NS}}}id")
            relation = workbook_relationships.get(relation_id or "")
            if not relation:
                continue
            sheet_part = resolve_relationship("xl/workbook.xml", relation["Target"])
            sheet_root = xml_root(archive, sheet_part)
            if sheet_root is None:
                continue
            sheet_name = sheet.attrib.get("name", f"Sheet{sheet_index + 1}")
            formulas: list[dict[str, Any]] = []
            number_format_counter: collections.Counter[str] = collections.Counter()
            labels: list[dict[str, str]] = []
            for cell in sheet_root.findall(".//x:sheetData/x:row/x:c", NS):
                reference = cell.attrib.get("r", "")
                style_index_text = cell.attrib.get("s")
                if style_index_text is not None:
                    try:
                        style_index = int(style_index_text)
                        if style_index < len(style_number_format_ids):
                            format_id = style_number_format_ids[style_index]
                            number_format_counter[number_formats.get(format_id, f"numFmtId:{format_id}")] += 1
                    except ValueError:
                        pass
                value = read_cell_value(cell, shared_strings)
                if isinstance(value, str) and value and cell.attrib.get("t") in {"s", "str", "inlineStr"}:
                    if len(labels) < 100:
                        labels.append({"cell": reference, "text": value})
                if cell.attrib.get("t") == "e":
                    error_cells.append({"sheet": sheet_name, "cell": reference, "value": value})
                formula = cell.find("x:f", NS)
                if formula is None:
                    continue
                formula_text = formula.text or ""
                record = {
                    "cell": reference,
                    "formula": formula_text,
                    "cachedValue": value,
                    "formulaType": formula.attrib.get("t"),
                    "sharedIndex": formula.attrib.get("si"),
                }
                formulas.append(record)
                if formula_text:
                    exact_formula_locations[formula_text].append(
                        {"sheet": sheet_name, "cell": reference}
                    )
                    canonical = canonical_formula(formula_text)
                    canonical_formula_locations[canonical].append(
                        {"sheet": sheet_name, "cell": reference}
                    )
                    formula_family_counts[formula_family_name(formula_text)] += 1
                    if re.search(r"\b(?:ROUND|ROUNDUP|ROUNDDOWN|MROUND|CEILING|FLOOR|TEXT)\s*\(", formula_text, re.IGNORECASE):
                        rounding_formulas.append(
                            {"sheet": sheet_name, "cell": reference, "formula": formula_text}
                        )

            sheet_defined_names = [
                item
                for item in defined_names
                if item["localSheetId"] == sheet_index
            ]
            print_area = next(
                (
                    item["formula"]
                    for item in sheet_defined_names
                    if item["name"] == "_xlnm.Print_Area"
                ),
                None,
            )
            print_titles = next(
                (
                    item["formula"]
                    for item in sheet_defined_names
                    if item["name"] == "_xlnm.Print_Titles"
                ),
                None,
            )
            page_setup = sheet_root.find("x:pageSetup", NS)
            page_margins = sheet_root.find("x:pageMargins", NS)
            formula_signature = hashlib.sha256(
                "\n".join(canonical_formula(item["formula"]) for item in formulas if item["formula"]).encode(
                    "utf-8"
                )
            ).hexdigest()[:16]
            sheets.append(
                {
                    "index": sheet_index,
                    "name": sheet_name,
                    "state": sheet.attrib.get("state", "visible"),
                    "part": sheet_part,
                    "dimension": (
                        sheet_root.find("x:dimension", NS).attrib.get("ref")
                        if sheet_root.find("x:dimension", NS) is not None
                        else None
                    ),
                    "formulaCellCount": len(formulas),
                    "explicitFormulaCount": sum(1 for formula in formulas if formula["formula"]),
                    "formulaSignature": formula_signature,
                    "formulaExamples": formulas[:12],
                    "textLabelExamples": labels[:30],
                    "numberFormats": [
                        {"format": number_format, "cellCount": count}
                        for number_format, count in number_format_counter.most_common(12)
                    ],
                    "tables": table_details(archive, sheet_part, sheet_root),
                    "queryTables": query_table_details(archive, sheet_part),
                    "printArea": print_area,
                    "printTitles": print_titles,
                    "pageSetup": dict(page_setup.attrib) if page_setup is not None else None,
                    "pageMargins": dict(page_margins.attrib) if page_margins is not None else None,
                }
            )

        signature_groups = [
            {
                "formulaSignature": signature,
                "sheets": [sheet["name"] for sheet in group],
                "formulaCellCount": group[0]["formulaCellCount"],
            }
            for signature, group in _group_by(sheets, lambda item: item["formulaSignature"]).items()
            if len(group) > 1 and group[0]["formulaCellCount"] > 0
        ]
        signature_groups.sort(key=lambda group: (-len(group["sheets"]), group["sheets"][0]))

        repeated_exact = [
            {
                "formula": formula,
                "occurrences": len(locations),
                "locations": locations[:20],
            }
            for formula, locations in exact_formula_locations.items()
            if len(locations) > 1
        ]
        repeated_exact.sort(key=lambda item: (-item["occurrences"], item["formula"]))

        repeated_canonical = [
            {
                "canonicalFormula": formula,
                "occurrences": len(locations),
                "locations": locations[:30],
            }
            for formula, locations in canonical_formula_locations.items()
            if len(locations) > 1
        ]
        repeated_canonical.sort(key=lambda item: (-item["occurrences"], item["canonicalFormula"]))

        vba = None
        if "xl/vbaProject.bin" in names:
            try:
                vba = extract_vba_sources(archive.read("xl/vbaProject.bin"))
            except Exception as error:  # keep the workbook map usable if VBA extraction fails
                vba = {"error": f"{type(error).__name__}: {error}"}

        calc_properties = workbook_root.find("x:calcPr", NS)
        workbook_properties = workbook_root.find("x:workbookPr", NS)
        result = {
            "file": {
                "relativePath": f"../{path.name}",
                "name": path.name,
                "sizeBytes": file_stat.st_size,
                "lastModified": dt.datetime.fromtimestamp(file_stat.st_mtime).astimezone().isoformat(
                    timespec="seconds"
                ),
                "sha256": sha256(path),
            },
            "workbook": {
                "sheetCount": len(sheets),
                "visibleSheetCount": sum(sheet["state"] == "visible" for sheet in sheets),
                "hiddenSheetCount": sum(sheet["state"] != "visible" for sheet in sheets),
                "formulaCellCount": sum(sheet["formulaCellCount"] for sheet in sheets),
                "explicitFormulaCount": sum(sheet["explicitFormulaCount"] for sheet in sheets),
                "tableCount": sum(len(sheet["tables"]) for sheet in sheets),
                "queryTableCount": sum(
                    sum(table["queryTable"] is not None for table in sheet["tables"])
                    for sheet in sheets
                ),
                "definedNameCount": len(defined_names),
                "connectionCount": len(workbook_connections(archive)),
                "calculationProperties": (
                    dict(calc_properties.attrib) if calc_properties is not None else None
                ),
                "properties": (
                    dict(workbook_properties.attrib) if workbook_properties is not None else None
                ),
            },
            "definedNames": defined_names,
            "connections": workbook_connections(archive),
            "externalLinkParts": sorted(name for name in names if name.startswith("xl/externalLinks/")),
            "powerQueryParts": sorted(
                name
                for name in names
                if "quer" in name.lower()
                or "mashup" in name.lower()
                or "customxml" in name.lower()
            ),
            "sheets": sheets,
            "duplicateSheetFormulaGroups": signature_groups,
            "formulaFamilies": [
                {"family": family, "occurrences": count}
                for family, count in formula_family_counts.most_common()
            ],
            "repeatedExactFormulas": repeated_exact[:100],
            "repeatedCanonicalFormulas": repeated_canonical[:100],
            "roundingFormulas": rounding_formulas,
            "errorCells": error_cells,
            "vba": vba,
        }
        return result


def _group_by(items: Iterable[Any], key) -> dict[Any, list[Any]]:
    groups: dict[Any, list[Any]] = collections.defaultdict(list)
    for item in items:
        groups[key(item)].append(item)
    return groups


def parse_args() -> argparse.Namespace:
    project_root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(
        description="Inspect the ATAG reference workbook without opening or modifying it."
    )
    parser.add_argument(
        "workbook",
        nargs="?",
        type=Path,
        default=project_root.parent / "(WIP Mitchell) Costing Sheet.xlsm",
    )
    parser.add_argument(
        "--section",
        choices=(
            "summary",
            "sheets",
            "names",
            "connections",
            "formulas",
            "vba",
            "all",
        ),
        default="summary",
    )
    return parser.parse_args()


def select_section(result: dict[str, Any], section: str) -> Any:
    if section == "all":
        return result
    if section == "summary":
        vba = result["vba"]
        if vba and "modules" in vba:
            vba = {
                "projectName": vba["projectName"],
                "declaredModuleCount": vba["declaredModuleCount"],
                "extractedModuleCount": len(vba["modules"]),
                "codeModules": [
                    {
                        "name": module["name"],
                        "declaredType": module["declaredType"],
                        "publicEntryPoints": module["publicEntryPoints"],
                    }
                    for module in vba["modules"]
                    if module["declaredType"] != "Document"
                ],
            }
        return {
            "file": result["file"],
            "workbook": result["workbook"],
            "sheetNames": [
                {"name": sheet["name"], "state": sheet["state"]}
                for sheet in result["sheets"]
            ],
            "duplicateSheetFormulaGroups": result["duplicateSheetFormulaGroups"],
            "formulaFamilies": result["formulaFamilies"][:30],
            "roundingFormulaCount": len(result["roundingFormulas"]),
            "errorCells": result["errorCells"],
            "vba": vba,
        }
    if section == "sheets":
        return result["sheets"]
    if section == "names":
        return result["definedNames"]
    if section == "connections":
        return {
            "connections": result["connections"],
            "externalLinkParts": result["externalLinkParts"],
            "powerQueryParts": result["powerQueryParts"],
            "queryTables": [
                {
                    "sheet": sheet["name"],
                    "table": table["name"],
                    "queryTable": table["queryTable"],
                }
                for sheet in result["sheets"]
                for table in sheet["tables"]
                if table["queryTable"] is not None
            ],
            "tables": [
                {"sheet": sheet["name"], "tables": sheet["tables"]}
                for sheet in result["sheets"]
                if sheet["tables"]
            ],
        }
    if section == "formulas":
        return {
            "duplicateSheetFormulaGroups": result["duplicateSheetFormulaGroups"],
            "formulaFamilies": result["formulaFamilies"],
            "repeatedExactFormulas": result["repeatedExactFormulas"],
            "repeatedCanonicalFormulas": result["repeatedCanonicalFormulas"],
            "roundingFormulas": result["roundingFormulas"],
            "errorCells": result["errorCells"],
        }
    if section == "vba":
        return result["vba"]
    raise ValueError(f"Unknown section: {section}")


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    args = parse_args()
    workbook_path = args.workbook.resolve()
    if not workbook_path.is_file():
        print(f"Workbook not found: {workbook_path}", file=sys.stderr)
        return 2
    result = inspect_workbook(workbook_path)
    print(json.dumps(select_section(result, args.section), indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
