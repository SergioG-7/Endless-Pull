# Roadmap de Produccion Endless Pull

## Fase 30 Audio Dinamico (COMPLETA)

- Ejecutar comando /team-audio
- BGM loop base torre tension y jefes con transiciones
- SFX combate espada impactos flechas magia criticos
- Audio UI y base clics modales ascension crafteo victoria
- Pendiente: AudioClip reales aplazados a etapa final del proyecto — generación IA vía
  `Unity_AssetGeneration_GenerateAsset` no disponible (sin modelos configurados) y BGM
  orquestal en loop generada por IA es alto riesgo de sonar mal. Usuario importará SFX/BGM
  gratuitos 100% necesarios para gameplay manualmente. Hoy suena con el sintetizador de
  reserva del AudioManager

## Fase 31 Game Feel VFX y Pulido (COMPLETA)

- Ejecutar comando /team-polish
- Impacto hitstop screen shake iluminacion 2D
- VFX particulas curacion decretos crafteo y cofre victoria
- Animaciones UI transiciones modales
- Prerrequisito: pooling de DamageTextManager/Projectile (bloqueante detectado por
  performance-analyst, resuelto antes del VFX)
- Iluminacion 2D sustituida por flash de sprite existente (sin infraestructura Light2D
  en el proyecto); animacion de modal solo al abrir, no al cerrar (ver notas de sesion)

## Fase 32 Reorganización, Agrupación y Estandarización de UI (Fase de Pulido) — MAYORMENTE COMPLETA:

- Hecho: bug BLD_*, 2/3 bugs de gameplay, Portal unificado salida+entrada con transición,
  5/6 puntos de UI (localización menú, HUD tapado, dropdown piedras, modal héroe, labels
  tienda), las 4 estructuras nuevas (Pozo de Maná/Forja/Sala de Guerra/Archivo del Santuario)
  con datos+lógica+colocación en escena+integración en BuildingInspectUI, TopBar condensado
  y realineado (Sidebar ya cumplía el rol de Dock, no necesitó cambios).
- Pendiente: unificar dimensiones de modales al 80% centrado (aplazado, riesgo alto sin
  verificación visual); labels cortados fuera de la tienda (sin causa concreta localizada);
  duplicar las 4 estructuras nuevas en `_Recovery/0.unity` si hace falta (solo se tocó
  `Base.unity`); verificación visual real en Editor del TopBar (los cambios de layout no se
  pudieron capturar por pantalla esta sesión, solo se verificó por consola sin errores);
  barrido general de QA por más bugs/huecos del prototipo (pedido explícitamente por el
  roadmap); resto de keys de idioma muertas de la auditoría (no bloqueante).

1. Estandarización de Canvas y Modales (UIManager.cs / UITheme.cs):

- Asegura que todos los Canvas tengan CanvasScaler en Scale With Screen Size (1920x1080, match 0.5).
- Unifica las dimensiones de los paneles modales (Roster, HeroDetailModal, SanctuaryUI, TowerGatewayUI) para que ocupen un 80% centrado con un fondo atenuador estándar (Dimmer).
- Revisa tamaños mínimos de botones de navegación (padding adecuado y texto legible).

2. Agrupación y Limpieza de HUD en Base (BaseUI / TopBar):

- Condensa el TopBar: iconos compactos alineados a la derecha/centro.
- Agrupa los accesos a edificios/paneles en un Dock de navegación inferior/lateral limpio para evitar botones flotantes desordenados.
- Asegura que los textos no se corten en resoluciones 16:9 y móviles.
- Ademas las keys del menu de "Torre" "Expediciones" "Ver heroes" y esas siguen estando hardcodeadas en Español.
- Los botones de autoretirada y x2 se esconden detrás de la barra de HUD de arriba y no se puede usar.
- Agrupa la creacion de piedras [menor,media,mayor,legendaria] en un desplegable o algo mejor en vez de todas en el mismo menu. Ademas de que al clickar fuera del menu actual lo cierre para mejor gamefeel.
- Revisa el tamaño de las cosas en el perfil de cada personaje, ademas el boton de cerrar se queda debajo de los botones para modificar el personaje porque se pegan arriba del todo, y el texto de la barra de moral es ilegible.
- Añadir estructuras que se desbloquean por niveles de torre dado que se ve muy vacio solo tener 3 estructuras y son duplicadas de anteriores, pregunta si no sabes que añadir.
- Los labels del santuario y de otros sitios se cortan a la mitad o no se ven del tood bien, tambien revisalo.
- Sigue habiendo un bug de que no todos los personajes vuelven a la estructura de la "torre" que no añadiste al final, donde se unificaba la torre y la expedicion para que salieran y entraran por ahi.
- Otro bug de que los personajes se van a zonas que no están desbloqueadas aún, no tendrian que poder pasar por encima ni nada.
- Tambien puedes spamear el boton de invocar sin haber revelado o cerrar el menu y te pierdes la animacion.
- Y habrá más bugs o cosas que falten para el prototipo decentemente jugable asi que revisa al final de esto.

3. Compilación y Validación:

- Compila limpio vía Unity MCP.
- Valida en Play Mode que abrir y cerrar paneles en secuencia no cause solapamientos visuales ni bloquee la entrada.
