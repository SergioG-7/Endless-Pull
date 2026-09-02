# Roadmap de Produccion Endless Pull

## Fase 44 — COMPLETADA (2026-09-02)

Storage con tarjetas/paginación, grid de base uniforme, bug de reclamo de expedición, y
auditoría exhaustiva de localización JA. Las 8 secciones del directivo original quedaron
resueltas y verificadas (compilación limpia + Play Mode donde el entorno lo permitió).

Detalle completo de qué se tocó, por qué, y qué se verificó: ver
`production/session-state/active.md`, sesión "Fase 44 (Storage cards, grid de base, claim de
expedición, JA)". Incluye también la investigación post-Fase 44 del reporte "botón Ascender no
reacciona" (sin bug reproducible, flujo verificado de punta a punta con clic real simulado).

Sin directivo pendiente. Esperar siguiente fase del usuario antes de inventar trabajo nuevo.

## Fase 45

[DIRECTIVE: WORLD SPATIAL ISOLATION, CAMERA PIVOT, UI LOCALIZATION AUDIT & AUDIO REFINEMENT]
Actúa en el hilo principal como desarrollador senior de Unity C# y uGUI.
PROHIBIDO invocar subagentes (/team-\*). Respuestas breves y centradas exclusivamente en el código.

Aplica el siguiente conjunto de correcciones críticas y saneamiento del mundo:

---

1. Aislamiento Espacial de la Arena y Corrección de Cámara (CameraDirector.cs, WaveManager.cs, Base.unity):

- Desplazamiento Masivo de la Arena: La arena de combate está demasiado cerca y se asoma por la izquierda al alejar la vista en la base. Mueve todo el contenedor de la Torre/Arena (`Env_Arena`, spawners, muros) a una posición lejana en el plano mundial (ej. `X = 500f` o `X = 1000f`) para que sea físicamente imposible verla desde la cámara de la base en cualquier nivel de zoom.
- Centrado del Pivot de Cámara: Corrige la interpolación del zoom en `CameraDirector.cs`. El zoom debe pivotar matemáticamente sobre el centro de la base `(0, 0)`, evitando que la vista se desplace a la derecha y corte el lateral izquierdo del campamento.

2. Limpieza de Colliders en Tower Gateway y Cartel Residual (BaseManager.cs, Base.unity):

- Elimina cualquier script de edificio bloqueado (`LockedBuilding`), collider invisible o trigger residual que haya quedado bajo la zona morada de `Tower Gateway`.
- Toda el área del portal debe responder exclusivamente a la interacción de la Torre; hacer clic en el halo morado NO debe activar el sonido de error (`Audio_Error`) ni reportar que el edificio está bloqueado.

3. Localización de Candados y Tamaño de Placas de Héroe (BuildingLockVisual.cs, HeroWorldBadge.cs, Base.unity):

- Candados de Edificios en Escena: Elimina el texto fijo en inglés ("Unlocks at Floor X"). Conéctalo a `LocalizationManager` para que se traduzca y refresque dinámicamente con `OnLanguageChanged`:
  - ES: `"Desbloquea en Piso {0}"`
  - EN: `"Unlocks at Floor {0}"`
  - JA: `"フロア{0}で解放"`
- Placas Flotantes de Héroes en la Base: Aumenta el tamaño del `TextMeshPro` / `RectTransform` del cartelito flotante sobre cada héroe (`Nombre · Nv. X`) en un 25-30% para que se lea cómodamente desde la vista general sin zoom.

4. Auditoría de Localización en Taller de Alquimia y Almacén (AlchemyWorkshopUI.cs, ShopUI.cs / StorageUI.cs):

- Taller de Alquimia (Pestañas Superiores):
  - "Stone Forging" $\rightarrow$ ES: "Forja de Piedras" | EN: "Stone Forging" | JA: "覚醒石錬成"
  - "Forge & Repair" $\rightarrow$ ES: "Forjar y Reparar" | EN: "Forge & Repair" | JA: "鍛造・修理"
  - "Alchemy" $\rightarrow$ ES: "Alquimia" | EN: "Alchemy" | JA: "錬金術"
- Almacén (Storage):
  - Traducir cabecera: ES: "Almacén" | EN: "Storage" | JA: "倉庫".
  - Traducir botón inferior: "Close" $\rightarrow$ ES: "Cerrar" | EN: "Close" | JA: "閉じる" (o sustituir por `UIBuild.CloseButtonTopRight`).
  - Añadir fila superior de filtros por categoría: "Todos", "Consumibles", "Materiales", "Equipo" localizados a los 3 idiomas.

5. Ajuste del SFX del Yunque (AudioManager.cs):

- Reducir el tiempo de corte del sonido de crafteo (`CraftAnvil`): ajustar para que se detenga a los 1.3s - 1.5s de reproducción (máximo 1–2 golpes de martillo en vez de los 4 que trae el archivo).

6. Verificación:

- Compilar limpio vía Unity MCP (filtro `error CS` = 0 errores).
- Validar en Play Mode: comprobar que la arena no sea visible al alejar el zoom de la base, que el zoom esté centrado, que pulsar la zona morada del portal no dé error, que los candados salgan en japonés (`フロアXで解放`), que las pestañas del taller y el almacén estén 100% traducidas con filtros funcionales, y que el yunque solo golpee un par de veces.
