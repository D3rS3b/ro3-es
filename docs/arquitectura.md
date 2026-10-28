# Arquitectura

## Restricciones del cliente

Investigación sobre `RO3-Full-Client-EN-PTBR` (2026.08.16):

- Unity **2022.3.62f3**, **Mono** (`ro3_Data/Managed`), no IL2CPP.
- `Assembly-CSharp.dll` contiene `LanguageMain`, `LangTransUtils_TMP`,
  `LangTransUtils_Image`, `LangTransUtils_RawImage`: todo el texto de UI pasa
  por ahí y acaba en TextMeshPro o UGUI.
- `StreamingAssets/Recovery/LuaPayload`: 13.474 módulos **cifrados**
  (contenedor `LuaT`, sin cadenas legibles).
- `StreamingAssets/VFS`: ~23 GB de bundles **firmados**.
- `tracked_files`, `SHA256SUMS.txt`, `LocalProto/*.sig` y el manifiesto del
  launcher permiten **verificar y reparar** archivos modificados.

## Decisión

Parche **en runtime** sobre el asignador de texto, no sobre archivos:

1. No rompe firmas ni provoca reparaciones.
2. Sobrevive a actualizaciones del cliente (los nombres de tipo/clase son estables).
3. Es reversible: se desactiva borrando `BepInEx/plugins/ro3-es`.
4. Permite recarga en caliente del diccionario mientras se juega.

## Flujo

```
Juego → TMP_Text.set_text / UI.Text.set_text
            │
            ├─ ¿está en es.jsonl?  → sí → escribe el español
            │
            └─ no → texto original + registro en capture.jsonl
```

`capture.jsonl` incluye escena y nombre del objeto para dar contexto al traductor.

## Hooks

| Objetivo | Motivo |
|---|---|
| `TMPro.TMP_Text.text` | todo el texto de TMP (la mayoría de la UI) |
| `TMPro.TextMeshProUGUI.text` | por si el setter se sobrescribe |
| `UnityEngine.UI.Text.text` | UI antigua / casos sueltos |
| `LangTransUtils_TMP` | pendiente: hook directo cuando se conozca la firma |

## Riesgos técnicos

- **Glifos**: si el *font asset* de TMP no incluye `á é í ó ú ñ ¿ ¡`, se verán
  cuadros. Mitigación: fuente de respaldo TMP o sustituir el atlas.
- **Texto en texturas**: imágenes con texto no se traducen con este método.
- **Texto dinámico del servidor**: si llega ya formateado, se captura igual,
  pero puede cambiar entre sesiones.
- **Placeholders**: `{0}`, `%s`, etiquetas rich text y variables deben
  conservarse; `tools/validate.py` lo comprueba.

## Bloqueo conocido (28/10/2026)

Probado sobre el cliente `EN-PTBR 2026.08.16` (Unity 2022.3.62f3, Mono):

- El `mscorlib.dll` del juego está **recortado**: no existe
  `System.Reflection.Module.GetPEKind`, y `System.dll` tampoco expone
  `ServicePointManager.set_ServerCertificateValidationCallback`.
- **BepInEx 5.4.21/5.4.22/5.4.23.x** fallan en `PreloaderRunner.PreloaderPreMain`
  con `MissingMethodException: GetPEKind` (`preloader_*.log`).
- **MelonLoader 0.7.3** falla en `MelonLoader.Core.Initialize` por
  `set_ServerCertificateValidationCallback`.
- `dllSearchPathOverride` de Doorstop 4.6 (`C:\...` y `Z:\...`) **no tiene
  efecto**: Mono ya resolvió `mscorlib` cuando se aplica.
- Sustituir `ro3_Data/Managed/mscorlib.dll` por el corlib completo de
  `unity.bepinex.dev/corlibs/2022.3.62.zip` **rompe el arranque**: el proceso
  muere antes de inicializar Unity y el launcher verifica/restaura archivos.

**Conclusión:** la modificación en runtime está bloqueada por las librerías
recortadas. Alternativas: esperar a un loader que no dependa de `GetPEKind`, o
una capa externa por OCR (peor experiencia, pero sin tocar el cliente).

## Fases

1. **Captura** (actual): hook + `capture.jsonl`.
2. **Diccionario**: `es.jsonl` + recarga en caliente.
3. **MT + glosario**: `tools/mt_fill.py`.
4. **Empaquetado**: instalador Linux/Wine, releases y CI.
