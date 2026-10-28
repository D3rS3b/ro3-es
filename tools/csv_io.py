#!/usr/bin/env python3
"""Convierte es.jsonl a CSV para traductores y de vuelta.

  python3 tools/csv_io.py export --file data/es.jsonl --out traduccion.csv
  python3 tools/csv_io.py import --file data/es.jsonl --csv traduccion.csv
"""

from __future__ import annotations

import argparse
import csv
import json
import sys

FIELDS = ["src", "dst", "status", "ctx"]


def export(jsonl_path: str, csv_path: str) -> int:
    count = 0
    with open(jsonl_path, encoding="utf-8") as source, \
            open(csv_path, "w", encoding="utf-8", newline="") as target:
        writer = csv.DictWriter(target, fieldnames=FIELDS, quoting=csv.QUOTE_ALL)
        writer.writeheader()
        for line in source:
            if not line.strip() or line.lstrip().startswith("#"):
                continue
            entry = json.loads(line)
            writer.writerow({field: entry.get(field, "") for field in FIELDS})
            count += 1
    print(f"Exportadas {count} entradas a {csv_path}")
    return 0


def import_csv(jsonl_path: str, csv_path: str) -> int:
    entries = []
    with open(csv_path, encoding="utf-8", newline="") as source:
        for row in csv.DictReader(source):
            src = (row.get("src") or "").strip()
            if not src:
                continue
            entries.append({
                "src": src,
                "dst": row.get("dst") or "",
                "status": row.get("status") or "draft",
                "ctx": row.get("ctx") or "",
            })
    with open(jsonl_path, "w", encoding="utf-8") as target:
        for entry in entries:
            target.write(json.dumps(entry, ensure_ascii=False) + "\n")
    print(f"Importadas {len(entries)} entradas en {jsonl_path}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    exp = sub.add_parser("export")
    exp.add_argument("--file", default="data/es.jsonl")
    exp.add_argument("--out", default="traduccion.csv")

    imp = sub.add_parser("import")
    imp.add_argument("--file", default="data/es.jsonl")
    imp.add_argument("--csv", default="traduccion.csv")

    args = parser.parse_args()
    if args.command == "export":
        return export(args.file, args.out)
    return import_csv(args.file, args.csv)


if __name__ == "__main__":
    sys.exit(main())
