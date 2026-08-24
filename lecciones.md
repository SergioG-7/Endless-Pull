# Lecciones — Endless Pull

Cosas que nos hemos encontrado trabajando en este proyecto y que conviene no volver a descubrir desde cero.

**Entorno:** Unity 6000.5.8f1 (6.5), plantilla URP 3D, Windows. MCP para Unity vía `~/.unity/relay/relay_win.exe`.

---

## Unity 6 / configuración del proyecto

**El tag `Enemy` ya no viene por defecto.** Unity 6 solo trae `Untagged`, `Respawn`, `Finish`, `EditorOnly`, `MainCamera`, `Player`, `GameController`. Hubo que añadirlo a mano en `ProjectSettings`.

**`activeInputHandler: 1` — solo el nuevo Input System.** `Input.GetKeyDown` lanza excepción en runtime. Hay que usar `Keyboard.current.spaceKey.wasPressedThisFrame`. Se comprueba con:
```
grep -n "activeInputHandler" ProjectSettings/ProjectSettings.asset
```

**Los botones de UI necesitan un `EventSystem` con `InputSystemUIInputModule`**, no el `StandaloneInputModule` clásico.

**TMP Essentials no viene importado.** Sin él, cualquier `TextMeshPro` sale roto. Se importa por código con `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false)`.

**La fuente por defecto (LiberationSans) no tiene emojis ni símbolos raros.** Un `💎` sale como cuadrado, y `★` (U+2605) también: TMP lo sustituye por `□` y **avisa por consola en cada rebuild del texto**. Con un roster que se refresca cada 0.5 s eso son cientos de warnings por minuto. En texto de UI, ASCII (`*`); en `Debug.Log` da igual, la consola sí los pinta.

**Un panel de tamaño fijo se sale de pantalla si el aspecto no es el de referencia del `CanvasScaler`.** El `RosterPanel` a 1000×720 sobre una Game View de 2658×960 quedaba en `y[-18..978]`. Se comprueba con `GetWorldCorners` contra `Screen.width`/`Screen.height` en Play.

**El proyecto sigue siendo la plantilla URP 3D.** Los sprites renderizan bien y la cámara es ortográfica, pero el renderer no es el 2D Renderer. Si algún día se quieren luces 2D, hay que cambiarlo.

---

## C# / Unity API

**`using System;` rompe `Object` y `Random`.** Se vuelven ambiguos con `UnityEngine.Object` y `UnityEngine.Random` (error CS0104). En cualquier script que necesite `System.Action`, hay que cualificar: `UnityEngine.Object.FindObjectsByType<T>()`, `UnityEngine.Random.Range()`. Alternativa mejor: no importar `System` y escribir `System.Action` a pelo.

**`FindObjectsByType` no garantiza ningún orden.** Da igual para contar o para buscar el más cercano, pero si con esa lista se pintan filas de UI **con botones**, las filas cambian de sitio en cada refresco y se pulsa lo que no era. Hay que ordenar explícitamente por algo estable (en el roster: estrellas y luego nombre).

**`FindObjectsByType` NO devuelve componentes deshabilitados.** Desactivar un `HeroController` lo hace invisible para los enemigos, no lo convierte en un objetivo quieto. Importa si algún día hay héroes aturdidos o en banquillo.

**Crear ScriptableObjects por código con `Upsert`, no con `CreateAsset` a secas.** `LoadAssetAtPath` primero: si existe se actualizan los campos, si no se crea. Así el mismo script sirve para poblar el catálogo la primera vez y para reajustar stats después, sin duplicar assets ni romper las referencias que ya apuntan a ellos (GUID intacto). Al terminar, `SetDirty` por asset y un `SaveAssets` al final.

**Un campo nuevo en un ScriptableObject ya existente coge el inicializador.** Al añadir `public int maxMP = 50;` a `HeroData`, los tres `.asset` que ya estaban en disco salieron con 50 sin tocarlos: su YAML no tiene entrada para ese campo, así que Unity deja el valor del inicializador. Lo mismo vale para un `[SerializeField]` de una clase `[System.Serializable]` en un prefab ya guardado (`HeroSkill` en el prefab del héroe).

**Nunca mutar el ScriptableObject compartido.** Subir de nivel un Loki no puede tocar `Hero_Loki_1Star.asset`: afectaría a todos los Loki y el cambio persistiría al salir de Play. Los bonus van **por instancia** (`bonusMaxHealth`, `bonusAttack` en `HeroController`; `statMultiplier` en `EnemyController`) y las propiedades públicas los suman sobre el dato base.

**Patrón para asignar datos tras `Instantiate`.** `Awake` ya corrió cuando puedes tocar el objeto, así que:
- `Awake` inicializa **solo si** hay datos, sin dar error.
- `Initialize(...)` asigna datos y dispara el evento de vida.
- `Start` valida y da error si siguen faltando datos.

Así la barra de vida (que lee en su `Start`) ya ve valores correctos.

**`Vector2.MoveTowards` hacia el centro del objetivo produce temblor.** El enemigo se metía encima del héroe y oscilaba. La solución tiene dos partes:
1. Comprobar la distancia **antes** de mover; si ya está en rango, cambiar de estado y `return` sin avanzar.
2. Clampar el paso: `Mathf.Min(speed * dt, distance - attackRange)`, así aterriza exacto en el borde.

Verificado frame a frame con el editor pausado: se queda en `x=1.200000`, `dist=1.100000`, sin variación.

**Rigidbody2D en Kinematic si mueves por `transform.position`.** En Dynamic pelea contra el solver cada frame.

---

## MCP para Unity

**El bridge se cae en cada domain reload.** Cualquier cosa que recompile (crear/editar scripts, entrar en Play) mata el bridge unos segundos. El patrón que funciona:
```bash
until ls "$HOME/.unity/mcp/connections/"*.json >/dev/null 2>&1; do sleep 2; done
```

**Las herramientas MCP se cargan al arrancar la sesión de Claude Code, no a mitad.** Si el servidor se conecta después, hay que salir y volver a entrar. `claude mcp list` puede decir "Connected" y aun así no haber ni una herramienta disponible.

**Bridges huérfanos.** `~/.unity/mcp/connections/` acumula ficheros de editores muertos. Con varios, el relay puede apuntar al proyecto equivocado. Conviene limpiarlos.

**`Unity_RunCommand` envuelve el código en un namespace propio** (`Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`). Eso rompe la resolución de tipos:
- `Image` → hay que escribir `UnityEngine.UI.Image`
- `CompilationPipeline` → `UnityEditor.Compilation.CompilationPipeline`

**`System.Reflection` está prohibido en `RunCommand`.** Devuelve "unauthorized namespaces". Hay que llamar a las APIs directamente.

**`result.Log` no aplica especificadores de formato.** `result.Log("x={0:F0}", v)` imprime `{0:F0}` literal. Solo funciona `{0}`; para decimales, concatenar o `Mathf.RoundToInt`. Y ojo: envuelve cada argumento en corchetes, así que `x={0}` sale como `x=[3]`.

**Campos `[SerializeField] private` se cablean con `SerializedObject`.** `new SerializedObject(comp).FindProperty("nombre").objectReferenceValue = obj` + `ApplyModifiedPropertiesWithoutUndo()`. No hace falta reflexión (que además está prohibida).

**Listeners persistentes desde código:** `UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(boton.onClick, new UnityEngine.Events.UnityAction(comp.Metodo))`. Quedan guardados en la escena y se ven en el Inspector, a diferencia de `AddListener`.

**El refresco no es automático.** Escribir un `.cs` con la herramienta `Write` no basta. Secuencia fiable:
```csharp
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
```

**Carrera al escribir varios scripts seguidos.** Si lanzas la compilación mientras el último `.cs` se está escribiendo, ese fichero queda fuera del assembly y aparece como CS0246. Hay que comprobar que el DLL es más nuevo que **el último** fichero tocado, no que uno cualquiera.

**`.meta` truncado = script invisible para el AssetDatabase.** Nos pasó con `BuildingUpgradeUI.cs`: el fichero y el `.meta` existían, pero `AssetDatabase.LoadAssetAtPath<MonoScript>` devolvía `null` y el tipo no compilaba. Diagnóstico y arreglo:
```csharp
var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);  // False = meta roto
```
Se borra el `.meta`, se hace `touch` al `.cs` y se refresca. Unity lo regenera. Solo es seguro si nada referencia ese script por GUID todavía.

**Escenas en aditivo para no pisar el trabajo del usuario.** `EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)` + `MoveGameObjectToScene` + `SaveScene` + `CloseScene` deja intacta la escena que hubiera abierta.

---

## Persistencia

**JsonUtility no serializa diccionarios, pero sí `List<T>` dentro de una clase `[System.Serializable]`.** Los niveles de edificio van como lista de `{id, nivel}` y se cotejan por nombre de GameObject al cargar. Los enums (`HeroTrait`) salen como int.

**Cargar en `Start`, no en `Awake`.** `BaseBuilding.All` se puebla en los `OnEnable` de los edificios, y Unity intercala `Awake`/`OnEnable` objeto a objeto: en el `Awake` del manager la lista puede estar a medias.

**El orden entre `Start` no se puede asumir, pero el de `OnEnable` sí.** Todos los `OnEnable` corren antes que cualquier `Start`. Por eso el `SaveManager` restaura disparando eventos (`GemsChanged`, `MaterialsChanged`, `FloorChanged`): quien escucha ya se suscribió en su `OnEnable`, y da igual quién arranque primero. Al `WaveManager` hubo que añadirle `FloorChanged` justo por esto; sin él, el label del piso se quedaba viejo si `MasterHUD.Start` corría antes.

**Desactivar antes de destruir al reconstruir el roster.** `Destroy` es diferido, así que un héroe destruido sigue apareciendo en `FindObjectsByType` durante ese frame. `SetActive(false)` + `Destroy` lo saca de la búsqueda ya (los componentes de objetos inactivos no se devuelven).

**Rehacer el nivel aplicando los bonus uno a uno.** Los bonus por instancia son acumulativos (+10% de la vida máxima *actual*), así que restaurar el nivel 5 es llamar cuatro veces a `ApplyLevelUpBonus`, no multiplicar por 1,1^4. Y como cada subida deja la vida al máximo, la vida guardada se escribe **después**.

**`OnApplicationQuit` salta al salir del Play Mode en el editor.** Sirve para que maná y fatiga, que cambian cada frame, se guarden con su valor real al cerrar.

**Sorteo ponderado sobre un subconjunto: hay que recalcular los pesos.** El gacha filtra el catálogo por lo que aún no se tiene, así que `RollRarity` solo puede sumar el peso de las rarezas que sigan teniendo a alguien libre. Con los pesos originales se sorteaba una rareza ya agotada y el *fallback* devolvía un héroe cualquiera **del catálogo entero**, es decir, un duplicado. El respaldo tiene que caer sobre la lista filtrada, nunca sobre la original.

**Cobrar después de comprobar, no antes.** La tirada validaba las gemas primero y devolvía el importe si el sorteo fallaba. Con una segunda causa de fallo (catálogo completo) sale más limpio bloquear antes de tocar la economía.

---

## Verificación en Play

**Disparar el `onClick` real, no el método.** `boton.onClick.Invoke()` prueba también el cableado del `UnityEvent`; llamar al método directamente no. Se comprueba con `GetPersistentMethodName(0)`.

**Cuidado al medir algo que el juego regenera solo entre comandos.** Intentando dejar a un héroe desmoralizado, la cantina le subía la moral entre una llamada MCP y la siguiente, y las restas no cuadraban. Todo lo que se mida contra un umbral hay que forzarlo y comprobarlo **en la misma pasada**.

**Un objeto recién destruido no es `== null` en el mismo frame.** `Destroy` es diferido, así que comprobar `fodder == null` justo después de sacrificarlo da `false`. La prueba buena es contar cuántos quedan con `FindObjectsByType`, que sí lo ignora si además se hizo `SetActive(false)`.

**Warnings en `Unity_RunCommand` cuentan como fallo del comando.** Un `Debug.LogWarning` durante la ejecución hace que la herramienta devuelva `UNEXPECTED_ERROR` aunque todo haya ido bien: los logs vienen en el mensaje de error. Si la prueba consiste en provocar avisos (una tirada bloqueada, por ejemplo), hay que leer el texto del error, no darlo por roto.

**Entrar en Play ya pausado para cotejar un estado inicial.** `EditorApplication.isPaused = true;` antes de `EditorApplication.EnterPlaymode()` deja el juego parado en el primer frame, con los `Start` ya ejecutados. Imprescindible para comparar lo cargado contra el fichero: entre el `Play` y el primer comando MCP pasan varios segundos reales y el maná se regenera, la fatiga baja y los héroes suben de nivel.

**Pausar y avanzar frame a frame para medidas exactas.** `EditorApplication.isPaused = true` + `EditorApplication.Step()` permite muestrear posiciones con 6 decimales. Mucho más fiable que `Time.timeScale`, que en varios intentos dejó morir al enemigo antes de poder medir.

**El Input System descarta el teclado si la Game View no tiene foco.** Ni inyectar eventos con `InputSystem.QueueStateEvent` ni mandar pulsaciones reales con `SendKeys` funcionó conduciendo el editor desde fuera. **La tecla Espacio sigue sin verificarse**; solo está probada la ruta de código (`HealAllHeroes`).

**Un `Debug.Log` por acción vale como prueba del disparador.** Filtrando la consola por el prefijo se ve el *stack trace* completo, y ahí queda claro desde dónde se llamó: `GachaManager.SummonAndSpawnHero`, `BaseBuilding.TryUpgrade` o `WaveManager.Report`. Mucho más convincente que comprobar que el fichero cambió de fecha.

**Medir el ancho de un texto con `TMP_Text.preferredWidth`.** Comparado contra `RectTransform.rect.width` del contenedor dice si una fila se va a salir, sin mirar la Game View. Se prueba con la fila más larga que pueda darse, no con la que hay en pantalla.

**Leer la consola con `IncludeStacktrace: false`.** Con trazas, 14 entradas pasan de 50 000 caracteres. Y ojo: buscar "Exception" da falsos positivos porque aparece en la firma de `ConsoleSink.LogToConsole`. Mejor contar por el campo `Type`.

---

## Control de versiones

El proyecto usa **Git y Plastic SCM a la vez**, y cada uno ignora al otro:
- `.gitignore` excluye `.plastic/`, `.plastic4/`, `ignore.conf`
- `ignore.conf` excluye `.git`, `.gitignore`, `.gitattributes`, `.github`

Consecuencia: `ignore.conf` **no se versiona en Git**, así que quien clone por GitHub no lo recibe.
