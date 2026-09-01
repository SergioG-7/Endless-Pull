# Roadmap de Produccion Endless Pull

## Fase 40

[DIRECTIVE: SYSTEMIC LOCALIZATION, ENEMY AI, SMART AUDIO INTEGRATION & UI HARDENING]
Actúa en el hilo principal como desarrollador senior de Unity C# y uGUI.
PROHIBIDO invocar subagentes si no es para hacerlo más rapido, si vas a invocarlo para tu no hacer nada en la rama principal no lo hagas (/team-\*). Respuestas breves, directas y centradas en la edición de código.

Ejecuta de forma exhaustiva el siguiente paquete de correcciones, arquitectura de audio y saneamiento integral:

---

1. Sistema de Audio Inteligente y Mapeo por Nombre (AudioManager.cs / Assets/Audio):

- Singleton AudioManager: Crea/actualiza `AudioManager.cs` con canales independientes (BGM y SFX), control de volumen y auto-detección de clips en `Assets/Audio` (o subcarpetas).
- Mapeo Inteligente por Palabras Clave: Inspecciona los nombres de archivo para asociar cada clip a su evento correspondiente según coincidencia de texto:
  - BGM Base: coincide con "base", "ambient", "peace", "village", "town", etc. (loop, crossfade).
  - BGM Combate: coincide con "combat", "battle", "tower", "fight", "arena", etc. (loop, crossfade).
  - UI Clic / Pestaña: coincide con "click", "tap", "button", "select", "tick".
  - Modales Open/Close: coincide con "open", "window", "popup", "close", "dismiss", "back".
  - Error / Bloqueo: coincide con "error", "deny", "locked", "cancel", "fail".
  - Crafteo / Yunque: coincide con "anvil", "craft", "forge", "hammer", "smith".
  - Recompensas / Monedas: coincide con "reward", "coin", "gold", "gem", "loot".
  - Curación / Poción: coincide con "heal", "potion", "magic_heal", "recovery".
  - Invocación / Ascenso: coincide con "summon", "ascend", "portal", "level_up", "fanfare".
  - Ataque Melee / Impacto: coincide con "hit", "slash", "sword", "strike", "punch".
  - Ataque Distancia / Disparo: coincide con "shoot", "arrow", "bow", "cast", "magic_shot".
  - Victoria / Derrota: coincide con "victory", "win", "defeat", "lose", "game_over".
- Límite de Duración para SFX Largos (ej. Yunque): Implementa reproducción controlada (`PlaySFX(clip, maxDuration = 3f)`) para que efectos con audios largos o repetitivos (como el golpeo de yunque) hagan fade-out y se detengan a los 3 segundos en vez de reproducir el archivo entero.

2. IA de Enemigos en Combate (EnemyController.cs, EnemyCombatAI.cs, WaveManager.cs):

- Target Acquisition de Enemigos: Corrige la IA enemiga. Los esqueletos/goblins DEBEN buscar activamente al héroe vivo más cercano (`AcquireNearestHeroTarget`) al spawnear y al morir su objetivo actual.
- Ataque y Daño Real: Los enemigos deben desplazarse hacia la posición X del héroe objetivo (-X) y ejecutar sus ataques y daño al entrar en rango cuerpo a cuerpo o distancia (resolver el bug de que caminan en línea recta hacia la izquierda ignorando a los héroes sin hacer daño).

3. Reglas de Asignación Exclusiva a Edificios (BuildingController.cs, WorkerManager.cs, BaseManager.cs):

- Unicidad de Trabajador: Un héroe SOLO puede estar asignado a un único edificio a la vez. Si se asigna a la Forja estando en la Granja, debe desasignarse automáticamente de la Granja previa.
- Prioridad de Despliegue: Si un héroe asignado a un edificio es enviado a la Torre o a una Expedición activa, debe desasignarse automáticamente del edificio para evitar duplicidad de presencia.

4. UI: Santuario, Roster [X] y Taller de Alquimia (SanctuaryUI.cs, RosterUI.cs, AlchemyWorkshopUI.cs):

- Santuario (Corte de Retratos): Ajusta el `Viewport` / `Content` y añade `padding.left = 20px` (y `offsetMin.x`) en el prefab de ítem del héroe para que los sprites nunca se corten por el borde izquierdo de la lista.
- Roster [X]: Sustituye el botón de cierre actual por `UIBuild.CloseButtonTopRight`, idéntico en tamaño, posición y comportamiento al de la Ficha de Héroe.
- Crafteo de Equipo:
  - Renombra el botón "Craft Weapon" a "Craft Equipment" / `BTN_CRAFT_EQUIPMENT`.
  - Aumenta el tamaño de fuente del toast/texto de resultado del ítem obtenido (armas, armaduras, pociones y piedras) para que sea claramente legible.

5. Auditoría y Cobertura Total de Localización (LocalizationManager.cs y scripts UI):
   Audita y sustituye CUALQUIER string hardcodeado en los 3 idiomas (ES / EN / JA):

- Edificios y Modales: Nombres y textos de ocupación de "Granja del Valle", "Sala de Guerra", "Archivo del Santuario", "Forja", cabeceras internas de "Storage" y "Sanctuary" ("Ascension", "Synthesis", "Needs to reach Lv. X", "Max rarity").
- Combate y HUD:
  - Sinapsis/Origen: Traducir "Reino Fronterizo" y demás afinidades de origen (`ORIGIN_FRONTIER_REALM`, etc.).
  - TopBar y Toasts: Clave de "TORRE / TOWER", y banner de victoria de piso ("Piso X superado", "Floor X cleared", etc.).
- Ficha de Héroe y Roster:
  - Personalidades/Rasgos: "Trabajador", "Glotón", "Feroz", "Perezoso", etc.
  - Estados de Moral: "Inspirado", "Normal", "Desmotivado".
  - Subclases y Equipo: Nombres de armas/armaduras ("Espada de Madera", etc.) sin sufijos en español al estar en JA/EN.
  - Botones: "View in Roster" (`BTN_VIEW_ROSTER`).

6. Verificación:

- Compilar limpio vía Unity MCP (filtro `error CS` = 0 errores).
- Validar en Play Mode: comprobar que los enemigos ataquen a los héroes e inflijan daño, que la música y SFX suenen en sus eventos (y el crafteo no dure más de 3s), que los retratos del Santuario no se recorten a la izquierda, que la asignación a edificios sea exclusiva y que al cambiar a Japonés no quede ni una sola palabra en español/inglés.
