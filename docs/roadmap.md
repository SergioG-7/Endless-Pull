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

## Fase 32 Reorganización, Agrupación y Estandarización de UI (Fase de Pulido):

1. Estandarización de Canvas y Modales (UIManager.cs / UITheme.cs):

- Asegura que todos los Canvas tengan CanvasScaler en Scale With Screen Size (1920x1080, match 0.5).
- Unifica las dimensiones de los paneles modales (Roster, HeroDetailModal, SanctuaryUI, TowerGatewayUI) para que ocupen un 80% centrado con un fondo atenuador estándar (Dimmer).
- Revisa tamaños mínimos de botones de navegación (padding adecuado y texto legible).

2. Agrupación y Limpieza de HUD en Base (BaseUI / TopBar):

- Condensa el TopBar: iconos compactos alineados a la derecha/centro.
- Agrupa los accesos a edificios/paneles en un Dock de navegación inferior/lateral limpio para evitar botones flotantes desordenados.
- Asegura que los textos no se corten en resoluciones 16:9 y móviles.

3. Compilación y Validación:

- Compila limpio vía Unity MCP.
- Valida en Play Mode que abrir y cerrar paneles en secuencia no cause solapamientos visuales ni bloquee la entrada.
