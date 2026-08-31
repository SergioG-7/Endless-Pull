# Level: Torre — Franjas de Bioma (Pisos 1-∞)

## Nota de desviación de formato

Este documento **no describe un nivel espacial** en el sentido tradicional de la plantilla (no hay mapa, ruta crítica caminable, ni secretos posicionales). `game-concept.md` confirma que Endless Pull no tiene generación procedural de niveles: el combate es una **auto-batalla en arena 2D fija** (`WaveManager.cs`), y "diseño de niveles" en este proyecto significa **reskin narrativo/visual de esa misma arena por franja de piso**, con la dificultad escalando vía fórmulas de `combate-de-torre.md` (nunca vía geometría). Por tanto:

- Las secciones **Layout / Overview Map / Critical Path / Optional Paths / Collectibles** de la plantilla estándar no aplican y se sustituyen por una sección por bioma (narrativa + lore + visual + relleno + jefe + transición).
- **Pacing Chart** se reinterpreta como progresión de intensidad **entre franjas** (no dentro de un nivel de X minutos), ya que la torre es interminable.
- Se añade una **sección transversal de sistemas** (fuera del formato estándar) para documentar el gap de código y los criterios de aceptación de QA, porque el "nivel" en este proyecto es en parte una feature de código, no solo contenido.

## Quick Reference

- **Área/Región**: La Torre — la totalidad de su ascenso interminable, dividido en franjas de bioma
- **Tipo**: Combate (auto-batalla en arena fija), sin exploración ni puzzle
- **Duración estimada**: No aplica por nivel — la franja Goblin (pisos 1-5) es la única con techo, ~5 combates; el resto es progresión abierta
- **Dificultad**: Escalado monótono sin meseta, ver `combate-de-torre.md` sección 4 (HardFloors desde piso 4, jefe cada 5 pisos ×1.8)
- **Prerrequisito**: Ninguno para Goblin (bioma inicial); cada franja siguiente requiere haber superado el jefe de la franja anterior
- **Estado**: Concept — este documento cierra Fase 28 (diseño); implementación de código (gap de `WaveManager`) y producción de arte quedan como trabajo posterior fuera de este pipeline

## Contexto Narrativo

- **Momento de historia**: Ninguno con gancho de trama — anti-pilar de Fase 27 preservado: el bioma cuenta algo ambiental sobre la Torre, nunca trama/diálogo/misión.
- **Propósito narrativo**: Cada bioma es la Torre "reflejando de vuelta" una acción de la expedición (`design/lore/la-torre.md`, "la Torre crece respondiendo a lo que se descubre"), no territorio de una facción — la Torre no pertenece a ningún reino ni facción, regla dura.
- **Objetivo emocional por franja**: Goblin = caótico → tensión creciente; Minas = claustrofóbico; Cripta = quietud amenazante; Templo = solemnidad de poder (ancla de narrative-director, ver `project_fase28-biomas.md`).
- **Descubrimientos de lore**: Rastros de facción **opcionales y discoverables**, marcados Provisional (no canon fijo, no pasaron por narrative-director como decisión final): Abadía de Sal ↔ Goblin (ya en su ficha original)/Cripta (especulativo); Torre de Marfil ↔ Templo (especulativo).

## Mapeo Piso → Bioma

| Franja | Rango de piso | Bioma | Jefe de cierre |
|---|---|---|---|
| 1 | 1-5 | Goblin | Piso 5 |
| 2 | 6-10 | Minas | Piso 10 |
| 3 | 11-15 | Cripta | Piso 15 |
| 4 | 16-20+ | Templo | Piso 20, 25, 30... (repite sin techo) |

Los límites de franja coinciden exactamente con los pisos de jefe — cada jefe es el "cierre catalogado y sellado" de su franja (lectura literal de la ficha de La Torre).

---

## Bioma 1: Goblin (Pisos 1-5)

**Narrativa/lore**: Bioma por defecto / plaga fronteriza. Rastro opcional de Abadía de Sal (custodian lo que la Torre expulsa en pisos bajos). Arco emocional: caótico → tensión creciente.

**Visual** (art-director, Paso 1 y 4): Acento musgo `#6E7F4A`, orgánico-improvisado, luz cálida difusa. Landmarks de silueta: empalizada torcida + fogata (el signposting ya cumple accesibilidad por depender de silueta, no solo color).

**Relleno de oleada**: Goblin de a pie base — sin reskin, es el estado actual del sistema.

**Patrón "semilla temprana" (preservado, global no por bioma)**: orco tanque desde piso 4, esqueleto y chamán desde piso 3 — ya sembrados dentro de esta franja aunque su "pago" visual pleno llega en franjas posteriores.

**Jefe**: Piso 5. Único jefe calibrado del juego hoy (`Enemy_GoblinKing.asset`, 450HP/40ATK/6DEF) — reciclado en todas las franjas actualmente (ver Bug de Balance en sección transversal).

**Transición de salida (bleed prop)**: en piso 4, un prop de fondo de Minas asoma en el borde del encuadre (ej. viga colapsada tras la empalizada); en piso 5 (jefe) se intensifica — la arena rota por el jefe revela más del bioma siguiente; en piso 6 el fondo cambia por completo.

**Assets de arte pendientes**: `Env_ArenaGoblin_Clean.png`, `Env_ArenaGoblin_Transition_PreBoss.png`, `Env_ArenaGoblin_Transition_BossBreak.png` (1536×864, 32px/unidad, sRGB sin mipmaps).

---

## Bioma 2: Minas (Pisos 6-10)

**Narrativa/lore**: Respuesta a la extracción de recursos del jugador — la recompensa de hierro por piso ya escala con `piso` en `combate-de-torre.md`; es la conexión de lore más sólida de las 4, 100% anclada en mecánica ya implementada, sin facción dueña. Arco emocional: claustrofóbico.

**Visual**: Acento óxido `#8C5A3A`, angular-industrial, contraste frío-roca/cálido-antorcha puntual. Landmarks: viga colapsada + vagoneta.

**Relleno de oleada**: **Minero corrupto** (nuevo) — silueta scavenger, pico. Sprite sheet `Enemy_MineroCorrupto.png`, rig LPC 64×64/9×4 estándar reutilizado, solo retex. Stats **idénticos** al goblin base (HP50/ATK12/DEF2/moveSpeed1.5/cd1/rango1.1/magicAttack false) — decisión final del usuario, sin variación ±10-15%. Ocupa banda media (ni tanque adelantado ni rango en retaguardia), coherente con su posición ya fijada por código.

**Jefe**: Piso 10. VFX de reveal: skin "rim-light cálido" sobre el patrón base de pulso de luz + partícula.

**Transición de entrada**: bleed prop en piso 9 (Minas), intensificado en piso 10 (jefe), corte completo en piso 11 (Cripta).

**Assets de arte pendientes**: `Env_ArenaMinas_Clean.png` + 2 variantes de transición; `Enemy_MineroCorrupto.png`.

**Fricción de accesibilidad señalada** (ver sección transversal): contraste Decreto Retirada `#972527` vs fondo Minas `#8C5A3A` ≈1.4:1, por debajo de WCAG 1.4.11 — ticket diferido, no bloquea Fase 28.

---

## Bioma 3: Cripta (Pisos 11-15)

**Narrativa/lore**: Respuesta a la muerte acumulada — lectura literal de "cada piso superado se cataloga y se sella". Rastro extendido (especulativo) de Abadía de Sal. Arco emocional: quietud amenazante.

**Visual**: Acento pizarra-teal `#52625F`, geométrico-funerario, luz azulada uniforme sin fuente visible. Landmarks: nichos sellados + arco con glifos.

**Relleno de oleada**: **Siervo amortajado** (nuevo) — sirviente envuelto, deliberadamente NO esqueleto (ese slot ya existe como tipo propio "esqueleto cada 2 desde piso 3"; usar esqueleto como filler sería redundancia visual). Sprite sheet `Enemy_SiervoAmortajado.png`, mismo rig/stats/posición que Minero.

**Jefe**: Piso 15. VFX de reveal: skin "apertura de nicho con glifos que se encienden".

**Transición de entrada**: bleed prop en piso 14, intensificado en piso 15 (jefe), corte completo en piso 16 (Templo).

**Assets de arte pendientes**: `Env_ArenaCripta_Clean.png` + 2 variantes de transición; `Enemy_SiervoAmortajado.png`.

---

## Bioma 4: Templo (Pisos 16-20+, repite sin techo)

**Narrativa/lore**: Respuesta al escrutinio erudito acumulado. Rastro (especulativo) de Torre de Marfil. Arco emocional: solemnidad de poder — coherente con el tema "poder sin techo" vía repetición idéntica del filler.

**Visual**: Acento travertino `#9C8A6B` (nuevo, no reutilizado de Fase 26), monumental-columnata, luz cálida abierta tipo atardecer. Landmarks: columnas + altar/dais. **Open item no bloqueante**: verificar hex exacto de travertino vs "Reino Fronterizo" antes de producción final.

**Relleno de oleada**: **Autómata guardián** (nuevo) — centinela repetido, mismo rig/stats/posición que Minero y Siervo. La repetición idéntica del centinela es intencional, refuerza el tema de poder sin techo.

**Jefe**: Piso 20, y cada 5 pisos después (25, 30, 35...) sin techo. VFX de reveal: skin "luz de altar que sube".

**Solución al problema de presupuesto visual infinito** (fijada por el usuario en este gate): **ciclo de iluminación del altar/columnas**, 4 estados fijos, cambia 1 vez por piso de jefe (20, 25, 30, 35...), rota cíclicamente sin techo — evita depender de set dressing nuevo infinito. Contraste de HUD en el estado más oscuro del ciclo: verificar en fase de Polish (nice-to-have, no bloqueante).

**Transición de entrada**: bleed prop en piso 19, intensificado en piso 20 (jefe); a partir de piso 21 el fondo Templo se repite sin cambio estructural adicional — solo el ciclo de iluminación marca progresión.

**Assets de arte pendientes**: `Env_ArenaTemplo_Clean.png` + 2 variantes de transición; `Enemy_AutomataGuardian.png`.

---

## Pacing entre franjas (no dentro de un nivel — la torre no tiene fin)

```
Intensidad
10 |                                          * (jefe 20+, repite)
 8 |                              *(jefe15)
 6 |                  *(jefe10)
 4 |      *(jefe5)
 2 | *  *
 0 |Piso1---5---10---15---20---25---30--->∞
     Goblin | Minas | Cripta | Templo (sin techo)
```

- No existe piso "de descanso" mecánico: las fórmulas de dificultad son estrictamente monótonas, nunca hay meseta de stats.
- Cualquier "respiro" percibido dentro de una franja de 5 pisos es puramente presentacional (framing/paleta más calmada al inicio de franja: ej. piso 1 vs piso 4), nunca una reducción real de dificultad.
- Los picos de intensidad de la gráfica son los pisos de jefe (5/10/15/20+); el resto de cada franja es una rampa continua sin valle.

## Audio Direction

No se generó dirección de audio específica en este pipeline (fuera del alcance de los 5 pasos completados de Fase 28 — no hubo consulta a `audio-director`). **Dependencia cruzada abierta**: coordinar con `audio-director` en fase posterior para música/ambiente por bioma y stingers de reveal de jefe.

## Sistemas Transversales (fuera del formato estándar de la plantilla)

### Gap de código — BLOCKING

`WaveManager.PickEnemyData()` y `SpawnBoss()` usan hoy un único campo `EnemyData`/`bossData` fijo, sin selección por piso/bioma. Fix quirúrgico propuesto por systems-designer: sustituir el `return enemyData` de fallback por resolución de rango de piso, mismo patrón que los campos existentes tirador/orco/chamán/esqueleto — 3 campos nuevos (minero/siervo/autómata), drop-in replacement, cero riesgo de aggro (confirmado contra `tankHealthThreshold=70`, `maxAggroPerTank=2`). Sin loot ni pasiva especial (no hay hook de sistema para eso). Requiere `unity-specialist`, es implementación posterior a este documento.

### Bug de balance de jefe — DIFERIDO A FASE 29

`bossData` nunca escala con piso (siempre `Enemy_GoblinKing.asset`, 450HP/40ATK/6DEF); desde ~piso 10-15 el relleno normal ya supera al jefe en poder. Baseline de extrapolación documentado para Fase 29 (Balance de Combate), no fijado aquí: Ratio_HP=4.5, Ratio_ATK=1.22, Ratio_DEF=3.0, calculados desde el único jefe calibrado.

### Accesibilidad — ticket diferido

Contraste Decreto Retirada `#972527` vs fondo Minas `#8C5A3A` ≈1.4:1, muy por debajo de WCAG 1.4.11 (patrón más leve también en Goblin/Curar y Templo/Enfocar). Mitigación propuesta: backing opaco universal en `MasterActionBar`/HUD de Decretos (posible reutilización de `UITheme.GlassDeep`), independiente del fondo. No bloquea el cierre de Fase 28 — se revisa junto al HUD, con este baseline numérico documentado.

### Criterios de aceptación ("Done" de Fase 28)

| Criterio | Tipo | Bloqueante |
|---|---|---|
| WAVE-01 a 04: relleno correcto por franja (minero/siervo/autómata en su rango de piso) | Logic | BLOCKING |
| WAVE-05: transición 5→6 con jefe funcional | Logic | BLOCKING |
| WAVE-06: framing de jefe por bioma (VFX de reveal correcto) | Visual/Feel | ADVISORY |
| WAVE-07/08: corte exacto de franja (piso 5 sigue Goblin, no Minas) | Logic | BLOCKING |
| WAVE-09: piso 21+, repetición Templo sin techo, ciclo de iluminación no se rompe | Logic | BLOCKING |
| WAVE-10: reintento de piso ya superado, consistencia determinista | Logic | BLOCKING |
| WAVE-11: bug de jefe reciclado (informativo, baseline Fase 29) | Logic | NO bloquea Fase 28 |
| Transiciones bleed-prop coherentes en contexto real | Visual/Feel | ADVISORY |
| Contraste Decreto-Minas medido en vivo con HUD real | Visual/Feel | ADVISORY (ticket diferido) |
| Aggro/posicionamiento idéntico con los 3 rellenos nuevos | Logic | BLOCKING |

Evidencia: pruebas de Logic en `tests/unit/wavemanager/` (primera vez que existe `tests/` en el proyecto); evidencia Visual/Feel en `production/qa/evidence/`.

## Dependencias Cruzadas Abiertas (fuera de este documento)

1. **Fase 29 (Balance de Combate)**: resolver el bug de jefe reciclado con la fórmula de extrapolación como baseline.
2. **Implementación (`unity-specialist`)**: fix de `WaveManager.PickEnemyData()`/`SpawnBoss()`; backing opaco de HUD para el ticket de contraste Decreto-Minas.
3. **Producción de arte**: 10 assets de fondo nuevos (4 limpios + 6 transición), 3 sprite sheets de enemigo nuevos, 4 VFX de reveal de jefe (skins por bioma sobre patrón base).
4. **`audio-director`**: dirección de audio por bioma, no cubierta en este pipeline.
5. **`narrative-director`**: si se decide promover algún rastro de facción (Abadía de Sal/Torre de Marfil) de "Provisional" a canon fijo.
</content>
