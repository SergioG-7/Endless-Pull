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

    private float timer;
    private float reformTimer;
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

        string texto = aDistancia > 0
            ? string.Format(LocalizationManager.Get("BATTLE_SIGHTING_MIXED"), cuerpoACuerpo, aDistancia)
            : string.Format(LocalizationManager.Get("BATTLE_SIGHTING_MELEE"), cuerpoACuerpo);

        Say(vigia, texto);
    }

    // Al colocarse, el que aguanta el frente anuncia el plan.
    private void ReportPlan()
    {
        HeroController frente = null;
        foreach (var hero in Squad())
            if (hero.IsTank) { frente = hero; break; }

        if (frente == null) return;

        Say(frente, LocalizationManager.Get("BATTLE_PLAN_HOLD"));
    }

    // Bocadillo sobre el héroe. Los rótulos de intención van más abajo, así que no se pisan.
    private void Say(HeroController hero, string texto)
    {
        if (hero == null || string.IsNullOrEmpty(texto)) return;

        DamageTextManager.Show(hero.transform.position + Vector3.up * 1.25f, texto, UITheme.Text);
    }

    public void EndEncounter()
    {
        Phase = BattlePhase.Idle;
        puestos.Clear();
        HasFrontLine = false;
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

        Overwhelmed = squad.Count > 0 && enemigosVivos > squad.Count * overwhelmedRatio;
        if (Overwhelmed) frente -= haciaElEnemigo * retreatStep;

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

        // Sin ningún tanque, el más duro de los de cuerpo a cuerpo hace de frente.
        if (tanques.Count == 0 && cuerpoACuerpo.Count > 0)
        {
            HeroController masDuro = cuerpoACuerpo[0];
            foreach (var hero in cuerpoACuerpo)
                if (hero.MaxHealth > masDuro.MaxHealth) masDuro = hero;

            cuerpoACuerpo.Remove(masDuro);
            tanques.Add(masDuro);
        }

        frontliners.Clear();
        foreach (var hero in tanques) frontliners.Add(hero);

        Colocar(tanques, frente, lateral);
        Colocar(cuerpoACuerpo, frente - haciaElEnemigo * meleeGap, lateral);
        Colocar(distancia, frente - haciaElEnemigo * rangedGap, lateral);
        Colocar(soporte, frente - haciaElEnemigo * supportGap, lateral);
    }

    // Reparte una fila a lo ancho, centrada en su línea.
    private void Colocar(List<HeroController> fila, Vector2 centroLinea, Vector2 lateral)
    {
        for (int i = 0; i < fila.Count; i++)
        {
            float desplazamiento = (i - (fila.Count - 1) * 0.5f) * rowSpacing;
            puestos[fila[i]] = centroLinea + lateral * desplazamiento;
        }
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
