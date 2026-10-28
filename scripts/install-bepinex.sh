#!/usr/bin/env bash
# Instala BepInEx 5 (Mono x64) en la carpeta del juego y prepara el plugin.
#
#   ./scripts/install-bepinex.sh "/home/$USER/Games/RO3/ROO_PC"
#
# Requiere: curl, unzip. No necesita root.
set -euo pipefail

GAME_DIR="${1:?uso: $0 /ruta/a/ROO_PC (la carpeta que contiene ro3.exe)}"
BEPINEX_VER="${BEPINEX_VER:-5.4.21}"
BEPINEX_URL="${BEPINEX_URL:-}"

[[ -f "$GAME_DIR/ro3.exe" ]] || { echo "ERROR: no encuentro ro3.exe en $GAME_DIR" >&2; exit 1; }

TMPDIR_INSTALL="$(mktemp -d)"
trap 'rm -rf "$TMPDIR_INSTALL"' EXIT

if [[ -z "$BEPINEX_URL" ]]; then
  for candidate in \
    "https://github.com/BepInEx/BepInEx/releases/download/v${BEPINEX_VER}/BepInEx_win_x64_${BEPINEX_VER}.0.zip" \
    "https://github.com/BepInEx/BepInEx/releases/download/v${BEPINEX_VER}.0/BepInEx_win_x64_${BEPINEX_VER}.0.zip"
  do
    echo "Probando $candidate"
    if curl -fL --retry 2 -o "$TMPDIR_INSTALL/bepinex.zip" "$candidate"; then
      BEPINEX_URL="$candidate"
      break
    fi
  done
fi

[[ -f "$TMPDIR_INSTALL/bepinex.zip" ]] || { echo "ERROR: no se pudo descargar BepInEx." >&2; exit 1; }

unzip -q -o "$TMPDIR_INSTALL/bepinex.zip" -d "$GAME_DIR"

mkdir -p "$GAME_DIR/BepInEx/plugins/ro3-es"
if [[ -f "data/es.jsonl" ]]; then
  cp "data/es.jsonl" "$GAME_DIR/BepInEx/plugins/ro3-es/"
fi

echo "BepInEx instalado en: $GAME_DIR"
echo "Plugin esperado en:    $GAME_DIR/BepInEx/plugins/ro3-es/ro3-es.dll"
echo "Diccionario:           $GAME_DIR/BepInEx/plugins/ro3-es/es.jsonl"
echo
echo "Arranca el juego una vez y revisa $GAME_DIR/BepInEx/LogOutput.log"
