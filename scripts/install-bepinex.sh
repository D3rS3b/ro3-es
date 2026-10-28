#!/usr/bin/env bash
# Instala BepInEx 5 (Mono x64) en la carpeta del juego y prepara el plugin.
#
#   ./scripts/install-bepinex.sh "/home/$USER/Games/RO3/ROO_PC"
#
# Requiere: curl, unzip. No necesita root.
set -euo pipefail

GAME_DIR="${1:?uso: $0 /ruta/a/ROO_PC (la carpeta que contiene ro3.exe)}"

# 5.4.23.x falla en Unity 2022 Mono con:
#   MissingMethodException: System.Reflection.Module.GetPEKind
# 5.4.22 es la última que carga el preloader en este cliente.
BEPINEX_VER="${BEPINEX_VER:-5.4.22}"
BEPINEX_URL="${BEPINEX_URL:-}"

[[ -f "$GAME_DIR/ro3.exe" ]] || { echo "ERROR: no encuentro ro3.exe en $GAME_DIR" >&2; exit 1; }

TMPDIR_INSTALL="$(mktemp -d)"
trap 'rm -rf "$TMPDIR_INSTALL"' EXIT

CANDIDATOS=()
if [[ -n "$BEPINEX_URL" ]]; then
  CANDIDATOS=("$BEPINEX_URL")
elif [[ "$BEPINEX_VER" == "latest" ]]; then
  # Última release 5.x con binario para Windows x64 (requiere jq).
  CANDIDATOS=("$(curl -sS "https://api.github.com/repos/BepInEx/BepInEx/releases?per_page=15" \
    | jq -r '[.[] | select(.tag_name | startswith("v5."))][0].assets[]
             | select(.name | test("win_x64.*\\.zip$"))
             | .browser_download_url' | head -1)")
else
  BASE="https://github.com/BepInEx/BepInEx/releases/download/v${BEPINEX_VER}"
  CANDIDATOS=(
    "${BASE}/BepInEx_x64_${BEPINEX_VER}.0.zip"
    "${BASE}/BepInEx_win_x64_${BEPINEX_VER}.zip"
    "${BASE}/BepInEx_win_x64_${BEPINEX_VER}.0.zip"
  )
fi

DESCARGADO=0
for url in "${CANDIDATOS[@]}"; do
  [[ -n "$url" && "$url" != "null" ]] || continue
  echo "Descargando $url"
  if curl -fL --retry 2 -o "$TMPDIR_INSTALL/bepinex.zip" "$url"; then
    BEPINEX_URL="$url"
    DESCARGADO=1
    break
  fi
done

[[ "$DESCARGADO" -eq 1 ]] || { echo "ERROR: no se pudo descargar BepInEx ${BEPINEX_VER}." >&2; exit 1; }

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
