using UnityEngine;
using UnityEngine.UI;

// Tokens del diseño "Endless Pull Mobile UI": colores, tipografía y sprites redondeados.
public static class UITheme
{
    // Fondos
    public static readonly Color BgDeep = Hex("0B0F17");
    public static readonly Color BgPanel = Hex("121824");
    public static readonly Color Bg = Hex("161826");
    public static readonly Color Card = Hex("1C1E2B");
    public static readonly Color Elevated = Hex("1A2234");
    public static readonly Color Glass = Hex("141A26", 0.85f);
    public static readonly Color GlassDeep = Hex("0F121C", 0.80f);
    public static readonly Color Disc = Hex("0F121C", 0.90f);
    public static readonly Color Backdrop = Hex("05060A", 0.85f);

    // Textos
    public static readonly Color Text = Hex("E9E9ED");
    public static readonly Color TextSoft = Hex("E9E9ED", 0.70f);
    public static readonly Color TextMuted = Hex("E9E9ED", 0.55f);
    public static readonly Color TextFaint = Hex("E9E9ED", 0.45f);

    // Acento y bordes; el borde azulado es un solo color con tres intensidades.
    public static readonly Color Accent = Hex("9184D9");
    public static readonly Color Accent2 = Hex("A7A1DB");
    public static readonly Color Cyan = Hex("00D2FF");
    public static readonly Color AccentSoft = Hex("9184D9", 0.12f);
    public static readonly Color AccentPick = Hex("9184D9", 0.22f);
    public static readonly Color Border = Hex("26334D");
    public static readonly Color BorderStrong = Hex("26334D");
    public static readonly Color BorderCard = Hex("26334D", 0.75f);
    public static readonly Color BorderSoft = Hex("26334D", 0.55f);

    // Botones neutros y tintados
    public static readonly Color Neutral = Hex("E9E9ED", 0.06f);
    public static readonly Color Amber = Hex("E6BE5A", 0.18f);
    public static readonly Color AmberSoft = Hex("E6BE5A", 0.15f);
    public static readonly Color Teal = Hex("9DC4C4", 0.12f);
    public static readonly Color DangerSoft = Hex("FF4757", 0.18f);

    // Estados
    public static readonly Color Danger = Hex("EF4444");
    public static readonly Color DangerLight = Hex("FF8A93");
    public static readonly Color Track = Hex("FFFFFF", 0.08f);
    public static readonly Color Dim = Hex("E9E9ED", 0.25f);

    // Barras del roster
    public static readonly Color BarHP = Hex("EF4444");
    public static readonly Color BarMP = Hex("3B82F6");
    public static readonly Color BarMorale = Hex("FFD700");

    // Rareza por estrellas, del 1 al 5.
    public static Color Rarity(int starRank)
    {
        switch (starRank)
        {
            case 2: return Hex("22C55E");
            case 3: return Hex("3B82F6");
            case 4: return Hex("A855F7");
            case 5: return Hex("FFD700");
            case 6: return Hex("FF5B3D");
            case 7: return Hex("7DF9FF");
        }
        return Hex("B2B6CA");
    }

    // Decretos del Maestro
    public static readonly Color DecreeHeal = Hex("43A65F");
    public static readonly Color DecreeFocus = Hex("DF911A");
    public static readonly Color DecreeRegroup = Hex("2A92BB");
    public static readonly Color DecreeRetreat = Hex("972527");

    // Escala tipográfica, en unidades de canvas (referencia 1920). 15 es el mínimo legible en móvil.
    public const float SizeTitle = 24f;
    public const float SizeValue = 19f;
    public const float SizeName = 17f;
    public const float SizeBody = 16f;
    public const float SizeLabel = 15f;
    public const float SizeCaption = 15f;
    public const float SizeMicro = 15f;

    // Alias antiguos, ya alineados con la escala nueva.
    public const float SizeSmall = SizeCaption;
    public const float SizeTiny = SizeMicro;
    public const float SizeChip = SizeValue;

    // Tamaño estándar de modal: 80% de la resolución de referencia (1920x1080), centrado.
    public static readonly Vector2 ModalSize = new Vector2(1536f, 864f);

    // Área táctil mínima recomendada para botones de navegación, a resolución de referencia.
    public const float MinTouchTarget = 88f;

    // Radios del mockup.
    public const float RadiusPill = 32f;
    public const float RadiusPanel = 16f;
    public const float RadiusDrawer = 14f;
    public const float RadiusCard = 12f;
    public const float RadiusItem = 9f;
    public const float RadiusButton = 8f;

    // Radio con el que se generaron los sprites; de ahí sale el multiplicador.
    private const float SpriteRadius = 16f;

    // El aro fino se dibujó con radio 32; en superficies grandes deja un filete de 1-2 px.
    private const float ThinRingRadius = 32f;
    private const float ThinRingFrom = 12f;

    private static Sprite rounded;
    private static Sprite roundedRing;
    private static Sprite roundedRingThin;
    private static Sprite circle;
    private static Sprite circleRing;

    public static Sprite Rounded => rounded != null ? rounded : (rounded = Resources.Load<Sprite>("UI/UI_Rounded"));
    public static Sprite RoundedRing => roundedRing != null ? roundedRing : (roundedRing = Resources.Load<Sprite>("UI/UI_RoundedRing"));
    public static Sprite RoundedRingThin => roundedRingThin != null ? roundedRingThin : (roundedRingThin = Resources.Load<Sprite>("UI/UI_RoundedRingThin"));
    public static Sprite Circle => circle != null ? circle : (circle = Resources.Load<Sprite>("UI/UI_Circle"));
    public static Sprite CircleRing => circleRing != null ? circleRing : (circleRing = Resources.Load<Sprite>("UI/UI_CircleRing"));

    public static Color Hex(string rgb, float alpha = 1f)
    {
        Color c;
        if (!ColorUtility.TryParseHtmlString("#" + rgb, out c)) return Color.magenta;

        c.a = alpha;
        return c;
    }

    // Cadena "RRGGBB" para incrustar el color en texto con etiquetas de TMP.
    public static string Tag(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // El sprite se dibujó con radio 16: el multiplicador reescala las esquinas.
    private static float Ppu(float radius) => SpriteRadius / Mathf.Max(1f, radius);

    // Fondo redondeado; si el borde no es transparente, cuelga un aro del mismo radio.
    public static Image Surface(GameObject go, Color fill, Color border, float radius)
    {
        var image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();

        image.sprite = Rounded;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = Ppu(radius);
        image.color = fill;

        if (border.a > 0f) Outline(go.transform, border, radius);
        return image;
    }

    // Aro de un pixel que hace de borde; se reutiliza si ya estaba puesto.
    public static Image Outline(Transform parent, Color color, float radius)
    {
        var previo = parent.Find("Border");
        var go = previo != null ? previo.gameObject
                                : new GameObject("Border", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Cuanto mayor es el radio, más se estiraría el aro grueso: ahí va el fino.
        bool fino = radius >= ThinRingFrom;

        var image = go.GetComponent<Image>();
        image.sprite = fino ? RoundedRingThin : RoundedRing;
        image.type = Image.Type.Sliced;
        image.fillCenter = false;
        image.pixelsPerUnitMultiplier = fino ? ThinRingRadius / radius : Ppu(radius);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Disco relleno; lo usan los botones circulares de decretos y el "+" de intentos.
    public static Image Disk(GameObject go, Color fill)
    {
        var image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();

        image.sprite = Circle;
        image.type = Image.Type.Simple;
        image.color = fill;
        return image;
    }

    // Aro circular hijo, para el marco de color de cada decreto.
    public static Image DiskOutline(Transform parent, string name, Color color)
    {
        var previo = parent.Find(name);
        var go = previo != null ? previo.gameObject
                                : new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = CircleRing;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
