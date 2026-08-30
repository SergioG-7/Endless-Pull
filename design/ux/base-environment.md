# UX Spec: Entorno de Base (fondo y zonas desbloqueables)

> **Status**: In Design
> **Author**: usuario + ux-designer
> **Last Updated**: 2026-08-27
> **Journey Phase(s)**: desconocida — no existe `design/player-journey.md`
> **Template**: UX Spec

---

## Purpose & Player Need

El jugador llega a la base entre expediciones y quiere sentir que su progreso en
la torre transforma un lugar físico, no solo una lista de stats. Necesita, de un
vistazo, distinguir qué zonas ya están abiertas, cuáles siguen bloqueadas y qué
piso las desbloquea — sin tener que abrir un panel para averiguarlo.

"El jugador llega a esta pantalla queriendo **ver y recorrer su base como un
lugar que crece con él**, y confirmar de un vistazo qué le falta desbloquear."

Si esto no existiera: el jugador seguiría viendo edificios aparecer/desaparecer
sin explicación (hueco #1 de `interaction-patterns.md`) — rompe la fantasía de
"la base vive contigo" (Pilar 3 de `game-concept.md`) porque el crecimiento es
invisible en vez de sentido.

---

## Player Context on Arrival

El jugador está siempre en la base cuando no está en combate ni dentro de un
panel modal (`Base.unity` es la escena persistente, no algo a lo que "se
navega"). La reencuentra: al arrancar sesión, y automáticamente tras cada
retirada/victoria de piso (Portal de Torre) o expedición de recursos.

Estado emocional por defecto: calmado/ambiente (sesiones de 5-10 min, perfil
casual) — mirando de reojo mientras decide el siguiente paso. Pico puntual de
anticipación/orgullo justo al volver tras superar un piso 5/10/15: es el
momento exacto en que debería notar que algo nuevo se abrió.

Llegada: siempre "enviado" por el juego (recall automático o inicio de
sesión), nunca una elección de navegación — el jugador no "abre" la base, ya
está en ella.

---

## Navigation Position

Esta pantalla vive en la raíz: `Base` → (nada) → **Entorno de Base**. No es un
destino al que se navega — es el hogar persistente sobre el que se abren todos
los paneles modales (Roster, Santuario, Torre, etc. vía `UIManager`) y donde
ocurre el propio combate (la cámara se desplaza a la arena dentro de la misma
escena, no cambia de escena). Siempre alcanzable: es el estado de reposo por
defecto de todo el juego.

---

## Entry & Exit Points

| Entrada | Disparador | Contexto que trae el jugador |
|---|---|---|
| Arranque de sesión | `MainMenuUI` → "Continuar"/"Nueva Partida" | Save cargado o base vacía inicial |
| Retirada/victoria de piso | `WaveManager.RecallParty` (Portal de Torre) | Escuadra recolocada cerca del Portal, piso actualizado (posible desbloqueo) |
| Fin de expedición de recursos | `ResourceExpeditionManager` **(cambio de esta spec)** — hoy es 100% abstracta (timer + contador, sin movimiento de héroes); pasa a devolver la escuadra por el **mismo `TowerGateway`**, con etiqueta/animación propia | Recursos añadidos a la economía; escuadra vuelve visible al Portal en vez de no haberse movido nunca |

| Salida | Disparador | Notas |
|---|---|---|
| Vuelta al título | `InGameMenuUI` → "Guardar y salir" | Guarda antes de salir; único "abandono" real de la base |
| Salida a expedición de recursos | Confirmación en `SquadManagementUI` (modo expedición) | **(cambio de esta spec)** la escuadra debería cruzar visiblemente el `TowerGateway` al salir, igual que al subir a la Torre, en vez de desaparecer sin más |
| (resto) | — | Las demás pantallas son modales que se cierran encima, no salidas de la base |

---

## Layout Specification

### Information Hierarchy

Estado actual (referencia): en la escena hoy solo `Building_Farm2` tiene
`requiredFloor` (piso 10); el resto son visibles desde piso 0. No existe
todavía el sistema de "cuadrantes" que pide el roadmap — esta spec lo define.

Ranking de prioridad:

1. **Zona actual desbloqueada** — edificios visibles a detalle completo, con
   su nivel. Lo que el jugador ya tiene y usa.
2. **Zonas bloqueadas como silueta con candado + piso requerido** — hoy son
   invisibles (hueco #1 de `interaction-patterns.md`); pasan a ser
   visibles-pero-bloqueadas.
3. **Portal de Torre** (`TowerGateway`, punto de entrada a Torre y
   Expedición de Recursos) — landmark siempre visible, nunca se bloquea.
4. **Estado individual de cada edificio** (nivel, ocupación) — descubrible al
   tocar, ya cubierto por `BuildingInspectUI`; no hace falta mostrarlo
   permanente en el mapa.
5. **Tema visual por cuadrante** ("bioma" que refuerza qué rango de pisos
   abrió esa zona) — refuerzo ambiental, no información crítica por sí sola.

### Layout Zones

Arreglo elegido: **cuadrantes direccionales** desde el Portal de Torre (N/E/S/O),
coherente con el layout de columnas ya existente (edificios en x=±3.2) y con la
palabra "cuadrantes" del roadmap. Cada cuadrante desbloqueado puede tener su
propio tema visual, coherente con los biomas de pisos de Fase 28
(goblin/minas/cripta/templo).

- **Hub central** (siempre visible, piso 0): `TowerGateway` en el centro + los
  6 edificios ya existentes (Entrenamiento, Cantina, Descanso, Granja, Taller,
  Altar de Invocación), sin reposicionar — se conserva el layout actual.
- **Cuadrante Este** (piso 5): zona nueva. Contenido exacto (qué edificio(s)
  van aquí) pendiente de decisión de game-design — ver Open Questions.
- **Cuadrante Sur** (piso 10): aquí se reubica `Building_Farm2`, que **ya
  existe en el código con `requiredFloor=10`** — esta spec formaliza un
  desbloqueo ya implementado, no inventa uno nuevo.
- **Cuadrante Oeste** (piso 15): zona nueva, contenido pendiente de
  game-design — ver Open Questions.

Cada cuadrante bloqueado se ve como silueta oscurecida + candado + etiqueta
"Piso N" (cierra el hueco #1 de `interaction-patterns.md`); al cruzar el piso,
una transición de desbloqueo (ver Transitions & Animations) revela el
cuadrante en detalle completo.

**Cámara**: `CameraDirector` hoy solo viaja entre 2 puntos fijos (base/arena),
sin pan libre. Se mantiene así — el `orthographicSize` de la vista de base
crece lo suficiente para que el hub y los 3 cuadrantes quepan en pantalla a la
vez (los bloqueados, como silueta lejana). Sin cámara nueva que programar;
encaja con "ver su base de un vistazo" del Purpose. Efecto secundario
aceptado: los edificios se ven más pequeños cuanto más crece la base
desbloqueada.

### Component Inventory

**Hub central**: los 6 edificios y `TowerGateway` ya existen (SpriteRenderer +
Label + Kicker), sin cambios. `TowerGateway` gana una variante visual/etiqueta
al cruzarlo según destino (Torre vs Expedición) — reutiliza el patrón #11
(texto flotante) para el rótulo momentáneo.

**Cuadrantes bloqueados** (Este/Oeste, y Sur hasta piso 10) — componentes
NUEVOS, no hay patrón existente exacto:
- **Silueta de zona bloqueada**: versión oscurecida/desaturada del arte de
  fondo del cuadrante, con icono de candado y etiqueta "Piso N" flotante sobre
  el centro de la zona.
- **Feedback de toque en zona bloqueada**: al tocar/clicar la silueta,
  mensaje breve tipo "Desbloquea en Piso N" (reutiliza estilo del patrón #11,
  disparado desde mundo en vez de combate) — cierra a la vez el hueco #1 y el
  #3 (toque en vacío sin feedback) de `interaction-patterns.md`.
- **Extensión de `WorldInteractionManager`**: hoy
  `if (!building.IsUnlocked) continue;` ignora por completo los edificios
  bloqueados — pasa a detectar el tap sobre la silueta y disparar el feedback
  de arriba en vez de descartarlo.

**Transición de desbloqueo** (dispara una sola vez, al cruzar el piso):
animación de "revelado" del cuadrante — reutiliza el criterio del patrón #14
(pop del cofre de recompensa: escala/rebote sin librería de tweening)
aplicado al fondo/edificios del cuadrante en vez de a un icono de cofre.

*Nota para el cierre de esta spec*: "Silueta de zona bloqueada" y "Feedback
de toque en zona bloqueada" son patrones nuevos — se añadirán a
`interaction-patterns.md` en el cross-reference check final.

### ASCII Wireframe

Vista top-down del mapa de base (no una pantalla UI tradicional — el "wireframe"
aquí es la disposición espacial del mundo 2D):

```
                    NORTE
              (reservado, sin uso
               en esta spec — ver
                Open Questions)

  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
  ░  CUADRANTE OESTE   ░   HUB CENTRAL    ░ CUADRANTE ESTE ░
  ░  (bloqueado hasta   ░  [Entrenam.] [Cantina] ░ (bloqueado hasta ░
  ░   Piso 15)          ░  [Granja]  🚪  [Taller] ░  Piso 5)         ░
  ░   🔒 "Piso 15"       ░  [Descanso][Altar]     ░  🔒 "Piso 5"      ░
  ░                     ░   🚪 = TowerGateway     ░  (contenido:      ░
  ░  (contenido:        ░   (siempre visible)     ░   pendiente,      ░
  ░   pendiente,        ░                         ░   ver Open Q.)    ░
  ░   ver Open Q.)      ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
  ░░░░░░░░░░░░░░░░░░░░░░░░
                  CUADRANTE SUR
                  (bloqueado hasta Piso 10)
                  🔒 "Piso 10"  →  al desbloquear: Building_Farm2
                  (ya implementado con requiredFloor=10)
```

Leyenda: `░` = silueta oscurecida/candado (zona bloqueada), `🚪` = Portal de
Torre (punto de entrada a Torre y Expedición de Recursos, siempre visible).

---

## Visual Design Specification

> Fase 2 (`/team-ui`) — art-director. `UITheme.cs` es la guía de estilo de
> facto del proyecto (no existe `design/art/art-bible.md` formal); esta
> sección reutiliza sus tokens en vez de inventar paleta nueva.

### Lenguaje visual

Dark Glassmorphism, extendido de mundo-UI a mundo-3D sin inventar nada nuevo:
los mismos tokens que ya usan los paneles modales (`UITheme.GlassDeep`,
`BorderStrong`) se reutilizan para la silueta de zona bloqueada. Refuerza un
aprendizaje que el jugador ya tiene: "vidrio oscuro = fuera de alcance ahora
mismo" es el mismo lenguaje que ya usa el backdrop de cualquier modal.

Hallazgo relevante: el arte de mundo hoy es 100% placeholder — los 4
edificios existentes (`Building_Farm`, `Cantina`, `Entrenamiento`, `Altar`)
son el mismo sprite `Square_White.png` reteñido vía `SpriteRenderer.color`,
sin fondo de suelo pintado. El fondo de bioma pintado (goblin/minas/cripta)
queda explícitamente fuera de esta pasada — es trabajo de Fase 28; esta spec
no simula ese arte con un "difuminado" prematuro.

### Tratamiento de estado bloqueado

- **Cuerpo de la silueta**: relleno `UITheme.GlassDeep` (`#0F121C` @ 80%
  alfa), rectángulo redondeado (9-slice), borde `UITheme.BorderStrong`
  (`#26334D`) de 1-2px efectivos en pantalla.
- **Tono uniforme en los 3 cuadrantes** mientras están bloqueados — un único
  color de relleno, sin variación por cuadrante. El color de bioma no
  aparece hasta el desbloqueo.
- **Icono de candado**: glifo geométrico plano (cuerpo redondeado + arco de
  grillete), tintado en runtime a `UITheme.TextSoft` (`#E9E9ED` @ 70%) sobre
  el `GlassDeep` — contraste >7:1 (AAA).
- **Etiqueta "Piso N"**: TMP existente (`MainFont SDF`), `UITheme.SizeLabel`
  (15), `UITheme.Text`. Sin asset nuevo.

### Feedback de toque en zona bloqueada

El texto "Desbloquea en Piso N" (patrón #28) es informativo, no de combate —
no debe heredar el ámbar de daño del patrón #11 (ese ámbar ya significa
"daño recibido" en todo el HUD). Color: `UITheme.TextSoft`/blanco neutro, sin
color de estado.

### Transición de desbloqueo (capa visual sobre el pop ya especificado)

1. La silueta (`GlassDeep` + candado + etiqueta) hace fade-out de alfa en
   paralelo al pop de escala (0.4→1.08→1.0, ya definido en Transitions).
2. En el pico del pop (rebote a 1.08), un flash breve y de baja intensidad
   del acento de bioma correspondiente (ver paleta abajo) ilumina el
   borde/relleno un instante antes de asentarse.
3. Los edificios reales del cuadrante (mismo `Square_White` + tinte que ya
   usan Farm/Cantina/etc.) aparecen a color completo en el mismo instante.
4. Duración exacta y curva quedan, como ya anota Transitions & Animations,
   para Fase 31 (Polish).

### Paleta por cuadrante (solo para el flash de desbloqueo, no arte de fondo permanente)

| Cuadrante | Piso | Bioma que anticipa (Fase 28) | Acento (solo flash) |
|---|---|---|---|
| Este | 5 | Goblin (pisos 1-5) | `#6E7F4A` — musgo/oliva desaturado |
| Sur | 10 | Minas (pisos 6-10) | `#8C5A3A` — óxido/cobre desaturado (no reskinea `Building_Farm2`, que conserva su verde) |
| Oeste | 15 | Cripta (pisos 11-15) | `#52625F` — pizarra-verdiazul desaturado |

Contexto de uso deliberadamente distinto al de la paleta de Decretos
(`DecreeHeal #43A65F`, `DecreeFocus #DF911A`, `DecreeRegroup #2A92BB`,
`DecreeRetreat #972527`): destello ambiental de un solo disparo en el mundo
de la base frente a un HUD de combate persistente y accionable — nunca
coinciden en pantalla. Adicionalmente, Este y Oeste están claramente
desaturados frente a cualquier color de Decreto; Sur (`#8C5A3A`, óxido) es
el caso más próximo en tono/saturación a `DecreeRetreat`, pero al no
coincidir nunca en pantalla el riesgo de confusión es igualmente bajo.

### Verificación — independencia del color (estado bloqueado)

- **Forma** (rectángulo redondeado vidrioso, distinto de un edificio sólido) — canal 1.
- **Icono** (candado reconocible incluso en escala de grises) — canal 2.
- **Texto** ("Piso N", literal) — canal 3.
- **Color** (tono neutro uniforme) — puramente decorativo, no aporta información nueva.

Los 3 cuadrantes bloqueados comparten el mismo tono neutro a propósito — la
diferencia entre Este/Sur/Oeste la da la posición (layout ya fijado) + el
texto, nunca el color.

### Asset Manifest

Convención de nombres verificada en disco: PascalCase con guion bajo entre
categoría/nombre/variante (`Hero_Aaron_2Star.png`, `UI_Rounded.png`,
`Square_White.png`). Ambos assets nuevos son máscaras planas de un solo
color (blanco + alfa), tintadas en runtime — cero material o shader nuevo,
mismo archivo sirve para bloqueado/desbloqueado y los 3 cuadrantes vía
`.color`.

| Asset | Ruta | Resolución | Formato / import | Uso |
|---|---|---|---|---|
| `Env_QuadrantVeil.png` | `Assets/_EndlessPull/Sprites/Environment/` | 128×128 px | PNG-32 RGBA, Sprite Mode: Single, 9-slice (borde 24px/lado), sRGB, sin mipmaps, `spritePixelsToUnits: 32`, compresión igual que assets existentes (calidad 50, máx. 2048) | `SpriteRenderer` en el `GameObject` del cuadrante bloqueado; color runtime = `UITheme.GlassDeep`; escala vía `m_Size` según footprint del cuadrante |
| `Env_IconLock.png` | `Assets/_EndlessPull/Sprites/Environment/` | 96×96 px | PNG-32 RGBA, Sprite Mode: Single (sin 9-slice), sRGB, sin mipmaps, `spritePixelsToUnits: 32` | `SpriteRenderer` hijo del veil, centrado sobre la etiqueta "Piso N"; color runtime = `UITheme.TextSoft`; mismo asset para los 3 cuadrantes |

Sin asset nuevo (reutilizan componentes/tokens ya existentes): etiqueta
"Piso N" (TMP), texto flotante "Desbloquea en Piso N" (reutiliza
`DamageTextManager`, patrón #11, con color `TextSoft` en vez de ámbar), borde
del veil (baked dentro del propio `Env_QuadrantVeil.png`, no es sprite
separado), edificios ya revelados (sin cambio, incluido `Building_Farm2`).

---

## States & Variants

| Estado / Variante | Disparador | Qué cambia |
|---|---|---|
| Inicial (solo hub) | Piso 1-4 | Solo hub visible en detalle; Este/Sur/Oeste como silueta bloqueada |
| Cuadrante Este desbloqueado | Piso ≥5 | Este pasa a detalle completo; Sur/Oeste siguen bloqueados |
| Cuadrante Sur desbloqueado | Piso ≥10 | Sur pasa a detalle completo (incluye `Building_Farm2`); Oeste sigue bloqueado |
| Cuadrante Oeste desbloqueado | Piso ≥15 | Los 4 cuadrantes en detalle completo — estado final de esta spec |
| Transición de desbloqueo | Justo al cruzar piso 5/10/15 | Animación de revelado de un solo disparo (ver Transitions), no repite en cargas posteriores del save |
| Toque en zona bloqueada | Tap/clic sobre silueta | Mensaje "Desbloquea en Piso N", sin abrir panel ni navegar |

---

## Interaction Map

Mapeando para: Mouse (PC) + Touch (móvil) — únicos controles del proyecto,
sin gamepad (`technical-preferences.md`).

| Acción del jugador | Input | Feedback inmediato | Resultado |
|---|---|---|---|
| Tocar/clicar zona bloqueada | Tap / clic izq. | Texto flotante "Desbloquea en Piso N" | Ninguno (no navega) |
| Tocar/clicar edificio desbloqueado | Tap / clic izq. | Micro-escala (patrón #6) | Abre `BuildingInspectUI` (patrón #1), sin cambio |
| Cruzar el Portal al confirmar Torre | Confirmación en `SquadManagementUI` | Escuadra se anima cruzando el Portal + etiqueta "A la Torre" | Cámara viaja a la arena (`CameraDirector.GoToArena`), sin cambio |
| Cruzar el Portal al confirmar Expedición | Confirmación en `SquadManagementUI` (modo expedición) | **(nuevo)** Escuadra se anima cruzando el Portal + etiqueta "A Expedición" | Escuadra "desaparece" tras el Portal en vez de no moverse nunca |
| Volver de expedición de recursos | Automático al completarse el timer | **(nuevo)** Escuadra reaparece en el Portal, igual que el recall de Torre | Recursos añadidos, héroes visibles otra vez |
| Cruzar piso 5/10/15 por primera vez | Automático (victoria de piso) | Transición de desbloqueo de cuadrante (una vez) | Cuadrante pasa a detalle completo |

---

## Events Fired

| Player Action | Event Fired | Payload / Data |
|---|---|---|
| Tocar zona bloqueada | `ZoneLockedTapped` | `zoneId`, `requiredFloor` |
| Confirmar salida a Expedición | `ExpeditionDeparted` (nuevo) | `expeditionType` |
| Vuelta de Expedición completada | `ExpeditionReturned` (nuevo) | `expeditionType` |
| Cruzar piso 5/10/15 (primera vez) | `QuadrantUnlocked` (nuevo) | `quadrantId`, `floor` |
| Tocar/clicar edificio desbloqueado | ninguno nuevo | ya cubierto por `BuildingInspectUI` |

**Estado persistente afectado**: `QuadrantUnlocked` y `ExpeditionDeparted`/
`ExpeditionReturned` tocan datos que hoy no existen en el save — necesitan
atención explícita de arquitectura:
- Flag de "animación de desbloqueo ya reproducida" por cuadrante (para no
  repetirla en cada carga).
- Posición/estado visual de la escuadra durante una expedición de recursos
  (hoy `ResourceExpeditionManager` no mueve héroes en absoluto).

---

## Transitions & Animations

- **Aparición de la base**: fade-in estándar ya existente al volver de
  `MainMenuUI` — sin cambio.
- **Revelado de cuadrante** (dispara una vez, al cruzar piso 5/10/15): pop de
  escala (mismo criterio sin librería de tweening que el patrón #14,
  0.4→1.08→1.0) aplicado al fondo + edificios del cuadrante recién revelado.
  Duración exacta y VFX de acompañamiento quedan para Fase 31 (Polish) — aquí
  solo se fija el timing/estructura, no el detalle visual.
- **Cruce de Portal**: la escuadra ya se reposiciona instantáneamente hoy
  (`TeleportToGateway`); esta spec añade encima una etiqueta momentánea "A la
  Torre" / "A Expedición" (reutiliza patrón #11), sin cambiar el
  reposicionamiento en sí.
- **Reducción de movimiento**: el juego no tiene hoy una opción de
  "movimiento reducido" (no existe `design/accessibility-requirements.md`).
  El pop de revelado de cuadrante es la única animación nueva con posible
  intensidad — queda anotado en Accessibility/Open Questions.

---

## Data Requirements

| Dato | Sistema fuente | Lectura/Escritura | Notas |
|---|---|---|---|
| Piso actual / máximo superado | `WaveManager.CurrentFloor`/`HighestClearedFloor` | Lectura | Ya existe |
| Desbloqueo por edificio | `BaseBuilding.IsUnlocked` | Lectura | Ya existe |
| Flag "animación de cuadrante ya reproducida" | Nuevo (probable `SaveManager`) | Lectura/Escritura | Dato nuevo — evita repetir el pop en cada carga |
| Tipo de expedición en curso | `ResourceExpeditionManager` | Lectura | Ya existe |
| Visibilidad/posición de la escuadra durante expedición | Nuevo (extender `ResourceExpeditionManager` o `PartyManager`) | Lectura/Escritura | **Concern arquitectónico**: hoy la UI no necesita esto porque nada mueve a los héroes; al añadir el cruce de Portal, algún sistema debe decidir dónde "está" la escuadra mientras la expedición corre — no lo decide esta spec de UX, es decisión de `technical-director`/`gameplay-programmer` |

---

## Accessibility

No es prioridad para este proyecto (portfolio personal, sin requisito de
accesibilidad formal). Controles: solo Mouse (PC) y Touch (móvil) — sin
gamepad.

- **Independencia del color**: el estado "bloqueado" ya se comunica con
  silueta + icono de candado + texto ("Piso N"), no solo con un tinte de
  color — gratis por seguir el mismo criterio que el resto del proyecto
  (patrones #11, #16, #17), no exige trabajo extra.
- **Contraste**: silueta + candado + etiqueta usan los tokens de `UITheme` ya
  usados en el resto de la UI, sin ajuste especial.

---

## Localization Considerations

- Cadenas nuevas: "Desbloquea en Piso {N}", "A la Torre", "A Expedición",
  "Piso {N}" (etiqueta sobre silueta bloqueada).
- Más larga y layout-crítica: "Desbloquea en Piso {N}" — es texto flotante
  autoajustable (mismo patrón que `DamageTextManager`/`ScreenBanner`), no un
  botón de ancho fijo, así que la variación ES↔EN↔JA no rompe layout.
- Números: `{N}` es un piso (entero simple), sin formato de moneda/fecha que
  dependa de locale.
- Todas las cadenas nuevas deben pasar por `LocalizationManager` (patrón #18,
  localización reactiva) para no quedar fijas en español si el jugador
  cambia de idioma en caliente.

---

## Acceptance Criteria

- [ ] Al cargar la base con piso < 5, los cuadrantes Este/Sur/Oeste se
      muestran como silueta bloqueada con candado y etiqueta "Piso N"
      correcta (5/10/15).
- [ ] Tocar/clicar una silueta bloqueada muestra "Desbloquea en Piso N" en
      menos de 100ms, sin abrir ningún panel.
- [ ] Al superar el piso 5/10/15 por primera vez, el cuadrante
      correspondiente se revela con la animación una sola vez; recargar el
      save después no la repite.
- [ ] `Building_Farm2` aparece dentro del cuadrante Sur y es funcional
      (asignar trabajador, producir comida) en cuanto se desbloquea.
- [ ] Con los 4 cuadrantes desbloqueados (piso 15+), la cámara fija muestra
      el hub y los 3 cuadrantes completos sin recorte en la resolución
      mínima soportada.
- [ ] Todas las cadenas nuevas cambian de idioma correctamente al usar el
      selector ES/EN/JA sin reabrir la escena.
- [ ] Cruzar el Portal para expedición de recursos muestra "A Expedición" y
      la escuadra reaparece visible al completarse (no queda invisible
      indefinidamente).

---

## Open Questions

- Mapa de journey del jugador no existe (`design/player-journey.md`) — spec
  diseñada sin él, asumiendo contexto razonable.
- ~~Contenido exacto del Cuadrante Este (piso 5) y Oeste (piso 15)~~ —
  **resuelto**: Este → `Building_RestArea` (`BuildingType.RestArea`, "Zona
  de Descanso"; tipo ya implementado en `BaseBuilding.cs`, sin instancia en
  escena hasta ahora — cierra el hueco entre el tick de código y el mundo).
  Oeste → `Building_TrainingDummy2` ("Campo de Entrenamiento Avanzado"),
  segundo Campo de Entrenamiento: alivia la contención de jerarquía social
  entre los rasgos Diligente/Feroz, que ya compiten por el único campo
  existente. Ninguno de los dos requirió tocar `BaseBuilding.cs` — ambos
  son instancias nuevas de tipos ya existentes, mismo patrón que
  `Building_Farm2` en el cuadrante Sur.
- Cuadrante Norte queda reservado sin uso en esta spec — abierto para una
  futura expansión más allá de piso 15 si hiciera falta.
- Dónde/cómo se representa visualmente la escuadra mientras dura una
  expedición de recursos (¿oculta tras el Portal? ¿animación de espera?) —
  decisión de `technical-director`/`gameplay-programmer`, no de esta spec.
- Tier de accesibilidad: no definido, y explícitamente no es prioridad para
  este proyecto (portfolio personal).
