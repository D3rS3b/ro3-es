#!/usr/bin/env python3
"""Rellena los huecos de es.jsonl con traducción automática.

Backends: deepl (DEEPL_API_KEY) o libre (LibreTranslate, --url).

Protege los términos del glosario con marcadores para que la MT no los
destroce, y los restaura en español al final.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

TOKEN = "[[T{}]]"
TOKEN_RE = re.compile(r"\[\[T(\d+)\]\]")


def load_glossary(path: str) -> list[tuple[str, str]]:
    rows: list[tuple[str, str]] = []
    if not path or not os.path.exists(path):
        return rows
    with open(path, encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            term = (row.get("term") or "").strip()
            target = (row.get("es") or "").strip()
            if term and target and term.lower() != target.lower():
                rows.append((term, target))
    rows.sort(key=lambda item: len(item[0]), reverse=True)
    return rows


def protect(text: str, glossary: list[tuple[str, str]], used: list[str]) -> str:
    for term, target in glossary:
        pattern = re.compile(rf"(?<!\w){re.escape(term)}(?!\w)", re.IGNORECASE)
        if not pattern.search(text):
            continue
        index = len(used)
        used.append(target)
        text = pattern.sub(TOKEN.format(index), text)
    return text


def restore(text: str, used: list[str]) -> str:
    def repl(match: re.Match) -> str:
        index = int(match.group(1))
        return used[index] if 0 <= index < len(used) else match.group(0)

    return TOKEN_RE.sub(repl, text)


def deepl_batch(texts: list[str], api_key: str) -> list[str]:
    endpoint = os.environ.get("DEEPL_URL", "https://api-free.deepl.com/v2/translate")
    data = [("auth_key", api_key), ("source_lang", "EN"), ("target_lang", "ES")]
    data += [("text", text) for text in texts]
    body = urllib.parse.urlencode(data).encode()
    request = urllib.request.Request(endpoint, data=body)
    with urllib.request.urlopen(request, timeout=60) as response:
        payload = json.loads(response.read().decode())
    return [item["text"] for item in payload["translations"]]


def libre_batch(texts: list[str], base_url: str) -> list[str]:
    endpoint = base_url.rstrip("/") + "/translate"
    payload = json.dumps({"q": texts, "source": "en", "target": "es", "format": "text"}).encode()
    request = urllib.request.Request(endpoint, data=payload,
                                     headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=60) as response:
        data = json.loads(response.read().decode())
    translated = data.get("translatedText")
    if isinstance(translated, list):
        return translated
    return [translated]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--file", default="data/es.jsonl")
    parser.add_argument("--glossary", default="data/glosario.csv")
    parser.add_argument("--backend", choices=["deepl", "libre"], default="deepl")
    parser.add_argument("--url", default="http://localhost:5000",
                        help="URL de LibreTranslate")
    parser.add_argument("--batch", type=int, default=25)
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--status", default="draft", help="estado de lo traducido")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    api_key = os.environ.get("DEEPL_API_KEY", "")
    if args.backend == "deepl" and not api_key:
        print("Falta DEEPL_API_KEY", file=sys.stderr)
        return 2

    glossary = load_glossary(args.glossary)
    entries = [json.loads(line) for line in open(args.file, encoding="utf-8")
               if line.strip() and not line.lstrip().startswith("#")]

    pending = [entry for entry in entries
               if not (entry.get("dst") or "").strip() and entry.get("status") != "keep"]
    if args.limit:
        pending = pending[:args.limit]

    print(f"Pendientes: {len(pending)} de {len(entries)}")
    if not pending:
        return 0

    done = 0
    for start in range(0, len(pending), args.batch):
        chunk = pending[start:start + args.batch]
        used_terms: list[list[str]] = []
        sources = []
        for entry in chunk:
            used: list[str] = []
            sources.append(protect(entry["src"], glossary, used))
            used_terms.append(used)

        try:
            if args.backend == "deepl":
                results = deepl_batch(sources, api_key)
            else:
                results = libre_batch(sources, args.url)
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError) as exc:
            print(f"Error de red en el lote {start}: {exc}", file=sys.stderr)
            return 1

        for entry, translated, used in zip(chunk, results, used_terms):
            entry["dst"] = restore(translated, used)
            entry["status"] = args.status
            done += 1

        print(f"  {done}/{len(pending)}")
        time.sleep(0.3)

    if args.dry_run:
        for entry in pending[:10]:
            print(entry["src"], "->", entry["dst"])
        return 0

    with open(args.file, "w", encoding="utf-8") as handle:
        for entry in entries:
            handle.write(json.dumps(entry, ensure_ascii=False) + "\n")

    print(f"Escrito {args.file}: {done} entradas traducidas ({args.status})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
