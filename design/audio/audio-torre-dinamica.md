# Audio Torre Dinámica — Dirección de Audio

---
**Estado**: Draft
**Fuente**: Dirección de `audio-director`, Fase 30 del roadmap ("Audio Dinámico")
**Fecha**: 2026-08-30
**Verificado por**: pendiente de revisión del usuario
**Estado de implementación**: No implementado — este documento es dirección, no código
---

## 1. Overview

Endless Pull es una auto-batalla de comandante (Pilar 1 "Comandar, no
controlar") en sesiones cortas y repetidas (5-10 min, referentes AFK Arena /
Guardian Tales). El audio no puede fatigar al jugador que vuelve varias veces
al día, y debe reforzar dos cosas por encima de todo: (1) que los 4 decretos
del Maestro se sientan como la única palanca real que tiene, y (2) que subir
la torre — piso a piso, bioma a bioma, jefe cada 5 pisos — se sienta cada vez
más pesado sin recurrir a volumen o densidad simplemente crecientes. Este
documento fija identidad sonora, dirección musical adaptativa, arquitectura
de eventos, mezcla y especificaciones de assets; no implementa código
(`AudioManager.cs` hoy solo cubre SFX de 2 canales, sin sistema de música) ni
escribe el evento-lista detallado de SFX (delegado a `sound-designer`).

**Decisiones ancla de este documento** (aprobadas por el usuario):
identidad orquestal-híbrida cálida; arquitectura de música híbrida
vertical+horizontal; disparador de tensión binario por presencia de jefe;
prioridad de mezcla Decretos > Aviso de Jefe > SFX de combate > Ambiente >
Música.

## 2. Identidad Sonora (Paleta General)

**Núcleo: orquestal-híbrida cálida.** Cuerdas y metales (sampleados de
calidad, no necesariamente grabación real) como columna vertebral de toda la
Torre — refuerzan la gravitas de "comandante" (Pilar 1) sin caer en el tono
sombrío-puro que rompería el ritmo mobile-casual de sesiones de 5-10 min.
Capas sintéticas ligeras existen solo como acento por bioma (nunca sustituyen
el núcleo orquestal), concentradas sobre todo en Cripta. Limpio por defecto
(clean, no distorsionado) — la distorsión se reserva como puntuación rara:
stingers de jefe y golpes críticos, nunca la textura base, precisamente para
que no fatigue en sesiones repetidas varias veces al día.

**Paleta por bioma** (alineada a los acentos visuales y arcos emocionales ya
fijados en `design/levels/torre-biomas.md`):

| Bioma (pisos) | Arco emocional | Textura orquestal | Acento sintético/color |
|---|---|---|---|
| Goblin (1-5) | Caótico → tensión creciente | Cuerdas pizzicato + percusión de madera tosca | Ninguno — bioma por defecto, sin sintético |
| Minas (6-10) | Claustrofóbico | Registro grave: cello/contrabajo, casi sin agudos | Resonancias metálicas sintéticas puntuales (óxido `#8C5A3A`) |
| Cripta (11-15) | Quietud amenazante | Cuerdas sostenidas muy escasas, silencios largos | Pad sintético frío + coro etéreo distante — el bioma con más presencia synth de los 4 |
| Templo (16-20+) | Solemnidad de poder | Metales (brass) solemnes + órgano/coro amplio | Ninguno — la textura más "grande", refuerza "poder sin techo" |

**Densidad**: sparse en exploración/piso normal, dense solo en presencia de
jefe — nunca dense por defecto, para no competir con los avisos de decreto.

**Referencias**: AFK Arena / Guardian Tales para el tono general "heroico
pero liviano, no fatigante" (ya citados como comparables en
`game-concept.md`); referencias de tracks concretas quedan como tarea de
`sound-designer` en la sección 8 — este documento fija dirección, no curación
de referencias finales.

## 3. Dirección Musical (BGM por bioma y estado)

**Arquitectura híbrida** (vertical dentro de un bioma, horizontal entre
biomas — ver decisión ancla):

- **Vertical (dentro del mismo bioma)**: cada bioma tiene un set de 2 stems
  en loop — *bed* (cama armónica, siempre sonando) y *rhythm/pulse*
  (percusión/pulso rítmico, siempre sonando). Un tercer stem, *boss*, se
  suma de forma aditiva (fade-in ~1.5s) solo mientras haya un jefe vivo en el
  piso — no reemplaza a bed+rhythm, se apila encima. Esto evita mantener
  pistas completas separadas para "calma" y "jefe": es la misma base con una
  capa más.
- **Horizontal (entre biomas)**: al cruzar el límite de franja (piso 6, 11,
  16 — ver `torre-biomas.md`), el set completo de stems (bed+rhythm+boss) se
  reemplaza por el del bioma siguiente vía crossfade. La ventana de crossfade
  se ancla al mismo framing de "bleed prop" ya diseñado: empieza en el piso
  N-1 (transición de salida), se intensifica en el piso N si es jefe de
  franja, y culmina en el piso N+1 con el bioma nuevo ya sonando en solitario.

**Stinger de reveal de jefe**: cue corto, no-loop, dispara en el instante del
VFX de reveal ya especificado por bioma (`torre-biomas.md`): patrón base para
Goblin, "pulso de luz + partícula" en Minas, "apertura de nicho con glifos"
en Cripta, "luz de altar que sube" en Templo. Suena una sola vez, layered
sobre el fade-in del stem *boss*, y dispara el ducking de música descrito en
la sección 4/5.

**Tabla de transiciones de estado**:

| Evento | Música |
|---|---|
| Piso normal (sin jefe) | bed + rhythm del bioma actual, loop continuo |
| Jefe aparece | + stem *boss* (fade-in 1.5s) + stinger de reveal one-shot |
| Jefe derrotado / piso superado | stem *boss* fade-out (~2s), vuelta a bed+rhythm; `SfxId.Victory` (SFX, ya existe) suena por encima |
| Cambio de bioma (piso 6/11/16) | crossfade horizontal completo, ventana de 2 pisos (bleed prop) |
| Retirada (`RetreatExpedition`) | fade-out corto y suave de toda la música de combate (no es fallo, no lleva puntuación punitiva) |
| Derrota total (squad wipe) | corte seco a `SfxId.Defeat` (SFX, ya existe), silencio breve antes de volver a menú/base |

**Nota Templo (piso 20+, sin techo)**: igual que el "ciclo de iluminación de
4 estados" resuelve el presupuesto visual infinito, el stem *boss* de Templo
no varía después del piso 20 — se reutiliza sin cambios en cada jefe
posterior (25, 30...). Consistente con la decisión de arte ya tomada: "la
repetición idéntica es intencional, refuerza el tema de poder sin techo".

## 4. Arquitectura de Eventos de Audio

**Prioridad de mezcla** (decisión ancla, de más a menos crítico):
1. **Decretos** (Curar/Enfocar/Reagrupar/Retirada) — la única entrada directa
   del jugador (Pilar 1); nunca puede quedar enmascarado.
2. **Aviso de jefe** (stinger de reveal) — evento raro (cada 5 pisos), merece
   interrumpir brevemente pero no compite con decretos.
3. **SFX de combate** (golpe, flecha, magia, crítico, impacto) — debe leerse
   con claridad pero nunca tapar 1 o 2.
4. **Ambiente** (bioma, drone/textura de fondo) — presencia sutil.
5. **Música** (stems BGM) — la capa más baja; se agacha bajo todo lo demás.

**Reglas de ducking**:
- Al disparar cualquier decreto: duck momentáneo (~200ms, -6 a -8 dB) de
  música y ambiente. Es el ducking más agresivo del sistema porque ocurre
  varias veces por combate y es la acción central del jugador.
- Al disparar el stinger de aviso de jefe: duck de música únicamente (no de
  SFX de combate, para no cortar el feedback de golpes en curso).
- SFX de combate no duckea nada por debajo de él — conviven con música/
  ambiente vía jerarquía de volumen (sección 5), no vía ducking activo.

**Triggers concretos por evento**:

| Trigger (fuente) | Efecto de audio |
|---|---|
| `MasterCommander.HealParty/FocusFire/Regroup/Retreat()` | SFX de decreto (prioridad 1) + duck de música/ambiente |
| Golpe/flecha/magia/crítico/impacto en combate | SFX de combate (prioridad 3), posicional en mundo |
| `WaveManager` — bandera de jefe vivo (nueva, no existe hoy) | Toggle aditivo del stem *boss* + duck de música al activarse |
| VFX de reveal de jefe por bioma | Stinger de aviso one-shot (prioridad 2) |
| Índice de piso actual → franja de bioma | Selección de set de stems (crossfade horizontal, sección 3) |
| `WaveManager.RetreatExpedition` / squad wipe | Fade-out o corte de música, según tabla de la sección 3 |

**Canales necesarios (gap sobre `AudioManager.cs` actual)**: el sistema hoy
solo tiene 2 canales (`UI`, `Combat`). Este documento requiere 2 canales
nuevos — `Music` y `Ambience` — con su propio volumen persistente
(mismo patrón que `uiVolume`/`combatVolume` ya implementado), para que el
ducking y la jerarquía de mezcla sean controlables sin tocar el volumen de
SFX. Implementación delegada a `gameplay-programmer`/`unity-specialist`
(sección 8).

### 4.1 Especificación Detallada de Eventos de SFX (`sound-designer`)

Cierra el gap de la sección 8: faltaban `SfxId` para Enfocar Objetivo,
Retirada, Crítico, Ascensión y Crafteo. Nuevos identificadores propuestos al
enum de `AudioManager.cs` (implementación delegada, sin tocar código aquí):
`DecreeFocusFire`, `DecreeRetreat`, `Critical`, `Ascension`, `CraftSuccess`.

**Decisión de arquitectura — `Critical`**: no es un clip por arma. Es una
capa de acento corta que se dispara *encima* del hit base (`MeleeHit`/
`ArrowShot`/`MagicBolt`/`Impact`) cuando el golpe es crítico, evitando 3
assets redundantes por arma. Es, además, el único SFX de combate que puede
llevar la distorsión reservada para puntuación rara (sección 2).

| `SfxId` | Trigger | Prioridad mezcla | Volumen (dentro de su capa LUFS, sección 5) | Pitch var. | Espacialización | Variantes / cooldown |
|---|---|---|---|---|---|---|
| `DecreeHeal` (existente) | `HealParty()` | 1 | techo de capa Decretos | ±3% jitter | 2D no posicional | 1 variante (cd 10s la protege de fatiga) |
| `DecreeFocusFire` (nuevo) | `FocusFire()` | 1 | techo de capa Decretos | ±4% jitter | 2D no posicional | 2 variantes round-robin — el decreto más usado (hasta 5x/combate, cd 8s), necesita variación |
| `DecreeRegroup` (existente) | `Regroup()` | 1 | techo de capa Decretos | ±2% jitter | 2D no posicional | 1 variante (cd 12s, el más raro de los 3 ofensivos) |
| `DecreeRetreat` (nuevo) | `Retreat()`, antes de vaciar la escuadra desplegada | 1 | techo de capa Decretos | sin jitter (evento único por piso) | 2D no posicional | 1 variante — layered bajo el fade-out de música de la sección 3 |
| `MeleeHit` (existente) | golpe cuerpo a cuerpo | 3 | base de capa Combate | ±6% | pan estéreo por posición X | ver mitigación Minas más abajo |
| `ArrowShot` (existente) | disparo de flecha | 3 | base de capa Combate | ±5% | pan estéreo por posición X | sin cambios |
| `MagicBolt` (existente) | proyectil mágico | 3 | base de capa Combate | ±5% | pan estéreo por posición X | sin cambios |
| `Impact` (existente) | impacto genérico (aterrizaje flecha/magia) | 3 | base de capa Combate | ±6% | pan estéreo por posición X | sin cambios |
| `Critical` (nuevo, acento) | flag de crítico en cualquier golpe, layered sobre el hit base | 3 (mismo tier que combate, nunca duckea) | +2 a +3 dB sobre el hit base, sin salir del techo de capa Combate | ±8% (el más variado, para no sonar repetitivo pese a ser frecuente) | 2D, sigue al hit base | 3 variantes round-robin |
| `UiClick` (existente) | click genérico de botón | canal UI, fuera de la jerarquía 1-5 | volumen UI actual | ±3% | 2D | ⚠️ pendiente — ver nota de conflicto abajo |
| `UiOpen`/`UiClose` (existente) | apertura/cierre de modal | canal UI | volumen UI actual | sin jitter | 2D | sin cambios |
| `Ascension` (nuevo) | confirmación de ascensión de héroe | canal UI/meta, evento raro | el más prominente del canal UI — momento de recompensa | sin jitter (evento único, se saborea) | 2D | 1 variante, ~1.0-1.2s, swell orquestal cálido con acento de coro/brass, coherente con la identidad "orquestal-híbrida cálida" del núcleo (sección 2) en vez de un genérico "level up" de UI |
| `CraftSuccess` (nuevo) | crafteo/mejora completada | canal UI, evento frecuente | discreto, por debajo de `Ascension` — lo mundano no compite con el hito | ±4% | 2D | 2 variantes round-robin — clink mecánico corto + chime suave, sin capa orquestal (reservada a Ascensión) |
| `Victory`/`Defeat` (existentes) | fin de combate | según tabla sección 3 | sin cambios | sin cambios | 2D | sin cambios |

**Nota de conflicto — pendiente para `gameplay-programmer` (no resuelta
aquí)**: `HookSceneButtons()` en `AudioManager.cs` engancha `UiClick` a
*todos* los `Button` de la escena, incluidos los botones táctiles de decreto
(`MasterActionBar`, Fase 29). Esto apilaría `UiClick` + `DecreeX` en el mismo
frame al pulsar un decreto por touch. Queda documentado como hallazgo, sin
decisión de mezcla ni de código tomada por este documento.

**Reglas de ducking — extensión** (no contradice la sección 4 base):
- `Critical` es parte de la capa 3 (SFX de combate) a todos los efectos de
  ducking — no dispara ducking propio, igual que el resto de combate.
- `Ascension` y `CraftSuccess` viven en el canal UI existente, fuera de la
  jerarquía de 5 capas (igual que `UiClick`/`UiOpen` hoy) — eventos de
  meta-progreso fuera de combate, no compiten con la prioridad de combate/
  decretos.

## 5. Estrategia de Mezcla

**Jerarquía de volumen relativo** (de más a menos prominente, LUFS integrado
objetivo — a validar en implementación real, cifras de partida):

| Capa | LUFS integrado objetivo | True peak |
|---|---|---|
| Decretos (SFX) | -9 a -8 LUFS | -1 dBTP |
| Aviso de jefe (stinger) | -9 a -8 LUFS | -1 dBTP |
| SFX de combate | -12 LUFS | -1 dBTP |
| Ambiente | -20 LUFS | -1 dBTP |
| Música (stems BGM) | -18 a -16 LUFS | -1 dBTP |

**Reglas espaciales**: la cámara de combate es fija (arena 2D fija, sin
exploración — `combate-de-torre.md`), así que no se necesita paneo 3D
completo. Recomendación: SFX de combate (golpe, flecha, magia) usan paneo
estéreo simple según posición X en pantalla, no `spatialBlend` 3D — el pool
de `AudioSource` en `AudioManager.cs` ya fija `spatialBlend = 0f`, mantenerlo
así y resolver el paneo por `AudioSource.pan` si se decide añadirlo, sin
tocar el pooling. Música, ambiente y UI son siempre 2D no posicionales.

**Balance de frecuencia — mitigación decidida (`sound-designer`)**: el
bioma Minas concentra su textura orquestal en registro grave (cello/
contrabajo) para reforzar lo claustrofóbico (sección 2), pero el SFX
sintetizado `MeleeHit` en `AudioManager.cs` también vive en un rango grave
con ruido (220→90 Hz). Solución de contenido, no de mixer (dentro del
mandato de `sound-designer`, sin tocar ducking ni EQ del motor): variante
`sfx_combat_sword_hit_mines_01`, con el mismo carácter percusivo pero el
sweep desplazado a 320→150 Hz — sale de la zona donde vive el cello/
contrabajo del bed de Minas sin cambiar el timbre reconocible del golpe. Se
usa solo mientras el piso activo está en la franja Minas (6-10). Compatible
y complementaria (no sustituye) al recorte suave de EQ en 150-300 Hz sobre
el stem de Minas que puede aplicar `technical-artist` en la mezcla del
motor — ambas mitigaciones pueden convivir. Gap de código nuevo (sumar a la
sección 8): `AudioManager` necesita conocer la franja de bioma activa para
elegir la variante correcta de `MeleeHit`.

**Regla dura de audibilidad**: el jugador debe poder escuchar siempre los
decretos y el aviso de jefe, incluso en el peor caso de solape (jefe +
crítico + decreto simultáneos). Esto es lo que justifica la prioridad de
mezcla fija en la sección 4 — no es una preferencia estética, es un
requisito de legibilidad de Pilar 1.

### 5.1 Arquitectura de Mixer (`technical-artist`)

Hoy `AudioManager.cs` no usa ningún `AudioMixer` — el pool de `AudioSource`
reproduce clips con volumen lineal directo (`uiVolume`/`combatVolume`), sin
mezcla real. La regla dura de audibilidad y el ducking de la sección 4
requieren atenuar dinámicamente pistas que ya están sonando (los stems de
música en loop) sin detenerlas — esto no es viable con el patrón lineal
actual, hace falta un `AudioMixer`.

**Asset propuesto**: un único `AudioMixer`
(`Assets/_EndlessPull/Audio/EndlessPullMixer.mixer`) con esta jerarquía de
grupos, mapeada 1:1 a las 5 capas de prioridad de la sección 4 más el canal
UI existente (fuera de jerarquía, como ya lo describe la sección 4.1):

```
Master
├─ Decrees        (prioridad 1)
├─ BossAlert      (prioridad 2)
├─ CombatSFX      (prioridad 3)
├─ Ambience       (prioridad 4)
├─ Music          (prioridad 5)
│  ├─ MusicBed
│  ├─ MusicRhythm
│  └─ MusicBoss
└─ UI             (fuera de jerarquía, canal actual)
```

- `Decrees`/`BossAlert`/`CombatSFX`/`UI`: alimentados por el pool de
  `AudioSource` ya existente. Cambio mínimo: en `BuildPool()`, asignar
  `outputAudioMixerGroup` según `ChannelOf(id)` extendido con las nuevas
  categorías. No cambia el patrón de pooling ni el volumen lineal por canal
  — solo enruta la salida.
- `Music` (3 hijos) y `Ambience`: `AudioSource` dedicados y persistentes (no
  del pool, porque son loops continuos, no one-shots) — uno por stem de
  música, uno para el ambiente del bioma activo. El hijo `MusicBoss` es el
  único que se activa/desactiva (fade-in/out aditivo) según la bandera de
  jefe vivo (sección 8).

**Ducking vía parámetros expuestos, no snapshots**: se exponen 2 parámetros
float en dB — `MusicDuck` y `AmbienceDuck` — sobre los grupos
`Music`/`Ambience`, animados directamente (`AudioMixer.SetFloat` en
corrutina) en vez de `AudioMixerSnapshot.TransitionTo`. Motivo: los
decretos pueden dispararse hasta 5 veces por combate con cooldowns tan
cortos como 8s — una cola de snapshots se pisaría o quedaría en estado
inconsistente si dos ducks se solapan; un valor float re-disparable (si ya
está agachado, extiende el temporizador en vez de encolar una transición
nueva) es más robusto. Además el doc pide dos composiciones de duck
distintas (decreto = Music+Ambience, aviso de jefe = solo Music) — con
parámetros float independientes por grupo basta una función
`DuckGroup(param, dB, attack, hold, release)` reutilizada para ambos casos,
en vez de 2 snapshots completos.

- Decreto disparado → `DuckGroup("MusicDuck", -8dB, ...)` +
  `DuckGroup("AmbienceDuck", -8dB, ...)` (coincide con "~200ms, -6 a -8dB"
  de la sección 4).
- Aviso de jefe → solo `DuckGroup("MusicDuck", ...)`, sin tocar
  `AmbienceDuck` ni `CombatSFX` (la sección 4 ya lo exige).

**Simplificación propuesta sobre el gap 4 original de la sección 8**: no
hace falta un hook nuevo de "decreto disparado" desde `MasterCommander`.
`AudioManager.PlayInternal()` ya se ejecuta en cada `SfxId.DecreeX`
existente (así suena hoy el SFX) — el duck puede dispararse ahí mismo
cuando el `SfxId` reproducido es de prioridad Decretos, sin tocar
`MasterCommander.cs`. Ver revisión del gap 4 en la sección 8.

## 6. Diseño de Audio Adaptativo

**Disparador primario de tensión: binario, por presencia de jefe.** No hay
un tercer estado intermedio de tensión — la música pasa de "piso normal"
(bed+rhythm) a "jefe presente" (+ stem *boss*) y vuelve, sin gradientes por
número de enemigos vivos ni por vida de escuadra. Esta decisión refleja
exactamente la gráfica de pacing ya documentada en `torre-biomas.md`: los
picos de intensidad son los pisos de jefe (5/10/15/20+), el resto de cada
franja es una rampa continua sin valle real de dificultad — el binario de
audio no inventa un estado que el diseño de dificultad no tiene.

**Disparador secundario: franja de bioma.** El piso actual determina el set
de stems activo (Goblin 1-5, Minas 6-10, Cripta 11-15, Templo 16-20+
repitiendo). Ventana de crossfade anclada al framing de bleed-prop ya
diseñado (sección 3).

**Disparador de decreto**: cualquier llamada a `MasterCommander` dispara SFX
de decreto a prioridad máxima + ducking momentáneo de música/ambiente
(sección 4/5). Es el disparador más frecuente del sistema (varias veces por
combate) y el que más directamente sirve al Pilar 1.

**Disparador de fin de combate**: victoria, derrota o retirada, cada uno con
su propia resolución musical (tabla de la sección 3).

**Escalabilidad sin techo (Templo, piso 20+)**: el stem *boss* de Templo no
cambia después del piso 20 — se reutiliza en cada jefe posterior (25, 30...)
sin generar contenido nuevo, igual que el ciclo de iluminación visual de 4
estados ya resuelve el problema de presupuesto infinito en arte. El audio no
necesita un mecanismo de variación adicional: la repetición es intencional.

**Tuning knob diferido (no implementado en este alcance)**: una capa de
tensión intermedia disparada por número de enemigos vivos o tipo de enemigo
presente (tanque/chamán) quedó descartada a favor del binario simple —
documentada aquí como opción a revisar solo si, en playtesting, los pisos
sin jefe se sienten planos musicalmente. No es un compromiso de esta fase.

## 7. Especificaciones de Assets de Audio

**Formato y sample rate**: OGG Vorbis para música/ambiente (streaming,
coherente con build PC/WebGL/Android) y para SFX (coherente con
`sampleRate = 44100` ya usado por los clips sintéticos de `AudioManager.cs`).
44.1 kHz en todos los assets, mono para SFX no posicionales de UI, estéreo
para música/ambiente.

**Nomenclatura** (sigue la convención del proyecto
`[categoría]_[contexto]_[nombre]_[variante].[ext]`):

| Categoría | Ejemplo |
|---|---|
| Música — cama de bioma | `mus_explore_goblin_bed_loop.ogg` |
| Música — pulso rítmico de bioma | `mus_explore_goblin_rhythm_loop.ogg` |
| Música — stem de jefe (aditivo) | `mus_combat_goblin_boss_stem_loop.ogg` |
| Música — stinger de reveal de jefe | `mus_stinger_goblin_bossreveal_01.ogg` |
| Ambiente — bioma | `amb_env_mines_drone_loop.ogg` |
| SFX — decreto | `sfx_decree_focusfire_01.ogg` |
| SFX — combate | `sfx_combat_sword_crit_01.ogg` |
| SFX — combate, variante de bioma | `sfx_combat_sword_hit_mines_01.ogg` |
| SFX — UI/base | `sfx_ui_button_click_01.ogg` |
| SFX — meta-progreso | `sfx_meta_ascension_01.ogg`, `sfx_meta_craftsuccess_01.ogg` |

**Loudness**: ver tabla LUFS de la sección 5 — se repite aquí como
requisito de entrega, no solo de mezcla en tiempo real.

**Presupuesto de tamaño (propuesta de partida, no aprobada por
`technical-director`)**: dado el target PC/WebGL/Android con sesiones
cortas repetidas, presupuesto conservador — cada stem de música en loop
comprimido a calidad media (~q4-5 Vorbis) ronda 150-400 KB; stingers
<100 KB; SFX de combate/UI <30 KB cada uno. Set completo de la Torre (4
biomas × 3 stems + 4 stingers + 4 ambientes + set de SFX de combate/UI/base)
estimado por debajo de 15 MB — cifra a validar formalmente con
`technical-director` antes de producción.

### 7.1 Presupuesto de Memoria de Audio (`technical-artist`)

El proyecto no tiene presupuesto de memoria fijado
(`.claude/docs/technical-preferences.md` lo deja como "TO BE CONFIGURED").
Propuesta de partida para un roguelike 2D indie PC + Android/iOS, a validar
por `technical-director`:

| Categoría | Import Setting (Unity) | Justificación |
|---|---|---|
| Música (stems bed/rhythm/boss) y Ambiente | **Streaming** (`Load Type = Streaming`), Vorbis q4-5 | Loops largos; hasta 4 fuentes simultáneas en piso de jefe (bed+rhythm+boss+ambiente) — decodificarlos enteros en RAM no aporta nada, solo gastaría varias veces el tamaño de disco en memoria. Mismo ajuste en PC y móvil, sin overrides por plataforma. |
| SFX de combate/decretos/UI (<30KB cada uno) | **Decompress On Load** | Clips cortos y frecuentes, solapados en el pool de 12 `AudioSource`; descomprimir en memoria evita el coste de CPU de decodificar Vorbis en cada `Play()`, y el tamaño total es trivial. |
| Stingers de reveal de jefe (<100KB, evento raro cada 5 pisos) | **Compressed In Memory** | Ni tan frecuente como para mantenerlo descomprimido permanentemente, ni tan largo como para necesitar streaming. |

**Presupuesto de RAM de audio** (footprint decomprimido activo en tiempo de
ejecución, no tamaño de build):

| Plataforma | Presupuesto RAM de audio |
|---|---|
| PC | 48 MB |
| Móvil (Android/iOS) | 20 MB |

Esto es independiente y compatible con el presupuesto de **tamaño de
build** ya propuesto arriba (<15 MB) — ese es disco, este es RAM en tiempo
de ejecución; ambos deben validarse juntos con `technical-director`.

## 8. Dependencias y Preguntas Abiertas

**Depende de**: `combate-de-torre.md` (piso, oleada, jefe cada 5),
`decretos-del-comandante.md` (4 decretos como disparadores de prioridad 1),
`design/levels/torre-biomas.md` (4 biomas, VFX de reveal de jefe,
transiciones bleed-prop), `Assets/_EndlessPull/Scripts/AudioManager.cs`
(sistema de SFX de 2 canales ya implementado, base a extender).

**Gaps de código a delegar** (`gameplay-programmer` / `unity-specialist`),
revisados por `technical-artist` con arquitectura resuelta en secciones 5.1
y 7.1 — sigue sin implementarse código aquí:
1. Crear el `AudioMixer` de la sección 5.1 (`EndlessPullMixer.mixer`, 5
   grupos + `UI`) y enrutar el pool de `AudioSource` existente
   (`BuildPool()`) a sus grupos vía `outputAudioMixerGroup`, extendiendo
   `ChannelOf()`. Los 2 canales `Music`/`Ambience` ya no son solo 2 floats
   de volumen persistente (patrón `uiVolume`/`combatVolume`): son grupos de
   mixer con `AudioSource` dedicados no-pool (loops continuos) — ver 5.1.
2. Sistema de reproducción de música por stems con crossfade (horizontal) y
   fade aditivo (vertical) sobre los `AudioSource` dedicados de `Music` —
   no existe ningún `AudioSource` de música hoy.
3. Exponer en `WaveManager` un evento `BossStateChanged(bool alive)`
   (recomendado como evento, no propiedad polled) y que `AudioManager` se
   suscriba localizando `WaveManager` una vez (`FindFirstObjectByType`,
   mismo patrón que `HookSceneButtons()`), sin referencia serializada en
   ningún sentido. Dispara el toggle del stem *boss* (fade-in 1.5s /
   fade-out 2s) y el `DuckGroup("MusicDuck", ...)` del aviso de jefe
   (sección 5.1) desde el mismo punto de suscripción.
4. **Eliminado como gap independiente** (ver 5.1): el ducking de decreto no
   necesita un hook nuevo en `MasterCommander` — se dispara dentro de
   `AudioManager.PlayInternal()` cuando el `SfxId` reproducido es de
   prioridad Decretos, reutilizando la llamada a `Play()` que ya existe hoy.
5. Añadir 5 `SfxId` nuevos al enum (`DecreeFocusFire`, `DecreeRetreat`,
   `Critical`, `Ascension`, `CraftSuccess`) — ver especificación completa en
   sección 4.1. **Ver también el hallazgo 8 abajo antes de seguir
   ampliando el enum.**
6. Crear la utilidad estática sin estado `TowerBiome`
   (`Assets/_EndlessPull/Scripts/TowerBiome.cs`, `IndexForFloor(int floor)`)
   como única fuente de verdad de los límites de bioma (1-5/6-10/11-15/
   16-20+, hoy solo viven en `torre-biomas.md`, no en código). `AudioManager`
   se suscribe al `FloorChanged` de `WaveManager` (ya existe, no hace falta
   tocar `WaveManager` para esto) usando la misma instancia localizada en
   el gap 3, cachea `currentBiomeIndex = TowerBiome.IndexForFloor(floor)` y
   lo usa para seleccionar `sfx_combat_sword_hit_mines_01` en Minas
   (sección 5). Sin `WaveManager` en escena (menú, tests), se queda en el
   valor por defecto y usa la variante base — sin excepción ni referencia
   nula.
7. **⚠️ Sin resolver por este documento** — decidir si los botones táctiles
   de decreto (`MasterActionBar`) deben excluirse del hook genérico de
   `UiClick` en `HookSceneButtons()`, para evitar que `UiClick` y `DecreeX`
   suenen apilados en el mismo frame (hallazgo documentado en sección 4.1).
   **Confirmado por `unity-specialist`** (Fase 30, Paso 3): el diagnóstico
   es correcto contra el código real — `HookSceneButtons()` engancha
   `UiClick` a *todos* los `Button` de la escena sin excepción, incluidos
   los de `MasterActionBar`; recomienda excluir explícitamente esos botones
   del enganche genérico (por tag, por componente marcador, o por lista
   explícita) en vez de dejarlos duplicar sonido con su propio `DecreeX`.
   Sigue pendiente de decisión de `gameplay-programmer` en el Paso 4 — ni
   `sound-designer` ni `technical-artist` resuelven aquí la mezcla ni el
   código.
8. **Nuevo — hallazgo de `unity-specialist`, pendiente para
   `gameplay-programmer` (Paso 4), no implementado aquí**: el `enum SfxId`
   plano no puede representar variantes round-robin por sí mismo
   (`DecreeFocusFire` con 2 variantes, `Critical` con 3, `MeleeHit` con
   variante de bioma en Minas — sección 4.1/5) sin lógica ad-hoc dispersa
   por cada caso. `unity-specialist` recomienda migrar a un patrón
   `SfxDefinition` (`ScriptableObject` con lista de `AudioClip`, modo de
   selección round-robin/aleatorio, rango de pitch jitter y flags de
   espacialización por entrada) en vez de seguir haciendo crecer el enum
   con casos especiales. No es una decisión de este documento — decidir en
   el Paso 4 si se adopta ahora (antes de añadir los 5 `SfxId` del gap 5,
   para no migrar dos veces) o se pospone.

**Gaps de evento-lista — resuelto en esta revisión** (`sound-designer`):
el evento-lista completo de SFX (Enfocar Objetivo, Retirada, Crítico,
Ascensión, Crafteo) queda especificado en la sección 4.1, incluyendo
prioridad de mezcla, volumen, variación de pitch, espacialización y
variantes por evento.

**Preguntas abiertas**:
1. ¿Referencias de tracks reales concretas para calibrar el tono
   "orquestal-híbrida cálida" contra AFK Arena/Guardian Tales? Delegado a
   `sound-designer` como research previo a producción.
2. Presupuesto final de tamaño de build de audio (MB) no fijado por
   `technical-director` — la cifra de la sección 7 es una propuesta de
   partida, no un límite aprobado.
3. **[RESUELTO — Fase 30]** El riesgo de solape de frecuencia graves
   Minas/`MeleeHit` se resuelve con una variante de contenido
   (`sfx_combat_sword_hit_mines_01`, sweep desplazado a 320→150 Hz) descrita
   en la sección 5, complementaria al recorte de EQ que puede aplicar
   `technical-artist` sobre el stem de Minas.
4. El conflicto `UiClick`/`DecreeX` en botones táctiles (punto 7 de gaps de
   código, sección 4.1) queda como pregunta abierta para
   `gameplay-programmer` — no resuelto por `sound-designer`.

## 9. Accesibilidad de Audio (Auditoría)

**Fuente**: `accessibility-specialist`, Fase 30, Paso 2 del pipeline (en
paralelo con `sound-designer`). Auditoría contra código real
(`MasterCommander.cs`, `EnemyController.cs`, `WaveManager.cs`,
`HeroController.cs`, `DamageTextManager.cs`, `ScreenBanner.cs`,
`BossHealthBarUI.cs`, `MasterHUD.cs`), no solo contra este documento de
dirección. Objetivo de cumplimiento: WCAG 2.1 Nivel AA como referencia
transversal, más las Game Accessibility Guidelines (GAG) donde WCAG no
tiene un criterio directo para "estado de juego comunicado solo por audio".

### 9.1 Auditoría de eventos críticos — ¿todo audio tiene respaldo visual?

| Evento de audio | Prioridad de mezcla (§4) | Respaldo visual encontrado en código | Estado | Criterio |
|---|---|---|---|---|
| Decreto Curar Escuadra (`SfxId.DecreeHeal`) | 1 | `DamageTextManager.Show(..., "+N", ...)` por héroe curado — `MasterCommander.cs:1292` | PASS | GAG Básico — sin info solo-audio |
| Decreto Enfocar Objetivo (sin `SfxId` propio aún — gap de §8) | 1 | `DamageTextManager.Show(..., "¡ENFOCAR!", ...)` sobre el objetivo — `MasterCommander.cs:110` | PASS | GAG Básico |
| Decreto Reagruparse (`SfxId.DecreeRegroup`) | 1 | `DamageTextManager.Show(..., "¡DEFENSA!", ...)` por héroe — `MasterCommander.cs:132` | PASS | GAG Básico |
| Decreto Retirada (sin `SfxId` propio aún — gap de §8) | 1 | `DamageTextManager.Show(..., UI_RETREAT, ...)` por héroe, pintado antes de retirar — `MasterCommander.cs:62-63` | PASS | GAG Básico |
| Stinger de aviso de jefe (reveal) | 2 | `ScreenBanner.Show(UI_BOSS_ARRIVAL, ...)` + VFX de reveal por bioma (`torre-biomas.md`) + `BossHealthBarUI` se activa — `WaveManager.cs:379` | PASS | GAG Básico |
| SFX combate: golpe/flecha/magia/impacto | 3 | `DamageTextManager.ShowDamage()` (número flotante) — `HeroController.cs`, `EnemyController.cs` | PASS | GAG Básico |
| SFX combate: crítico | 3 | `DamageTextManager.Show(..., "¡CRÍTICO!", ...)` — `HeroController.cs:412,1165` | PASS | GAG Básico |
| SFX combate: esquiva | 3 | `DamageTextManager.ShowDodge()` — `"¡ESQUIVA!"` | PASS | GAG Básico |
| Telegraph de golpe de jefe ("pisotón", `EnemyController.StartWindup`) | No cubierto hoy por este documento — SFX pendiente de `sound-designer` | Tinte de sprite (`bossWindupTint`) + círculo de telegraph en el suelo + texto flotante `"¡CARGANDO PISOTÓN!"` — `EnemyController.cs:266-300` | GAP INVERSO | GAG Avanzado — el aviso es 100% visual hoy, sin SFX (solo `Debug.Log`, inaudible). No bloqueante por la regla "nada solo-audio", pero incompleto para quien mira otra parte de la pantalla. Recomendación: `sound-designer` añade un SFX de carga distintivo (no un duplicado del stinger de jefe) |
| Victoria de piso (`SfxId.Victory`) | 5 (música se agacha, no el SFX en sí) | `Report(ExpeditionState.Won, mensaje)` → evento `ExpeditionChanged` → texto de estado en `MasterHUD.cs:109` | PASS | GAG Básico |
| Derrota (`SfxId.Defeat`) | — | `Report(ExpeditionState.Lost, mensaje)` → mismo camino visual que Victoria | PASS | GAG Básico |

**Conclusión de la auditoría**: no existe hoy ningún estado de juego crítico
comunicado exclusivamente por audio. El único hallazgo real es el inverso
(telegraph de jefe 100% visual, sin SFX) — se documenta como recomendación
a `sound-designer`, no como bloqueante de este documento.

### 9.2 Requisitos de subtítulos / captions

**No hay diálogo hablado en el proyecto** (sin VO confirmado en el código:
`SpeechBubble.cs` y `LocalizedText.cs` son siempre texto, nunca audio), así
que SC 1.2.2 (Captions - Prerecorded) no aplica en su forma clásica. El
requisito de "captions" aquí se traduce a **texto de evento** para cada SFX
de prioridad 1 y 2 — mecanismo que el juego ya usa (`DamageTextManager`,
`ScreenBanner`) pero sin las opciones de tamaño/duración que exige el
estándar de accesibilidad de audio del estudio:

| Requisito | Estado actual (`DamageTextManager.cs`) | Acción |
|---|---|---|
| Al menos 3 tamaños de fuente para captions de evento | `fontSize` fijo (4, world-space), sin opción de usuario | GAP — exponer un multiplicador de tamaño en ajustes (Pequeño/Mediano/Grande), aplicado a `DamageTextManager.fontSize` y `ScreenBanner.fontSize` |
| Duración en pantalla ajustable | `lifetime` fijo en 0.8s para todo texto flotante (`DamageTextManager.cs:8`) | GAP — 0.8s es insuficiente para leer con calma frases largas como `"¡CARGANDO PISOTÓN!"`; exponer un multiplicador de duración (x1/x1.5/x2) en ajustes, análogo al de tamaño |
| Formato de texto | Frase corta en mayúsculas + color semántico (ya consistente: ámbar=enfocar, azul=defensa, rojo=peligro/jefe, verde=curación) | PASS — el color nunca es el único portador de información: siempre va con texto/icono textual, cumple SC 1.4.1 (Use of Color) |
| Identificación de hablante / descripción de fondo | N/A — no hay diálogo ni narrador de audio en el diseño actual | N/A |

**Recomendación de formato para futuros eventos con SFX nuevo** (Enfocar,
Retirada, Crítico, Ascensión, Crafteo — gap de §8 delegado a
`sound-designer`): todo `SfxId` nuevo de prioridad 1 o 2 debe nacer con su
texto de evento ya emparejado en el mismo commit, para no reabrir el gap
que esta auditoría acaba de cerrar en decretos y aviso de jefe.

### 9.3 Sensibilidad auditiva — riesgo de picos de volumen súbitos

La tabla de LUFS de la sección 5 fija Decretos y Aviso de Jefe en -9/-8
LUFS contra una base de música/ambiente en -20/-16 LUFS — un salto de
**~8-10 dB** cada vez que suenan, y los decretos son el evento más
frecuente del sistema (varias veces por combate, sección 4). Esto es un
riesgo real de sensibilidad auditiva (hiperacusia, sensibilidad sensorial,
uso con auriculares) aunque el true peak (-1 dBTP) evite el clipping.

- **Recomendación (no bloqueante para este documento, sí para
  implementación)**: la opción "reducir sonidos súbitos / normalizar audio"
  del estándar de Audio Accesibilidad del estudio debe aplicarse
  específicamente sobre la capa de Decretos + Aviso de Jefe cuando esté
  activada — comprimiendo el salto de LUFS en vez de solo bajar el volumen
  general, para no sacrificar la regla dura de audibilidad de la sección 5.
- **Frecuencia**: este documento no especifica el perfil de frecuencia de
  los SFX de decreto/aviso de jefe (delegado a `sound-designer`, sección 8).
  Recomendación: evitar picos de alta frecuencia (>4 kHz) sostenidos en
  estos dos eventos — son los que más fatigan y más disparan malestar en
  jugadores con hipersensibilidad auditiva, y no aportan nada a la
  identidad "orquestal-híbrida cálida" ya fijada en la sección 2.
- **Mono**: los SFX de decreto/aviso de jefe son 2D no posicionales
  (sección 5) — deben sonar correctamente en la opción de audio mono del
  estándar de Audio Accesibilidad del estudio sin perder información (no
  dependen de paneo estéreo para leerse).

### 9.4 Gaps de implementación a delegar

1. `sound-designer`: SFX de telegraph de golpe de jefe (§9.1) — hoy 100%
   visual, sin sonido.
2. `sound-designer`: todo `SfxId` nuevo de prioridad 1/2 (Enfocar, Retirada,
   Crítico, Ascensión, Crafteo) nace con su texto de evento emparejado
   (§9.2) — ya no hay gap hoy, pero no debe reabrirse.
3. `unity-ui-specialist`: exponer tamaño y duración configurables de
   `DamageTextManager`/`ScreenBanner` en ajustes (§9.2) — hoy son valores
   fijos de Inspector, no opciones de jugador.
4. `sound-designer`/`technical-artist`: mitigación de picos de volumen
   súbitos en Decretos + Aviso de Jefe bajo la opción de audio "sonidos
   súbitos" (§9.3), y perfil de frecuencia sin picos sostenidos >4 kHz en
   esos dos eventos.

## 10. Version History

| Fecha | Autor | Cambios |
|---|---|---|
| 2026-08-30 | Claude (audio-director, Fase 30) | Documento inicial — dirección de audio para BGM, SFX de combate y UI/base |
| 2026-08-30 | Claude (accessibility-specialist, Fase 30, Paso 2) | Añade sección 9 — auditoría de accesibilidad de audio contra código real, requisitos de captions y riesgo de sensibilidad auditiva |
| 2026-08-30 | Claude (sound-designer, Fase 30) | Añadida sección 4.1 (evento-lista completo de SFX: 5 `SfxId` nuevos, tabla de prioridad/volumen/pitch/espacialización); resuelta mitigación de solape Minas/`MeleeHit` (sección 5); cerrados gaps de evento-lista en sección 8; documentado como pendiente para `gameplay-programmer` el conflicto `UiClick`/decreto en botones táctiles |
| 2026-08-30 | Claude (technical-artist, Fase 30, Paso 3) | Añadidas secciones 5.1 (arquitectura de `AudioMixer`, ducking por parámetros expuestos) y 7.1 (presupuesto de memoria de audio PC/móvil, propuesta de partida); revisados los gaps de código de la sección 8 (mixer + `TowerBiome` + evento `BossStateChanged` de `WaveManager`, eliminado el gap de hook en `MasterCommander` por innecesario); añadidos 2 hallazgos de `unity-specialist` a la sección 8 como pendientes para `gameplay-programmer` (Paso 4): patrón `SfxDefinition` (ScriptableObject) para variantes round-robin en vez de seguir ampliando el enum `SfxId`, y confirmación del conflicto `UiClick`/botones de decreto en `HookSceneButtons()` |
