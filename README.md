# ro3-es — Localización al español de Ragnarok Online 3

Capa de traducción **en tiempo de ejecución** para el cliente de Ragnarok Online 3
(Unity 2022 + XLua, Mono). No modifica ni un byte del juego: se engancha al sistema
de texto (TextMeshPro) y sustituye las cadenas por su versión en español.

> **Estado:** v0.1 — esqueleto funcional. El plugin compila y engancha TMP/UGUI,
> pero aún no se ha validado en partida.

## Por qué no es un parche de archivos

El cliente está protegido contra parches estáticos:

| Componente | Situación |
|---|---|
| `LuaPayload` (13.474 módulos) | bytecode cifrado/ofuscado |
| `VFS/` (23 GB) | bundles de assets firmados |
| `tracked_files` + `.sig` + SHA256 | el launcher verifica integridad y repara |

Editar archivos se rompe en cada actualización (y puede provocar reparaciones).
Un hook en runtime sobrevive a los parches del juego y se desactiva borrando una carpeta.

## Cómo funciona

```
Juego (Unity)  ──►  TMP_Text.text / UI.Text.text
                          │
                          ▼
                  [Plugin ro3-es]
                          │  busca en data/es.jsonl
                          ▼
                 texto traducido o el original
```

1. **Captura:** todo texto que el juego asigna y no está en el diccionario se
   registra en `capture.jsonl` (con objeto y escena como contexto).
2. **Traducción:** se traduce offline (MT + glosario) y se revisa.
3. **Inyección:** el plugin lee `es.jsonl` en caliente; no hace falta reiniciar.

## Instalación (Linux/Wine)

```bash
# 1. BepInEx 5 (Mono x64) dentro de la carpeta del juego
./scripts/install-bepinex.sh "/home/$USER/Games/RO3/ROO_PC"

# 2. Compilar el plugin (necesita el SDK de .NET y la carpeta Managed del juego)
RO3_GAME_DIR="/home/$USER/Games/RO3/ROO_PC" dotnet build src/Ro3Es.Plugin -c Release

# 3. Copiar el DLL
cp src/Ro3Es.Plugin/bin/Release/netstandard2.0/ro3-es.dll \
   "/home/$USER/Games/RO3/ROO_PC/BepInEx/plugins/ro3-es/"

# 4. Copiar el diccionario
cp data/es.jsonl "/home/$USER/Games/RO3/ROO_PC/BepInEx/plugins/ro3-es/"
```

### Proton / Wine

BepInEx necesita que se use su `winhttp.dll` en lugar del integrado de Wine:

```bash
export WINEDLLOVERRIDES="winhttp=n,b"
```

En Lutris: **Configuración del juego → Sistema → Variables de entorno** y añade
`WINEDLLOVERRIDES=winhttp=n,b`. Sin esto el juego arranca sin cargar el plugin
(no aparece `BepInEx/LogOutput.log`).

## Estructura

```
data/es.jsonl        diccionario original → español
data/glosario.csv    términos del juego (stats, skills, UI)
src/Ro3Es.Plugin/    plugin BepInEx (C#)
tools/               validación, MT, conversión CSV
docs/                arquitectura, glosario, aviso legal
```

## Formato de `es.jsonl`

Una línea por cadena:

```json
{"src":"Inventory","dst":"Inventario","status":"review","ctx":"UI/MainMenu"}
```

`status`: `draft` (MT sin revisar), `review` (revisado), `keep` (no traducir).

## Contribuir

1. Juega con el plugin y activa la captura.
2. Envía tu `capture.jsonl` o abre un PR con entradas nuevas en `data/es.jsonl`.
3. Pasa `python3 tools/validate.py data/es.jsonl` antes del PR.

## Aviso legal

Proyecto no oficial, sin relación con Gravity ni con el equipo de RO3.
No se distribuyen archivos del juego. Modificar el cliente puede ir contra los
términos de servicio: úsalo bajo tu responsabilidad. Ver `docs/legal-y-tos.md`.

## Licencia

Código: MIT. Traducciones: CC BY-SA 4.0.
