# Endless Pull — Estado y pendientes

Gacha autobattler con permadeath. Unity 6000.5.8f1, 2D. Arquitectura híbrida: C# + FSM para base y macro-lógica, ML-Agents/Sentis previsto para el micro-combate.

Última sesión: **24 ago 2026** (Fases 5, 6 y 7 cerradas y verificadas).

---

## Estructura

```
Assets/_EndlessPull/
  Scripts/
    HeroData.cs  HeroController.cs  HeroProgress.cs  HeroTrait.cs
    EnemyData.cs  EnemyController.cs
    IHealthOwner.cs  FloatingHealthBar.cs
    HeroSkill.cs  SaveManager.cs  SynthesisManager.cs
    MasterCommander.cs  GachaManager.cs  EconomyManager.cs  WaveManager.cs
    MasterHUD.cs  RosterUI.cs  BuildingUpgradeUI.cs  BaseBuilding.cs
  Prefabs/    Hero_Base  Enemy_Base  Building_TrainingArea  Building_Canteen
  ScriptableObjects/  12 HeroData (1★ Loki/Jenna/Brak, 2★ Aaron/Karon/Nix,
                      3★ Velvet/Cedric/Eol, 4★ Iselia/Vargas, 5★ Han Ysl) + Enemy_Goblin_Test
  Sprites/    Square_White  Square_Red
  Scenes/     Base.unity
```

---

## Fases cerradas y verificadas en Play

**Fase 1 — Héroe y base.** FSM `BaseIdle`/`BaseWander`, `HeroData` como ScriptableObject, permadeath por `Destroy`.

**Fase 2 — Combate bidireccional.** Enemigo con FSM propia, barras de vida flotantes por `SpriteRenderer` actualizadas por evento (`IHealthOwner.HealthChanged`), no por polling.

**Fase 3 — Gacha y UI.** Tirada ponderada 60/30/10, catálogo de 3 héroes, Canvas Overlay con TMP. Jitter de combate eliminado y medido frame a frame.

**Fase 4 — Expediciones y economía.** Gemas (300 inicial), pull a 100, oleadas por piso (`baseCount + floor - 1`, ×1.20 acumulativo), victoria/derrota, recompensas.

**Fase 5 — Idle progression, roster y mejora de edificios.** Nivel/EXP por héroe con bonus **por instancia**, rasgos (Trabajador, Glotón, Perezoso, Feroz), edificios interactivos con estado `Training`, materiales Madera/Hierro y mejora con coste creciente. `RosterPanel` (ScrollRect + `RectMask2D` + `VerticalLayoutGroup` + `ContentSizeFitter`) y `UpgradePanel` montados en `UI_Master`; `RosterUI` y `BuildingUpgradeUI` cuelgan de `Master`.

**Fase 6 — Recursos de combate, habilidad activa y persistencia.** Maná y fatiga por instancia en `HeroController`, `HeroSkill` como habilidad activa con coste y enfriamiento, y `SaveManager` con guardado JSON en `Application.persistentDataPath/savegame.json`.

**Fase 7 — Catálogo, moral y síntesis.** Doce héroes en el catálogo, tirada sin duplicados, moral con efectos de combate y sacrificio de unidades para dar EXP.

### Catálogo

| ★ | Héroes | HP / ATK / DEF |
|---|---|---|
| 1 | Loki, Jenna, Brak | 100/15/5 · 90/18/3 · 130/11/8 |
| 2 | Aaron, Karon, Nix | 160/24/8 · 140/30/6 · 180/20/12 |
| 3 | Velvet, Cedric, Eol | 240/38/12 · 310/28/18 · 220/45/9 |
| 4 | Iselia, Vargas | 380/55/20 · 460/48/26 |
| 5 | Han Ysl | 600/80/35 |

Pesos por rareza **52/28/13/5/2** — antes eran 60/30/10/0/0, que dejaba 4★ y 5★ inalcanzables. La tirada filtra el catálogo por los nombres de los héroes vivos, sortea la rareza **solo entre las que aún tienen alguien libre** y se bloquea sin cobrar cuando están todos.

### Moral

Arranca en 80, rango 0-100.

| | |
|---|---|
| Superar un piso | +10 a todo héroe vivo |
| Tick en Cantina o Zona de descanso | +5 |
| Caer por debajo del 25% de vida | -15, una sola vez por bajada |
| Ver morir a un aliado a menos de 5 unidades | -20 |
| Moral > 80 (Inspirado) | +10% ATK |
| Moral < 30 (Desmoralizado) | velocidad -20%, cadencia +0,5 s |

Las penalizaciones de moral y de fatiga se acumulan.

### Síntesis

`SynthesisManager` con un botón por fila en el roster: el primer clic marca el objetivo, el segundo sacrifica. El fodder se destruye para siempre y el objetivo recibe `estrellas × 30 × nivel` de EXP (un 5★ de nivel 1 son 150).

### Recursos de combate

| | Valor |
|---|---|
| Maná máximo (`HeroData.maxMP`) | 50 |
| Regeneración de maná | 2 MP/s fuera de combate, 1 MP/s en combate |
| Fatiga | 0-100, arranca en 0 |
| Fatiga al correr en combate | +2/s (solo en `CombatApproach`) |
| Fatiga por golpe recibido | +5 |
| Recuperación en `BaseIdle` | -5/s |
| Recuperación en Cantina / Zona de descanso | -8/s |
| Umbral de agotamiento | > 80 |
| Penalización al agotarse | velocidad -30%, enfriamiento de ataque +0,3 s |

Habilidad **Golpe Potente**: 20 MP, 5 s de enfriamiento, ×2 sobre el ataque efectivo (no sobre `baseAttack` a secas, así que el nivel y el rasgo cuentan). En `CombatAttack` sustituye al golpe básico cuando hay maná y el enfriamiento ha pasado; si no, sale el ataque de siempre.

### Persistencia

`savegame.json` guarda gemas, madera, hierro, piso, el nivel de cada edificio (por nombre de GameObject) y el roster completo: asset del héroe, nivel, EXP, vida, maná, fatiga y rasgo.

- **Carga** en `SaveManager.Start`, no en `Awake`: los edificios ya se han registrado en `BaseBuilding.All`. Los héroes de la escena se destruyen y el roster se reconstruye entero desde el fichero.
- **Autoguardado** tras invocar en el gacha, mejorar un edificio y terminar una expedición (ganada o perdida), más `OnApplicationQuit`, que en el editor salta al salir de Play.
- Sin fichero, la escena arranca como siempre y `Hero_Loki` sigue siendo el héroe inicial.

### Balance actual

| | HP | ATK | DEF | Speed |
|---|---|---|---|---|
| Loki 1★ | 100 | 15 | 5 | 2.5 |
| Aaron 2★ | 160 | 24 | 8 | 2.3 |
| Velvet 3★ | 240 | 38 | 12 | 2.8 |
| Goblin | 50 | 12 | 2 | 1.5 (cooldown 1.0s) |

Subida de nivel: +10% HP máx (acumulativo) y +2 ATK. `MaxEXP` ×1.5 por nivel (20 → 30 → 45).

---

## Verificación de la Fase 5 (24 ago 2026)

Todo comprobado en Play disparando el `onClick` real de cada botón, no los métodos:

- `Btn_Roster` → `RosterUI.Toggle`, `Btn_Close` → `RosterUI.Close`. Ambos como listeners persistentes.
- Roster con 4 héroes: rasgos distintos (dos Glotón, un Trabajador, un Feroz), nombre, estrellas, nivel, HP y estado. `EmptyLabel` se apaga con héroes vivos y el `ContentSizeFitter` da 258 px con 4 filas.
- `BuildingUpgradeUI` genera un botón por edificio en su `Start` (Campo de Entrenamiento y Cantina), rojo y no interactuable con 0 materiales.
- Expedición al piso 1: ganada, 2 enemigos, recompensa 20 madera / 10 hierro. Los botones pasan a verde e interactuables.
- Mejora del Campo de Entrenamiento: Nv.1 → Nv.2, EXP por tick 5 → 10, materiales 20/10 → 0/0, HUD y etiqueta del botón actualizados (`40M / 20H`), botón bloqueado otra vez.
- Consola sin errores.

Cambios de esta sesión: `RosterUI` usa `*` en vez de `★` (la fuente por defecto no lo tiene y llenaba la consola de avisos) y el `RosterPanel` bajó a 640 px de alto, porque a 720 se salía de pantalla en aspectos más anchos que 16:9.

---

## Verificación de la Fase 6 (24 ago 2026)

- **Habilidad:** `[Habilidad] Loki lanza Golpe Potente: 42 de daño (-20 MP, quedan 30/50)` con ATK 21. Enfriamiento arrancado y maná descontado.
- **Fatiga:** subió a 20,7 en un combate (correr + dos golpes encajados) y bajó sola a 3,1 descansando en la base.
- **Agotamiento:** con fatiga 100, velocidad 2,5 → 1,75 (exactamente -30%) y enfriamiento 1,00 → 1,30 s. A 75 no penaliza. El tope de 100 aguanta sumar 500.
- **Autoguardado:** tres disparadores comprobados por separado en la consola, cada uno desde su ruta real (`GachaManager.SummonAndSpawnHero`, `BaseBuilding.TryUpgrade`, `WaveManager.Report` vía `SaveManager.OnExpeditionChanged`).
- **Ciclo completo:** 4 héroes con nivel, EXP, vida, maná, fatiga y rasgo distintos → salir de Play → el JSON de disco recoge esos valores → volver a entrar (en pausa, para que no derivaran) → los 4 héroes se restauran uno a uno idénticos, incluida la vida máxima recalculada por nivel (146, 176, 110, 100). Recursos, piso y niveles de edificio también. HUD al día sin depender del orden de los `Start`.
- Consola sin errores.

Cambio de apoyo: el `RosterPanel` pasó a 1300 px de ancho porque la fila más larga posible (con maná, fatiga y `AGOTADO`) pide 1108 y el content solo daba 940.

---

## Verificación de la Fase 7 (24 ago 2026)

- **Sin duplicados:** 20 tiradas seguidas dejaron los 12 héroes del catálogo, 0 repetidos. Las 9 sobrantes se bloquearon y las gemas no se movieron (4200 → 4200).
- **Síntesis:** Karon Nv.2 (15/30 EXP) + Han Ysl 5★ Nv.1 → Karon Nv.5 (22/101). Roster de 12 a 11.
- **Inspirado:** Nix a moral 95, ATK 24 → 26.
- **Desmoralizado:** a moral 0, velocidad 3,000 → 2,400 (-20% exacto) y cadencia 1,00 → 1,50. A moral 31 deja de penalizar.
- **Crítico:** Brak a 30/157 PV, moral 90 → 75; el segundo golpe estando ya en crítico no vuelve a restar.
- **Radio de muerte aliada:** con la víctima en x=0, el aliado a 3 unidades perdió 20 (90 → 70) y el de 20 unidades no se enteró (95 → 95).
- **Roster:** moral visible por fila (`Mor 100 Inspirado`) y botón "Sintetizar" que pasa a "OBJETIVO" al pulsarlo. Etiqueta más ancha 924 px sobre 1294 disponibles.
- **Persistencia:** la moral viaja en el JSON con valores distintos por héroe (61, 80, 100).
- Compila sin errores.

Cambios de apoyo: el `RosterPanel` pasó a 1560 px para la columna de moral más el botón, y `RosterUI` ordena las filas por estrellas y nombre — `FindObjectsByType` no garantiza orden y, con botones en cada fila, las filas bailaban en cada refresco.

---

## Deuda conocida

- [ ] **La tecla Espacio nunca se ha verificado.** El Input System descarta teclado sin foco en la Game View. Solo está probada la ruta `HealAllHeroes`. Requiere prueba manual.
- [ ] **Softlock posible:** con 0 gemas y 0 héroes no hay forma de recuperarse. Hoy no se da porque `Hero_Loki` está fijo en la escena, pero con permadeath real hará falta un ingreso mínimo o un héroe gratis.
- [ ] **El héroe gana siempre.** Un Loki solo mata a un goblin en 4 golpes; el goblin necesita ~15. La amenaza real solo aparece con varios enemigos.
- [ ] **La cámara es fija.** Los enemigos aparecen en x≈6 y los edificios están en x≈-3.2; con más zonas la acción se saldrá de plano.
- [ ] **Los héroes entrenando pueden ignorar la oleada** hasta que los enemigos se acercan (`detectionRange` 6, spawn en x=6). Funciona por poco.
- [ ] **El roster no tiene identidad propia.** Al no haber duplicados, hoy el nombre del héroe basta como clave; en cuanto se permitan copias hará falta un id por unidad, tanto para el guardado como para el botón de síntesis.
- [ ] **La síntesis no pide confirmación.** Dos clics destruyen un héroe para siempre, sin deshacer. Con un 5★ eso duele.
- [ ] **Nada repuebla el catálogo.** Con 12 héroes y sin duplicados, el gacha se agota y las gemas dejan de tener uso. Hace falta más catálogo, o duplicados que sirvan de fodder.
- [ ] **La habilidad se lanza sola en cuanto hay maná.** No hay criterio táctico: gasta los 20 MP en el primer golpe disponible aunque el enemigo esté a punto de morir. Es justo la decisión que se quiere delegar en ML-Agents.
- [ ] **`ignore.conf` no se versiona en Git** (decisión explícita). Quien clone por GitHub no lo recibe.

---

## Siguientes fases posibles

- **Panel de gestión de base** — construir edificios nuevos, no solo mejorar.
- **Barra de maná y fatiga flotante** — hoy solo se ven en el roster; en combate no hay lectura visual.
- **Más habilidades** — `HeroSkill` es una sola por héroe y sale sola; falta una lista y un criterio de elección.
- **Cámara** — seguimiento o zoom que encuadre la acción.
- **ML-Agents** — no está instalado. `com.unity.ai.inference` (Sentis) sí. Es la pieza que falta para el micro-combate táctico previsto en la arquitectura.

---

## Recordatorios de trabajo

- Comentarios **solo en español**, `//` de 1-2 líneas, `[Tooltip]` para campos serializados, **nunca `[Header]`** ni XML docs.
- Ver `lecciones.md` antes de pelearse con MCP, compilación o verificación en Play.
