using System.Collections.Generic;
using UnityEngine;

// Los tres tiempos de un encuentro antes de que empiecen los golpes.
public enum BattlePhase
{
    Idle,
    Scouting,   // otean: uno se adelanta y la escuadra lee lo que hay enfrente
    Forming,    // se colocan por papel: tanques delante, daño detrás, soporte al fondo
    Engaging    // ya pelean, con la línea puesta
}

// Coreografía de entrada al piso. Antes la escuadra aparecía y se lanzaba en tromba; esto le da
// el rato de reconocimiento y colocación que hace legible todo lo demás.
public class BattleChoreographer : MonoBehaviour
{
    [Tooltip("Segundos que la escuadra dedica a mirar lo que tiene enfrente.")]
    [SerializeField] private float scoutSeconds = 2.5f;

    [Tooltip("Segundos que tardan en colocarse en formación.")]
    [SerializeField] private float formSeconds = 2.5f;

    [Tooltip("Separación entre la línea de tanques y la de daño cuerpo a cuerpo.")]
    [SerializeField] private float meleeGap = 1.6f;

    [Tooltip("Separación entre el frente y los que tiran de lejos.")]
    [SerializeField] private float rangedGap = 3.2f;

    [Tooltip("Separación entre el frente y el soporte, que va al fondo.")]
    [SerializeField] private float supportGap = 4.4f;

    [Tooltip("Separación vertical entre los que comparten línea.")]
    [SerializeField] private float rowSpacing = 1.3f;

    [Tooltip("A qué velocidad se colocan; por encima de la suya normal, que es paseo militar.")]
    [SerializeField] private float formingSpeed = 3.5f;

    public BattlePhase Phase { get; private set; } = BattlePhase.Idle;

    // Mientras no estén desplegados del todo no se pelea: lo consulta el WaveManager.
    public bool CombatReady => Phase == BattlePhase.Engaging || Phase == BattlePhase.Idle;

    // X de la línea de frente: la de los tanques ya colocados. Los de detrás no la rebasan.
    public float FrontLineX { get; private set; }
    public bool HasFrontLine { get; private set; }

    [Tooltip("Enemigos por héroe a partir de los cuales la escuadra se considera desbordada y cede terreno.")]
    [SerializeField] private float overwhelmedRatio = 2f;

    [Tooltip("Cuánto retrocede la línea cuando les superan en número.")]
    [SerializeField] private float retreatStep = 2f;

    [Tooltip("Cada cuánto se recolocan las líneas durante el combate.")]
    [SerializeField] private float reformInterval = 3f;

    [Tooltip("Cuánto se adelanta o retrasa cada héroe dentro de su fila; 0 la deja recta.")]
    [SerializeField] private float rowStagger = 0.55f;

    private float timer;
    private float reformTimer;

    // Reagrupamiento entre tandas de refuerzo: la escuadra vuelve a un punto fijo y rehace la
    // línea en vez de quedarse donde la dejó el último enemigo que mató.
    private bool regrouping;
    private Vector2 rallyPoint;
    private readonly Dictionary<HeroController, Vector2> puestos = new Dictionary<HeroController, Vector2>();

    // Quiénes aguantan el frente en este encuentro. Sale de aquí y no de HeroController.IsTank,
    // que exige escudo equipado y deja sin línea a la mayoría de escuadras.
    private readonly HashSet<HeroController> frontliners = new HashSet<HeroController>();

    public bool IsFrontliner(HeroController hero) => hero != null && frontliners.Contains(hero);

    // La escuadra está desbordada: hay demasiados enemigos encima para mantener el avance.
    public bool Overwhelmed { get; private set; }

    // La llama el WaveManager al soltar la escuadra en el piso.
    public void BeginEncounter()
    {
        Phase = BattlePhase.Scouting;
        timer = Mathf.Max(0f, scoutSeconds);
        puestos.Clear();
        HasFrontLine = false;

        ReportSighting();
    }

    // Lo que ve el que va delante, dicho en voz alta. Es el primero de los dos avisos: este
    // cuenta qué hay, y el de la formación dice qué se va a hacer.
    private void ReportSighting()
    {
        var squad = Squad();
        if (squad.Count == 0) return;

        int aDistancia = 0, cuerpoACuerpo = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (e == null || e.CurrentHealth <= 0) continue;

            if (e.IsRanged) aDistancia++;
            else cuerpoACuerpo++;
        }

        if (cuerpoACuerpo + aDistancia == 0) return;

        // Habla el que mejor ve: más alcance de detección es mejor ojo.
        HeroController vigia = squad[0];
        foreach (var hero in squad)
            if (hero.EffectiveDetectionRange > vigia.EffectiveDetectionRange) vigia = hero;

        string clave = aDistancia > 0
            ? Una("BATTLE_SIGHTING_MIXED", "BATTLE_SIGHTING_MIXED_2", "BATTLE_SIGHTING_MIXED_3")
            : Una("BATTLE_SIGHTING_MELEE", "BATTLE_SIGHTING_MELEE_2", "BATTLE_SIGHTING_MELEE_3");

        string texto = aDistancia > 0
            ? string.Format(LocalizationManager.Get(clave), cuerpoACuerpo, aDistancia)
            : string.Format(LocalizationManager.Get(clave), cuerpoACuerpo);

        Say(vigia, texto);
    }

    // Al colocarse, el que aguanta el frente anuncia el plan.
    private void ReportPlan()
    {
        HeroController frente = null;
        foreach (var hero in Squad())
            if (hero.IsTank) { frente = hero; break; }

        if (frente == null) return;

        // Con jefe delante el aviso es otro: no es un piso cualquiera.
        if (HayJefe())
        {
            Say(frente, LocalizationManager.Get(
                Una("BATTLE_BOSS_1", "BATTLE_BOSS_2", "BATTLE_BOSS_3")));
            return;
        }

        Say(frente, LocalizationManager.Get(
            Una("BATTLE_PLAN_HOLD", "BATTLE_PLAN_HOLD_2", "BATTLE_PLAN_HOLD_3", "BATTLE_PLAN_HOLD_4")));
    }

    private static string Una(params string[] keys) => keys[Random.Range(0, keys.Length)];

    // Un enemigo mucho más gordo que el resto de la oleada es el jefe del piso.
    private static bool HayJefe()
    {
        var enemigos = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        if (enemigos.Length == 0) return false;

        int mayor = 0, total = 0, vivos = 0;
        foreach (var e in enemigos)
        {
            if (e == null || e.CurrentHealth <= 0) continue;

            vivos++;
            total += e.MaxHealth;
            if (e.MaxHealth > mayor) mayor = e.MaxHealth;
        }

        return vivos > 1 && mayor > (total / (float)vivos) * 2.5f;
    }

    // Bocadillo sobre el héroe. Los rótulos de intención van más abajo, así que no se pisan.
    private void Say(HeroController hero, string texto)
    {
        if (hero == null || string.IsNullOrEmpty(texto)) return;

        DamageTextManager.Show(hero.transform.position + Vector3.up * 1.25f, texto, UITheme.Text);
    }

    // La llama el WaveManager cuando la oleada se queda sin enemigos en pantalla pero aún faltan
    // refuerzos por entrar. La escuadra tiene el avance frenado (holdPosition) mientras dure.
    public void BeginRegroup(Vector2 rally)
    {
        if (regrouping) return;

        regrouping = true;
        rallyPoint = rally;
        AssignPosts();
    }

    // Mueve el punto de reunión sin reiniciar el reagrupamiento; lo usa la escolta, que puede
    // no estar donde estaba cuando empezó.
    public void SetRallyPoint(Vector2 rally) => rallyPoint = rally;

    public void EndRegroup()
    {
        if (!regrouping) return;

        regrouping = false;
        AssignPosts();
    }

    public void EndEncounter()
    {
        Phase = BattlePhase.Idle;
        puestos.Clear();
        HasFrontLine = false;
        regrouping = false;
    }

    void Update()
    {
        if (Phase == BattlePhase.Idle) return;

        if (Phase == BattlePhase.Engaging)
        {
            UpdateFrontLine();

            // Sin esto la formación era solo la foto del principio: al despejar un lado la línea
            // tiene que avanzar, y si les desbordan tiene que ceder terreno.
            reformTimer -= Time.deltaTime;
            if (reformTimer <= 0f)
            {
                reformTimer = Mathf.Max(0.5f, reformInterval);
                AssignPosts();
            }

            // En combate normal los mueve su propia IA; esperando refuerzos la tiene frenada,
            // así que aquí es donde vuelven andando a su sitio.
            if (regrouping) MoveToPosts();

            return;
        }

        timer -= Time.deltaTime;

        if (Phase == BattlePhase.Scouting)
        {
            if (timer > 0f) return;

            AssignPosts();
            ReportPlan();
            Phase = BattlePhase.Forming;
            timer = Mathf.Max(0f, formSeconds);
            return;
        }

        // Forming: cada uno camina a su puesto; cuando llegan (o se acaba el tiempo) se pelea.
        bool todosEnSitio = MoveToPosts();
        if (todosEnSitio || timer <= 0f)
        {
            Phase = BattlePhase.Engaging;
            UpdateFrontLine();
        }
    }

    private List<HeroController> Squad()
    {
        var lista = new List<HeroController>();

        foreach (var hero in Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            if (hero != null && hero.IsDeployed && hero.CurrentHealth > 0) lista.Add(hero);

        return lista;
    }

    // Reparte puestos por papel, tomando como referencia dónde está el enemigo.
    private void AssignPosts()
    {
        puestos.Clear();

        var squad = Squad();
        if (squad.Count == 0) return;

        var enemigos = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        Vector2 centroEscuadra = Vector2.zero;
        foreach (var hero in squad) centroEscuadra += (Vector2)hero.transform.position;
        centroEscuadra /= squad.Count;

        // Esperando refuerzos la línea se rehace en el punto de despliegue, no donde acabaron
        // persiguiendo al último enemigo.
        if (regrouping) centroEscuadra = rallyPoint;

        // Sin enemigos a la vista se forma mirando al frente de siempre (la arena va de izquierda
        // a derecha), que es mejor que no formar nada.
        Vector2 haciaElEnemigo = Vector2.right;
        if (enemigos.Length > 0)
        {
            Vector2 centroEnemigo = Vector2.zero;
            int vivos = 0;
            foreach (var e in enemigos)
            {
                if (e == null || e.CurrentHealth <= 0) continue;
                centroEnemigo += (Vector2)e.transform.position;
                vivos++;
            }

            if (vivos > 0)
            {
                centroEnemigo /= vivos;
                var dir = centroEnemigo - centroEscuadra;
                if (dir.sqrMagnitude > 0.01f) haciaElEnemigo = dir.normalized;
            }
        }

        // El frente se planta por delante del centro de la escuadra, no encima del enemigo.
        Vector2 frente = centroEscuadra + haciaElEnemigo * meleeGap;
        Vector2 lateral = new Vector2(-haciaElEnemigo.y, haciaElEnemigo.x);

        // Desbordados: el frente se planta más atrás en vez de seguir empujando.
        int enemigosVivos = 0;
        foreach (var e in enemigos)
            if (e != null && e.CurrentHealth > 0) enemigosVivos++;

        bool desbordadosAntes = Overwhelmed;
        Overwhelmed = squad.Count > 0 && enemigosVivos > squad.Count * overwhelmedRatio;
        if (Overwhelmed) frente -= haciaElEnemigo * retreatStep;

        // Solo al cruzar el umbral: repetirlo en cada recolocación sería un bucle de gritos.
        if (Overwhelmed && !desbordadosAntes && squad.Count > 0)
            squad[Random.Range(0, squad.Count)].Bark(1f,
                "BATTLE_OVERWHELMED_1", "BATTLE_OVERWHELMED_2", "BATTLE_OVERWHELMED_3");

        var tanques = new List<HeroController>();
        var cuerpoACuerpo = new List<HeroController>();
        var distancia = new List<HeroController>();
        var soporte = new List<HeroController>();

        foreach (var hero in squad)
        {
            if (hero.IsSupport) soporte.Add(hero);
            else if (hero.IsTank) tanques.Add(hero);
            else if (hero.IsRanged) distancia.Add(hero);
            else cuerpoACuerpo.Add(hero);
        }

        // Quien pega de cerca pega en el frente, lleve escudo o no: antes los de espada sin
        // escudo se quedaban una fila por detrás mirando cómo peleaba el único tanque.
        // Atrás solo van los que necesitan distancia para hacer su trabajo.
        var linea = new List<HeroController>(tanques);
        linea.AddRange(cuerpoACuerpo);

        frontliners.Clear();
        foreach (var hero in linea) frontliners.Add(hero);

        // Dentro de la línea, Escalon adelanta a los que más aguantan; el hueco es entre líneas.
        ColocarFila(linea, frente, lateral, haciaElEnemigo, 0);
        ColocarFila(distancia, frente - haciaElEnemigo * rangedGap, lateral, haciaElEnemigo, 2);
        ColocarFila(soporte, frente - haciaElEnemigo * supportGap, lateral, haciaElEnemigo, 3);
    }

    // Reparte una fila a lo ancho, centrada en su línea.
    private void Colocar(List<HeroController> fila, Vector2 centroLinea, Vector2 lateral,
                         Vector2 haciaElEnemigo, int indiceFila)
    {
        // Las cuatro líneas van centradas en el mismo eje, así que una escuadra con un héroe por
        // papel salía en fila india, uno detrás de otro. Las impares se corren medio hueco al
        // lado: con pocos efectivos se lee un zigzag y con muchos, un tablero.
        float sesgoFila = (indiceFila % 2 == 0 ? -1f : 1f) * rowSpacing * 0.5f;

        for (int i = 0; i < fila.Count; i++)
        {
            float desplazamiento = (i - (fila.Count - 1) * 0.5f) * rowSpacing + sesgoFila;

            // La fila no se cuadra a escuadra: el que mejor aguanta pisa medio paso por delante
            // y el que menos se queda algo atrás, para que se lea una línea de combate y no una
            // formación de desfile.
            float adelanto = Escalon(fila[i]) * rowStagger;

            puestos[fila[i]] = centroLinea + lateral * desplazamiento + haciaElEnemigo * adelanto;
        }
    }

    // De -1 (se queda atrás) a +1 (pisa delante), según lo que aguante el héroe respecto a la
    // media de su fila. Sin fila que comparar, todos a la misma altura.
    private float Escalon(HeroController hero)
    {
        if (hero == null || filaVidaMedia <= 0f) return 0f;

        float relativo = (hero.MaxHealth - filaVidaMedia) / filaVidaMedia;
        return Mathf.Clamp(relativo * 2f, -1f, 1f);
    }

    // Vida media de la fila que se está colocando; la fija Colocar antes de repartir puestos.
    private float filaVidaMedia;

    private void ColocarFila(List<HeroController> fila, Vector2 centroLinea, Vector2 lateral,
                             Vector2 haciaElEnemigo, int indiceFila)
    {
        filaVidaMedia = 0f;
        foreach (var hero in fila)
            if (hero != null) filaVidaMedia += hero.MaxHealth;

        if (fila.Count > 0) filaVidaMedia /= fila.Count;

        Colocar(fila, centroLinea, lateral, haciaElEnemigo, indiceFila);
    }

    // Devuelve true cuando ya están todos en su sitio.
    private bool MoveToPosts()
    {
        bool todos = true;

        foreach (var par in puestos)
        {
            var hero = par.Key;
            if (hero == null || hero.CurrentHealth <= 0) continue;

            Vector2 actual = hero.transform.position;
            if (Vector2.Distance(actual, par.Value) <= 0.2f) continue;

            todos = false;
            hero.transform.position = Vector2.MoveTowards(actual, par.Value, formingSpeed * Time.deltaTime);
        }

        return todos;
    }

    // La línea de frente es la del frontliner más adelantado; la usan los de detrás para no
    // rebasarla. Si caen todos los del frente, la línea desaparece y cada uno va a lo suyo.
    private void UpdateFrontLine()
    {
        HasFrontLine = false;
        if (Phase != BattlePhase.Engaging) return;

        float mejor = float.MinValue;

        foreach (var hero in frontliners)
        {
            if (hero == null || hero.CurrentHealth <= 0) continue;

            HasFrontLine = true;
            if (hero.transform.position.x > mejor) mejor = hero.transform.position.x;
        }

        if (HasFrontLine) FrontLineX = mejor;
    }
}
