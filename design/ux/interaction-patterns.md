# Interaction Pattern Library

> **Status**: In Design
> **Author**: ux-designer (ingeniería inversa) + usuario
> **Last Updated**: 2026-08-27
> **Template**: Interaction Pattern Library

---

## Overview

Este catálogo se generó por ingeniería inversa a partir del código ya construido
(`Assets/_EndlessPull/Scripts/`, 27 scripts leídos), no como diseño previo a la
implementación — el juego ya existe como demo jugable antes de que existiera este
documento (mismo criterio que `design/gdd/game-concept.md`). Captura los patrones
de interacción que el proyecto YA usa de forma repetida, para que las pantallas
nuevas (empezando por Fase 26 del roadmap) los reutilicen en vez de reinventarlos,
y para que una pasada de consistencia de UI/HUD tenga una referencia contra la que
auditar.

26 patrones documentados en 5 categorías (Navigation, Input, Feedback, Data
Display, Modal/Overlay), más 10 huecos identificados — 2 de ellos directamente
relevantes para el trabajo de Fase 26 (desbloqueo de zonas de base).

---

## Pattern Catalog

| # | Patrón | Categoría | Usado en |
|---|--------|-----------|----------|
| 1 | Modal exclusivo con backdrop | Navigation | Casi todas las pantallas de gestión |
| 2 | Barra lateral colapsable con secciones | Navigation | `SideMenuUI` |
| 3 | Handoff encadenado entre modales | Navigation | `TowerPanelUI`, `ResourceExpeditionUI` → `SquadManagementUI` |
| 4 | Pestañas de filtro/categoría | Input | `RosterUI`, `SanctuaryUI` |
| 5 | Botón con estado de asequibilidad | Input | `SanctuaryUI`, `AlchemyWorkshopUI`, `CraftingUI`, `ShopUI`, `SummonAltarUI`, `QuestBoardUI`, `BuildingInspectUI`, `HeroDetailModal`, `HeroQuickCardUI` |
| 6 | Micro-escala al pulsar | Input | Todos los botones vía `UIBuild.Button` |
| 7 | Fila-botón con estado de toggle | Input | `SquadManagementUI`, `HeroDetailModal`, `QuestBoardUI`, `BuildingInspectUI` |
| 8 | Slider construido a mano | Input | `InGameMenuUI`, `MainMenuUI` |
| 9 | Selector de opción única en fila | Input | `InGameMenuUI`, `MainMenuUI` |
| 10 | Toque en mundo con radio de gracia y prioridad | Input | `WorldInteractionManager` |
| 11 | Texto flotante de combate | Feedback | `DamageTextManager` |
| 12 | Rótulo de aviso a pantalla completa | Feedback | `ScreenBanner` |
| 13 | Overlay de confirmación destructiva anidado | Feedback | `SanctuaryUI` (Síntesis) |
| 14 | Cofre de recompensa con animación de pop | Feedback | `TowerRewardUI` |
| 15 | Carta boca abajo con revelado | Feedback | `SummonAltarUI` |
| 16 | Barra de progreso con etiqueta flotante | Feedback | `RosterUI`, `HeroDetailModal`, `HeroQuickCardUI`, `BossHealthBarUI` |
| 17 | Disco de decreto con barrido de cooldown radial | Feedback | `MasterActionBar` |
| 18 | Localización reactiva | Feedback | 8 pantallas suscritas a `LanguageChanged` |
| 19 | Lista con pooling de widgets | Data Display | `RosterUI`, `SanctuaryUI` |
| 20 | Maestro-detalle (lista + vista previa) | Data Display | `SanctuaryUI` |
| 21 | Ficha de detalle a dos columnas | Data Display | `HeroDetailModal`, `HeroQuickCardUI` |
| 22 | Tarjeta de rejilla para elección forzada | Data Display | `SubclassSelectionUI` |
| 23 | Texto alineado por `<pos=%>` | Data Display | `AlchemyWorkshopUI` |
| 24 | Tarjeta compacta de operación | Data Display | `AlchemyWorkshopUI` |
| 25 | Panel de pausa con `Time.timeScale = 0` | Modal/Overlay | `InGameMenuUI`, `MainMenuUI` |
| 26 | Panel anidado dentro de otro panel | Modal/Overlay | `MainMenuUI` (opciones) |
| 27 | Silueta de zona bloqueada | Data Display | `design/ux/base-environment.md` (nuevo) |
| 28 | Feedback de toque en zona bloqueada | Feedback | `design/ux/base-environment.md` (nuevo) |

---

## Patterns

### 1. Modal exclusivo con backdrop

**Categoría**: Navigation
**Usado en**: `RosterUI`, `HeroDetailModal`, `SanctuaryUI`, `AlchemyWorkshopUI`, `CraftingUI`, `ShopUI`, `TowerPanelUI`, `QuestBoardUI`, `SubclassSelectionUI`, `EquipmentSelectModalUI`, `SummonAltarUI`, `BuildingInspectUI`, `InGameMenuUI`, `HeroQuickCardUI`, `ResourceExpeditionUI`, `SquadManagementUI`

**Descripción**: Un único gestor (`UIManager`) mantiene una lista de paneles registrados; `OpenExclusive(panel)` apaga todos los demás, enciende el backdrop oscuro (bloquea clics de fondo) y recoloca el panel en el centro de la zona segura del dispositivo.

**Especificación**:
- Solo un modal abierto a la vez; abrir uno cierra el anterior automáticamente.
- Sonido de apertura/cierre (`UiOpen`/`UiClose`) disparado centralmente al detectar el cambio, no por cada panel.
- Reposicionamiento respeta `Screen.safeArea` vía `CenterInSafeArea` (notch/gestos).
- Clic/tap fuera del panel NO cierra — cierre siempre por botón explícito.

**Cuándo usarlo**: Cualquier pantalla de gestión de la base a pantalla completa o casi completa.
**Cuándo NO usarlo**: HUD de combate persistente (`CombatHUD`, `BossHealthBarUI`, `MasterActionBar`) — deben convivir con el mundo, no taparlo.

---

### 2. Barra lateral colapsable con secciones

**Categoría**: Navigation
**Usado en**: `SideMenuUI`

**Descripción**: Cajón lateral plegable (arranca colapsado en móvil) que agrupa el acceso a 10 pantallas en tres secciones con cabecera (Gestión, Personal, Instalaciones).

**Especificación**:
- Respeta `Screen.safeArea`.
- Se esconde por completo durante combate (`MasterActionBar.ShouldShow == true`) y reaparece en la base — nunca conviven los dos sistemas de navegación.
- Toggle por botón "☰"; scroll vertical si el contenido no cabe.

**Cuándo usarlo**: Agrupar accesos de gestión de base que crecen con el tiempo.
**Cuándo NO usarlo**: En combate (ya excluido explícitamente por código).

---

### 3. Handoff encadenado entre modales

**Categoría**: Navigation
**Usado en**: `TowerPanelUI` → `SquadManagementUI.OpenForConfirm` → arranque de piso; `ResourceExpeditionUI` → `SquadManagementUI.OpenForConfirm` → arranque de expedición

**Descripción**: Un panel cierra y delega en otro pasándole una `Action` de callback (`pendingConfirmAction`) que solo se ejecuta si el jugador confirma en el segundo modal. `SquadManagementUI` sirve dos propósitos con el mismo panel: gestión libre y modo confirmación (botón extra "Confirmar" solo en ese modo).

**Especificación**:
- Si la escuadra está vacía al confirmar: mensaje inline en rojo (`UI_CONFIRM_NEED_HEROES`) en vez de bloquear el botón silenciosamente.

**Cuándo usarlo**: Cualquier acción que compromete recursos/tiempo (entrar a un piso, mandar expedición) donde el jugador debe poder revisar/ajustar antes de comprometerse.
**Cuándo NO usarlo**: Acciones triviales y reversibles (equipar una pieza, cambiar de pestaña).

---

### 4. Pestañas de filtro/categoría

**Categoría**: Input
**Usado en**: `RosterUI` (filtro por rareza), `SanctuaryUI` (Ascensión/Síntesis)

**Descripción**: Fila de botones tipo pestaña; la activa se tiñe con `UITheme.AccentPick` y borde `UITheme.Accent`, el resto queda transparente con borde `BorderStrong`.

**Especificación**:
- Input: clic/tap. Comportamiento radio (un solo estado activo), no checkbox.
- Feedback: color de fondo + color de borde + color de texto.

**Cuándo usarlo**: 3-6 categorías mutuamente excluyentes sobre una misma lista.
**Cuándo NO usarlo**: —

---

### 5. Botón de acción con estado de asequibilidad

**Categoría**: Input
**Usado en**: `SanctuaryUI`, `AlchemyWorkshopUI`, `CraftingUI`, `ShopUI`, `SummonAltarUI`, `QuestBoardUI`, `BuildingInspectUI`, `HeroDetailModal`, `HeroQuickCardUI`

**Descripción**: El mismo botón cambia `interactable`, color de fondo (acento si se puede pagar/hacer, `Neutral` si no) y color de texto (`Text` vs `TextFaint`) según si el jugador puede pagar el coste o cumple la condición.

**Especificación**:
- Se recalcula cada frame mientras el panel está abierto (`Update` → `Refresh`), porque los recursos cambian solos (granjas, talleres).

**Cuándo usarlo**: Cualquier acción con coste en recursos o requisito de estado.
**Cuándo NO usarlo**: Acciones sin condición (cerrar, cancelar) — mantienen siempre color neutro/interactable.

---

### 6. Micro-escala al pulsar

**Categoría**: Input
**Usado en**: Todos los botones creados vía `UIBuild.Button` (prácticamente toda la UI, se auto-adjunta)

**Descripción**: Al presionar, el `RectTransform` del botón escala a 0.95x; vuelve a 1x al soltar o al salir del área.

**Especificación**:
- `IPointerDownHandler`/`UpHandler`/`ExitHandler` — funciona igual con mouse y touch, no es hover-specific.
- Pointer exit cubre arrastrar el dedo fuera sin soltar.
- Audio: `SfxId.UiClick` disparado centralmente desde `UIBuild.Button`, incluso en botones sin acción real.

**Cuándo usarlo**: Cualquier botón nuevo debería pasar por `UIBuild.Button` para heredar esto gratis.
**Cuándo NO usarlo**: —

---

### 7. Fila-botón con estado de toggle

**Categoría**: Input
**Usado en**: `SquadManagementUI` (Torre/Recolección), `HeroDetailModal` (Bloquear/Escuadra), `QuestBoardUI` (Cobrar/Cobrado), `BuildingInspectUI` (Asignar/Desasignar trabajador)

**Descripción**: Botón que representa un estado binario — color de acento si está activo, `Neutral` si no, texto cambia (p. ej. "ASIGNAR" ↔ "DESASIGNAR").

**Especificación**:
- Edge case: si el héroe ya tiene otro puesto ocupado, el toggle no cambia nada y aparece un aviso explicando por qué (`HeroAssignment.BusyWarning`) — evita que el jugador piense que el botón está roto.

**Cuándo usarlo**: Asignaciones exclusivas (un héroe solo puede estar en un sitio a la vez).
**Cuándo NO usarlo**: —

---

### 8. Slider construido a mano

**Categoría**: Input
**Usado en**: `InGameMenuUI` (volumen UI/combate), `MainMenuUI` (volumen maestro)

**Descripción**: No hay builder compartido en `UIBuild` — cada pantalla monta su propio track + fill + `Slider` manualmente. `InGameMenuUI` usa tokens de `UITheme`; `MainMenuUI` usa colores hardcodeados y su propio set de helpers (no reutiliza `UIBuild`).

**Especificación**: Ver hueco #3 — candidato claro a extraer a `UIBuild.Slider(...)`.

**Cuándo usarlo**: Valores continuos (volumen).
**Cuándo NO usarlo**: —

---

### 9. Selector de opción única en fila

**Categoría**: Input
**Usado en**: `InGameMenuUI`, `MainMenuUI` (idioma ES/EN/JA)

**Descripción**: Fila de 3 botones, el seleccionado se tiñe con `selectedColor` (ámbar); los nombres se muestran en su propio idioma (no traducidos: "Español", "English", "日本語").

**Especificación**: —

**Cuándo usarlo**: Selección única entre pocas opciones fijas y conocidas de antemano.
**Cuándo NO usarlo**: —

---

### 10. Toque en mundo con radio de gracia y prioridad

**Categoría**: Input
**Usado en**: `WorldInteractionManager` (dispara `HeroQuickCardUI`, `BuildingInspectUI`, `SummonAltarUI`)

**Descripción**: Un único gestor traduce clic/tap a coordenadas de mundo y decide qué se tocó, con prioridad explícita: héroe > Altar > edificio.

**Especificación**:
- Unifica `Touchscreen.current` y `Mouse.current` (nuevo Input System) en un solo camino.
- Usa `EventSystem.IsPointerOverGameObject()` como guardia para que un tap sobre UI no dispare también una interacción de mundo.
- Radio de toque generoso en héroes (`heroTouchRadius = 0.7`, el más cercano dentro del radio gana — Fitts's Law aplicado explícitamente vía comentario del código); Altar con radio propio (2.0) por ser objeto de mundo más grande.

**Cuándo usarlo**: Cualquier objeto interactivo nuevo en el escenario 2D de la base.
**Cuándo NO usarlo**: —

---

### 11. Texto flotante de combate

**Categoría**: Feedback
**Usado en**: `DamageTextManager` (mundo, combate)

**Descripción**: Texto que aparece sobre el objetivo, sube y se desvanece en 0.8s; jitter horizontal para que golpes simultáneos no se solapen. Variantes: daño (ámbar), curación (verde, con "+"), esquiva (celeste, "¡ESQUIVA!").

**Especificación**:
- No depende solo del color: el texto/signo es la fuente primaria de información, el color es refuerzo.

**Cuándo usarlo**: Cualquier evento numérico puntual de combate.
**Cuándo NO usarlo**: —

---

### 12. Rótulo de aviso a pantalla completa

**Categoría**: Feedback
**Usado en**: `ScreenBanner` (combate — "¡VICTORIA!", "¡DERROTA!", etc.)

**Descripción**: Texto grande centrado que corre con `unscaledDeltaTime` (se ve aunque `Time.timeScale = 0`), con duración y color configurables por llamada.

**Especificación**: —

**Cuándo usarlo**: Eventos de combate de alto impacto, poco frecuentes.
**Cuándo NO usarlo**: —

---

### 13. Overlay de confirmación destructiva anidado

**Categoría**: Feedback
**Usado en**: `SanctuaryUI` (confirmación de Síntesis — sacrificar un héroe)

**Descripción**: Segundo overlay (fondo oscurecido + caja centrada) montado dentro del propio panel modal, no registrado en `UIManager` (evita que abrir la confirmación cierre el modal padre). Mensaje explícito con nombres y consecuencia numérica.

**Especificación**:
- Dos botones: acción destructiva en `DangerSoft`, cancelar en `Neutral`.
- La acción real no se aplica hasta la confirmación — el estado no se toca al hacer el primer/segundo clic de selección, solo al confirmar.

**Cuándo usarlo**: Cualquier acción irreversible con pérdida de progreso.
**Cuándo NO usarlo**: —
**Nota**: única instancia actual; candidato a generalizar (ver hueco #4).

---

### 14. Cofre de recompensa con animación de pop

**Categoría**: Feedback
**Usado en**: `TowerRewardUI` (victoria de piso de torre)

**Descripción**: Backdrop propio + panel con icono de cofre que hace un rebote de escala (0.4 → 1.08 → 1.0, sin librería de tweening, con `Mathf.Sin`) al aparecer, y tres líneas de desglose (gemas, materiales, EXP) con icono/color por recurso.

**Especificación**: —

**Cuándo usarlo**: Recompensas de victoria con desglose numérico.
**Cuándo NO usarlo**: —
**Nota de arquitectura**: este backdrop no pasa por `UIManager` — ver hueco #5.

---

### 15. Carta boca abajo con revelado

**Categoría**: Feedback
**Usado en**: `SummonAltarUI` (gacha)

**Descripción**: Cada tirada crea cartas en rejilla (`GridLayoutGroup`, 5 columnas) con retrato oculto tras "?" y marco neutro; tap individual revela esa carta (tiñe marco/retrato con color de rareza, sonido `CardReveal`), o `RevealAll` las revela todas de golpe.

**Especificación**:
- Seguridad de progreso: cerrar el modal con cartas pendientes NO las descarta — `Close()` llama automáticamente a `Accept()` primero, igual antes de una tirada nueva.

**Cuándo usarlo**: Cualquier mecánica de recompensa aleatoria con revelado dramático.
**Cuándo NO usarlo**: —

---

### 16. Barra de progreso con etiqueta flotante

**Categoría**: Feedback
**Usado en**: `RosterUI`, `HeroDetailModal`, `HeroQuickCardUI`, `BossHealthBarUI` (HP/MP/Moral/Fatiga)

**Descripción**: Builder compartido (`UIBuild.Bar`/`SetBar`) — fondo (`Track`) + relleno ajustado con `anchorMax.x = ratio` + label superpuesto con el valor numérico (p. ej. "120/150").

**Especificación**:
- Accesibilidad: el número siempre acompaña a la barra — no depende solo del color/longitud.

**Cuándo usarlo**: Cualquier stat con máximo conocido (vida, maná, moral, fatiga, cuenta atrás de cooldown).
**Cuándo NO usarlo**: —

---

### 17. Disco de decreto con barrido de cooldown radial

**Categoría**: Feedback
**Usado en**: `MasterActionBar`

**Descripción**: Botón circular con aro de color (activo = color del decreto, inactivo = `Dim`), un "barrido" (`Image.Type.Filled`, `Radial360` desde arriba) que tapa progresivamente el disco mientras recarga, número de segundos restantes, y aro rojo de alerta (`Danger`) cuando la acción es crítica.

**Especificación**:
- Alerta crítica combina aro rojo + cambio de texto (p. ej. "Curar" → "¡Curar!") — no depende solo del color.

**Cuándo usarlo**: Acciones con cooldown que el jugador dispara activamente durante combate.
**Cuándo NO usarlo**: —

---

### 18. Localización reactiva

**Categoría**: Feedback
**Usado en**: `AlchemyWorkshopUI`, `QuestBoardUI`, `SquadManagementUI`, `EquipmentSelectModalUI`, `SummonAltarUI`, `InGameMenuUI`, `SideMenuUI`, `MainMenuUI`

**Descripción**: Cada panel se suscribe a `LocalizationManager.LanguageChanged` y repinta todos sus textos (`RefreshTexts`/`Rebuild`) cuando el jugador cambia de idioma en caliente, sin reabrir el panel.

**Especificación**: —

**Cuándo usarlo**: Cualquier panel nuevo con texto visible debería suscribirse a este evento para no quedar congelado en el idioma anterior tras un cambio en caliente.
**Cuándo NO usarlo**: —

---

### 19. Lista con pooling de widgets

**Categoría**: Data Display
**Usado en**: `RosterUI`, `SanctuaryUI` (mismo patrón, copiado explícitamente entre ambos)

**Descripción**: En vez de destruir/recrear filas en cada refresco, se mantiene un `List<Widgets>` reutilizado; filas sobrantes se desactivan (`SetActive(false)`) en vez de destruirse. Necesario para evitar caída de FPS con rosters de 50+ héroes.

**Especificación**:
- Otros paneles (`QuestBoardUI`, `TowerPanelUI`, `BuildingInspectUI`, `EquipmentSelectModalUI`, `SquadManagementUI`) usan el patrón más simple de destruir todo y recrear — aceptable en listas ≤10-20 filas o que no se refrescan con la frecuencia del roster.

**Cuándo usarlo pooling**: Listas que pueden crecer sin límite claro y se refrescan cada ~0.5s mientras el panel está abierto.
**Cuándo NO hace falta**: Listas acotadas por diseño (contratos = 6, presets = 2-3).

---

### 20. Maestro-detalle (lista + vista previa)

**Categoría**: Data Display
**Usado en**: `SanctuaryUI` (lista de héroes a la izquierda, vista previa de Ascensión/Síntesis a la derecha)

**Descripción**: Clic en fila de lista alimenta un panel de detalle a la derecha con retrato, texto contextual y un único botón de acción principal cuyo texto/color cambia según contexto.

**Especificación**: —

**Cuándo usarlo**: Flujos de selección + revisión antes de una acción cara (gemas, sacrificio).
**Cuándo NO usarlo**: —

---

### 21. Ficha de detalle a dos columnas

**Categoría**: Data Display
**Usado en**: `HeroDetailModal` (retrato+identidad+bio+3 barras+equipo | 6 botones en grid 2×3), `HeroQuickCardUI` (variante "quick look", 4 barras, menos acciones)

**Descripción**: Columna izquierda con retrato/stats, columna derecha con acciones en grid.

**Especificación**: —

**Cuándo usarlo**: Fichas de detalle de una sola entidad con stats + acciones.
**Cuándo NO usarlo**: —
**Nota de redundancia**: `HeroDetailModal` y `HeroQuickCardUI` son dos implementaciones paralelas de casi la misma información, sin compartir código — ver hueco #6.

---

### 22. Tarjeta de rejilla para elección forzada

**Categoría**: Data Display
**Usado en**: `SubclassSelectionUI` (elección de subclase al llegar a 3★)

**Descripción**: Modal bloqueante de 3 cartas grandes con texto (rol, descripción, habilidad, coste de MP/cooldown); sin botón de cerrar — el jugador debe elegir. Se abre vía patrón singleton estático (`SubclassSelectionUI.Offer(...)`) llamado desde `HeroProgress`, no desde un botón de UI.

**Especificación**: —

**Cuándo usarlo**: Decisiones de build únicas y permanentes que el diseño quiere forzar a elegir conscientemente (no delegar al azar).
**Cuándo NO usarlo**: —

---

### 23. Texto alineado por `<pos=%>`

**Categoría**: Data Display
**Usado en**: `AlchemyWorkshopUI` (coste: recurso a la izquierda, cifra alineada a 62%)

**Descripción**: Uso de la etiqueta rich-text `<pos=62%>` de TextMeshPro para alinear cifras sin montar un layout horizontal completo.

**Especificación**: —

**Cuándo usarlo**: Pares etiqueta-valor cortos donde no compensa montar un `HorizontalLayoutGroup`.
**Cuándo NO usarlo**: —

---

### 24. Tarjeta compacta de operación

**Categoría**: Data Display
**Usado en**: `AlchemyWorkshopUI` (piedras, armas, reparación, poción, mejora — hasta 8 tarjetas)

**Descripción**: Tarjeta con 4 bandas fijas (título, cuerpo, coste, botón al pie) repetida en grid de hasta 3 columnas por fila.

**Especificación**: —

**Cuándo usarlo**: Catálogo de acciones/recetas del mismo tipo con coste y resultado.
**Cuándo NO usarlo**: —

---

### 25. Panel de pausa con `Time.timeScale = 0`

**Categoría**: Modal/Overlay
**Usado en**: `InGameMenuUI`, `MainMenuUI`

**Descripción**: Al abrir, `Time.timeScale = 0`; al cerrar (reanudar), vuelve a 1. Ambos se diseñan para no pisarse (`InGameMenuUI.Open()` comprueba que `MainMenuUI` no esté abierto).

**Especificación**:
- `MainMenuUI` bloquea el guardado (`SaveManager.SavingAllowed = false`) mientras está al mando, para no pisar el JSON de disco antes de que el jugador elija "Continuar" o "Nueva Partida".

**Cuándo usarlo**: Cualquier menú que deba congelar la simulación mientras está abierto.
**Cuándo NO usarlo**: —

---

### 26. Panel anidado dentro de otro panel

**Categoría**: Modal/Overlay
**Usado en**: `MainMenuUI` (root → `optionsPanel` como hijo)

**Descripción**: Navegación "atrás" simple de dos niveles sin pila de navegación genérica — cada nivel es un `GameObject` que se activa/desactiva a mano, mismo patrón de mostrar/ocultar que un modal pero sin pasar por `UIManager`.

**Especificación**: —

**Cuándo usarlo**: Jerarquías de 2 niveles muy simples (pantalla de título).
**Cuándo NO usarlo**: No escalaría bien a 3+ niveles sin una pila explícita.

---

### 27. Silueta de zona bloqueada

**Categoría**: Data Display
**Usado en**: `design/ux/base-environment.md` (Fase 26, nuevo — pendiente de implementar)

**Descripción**: Versión oscurecida/desaturada del arte de fondo de una zona no desbloqueada, con icono de candado y etiqueta "Piso N" flotante sobre el centro de la zona. Reemplaza el patrón previo (invisibilidad total, hueco #1) por un estado visible-pero-bloqueado.

**Especificación**:
- El contenido real de la zona (edificios) no se instancia/renderiza en detalle mientras está bloqueada — solo la silueta.
- La etiqueta de piso requerido usa los tokens de `UITheme`.
- No depende solo del oscurecimiento: candado + texto son la fuente primaria de información.
- Implementación de referencia (Fase 26): máscara plana de un solo color + tinte en runtime (`UITheme.GlassDeep`), coherente con cómo ya funciona todo el arte de mundo del proyecto (`Square_White.png` + `SpriteRenderer.color`) — sin shader ni material nuevo. Tono neutro uniforme entre instancias de la zona (no varía por bioma mientras está bloqueada).

**Cuándo usarlo**: Contenido de mundo (no panel UI) que se desbloquea por progreso y necesita ser visible-pero-inaccesible antes de cumplir el requisito.
**Cuándo NO usarlo**: Contenido de panel UI (usar en su lugar el patrón #5, botón con estado de asequibilidad).

---

### 28. Feedback de toque en zona bloqueada

**Categoría**: Feedback
**Usado en**: `design/ux/base-environment.md` (Fase 26, nuevo — pendiente de implementar)

**Descripción**: Al tocar/clicar una zona en estado "Silueta de zona bloqueada" (#27), aparece un mensaje breve tipo "Desbloquea en Piso N" — reutiliza el estilo visual del texto flotante de combate (patrón #11) pero disparado desde `WorldInteractionManager` en vez de desde combate.

**Especificación**:
- Requiere extender `WorldInteractionManager` para detectar el tap sobre la silueta (hoy `if (!building.IsUnlocked) continue;` la ignora por completo).
- Sin sonido de error — es informativo, no un rechazo.
- Color: `UITheme.TextSoft`/blanco neutro — NO el ámbar de daño del patrón #11 (ese ámbar ya significa "daño recibido" en el HUD de combate; usarlo aquí se leería como feedback negativo).

**Cuándo usarlo**: Cualquier objeto de mundo bloqueado que el jugador pueda intentar tocar, para confirmar que el toque se registró y explicar por qué no pasa nada más.
**Cuándo NO usarlo**: Elementos de panel UI ya cubiertos por el patrón #5 (ahí el propio estado del botón ya comunica esto).

---

## Gaps & Patterns Needed

Huecos identificados por ingeniería inversa del código actual, ordenados por relevancia para el trabajo activo.

### Relevantes para Fase 26 (fondo de base con zonas desbloqueables)

1. **Desbloqueo de edificios binario e invisible.** `BaseBuilding.IsUnlocked` (`TowerFloor >= requiredFloor`) solo activa/desactiva `SpriteRenderer`/`TextMeshPro` — un edificio bloqueado no se ve en absoluto, sin silueta, candado, ni indicación de "se desbloquea en el piso X". No hay affordance de que ahí hay algo por desbloquear.
2. **Sin patrón de "vista previa de contenido bloqueado"** en ningún panel (a diferencia de cómo `SanctuaryUI` sí explica por qué un héroe no puede ascender). Si Fase 26 añade zonas de base desbloqueables, hace falta: (a) estado visual "bloqueado" distinto de "invisible" (silueta/candado), (b) tap sobre esa zona que abra un mensaje tipo "Desbloquea en Piso N" en vez de no reaccionar, (c) extender `WorldInteractionManager.BuildingAt` para que detecte edificios bloqueados y dispare ese feedback en vez de ignorarlos (`if (!building.IsUnlocked) continue;` los descarta hoy).
3. **Sin feedback de "toque en vacío"** en `WorldInteractionManager** — si el jugador toca el suelo sin nada encima, no pasa nada visible. Con una base más grande y zonas por desbloquear, conviene un indicador breve que confirme que el toque se registró.

### Relevantes para consistencia UI/HUD (también Fase 26)

4. **`MainMenuUI` no usa `UITheme`/`UIBuild`.** Tiene su propio `NewButton`/`NewLabel`/`NewPanel`/`Stretch` con colores hardcodeados en vez de los tokens compartidos — la pantalla de título (primer contacto del jugador) no lleva el estilo "Dark Glassmorphism" del resto.
5. **`TowerPanelUI` y `ResourceExpeditionUI` tampoco usan `UITheme`/`UIBuild.Panel`.** Construyen su panel raíz a mano con colores RGB directos. Además, `TowerPanelUI`/`BuildingInspectUI`/`HeroQuickCardUI` mezclan claves localizadas con texto español fijo hardcodeado (p. ej. "Mejorar Edificio", "Usar Poción", "Ver en Roster") — esas cadenas no reaccionan al patrón #18 (Localización reactiva).
6. **No hay builder compartido de `Slider`** en `UIBuild` (ver patrón #8) — candidato claro a `UIBuild.Slider(...)`.
7. **El overlay de confirmación destructiva (patrón #13) es una implementación única, no un builder reutilizable.** Si hacen falta más confirmaciones irreversibles (desbloquear una zona cara, deshacer una asignación con coste), hoy tocaría copiar/pegar en vez de llamar a `UIBuild.ConfirmOverlay(...)`.
8. **`TowerRewardUI` monta su propio backdrop fuera de `UIManager`** (ver patrón #14) — puede solaparse con un modal gestionado por `UIManager` sin que ninguno lo sepa; no hay jerarquía de prioridad ni registro común de "qué bloquea la pantalla ahora mismo".
9. **`HeroDetailModal` y `HeroQuickCardUI` duplican casi la misma información** (ver patrón #21) con dos implementaciones de layout independientes — candidato a unificar en un componente parametrizable (modo "completo" vs "rápido").
10. **Las pestañas de filtro (patrón #4) no tienen atajo de teclado/gamepad** — dependen 100% de clic/tap directo. El proyecto es PC-first con capa touch después; no hay navegación por foco (Tab/D-pad) documentada en ningún panel — hueco de accesibilidad real, no solo de Fase 26.

---

## Open Questions

- Los huecos #4-#5 (pantallas fuera del sistema `UITheme`/`UIBuild`) son candidatos directos para la "consistencia de UI/HUD" que pide Fase 26 del roadmap — pendiente decidir si se migran en esta fase o se registran como deuda técnica aparte (`/tech-debt`).
- El hueco #1-#3 (desbloqueo de zonas) es el bloqueante de diseño real para la parte de "expansión visual y funcional según piso superado" de Fase 26 — necesita una UX spec propia (`design/ux/base-environment.md`) antes de implementar.
