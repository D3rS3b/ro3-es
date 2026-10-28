# Cómo contribuir

## Traducir

1. Exporta el diccionario a CSV:
   ```bash
   python3 tools/csv_io.py export --file data/es.jsonl --out traduccion.csv
   ```
2. Traduce la columna `dst`. No toques `src`.
3. Importa y valida:
   ```bash
   python3 tools/csv_io.py import --file data/es.jsonl --csv traduccion.csv
   python3 tools/validate.py data/es.jsonl
   ```
4. Abre un PR con `data/es.jsonl` (y `data/glosario.csv` si añades términos).

## Capturar cadenas nuevas

Con el plugin instalado y `Capture.Enabled = true` (por defecto), juega un rato.
Las cadenas desconocidas se guardan en:

```
<Juego>/BepInEx/plugins/ro3-es/capture.jsonl
```

Adjúntalo al issue o conviértelo a entradas de `es.jsonl`.

## Código

- C# del plugin: `src/Ro3Es.Plugin/`
- Herramientas: `tools/` (solo biblioteca estándar de Python)
- Antes de un PR: `python3 -m compileall tools` y `python3 tools/validate.py data/es.jsonl`

## Qué no aceptamos

- Archivos del juego (ejecutables, bundles, DLLs, fuentes).
- Traducciones generadas por MT sin marcar como `draft`.
- Cambios que rompan placeholders o etiquetas rich text.
