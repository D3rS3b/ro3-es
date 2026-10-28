#!/usr/bin/env python3
"""Valida data/es.jsonl: JSON, duplicados, placeholders y estados."""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter

STATUSES = {"new", "draft", "review", "keep"}

# Grupos 0..n, %s/%d/%1$s, etiquetas rich text y variables tipo [NAME]
PLACEHOLDER_PATTERNS = [
    re.compile(r"\{[0-9]+(?:[:!][^}]*)?\}"),
    re.compile(r"%[0-9]+\$[sdif]"),
    re.compile(r"%[sdif]"),
    re.compile(r"</?[a-zA-Z][^>]*>"),
    re.compile(r"\[[A-Z0-9_]+\]"),
]


def placeholders(text: str) -> Counter:
    found = Counter()
    for pattern in PLACEHOLDER_PATTERNS:
        for match in pattern.finditer(text):
            found[match.group(0)] += 1
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("files", nargs="*", default=["data/es.jsonl"])
    parser.add_argument("--strict", action="store_true",
                        help="trata los avisos como errores")
    args = parser.parse_args()

    errors: list[str] = []
    warnings: list[str] = []
    seen: dict[str, str] = {}
    by_status: Counter = Counter()
    total = 0

    for path in args.files:
        try:
            handle = open(path, encoding="utf-8")
        except OSError as exc:
            errors.append(f"{path}: no se puede abrir ({exc})")
            continue

        with handle:
            for line_no, line in enumerate(handle, 1):
                line = line.strip()
                if not line or line.startswith("#"):
                    continue
                where = f"{path}:{line_no}"
                total += 1

                try:
                    entry = json.loads(line)
                except json.JSONDecodeError as exc:
                    errors.append(f"{where}: JSON inválido ({exc.msg})")
                    continue

                src = entry.get("src")
                dst = entry.get("dst", "")
                status = entry.get("status", "")

                if not isinstance(src, str) or not src:
                    errors.append(f"{where}: falta 'src'")
                    continue
                if not isinstance(dst, str):
                    errors.append(f"{where}: 'dst' no es texto")
                    continue
                if status not in STATUSES:
                    errors.append(f"{where}: estado desconocido '{status}'")
                by_status[status] += 1

                if src in seen:
                    if seen[src] != dst:
                        errors.append(f"{where}: '{src[:40]}' duplicado con distinta traducción")
                    else:
                        warnings.append(f"{where}: '{src[:40]}' duplicado idéntico")
                else:
                    seen[src] = dst

                if not dst and status not in {"keep", "new"}:
                    errors.append(f"{where}: sin traducción (estado '{status}')")

                if src.count("\n") != dst.count("\n"):
                    errors.append(f"{where}: distinto número de saltos de línea")

                src_tokens = placeholders(src)
                dst_tokens = placeholders(dst)
                if dst and src_tokens != dst_tokens:
                    errors.append(
                        f"{where}: placeholders distintos {dict(src_tokens)} != {dict(dst_tokens)}")

                if dst and src == dst and status != "keep":
                    warnings.append(f"{where}: '{src[:40]}' sin traducir y estado '{status}'")

                if dst and (src.strip() != src or dst.strip() != dst):
                    warnings.append(f"{where}: espacios al principio/final")

    print(f"Entradas: {total}")
    for status, count in sorted(by_status.items()):
        print(f"  {status:<7} {count}")

    for warning in warnings[:50]:
        print(f"AVISO  {warning}")
    if len(warnings) > 50:
        print(f"... {len(warnings) - 50} avisos más")

    for error in errors:
        print(f"ERROR  {error}")

    print(f"\nResultado: {len(errors)} errores, {len(warnings)} avisos")
    if errors or (args.strict and warnings):
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
