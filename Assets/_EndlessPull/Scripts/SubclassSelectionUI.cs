using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal de tres cartas que sale al llegar a 3★; la subclase la elige el jugador, no el azar.
public class SubclassSelectionUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el modal; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1320f, 700f);

    [Tooltip("Tamaño máximo de cada carta de subclase; el alto real se ajusta al texto.")]
    [SerializeField] private Vector2 cardSize = new Vector2(400f, 470f);

    [Tooltip("Alto mínimo de la carta aunque el texto ocupe menos.")]
    [SerializeField] private float minCardHeight = 260f;

    [Tooltip("Margen que se suma al alto del texto dentro de la carta.")]
    [SerializeField] private float cardPadding = 44f;

    [Tooltip("Franja de la cabecera del modal, por encima de las cartas.")]
    [SerializeField] private float headerHeight = 120f;

    [Tooltip("Margen que queda por debajo de las cartas.")]
    [SerializeField] private float bottomMargin = 40f;

    private static SubclassSelectionUI instance;

    private GameObject panel;
    private TMP_Text titulo;
    private HeroController hero;
    private readonly Button[] cartas = new Button[3];
    private readonly TMP_Text[] textos = new TMP_Text[3];
    private HeroSubclass[] opciones = new HeroSubclass[0];

    // Si la elección salió desde el Santuario, se vuelve ahí al elegir en vez de dejar al
    // jugador en la base: la ascensión y la subclase son el mismo gesto.
    private SanctuaryUI volverA;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        instance = this;
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // La llama HeroProgress al ascender; devuelve false si no hay modal en escena.
    public static bool Offer(HeroController target, WeaponType archetype)
    {
        if (instance == null) return false;

        return instance.Open(target, archetype);
    }

    private bool Open(HeroController target, WeaponType archetype)
    {
        if (panel == null || target == null) return false;

        int nacimiento = target.Data != null ? target.Data.starRank : target.StarRank;
        opciones = HeroSubclasses.OptionsFor(archetype, nacimiento);
        if (opciones.Length < 2) return false;

        hero = target;

        var santuario = UnityEngine.Object.FindFirstObjectByType<SanctuaryUI>();
        volverA = santuario != null && santuario.IsOpen ? santuario : null;

        titulo.text = string.Format(LocalizationManager.Get("UI_SUBCLASS_OFFER_TITLE"),
            hero.Data.heroName, hero.StarRank, WeaponTypes.DisplayName(archetype));

        // Normalmente son 3 cartas; un héroe nacido plebeyo con báculo se queda sin la de Mago
        // Arcano y ve solo 2 — la tercera carta se apaga en vez de mostrarse vacía.
        for (int i = 0; i < cartas.Length; i++)
        {
            bool activa = i < opciones.Length;
            cartas[i].gameObject.SetActive(activa);
            if (!activa) continue;

            var sub = opciones[i];
            textos[i].text = $"<b>{HeroSubclasses.DisplayName(sub)}</b>\n" +
                             $"<color=#B9A96A>{RolDe(sub)}</color>\n\n" +
                             $"{ModificadoresDe(sub)}\n\n" +
                             $"<b>{HeroSubclasses.SkillName(sub)}</b>\n" +
                             $"{HeroSubclasses.DescribeSkill(sub)}\n" +
                             $"{HeroSubclasses.MakeSkill(sub).mpCost} MP · " +
                             $"{HeroSubclasses.MakeSkill(sub).cooldown:0.#}s";
        }

        FitToContent(opciones.Length);
        RepositionCards(opciones.Length);

        UIManager.OpenExclusive(panel);
        return true;
    }

    // Las cartas traen poco texto y la caja quedaba medio vacía. Se mide lo que ocupa de verdad
    // y la carta y el modal se encogen hasta ahí, sin bajar del mínimo legible.
    private void FitToContent(int count)
    {
        float alto = 0f;
        for (int i = 0; i < count; i++)
        {
            textos[i].ForceMeshUpdate();
            alto = Mathf.Max(alto, textos[i].preferredHeight);
        }

        float cartaAlto = Mathf.Clamp(alto + cardPadding, minCardHeight, cardSize.y);
        float panelAlto = headerHeight + cartaAlto + bottomMargin;

        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, panelAlto);

        // El centro de la carta cuelga de la cabecera: el hueco de arriba no cambia al encoger.
        float centroY = panelAlto * 0.5f - headerHeight - cartaAlto * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var rt = cartas[i].GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cardSize.x, cartaAlto);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, centroY);
        }
    }

    // Con 3 cartas reproduce exactamente las posiciones de siempre ((i-1)*paso); con 2 las
    // centra como pareja en vez de dejarlas descuadradas a la izquierda.
    private void RepositionCards(int count)
    {
        float step = cardSize.x + 24f;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * step;
            var rt = cartas[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        }
    }

    private void OnCardPressed(int index)
    {
        if (hero == null || index >= opciones.Length) return;

        hero.SetSubclass(opciones[index]);
        Debug.Log($"[Subclase] {hero.Data.heroName} elige {HeroSubclasses.DisplayName(opciones[index])}.", this);

        SaveManager.RequestSave();
        hero = null;
        UIManager.CloseEverything();

        if (volverA != null) volverA.Open();
        volverA = null;
    }

    // Rol de un vistazo, para no tener que leerse los números.
    private static string RolDe(HeroSubclass sub)
    {
        switch (sub)
        {
            case HeroSubclass.IronBlade:
            case HeroSubclass.PikeGuard:
            case HeroSubclass.LightPaladin:
            case HeroSubclass.Juggernaut:
            case HeroSubclass.ImmortalBastion:
                return LocalizationManager.Get("ROLE_TAG_TANK");

            case HeroSubclass.StormPiercer:
            case HeroSubclass.VolleyShooter:
            case HeroSubclass.Chronomage:
                return LocalizationManager.Get("ROLE_TAG_CONTROL");

            case HeroSubclass.HighPriest:
            case HeroSubclass.ProtectiveOracle:
            case HeroSubclass.WarCleric:
                return LocalizationManager.Get("ROLE_TAG_SUPPORT");
        }
        return LocalizationManager.Get("ROLE_TAG_DPS");
    }

    // Resumen honesto de para qué sirve: sale de lo que hace su habilidad.
    private static string ModificadoresDe(HeroSubclass sub)
    {
        if (sub != HeroSubclass.None) return HeroSubclasses.RoleDescription(sub);

        return ModificadoresFallback(sub);
    }

    private static string ModificadoresFallback(HeroSubclass sub)
    {
        switch (sub)
        {
            case HeroSubclass.ShadowBlade: return "Daño sostenido\nVeneno que ignora la defensa";
            case HeroSubclass.IronBlade: return "Aguante alto\nEscudo propio al golpear";
            case HeroSubclass.ZephyrBlade: return "Tres cortes por uso\nSangrado acumulado";
            case HeroSubclass.DragonLancer: return "Daño en hilera\nCastiga formaciones cerradas";
            case HeroSubclass.PikeGuard: return "Empuja y ralentiza\nRompe el avance rival";
            case HeroSubclass.StormPiercer: return "Ignora armadura\nAturde 1,5 s";
            case HeroSubclass.LightPaladin: return "Provoca a los cercanos\nEscudo a toda la escuadra";
            case HeroSubclass.Juggernaut: return "Aturde y se quita la fatiga";
            case HeroSubclass.ImmortalBastion: return "Escudo enorme\nAguanta el golpe del jefe";
            case HeroSubclass.Sniper: return "Golpe crítico a distancia\nEl mayor daño de un solo tiro";
            case HeroSubclass.VolleyShooter: return "Área a distancia\nRalentiza a los alcanzados";
            case HeroSubclass.ShadowHunter: return "Veneno y retroceso\nMantiene la distancia";
            case HeroSubclass.Pyromancer: return "Área ígnea\nIgnora armadura";
            case HeroSubclass.Chronomage: return "Ralentiza a todo el campo";
            case HeroSubclass.ArcaneMage: return "Gasta todo el maná\nDaño proporcional al restante";
            case HeroSubclass.HighPriest: return "Cura al aliado más herido";
            case HeroSubclass.ProtectiveOracle: return "Escudos de absorción en área";
            case HeroSubclass.WarCleric: return "Sube la moral de la escuadra";
        }
        return string.Empty;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "SubclassSelection", size, new Color(0.10f, 0.09f, 0.16f, 0.99f));

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 60f, -20f,
            TextAlignmentOptions.Center);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var carta = UIBuild.Button(panel.transform, "Card_" + i, string.Empty,
                new Color(0.20f, 0.19f, 0.30f), cardSize,
                new Vector2((i - 1) * (cardSize.x + 24f), -110f), () => OnCardPressed(index));

            var etiqueta = carta.GetComponentInChildren<TMP_Text>();
            etiqueta.fontSize = UIBuild.BodySize;
            etiqueta.alignment = TextAlignmentOptions.Top;
            etiqueta.margin = new Vector4(14f, 14f, 14f, 14f);

            cartas[i] = carta;
            textos[i] = etiqueta;
        }
    }
}
