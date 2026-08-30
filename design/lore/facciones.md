# Facciones y Orígenes — Lore

---
**Canon Level**: Established
**Visible To Player**: Discoverable (nombre de origen visible en ficha de
héroe; el resto de esta entrada es contexto ambiental de referencia, no
todo aparece necesariamente en texto de juego)
**Fecha**: 2026-08-30
**Autor**: world-builder (Fase 27, `/team-narrative`)
**Source**: `Assets/_EndlessPull/Scripts/HeroData.cs` (campo `origin`),
10 `Hero_*.asset` con `origin` no-default, `design/gdd/combate-de-torre.md`
(sinergia de origen +5% ATK/DEF), `design/gdd/game-concept.md` (referencia
a *Pick Me Up*)
**Cross-References**: [[la-torre]], [[decretos-del-maestro]],
[[jerarquia-social]]
---

## Nota de nomenclatura — evitar confusión

**"Torre de Marfil" (facción) no es el mismo edificio que "la Torre"**
(la mazmorra endless que escala el jugador). Torre de Marfil es una academia
de eruditos que **estudia** la Torre desde fuera; comparte la palabra
"Torre" por convención idiomática (torre de marfil = claustro académico
alejado del mundo), no porque sea la misma estructura. Cualquier bio, UI o
texto de arte debe distinguirlas explícitamente si aparecen juntas.

## Por qué compartir origen tiene sentido narrativo (no solo mecánico)

La sinergia de +5% ATK/DEF por origen compartido (`combate-de-torre.md`) no
es un bono arbitrario: cada facción de esta lista tiene una razón de mundo
para que sus miembros luchen mejor codo a codo — entrenamiento compartido,
disciplina de clan, o simplemente conocerse de antes. Cada ficha incluye esa
razón en su sección de Relevancia para el jugador.

---

## 1. Reino Fronterizo

**Concepto**: el reino grande y ordinario junto a la Torre — ni rico ni
pobre, ni en guerra ni en paz duradera. Agricultores, guarniciones y pueblos
de paso que llevan generaciones viviendo a su sombra.

**Historia**: sin evento fundacional especial — es, precisamente, el "reino
por defecto" del mundo. Su proximidad a la Torre lo convierte, sin más
mérito narrativo que la geografía, en su mayor cantera de reclutas.

**Conexiones**: [[la-torre]] (proximidad geográfica).

**Relevancia para el jugador**: la mayoría de los ~50 héroes proceden de
aquí porque es, literalmente, el reino más cercano a la Torre. Compartir
origen entre dos Fronterizos representa "compañeros de pueblo o de
guarnición" — el vínculo más común y menos dramático del roster, a
propósito: no debe competir en protagonismo con las 6 facciones menores.

**Chequeo de contradicciones**: ninguna — `origin` por defecto en
`HeroData.cs` ya es `"Reino Fronterizo"`.

---

## 2. Torre de Marfil

**Concepto**: orden de eruditos-magos que estudia la Torre desde fuera de
sus muros (ver nota de nomenclatura arriba). Disciplina, observación y
paciencia por encima de la fuerza bruta.

**Historia**: fundada por quienes primero catalogaron los pisos y los
decretos; hoy mantiene la doctrina de mando que hace posible el vínculo del
Maestro (ver [[decretos-del-maestro]]).

**Conexiones**: [[decretos-del-maestro]] (origen de la doctrina).

**Relevancia para el jugador**: Astraea (5★, "Saeta del Alba", francotiradora
legendaria) viene de aquí — su precisión a distancia es un reflejo directo
de la disciplina de observación de la academia. Dos eruditos de la Torre de
Marfil comparten método de entrenamiento y postura de combate, de ahí la
sinergia.

**Chequeo de contradicciones**: la bio de Astraea (`Hero_Astraea_5Star.asset`)
no menciona facción; no hay contradicción.

---

## 3. Abadía de Sal

**Concepto**: orden monástica **militante** — su vigilia es física, no
contemplativa. La sal es símbolo de preservación y purificación contra la
corrupción que a veces trae consigo lo que baja de los primeros pisos de la
Torre.

**Historia**: originalmente guardianes de salinas fronterizas, la orden se
volvió militante cuando empezó a custodiar activamente lo que la Torre
"expulsaba" en sus pisos más bajos.

**Conexiones**: [[la-torre]] (custodia de lo que sale de los pisos bajos).

**Relevancia para el jugador**: Ceres (2★, "Hermana de la Vigilia", combate
frontal cuerpo a cuerpo) viene de aquí — coherente con una orden militante,
no una de sanadoras pasivas. Dos hermanas de la Abadía comparten disciplina
de vigilia y no rompen formación entre sí, de ahí la sinergia.

**Chequeo de contradicciones**: el título "Hermana de la Vigilia"
(`Hero_Ceres_2Star.asset`) refuerza directamente el concepto; no hay
contradicción. Nota: el `origin` en el asset está escrito sin tilde
(`Abadia de Sal`) — fuera del alcance de esta entrega, no se ha tocado el
asset.

---

## 4. Ciudad Baja

**Concepto**: el estrato más pobre y marginal de una gran ciudad no
nombrada — callejones, mercado negro, supervivencia diaria.

**Historia**: sin fundación propia; es simplemente el nivel bajo de una
urbe mayor. Manda gente a la Torre porque es una de las pocas vías reales
de ascenso social.

**Conexiones**: ninguna cruzada con otra facción — deliberadamente aislada,
coherente con su aislamiento social dentro de su propia ciudad.

**Relevancia para el jugador**: Garrick (3★, "Sargento de Brecha", tanque
veterano) y Pip (1★, "Ratero de los Tejados", novato) son dos extremos de
la misma calle — el que ya sobrevivió años, el que recién se atreve. Se
cubren la espalda igual que en su barrio, de ahí la sinergia.

**Chequeo de contradicciones**: ninguna en las bios existentes.

---

## 5. Marca Quemada

**Concepto**: territorio fronterizo curtido por guerra e incendios
recurrentes; sus habitantes son duros por necesidad, no por elección.

**Historia**: tierra disputada y quemada de forma recurrente (se deja el
conflicto exacto sin nombrar, a propósito — detalle de trama fuera de
alcance de lore ambiental).

**Conexiones**: ninguna cruzada — territorio aislado por el propio
conflicto que lo define.

**Relevancia para el jugador**: Kraven (5★, "Verdugo del Ocaso", cuerpo a
cuerpo letal) y Nerezza (4★, "Tejedora de Cenizas", a distancia) son dos
maneras de sobrevivir al mismo fuego — golpear primero, o herir desde
lejos. Los títulos ya evocan fuego y ceniza, reforzando por qué luchan bien
juntos: comparten instinto de superviviente de guerra.

**Chequeo de contradicciones**: "Tejedora de Cenizas" y "Verdugo del
Ocaso" refuerzan el concepto en vez de contradecirlo.

---

## 6. Fiordos del Norte

**Concepto**: clanes guerreros de costas frías, resistentes, con tradición
marcial transmitida dentro del propio clan.

**Historia**: pueblos aislados por el clima que envían a sus jóvenes a la
Torre como rito de paso y fuente de prestigio para el clan.

**Conexiones**: ninguna cruzada — aislamiento geográfico coherente con el
clima.

**Relevancia para el jugador**: es el origen más poblado de las 6 facciones
menores — Solveig (3★, "Lectora de Escarcha"), Torvald (2★, "Escudero del
Risco") y Ulric (4★, "Yunque de Invierno") comparten disciplina de clan y
formación de combate en hielo, lo que hace especialmente natural formar
escuadra completa de Fiordos para la sinergia de origen.

**Chequeo de contradicciones**: ninguna — los tres títulos ya comparten
motivos de hielo/frío/roca, coherente con el concepto de clan norteño.

---

## 7. Bosque de Alder

**Concepto**: comunidad forestal de rastreadores y exploradores que vive en
equilibrio con un bosque que no siempre es benigno.

**Historia**: envía batidores a la Torre porque su habilidad de rastreo y
observación es valiosa para cualquier expedición que necesite anticipar
peligro.

**Conexiones**: ninguna cruzada — comunidad autosuficiente por diseño.

**Relevancia para el jugador**: Wren (2★, "Ojo del Sotobosque", exploradora
a distancia) viene de aquí — encaja directamente con el concepto de
rastreo. Es, de momento, un origen de un solo héroe: la sinergia queda lista
para cuando se añadan más héroes de Alder.

**Chequeo de contradicciones**: ninguna.
