# Referencia: Pick Me Up, Infinite Gacha!

Fuente de inspiración del juego (confirmado por el usuario 2026-08-31). Info sacada del
[Pick Me Up! Infinite Gacha Wiki](https://infinite-gacha.fandom.com/wiki/Pick_Me_Up!) vía
la API de MediaWiki (`action=parse&prop=wikitext`) — `WebFetch` normal da 402/403 en ese
dominio (protección anti-bot), pero el endpoint API sí responde. Guardado aquí para no tener
que re-investigar cada vez. **No implementar nada de esto en el prototipo todavía** — el
usuario pidió guardarlo para después de enseñar el prototipo actual.

Protagonista: Han Islat / Seojin Han (jugador reencarnado como héroe 1★) → coincide con
`Hero_Loki` ya presente en la escena del proyecto, buena señal de que el nombre ya venía de
aquí desde antes.

## Torre (100 pisos por mundo)

- Dificultad sube cada 5 pisos, salvo pisos evento (pensados para dar un respiro o un
  contratiempo, como perder al equipo principal).
- **Variedad de tipo de misión por piso** (esto es la clave para el punto de "ritmo" que pidió
  el usuario — el canon NO es pelea pura piso tras piso):
  Subjugation (matar a todos), Conquest, Survival (aguantar oleada), Explore/Exploration
  (investigar sin combate necesariamente), Defend, Guard (proteger a un NPC), Escape, Siege,
  Chase, Capture, Seizure, Standby (misión de fondo mientras la party puede volver al lobby),
  misiones en bucle ("intento X/5"), pisos ocultos/bonus, y hasta un evento tipo torneo con
  varios minijuegos (battle royale, raid por equipos, deathmatch, ranking).
- Los pisos pueden tener objetivos secundarios no declarados en las condiciones de victoria.
- Al subir de piso y mejorar la "sala de espera" (lobby), se desbloquea contenido nuevo y la
  forma en que se mueven los héroes gana variedad (les cuesta acostumbrarse a la realidad del
  juego).

## Lobby / Base

- Cada Maestro tiene un lobby con el mismo layout base, pero variaciones. El lobby puede subir
  de nivel.
- Comida y objetos básicos se reponen solos cada día ("si te imaginas algo que quieres, aparece
  en un cajón").
- **Edificios base** (todos los lobbies los tienen): Restaurant, Training Center, Smithy,
  Woodworking Shop, Metal Processing → Equipment Workshop (cadena de dos pasos), Square (plaza
  central, punto de encuentro).
- Equivalencia con lo que ya existe en Endless Pull: Restaurant≈`Canteen`, Training Center≈
  `TrainingArea`/`TrainingDummy2`, Smithy≈`Forge`. **Sin equivalente todavía**: Woodworking
  Shop, Metal Processing→Equipment Workshop (cadena de crafteo en dos pasos, distinto de
  nuestro `CraftingManager` actual de un paso), Square.

## Héroes

- Rareza 1★ a **7★**. 1★ son desechables (comunes, nunca estuvieron en primera línea, stats
  pobres, crecimiento bajo). Desde 3★ en adelante ya nacen con clase y habilidades.
- **El entrenamiento NO sube stats ni nivel** — solo refina el aprendizaje de habilidades. Esto
  es distinto a como funciona hoy nuestro sistema (donde subir nivel sube stats directo).
- Los héroes se clasifican en 3 ejes: Mentalidad (resistencia al pánico), Habilidades (los de
  alto rango ya vienen con las suyas), Capacidad Física (en los de bajo rango el talento está
  oculto, hay que buscarlo activamente entrenando).
- **Clases**: warrior, thief, spearman, archer, mage. 1★-2★ nacen sin clase. Desde 3★ pueden
  nacer con clase. Cualquier clase se puede conseguir subiendo a un héroe 1★ **excepto mago**
  — mago es rarísimo y solo sale directamente del gacha, no se puede "cultivar" hacia esa clase.
- **Ascensión** requiere "Elemental Stones", que se consiguen en **Daily Dungeons** (mazmorras
  diarias separadas de la Torre principal) — no solo de cofres de piso como tenemos ahora.
- Los héroes son personas reales resumidas de su mundo (no IAs) — pueden opinar, pedir cosas,
  desobedecer al Maestro. Con el tiempo se dan cuenta de que los tratan como mascotas/juguetes
  y eso genera fricción narrativa.

## Notas para cuando se retome esto (no ahora)

- El punto #13 del roadmap (ritmo de Torre menos repetitivo) tiene aquí soporte canon directo:
  variedad de tipos de misión por piso, no solo combate.
- El punto #15 (edificios nuevos: Woodworking Shop + Magic Lab) — Magic Lab no apareció en esta
  pasada de investigación (viene de una búsqueda anterior por WebSearch, sin confirmar en el
  wiki directamente); Woodworking Shop y la cadena Metal Processing→Equipment Workshop sí están
  confirmados aquí.
- Tope de rareza 5★→7★ (ya decidido, ver `production/session-state/active.md`) encaja con este
  documento.
- Posible sistema nuevo a valorar más adelante: Daily Dungeons independientes de la Torre
  (fuente de materiales de ascensión), y que el entrenamiento no suba stats sino que refine
  habilidades (cambio de diseño más profundo, no meterlo sin discutirlo primero).
