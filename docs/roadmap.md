# Roadmap de Produccion Endless Pull

## Fase 32 Reorganización, Agrupación y Estandarización de UI (Fase de Pulido) — PENDIENTE

- Unificar dimensiones de modales al 80% centrado (aplazado, riesgo alto sin
  verificación visual).
- Labels cortados fuera de la tienda (sin causa concreta localizada, distinto del
  caso ya resuelto de `Txt_Inventory`).
- Verificación visual real en Editor del TopBar (los cambios de layout no se
  pudieron capturar por pantalla, solo se verificó por consola sin errores).
- Barrido general de QA por más bugs/huecos del prototipo.

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

## Fase 33 BUGS + FIXES (COMPLETA)

Compila limpio vía Unity MCP (0 errores, solo 2 warnings cosméticos sin relación:
pipeline server no-automated, glifo ▾ sin fallback en LiberationSans SDF). Escena
`Base.unity` carga bien, `Quadrant_North` con veil+ground presente, sin objetos
huérfanos. Verificado en Play Mode (arranca sin excepciones).

Pendiente de verificación visual humana (no se pudo confirmar con capturas):
posiciones/tamaños exactos del cuadrante Norte y los 4 suelos placeholder,
sensibilidad de zoom/pan de cámara, ancho del chip de recursos fusionado
(Gemas+Madera+Hierro+Comida, puede desbordar), reflow de las 3 tarjetas de
Equipo en el Taller (quedaron en su posición original dentro de su pestaña,
no en fila limpia), y si el menú "Tienda" debería renombrarse ahora que ya
no vende equipo con gemas (solo queda el inventario/almacén).

Nombres de archivo reales usados (el roadmap original citaba nombres que no
existen en el repo): wander en `HeroController.cs` (no `HeroWanderAI.cs`),
cuadrantes en `QuadrantController.cs` (no `BaseManager.cs`), TopBar en
`MasterHUD.cs` (no `TopBarUI.cs`), Taller en `AlchemyWorkshopUI.cs` (no
`WorkshopUI.cs`; `CraftingUI.cs` quedó como código muerto/legacy, sustituido
por `AlchemyWorkshopUI` que ya redirige ahí), feedback en `MasterCommander.cs`
+ `UnaffordableFeedback.cs` nuevo (no `HUDController.cs`).

Corrige en bloque los siguientes problemas visuales, de layout y ergonomía:

1. Espaciado de Base, Cámara y Biomas (BaseManager.cs, HeroWanderAI.cs, CameraDirector.cs):

- Separación de Edificios: Redistribuye las coordenadas (x, y) de los edificios en los 4 cuadrantes. Duplica la distancia entre ellos para que no se solapen carteles, textos ni colliders.
- Área de Deambulación: Asigna zonas de wander independientes por cuadrante para que los 50 héroes no se agrupen en un único enjambre central.
- Fondo/Suelo: Añade una base visual o suelo con color/malla a los cuadrantes para que la base no parezca bloques flotando en un fondo negro.
- Control de Cámara: Implementa zoom básico (rueda del ratón / pinch táctil) y arrastre (pan) con límites (clamping) sobre los cuadrantes desbloqueados.
- Biomas de Torre: Asegura que el fondo/iluminación del bioma correspondiente (Bosque, Minas, Cripta, Templo) se active en la arena de combate según el piso actual.

2. TopBar HUD y Localización (TopBarUI.cs, MasterHUD.cs):

- Compactación: Agrupa los recursos (Gemas, Madera, Hierro, Comida) en un contenedor compacto en la esquina superior derecha con icono + cifra.
- Localización limpia: Reemplaza todos los textos hardcodeados ("MADERA", "HIERRO", "COMIDA", "TORRE", "Piso") por claves dinámicas de `LocalizationManager.cs`.

3. Modal de Detalle de Héroe (HeroDetailModal.cs / Prefab):

- Botón de Cierre [X]: Reubica el botón de cerrar en la esquina superior derecha del panel principal con padding suficiente, completamente separado de "EN ESCUADRA" y botones de acción.
- Aprovechamiento del Espacio: Redistribuye las pasivas, equipo y atributos en dos columnas equilibradas para eliminar el espacio muerto inferior.

4. Santuario (SanctuaryUI.cs):

- Lista de Héroes: Ajusta el padding/offset del scrollview para que el avatar/foto del héroe se renderice completo y sin cortes en el borde izquierdo.
- Previsualización de Síntesis: Muestra tanto el héroe base/sacrificio como la vista previa del héroe resultante (estrellas y stats proyectadas) en el panel derecho.

5. Taller Organizado por Pestañas (WorkshopUI.cs):

- Sistema de Pestañas (Tabs): Reestructura el Taller en 3 pestañas independientes para evitar paneles superpuestos:
  - Pestaña 1: Forja de Piedras (Piedra Menor/Media/Mayor).
  - Pestaña 2: Forja y Reparación de Equipo (Fabricar, Mejorar, Reparar).
  - Pestaña 3: Alquimia (Pociones de Curación).
- Limpieza de Tienda: Elimina la pestaña de compra directa de equipo con gemas en `ShopUI.cs` (el equipo se obtiene exclusivamente por crafteo y recompensas).

6. Game Feel y Feedback (HUDController.cs, ShopUI.cs):

- Botón "Curar Todos": Si todos los héroes ya tienen el 100% de HP, muestra un aviso flotante (Toast/Banner) tipo "Todos los héroes ya están al máximo de salud" o reproduce un sonido neutro.
- Compra fallida / Recursos insuficientes: Muestra feedback visual (parpadeo rojo del recurso o shake leve) cuando el jugador no tenga suficientes gemas/materiales.

7. Verificación:

- Compila limpio vía Unity MCP (0 errores).
- Valida en Play Mode que todos los modales abran sus pestañas sin solapamientos de capas (Z-index/Raycast).
