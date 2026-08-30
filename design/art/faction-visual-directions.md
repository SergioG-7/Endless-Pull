# Dirección Visual por Facción de Origen

**Alcance**: Fase 27 (Narrativa, Lore y Localización), Fase 2 de `/team-narrative`.
Esto NO es un art bible completo — es dirección mínima para que world-builder
y writer no contradigan visualmente lo que ya existe en el proyecto mientras
redactan la ficha de lore de cada una de las 7 facciones/orígenes de héroe.
Sirve además como semilla del futuro art bible (no existía ningún doc en
`design/art/` antes de este archivo).

Anti-pilar respetado: nada de cinemáticas ni dirección de escena — solo
silueta/paleta/motivo aplicable a iconografía y sprites de héroe.

## Contexto técnico verificado

- Los 50 héroes ya tienen sprite art real (`Assets/_EndlessPull/Sprites/Heroes/*.png`),
  no el técnica de "cuadrado plano teñido" que usa la Base — son sprite sheets
  chibi de 4 direcciones con pelo/piel/ropa/arma variables por héroe
  (pipeline tipo generador, ej. `Hero_Aaron_2Star.png`, `Hero_Astraea_5Star.png`).
- El campo `origin` en `HeroData.cs` es hoy un string libre (default
  `"Reino Fronterizo"`) sin ningún dato visual asociado — este doc es la
  primera dirección visual que existe para ese campo.
- Fuente de verdad de color del proyecto: `Assets/_EndlessPull/Scripts/UITheme.cs`.
  No se propone ninguna paleta nueva que no derive de ahí.

## Principio de jerarquía visual

La rueda de color del proyecto ya está saturada de significado: acentos de
`UITheme` (violeta/cian/ámbar/teal), los 4 colores de Decreto (verde/naranja/
azul/rojo, HUD de combate), la rampa de rareza de 5 niveles (verde/azul/
morado/dorado/gris) y los acentos de bioma por cuadrante recién aprobados
(musgo `#6E7F4A`, óxido `#8C5A3A`, pizarra-teal `#52625F`).

Darle a cada una de las 7 facciones su propio "color de equipo" saturado
competiría con la rareza — que es la señal que más importa en una carta de
héroe (valor de invocación) — y arriesga la misma casi-colisión que ya se
señaló entre el óxido de Sur y `DecreeRetreat`. Por eso la identidad de
facción se construye así, en este orden de prioridad:

1. **Silueta/arma** (diferenciador primario — se lee más rápido que el color
   en iconos pequeños).
2. **Paleta desaturada/terrosa** (diferenciador secundario — nunca un bloque
   de color saturado que compita con rareza o Decretos).
3. **Motivo/accesorio pequeño** (diferenciador terciario — insignia, colgante
   o parche aplicable sin rehacer el sprite base).

## Tabla de dirección visual

| Origen | Silueta / arma | Paleta (desaturada) | Motivo |
|---|---|---|---|
| **Reino Fronterizo** (mayoritaria, ~40/50) | Equipo rústico y práctico, envolturas de cuero/piel, armas improvisadas (daga, lanza, honda) — ya coincide con `Hero_Aaron` existente. | Neutros cálidos: cuero tostado, tela cruda sin teñir, un toque de ocre. Deliberadamente la paleta más silenciosa — al ser la facción mayoritaria debe leerse como línea base visual, no competir por atención. | Ninguno, o una simple correa de cuero — el "sin marcar" contra el que las demás facciones destacan. |
| **Torre de Marfil** (élite académica/mágica) | Proporciones altas/esbeltas, túnica estructurada de cuello alto, arma tipo bastón/tomo. | Neutros pálidos y fríos: marfil/hueso, gris pizarra pálido, un filo escaso de azul pálido cercano a `UITheme.Cyan` solo como ribete de prestigio — nunca un bloque de color completo. | Sigilo geométrico pequeño (rosa de los vientos o libro abierto) como pin de cuello. |
| **Abadía de Sal** (monástica costera) | Manto monástico en capas, cinturón de cuerda o toca, arma tipo maza/mayal/símbolo sagrado. | Blancos y grises curtidos por el sol, con un toque de rosa polvo o coral apagado — más cálido y curtido que el marfil frío de Torre de Marfil, para que no se lean como el mismo "blanco". | Medallón circular pequeño (anillo de sal/halo) en el cuello. |
| **Ciudad Baja** (bajos fondos urbanos) | Capas de retazos/recuperado, asimetría deliberada (una manga distinta, un hombro descubierto), arma tipo daga/gancho/herramienta improvisada, proporciones ajustadas (lectura ágil). | Gris hollín y índigo desteñido, con un parche de color visiblemente desparejado en vez de un tono unificado — lo desparejado ES la identidad. | Brazalete de tela envuelta, no un emblema limpio (refuerza lo improvisado, contrasta con la uniformidad de Torre de Marfil). |
| **Marca Quemada** (mercenaria de yermo, cicatrizada) | Placas asimétricas pesadas o metal recuperado, silueta de arma más grande (hacha/cuchilla), lectura de vendaje. | Neutros carbonizados: negro hollín, gris ceniza, un acento ember apagado — siena quemado (~`#7A4B33`), no un naranja/rojo limpio, para mantenerse alejado de `DecreeFocus`/`DecreeRetreat`. | Sigilo marcado/cauterizado pequeño en la armadura — literaliza "Marca". |
| **Fiordos del Norte** (nórdica) | Envolturas en capas con ribete de piel, proporciones más anchas/robustas, arma tipo hacha/martillo, pelo trenzado. | Azul-grises fríos desaturados, inclinados a azul-escarcha (~`#5E7A99`) en vez de teal — mantiene separación clara del teal-pizarra de Oeste/cripta `#52625F` y de `UITheme.Teal`, combinado con piel color hueso. | Cierre tallado en hueso/asta o broche de nudos. |
| **Bosque de Alder** (dracónico/élfico, forestal) | Capas naturalistas de cuero/enredadera, arma tipo arco/honda, pelo largo y suelto — ya coincide con `Hero_Astraea` existente. | Verde hoja-viva (~`#4C8A5E`, más frío y saturado que el musgo apagado de Este/goblin `#6E7F4A`) combinado con marrón corteza cálido, nunca gris — evita leerse como el mismo token que el flash del piso goblin. | Brazalete tejido de enredadera/hoja o dije pequeño de ramita y asta. |

## Nota de coordinación para world-builder y writer

Ninguna de estas paletas es grimdark — sin negro-y-sangre, sin neón, sin
motivos de horror. Se mantienen en el mismo registro pictórico tipo
cuento-ilustrado ya visible en `Hero_Aaron` (rústico pero cálido) y
`Hero_Astraea` (elegante pero no santoral), coherente con "fantasía ligera,
toque de gravedad" ya fijado.

Si la ficha de lore de alguna facción termina inclinándose con más fuerza
en una dirección distinta a la asumida aquí (ej. si Torre de Marfil resulta
más militante-académica que puramente arcana, o Ciudad Baja se escribe como
gremio organizado en vez de improvisada/desorganizada), la silueta debe
seguir al lore, no al revés — señalarlo de vuelta a art-director para
ajustar esta tabla.

Al escribir las fichas de lore, evitar asignarle a ninguna facción un color
propio "de marca" saturado (ni siquiera como metáfora en el texto) que
choque con: rareza (verde/azul/morado/dorado/gris), Decretos (verde=Curar,
naranja=Enfocar, azul=Reagrupar, rojo=Retirada) o los acentos de bioma por
cuadrante (musgo/óxido/pizarra-teal). Esos tres sistemas de color ya son
gameplay-crítico y deben seguir siendo inequívocos.
