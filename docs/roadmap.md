# Roadmap de Produccion Endless Pull

## Fase 0 Saneamiento de Infraestructura y Hooks

- Fix de rutas en hooks corregir resolucion de paths en claude hooks para eliminar fallos en validate commit validate push y session stop
- Permisos y normalizacion POSIX asegurar compatibilidad en Git Bash Windows macOS y salida limpia
- Ejecutar comando /skill-test

## Fase 24 Cierre de UX UI Santuario y Portal de Retorno

- Checkpoints 1 a 3 completados
- Checkpoint 4 Portal y Retorno Base conectar TowerGateway en escena para RecallParty y limitar a 6 u 8 heroes visibles en plaza
- Checkpoint 5 Santuario de Ascension y Sintesis crear SanctuaryUI desacoplado de Roster panel ascension con piedras por tier menor media mayor legendaria y panel sintesis dos columnas con confirmacion
- Checkpoint 6 Menu In Game conectar boton Menu a InGameMenuUI con reanudar sliders audio selector ES EN JA y salir guardando

## Fase 25 Gimnasio de IA de Heroes Gym Combat y Comportamiento

- Entorno simulacion 10x integrar Gym Combat a Time timeScale 10
- Personalizacion de tacticas prioridades por heroe
- Entrenamiento de pesos de IA balance de agresividad distancia de seguridad y decretos

## Fase 26 UI y Entorno de Base

- Ejecutar comando /team-ui
- Fondo visual de la base mapa pixel art estructurado con zonas
- Desbloqueo de cuadrantes pisos 5 10 15 expansion visual y funcional segun piso superado
- Consistencia UI HUD estandarizar marcos tipografias contraste y Safe Area

## Fase 27 Narrativa Lore y Localizacion

- Ejecutar comando /team-narrative
- Lore de la Torre y Decretos
- Auditoria de bios 50 heroes descripciones y personalidades en HeroData
- Tablon de misiones contratos tematicos con recompensas
- Localizacion ES EN JA validar todas las cadenas en LocalizationManager

## Fase 28 Diseno de Niveles Biomas y Progresion

- Ejecutar comando /team-level
- Biomas fondos por pisos 1 a 5 goblin 6 a 10 minas 11 a 15 cripta 16 a 20 templo
- Formaciones de enemigos tanques delante tiradores y chamanes detras
- Jefes cada 5 pisos patrones unicos

## Fase 29 Balance de Combate y Decretos

- Ejecutar comando /team-combat
- Balance DPS HP mitigacion simulacion numerica y curvas de escalado
- Decretos cooldowns costes mana moral de curacion foco reagrupar retirada
- Sinergias subclases validar 18 habilidades y maestrias

## Fase 30 Audio Dinamico

- Ejecutar comando /team-audio
- BGM loop base torre tension y jefes con transiciones
- SFX combate espada impactos flechas magia criticos
- Audio UI y base clics modales ascension crafteo victoria

## Fase 31 Game Feel VFX y Pulido

- Ejecutar comando /team-polish
- Impacto hitstop screen shake iluminacion 2D
- VFX particulas curacion decretos crafteo y cofre victoria
- Animaciones UI transiciones modales

## Fase 32 QA y Entrega

- Ejecutar comando /team-qa
- Pruebas de estres y persistencia validar savegame json
- Deuda tecnica ejecutar /tech-debt /architecture-review /gate-check
- Build ejecutar /release-checklist y /launch-checklist
