# Roadmap de Produccion Endless Pull

## Fase 37

[DIRECTIVE: SYSTEMIC REFACTOR, GRID ALIGNMENT & COMBAT FSM]
Actúa en el hilo principal como desarrollador senior de Unity C# y uGUI.
PROHIBIDO invocar subagentes (/team-\*). Respuestas directas al grano y orientadas a edición directa de código.

Hecho y verificado (grid/muralla/portal/zoom, FSM de héroes con prueba real en Play Mode que
confirma que ya no hay fuga a base al morir el objetivo en combate, highestClearedFloor+toast
de expedición, modales/botón cerrar, piedras en 4 tarjetas sin desplegable, pausa del menú de
ajustes, localización de edificios). Detalle completo en `production/session-state/active.md`.

Queda pendiente solo esto:

1. Verificación visual real (captura de pantalla o revisión a mano en el Editor):

- Base ordenada en cuadrícula, sin solapes visuales de verdad (ya confirmado por matemática de
  posiciones, falta verlo).
- Muralla perimetral y suelo cuadrado grande, Portal aislado arriba-centro.
- Sensibilidad de zoom x3 y clamp ampliado, en uso real con rueda del ratón.
- Las 4 piedras del Taller en tarjetas limpias, sin desplegable.
- Ficha de héroe (`HeroQuickCardUI`) mostrando el sprite correctamente.

Las tools de captura de cámara (`Unity_SceneView_Capture2DScene`, `Unity_Camera_Capture`)
fallaron esta sesión — reintentar o revisar a mano en el Editor.

2. Auditoría cosmética opcional (bajo impacto, no bloqueante):

- Nombres exactos de edificio pedidos por el roadmap original ("Granja del Valle", "Campo de
  Entrenamiento Avanzado") — los nombres actuales están traducidos ES/EN/JA pero no coinciden
  literalmente; no se renombraron.

3. Revisión de regresión puntual:

- Confirmar en Play Mode que `QuadrantController.ContainsPoint` (usado por wander/clic) sigue
  funcionando bien en Este/Sur/Oeste tras redimensionar sus veils a mano en la sección 1.
