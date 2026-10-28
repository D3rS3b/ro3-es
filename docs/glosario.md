# Glosario y normas de traducción

Fuente de verdad: [`data/glosario.csv`](../data/glosario.csv). Este documento
explica las reglas; el CSV es lo que aplican las herramientas.

## Normas

1. **No traducir** siglas de estadísticas: `HP`, `SP`, `ATK`, `MATK`, `DEF`,
   `MDEF`, `HIT`, `FLEE`, `CRIT`, `ASPD`, `NPC`, `PvP`, `PvE`, `EXP`.
2. **Conservar** placeholders y etiquetas: `{0}`, `%s`, `%1$s`, `<color=...>`,
   `[NAME]`, saltos de línea.
3. **No traducir** nombres propios de ciudades, NPC ni ítems si el juego no los
   traduce en otros idiomas: se mantiene el nombre en inglés para que coincida
   con wikis y con el mercado.
4. **Términos con convención fija** (usar siempre el mismo):

| Inglés | Español | Nota |
|---|---|---|
| Level | Nivel | |
| Experience | Experiencia | |
| Inventory | Inventario | |
| Equipment | Equipamiento | |
| Quest | Misión | |
| Party | Grupo | no "Partido" |
| Guild | Gremio | no "Clan" |
| Dungeon | Mazmorra | |
| Boss | Jefe | |
| Skill | Habilidad | |
| Stat | Atributo | |
| Damage | Daño | |
| Defense | Defensa | |
| Trade | Intercambio | |
| Storage | Almacén | |
| Whisper | Susurro | |

5. **Estilo**: tuteo, frases cortas, mayúscula inicial y sin punto final en
   etiquetas de botón.
6. **Género**: "la party" → "el grupo"; evitar construcciones con género
   ambiguo en mensajes del sistema.

## Estados

| Estado | Significado |
|---|---|
| `new` | capturada automáticamente, sin tocar |
| `draft` | traducida por MT, pendiente de revisión |
| `review` | revisada por una persona |
| `keep` | no se traduce (siglas, nombres propios) |

## Añadir términos

Edita `data/glosario.csv` (`term,es,notes`). `tools/mt_fill.py` los protege
antes de llamar al traductor automático y los restaura después.
