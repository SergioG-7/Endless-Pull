using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Constructores de UI compartidos por los paneles que se montan por código.
public static class UIBuild
{
    // Un solo tamaño para todos los nombres, para que ninguna ficha desentone.
    public const float TitleSize = UITheme.SizeTitle;
    public const float NameSize = UITheme.SizeName;
    public const float BodySize = UITheme.SizeBody;
    public const float ButtonSize = UITheme.SizeBody;

    public static GameObject Panel(Transform parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        UITheme.Surface(go, color, UITheme.Border, UITheme.RadiusPanel);
        return go;
    }

    public static TMP_Text Label(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = UITheme.Text;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Etiqueta anclada arriba, que ocupa todo el ancho del padre.
    public static TMP_Text TopLabel(Transform parent, string name, float size, float height, float y,
                                    TextAlignmentOptions align)
    {
        var tmp = Label(parent, name, size, align);
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(16f, 0f);
        rt.offsetMax = new Vector2(-16f, 0f);
        rt.sizeDelta = new Vector2(-32f, height);
        rt.anchoredPosition = new Vector2(0f, y);
        return tmp;
    }

    public static Button Button(Transform parent, string name, string text, Color color,
                                Vector2 size, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        var image = UITheme.Surface(go, color, UITheme.BorderCard, UITheme.RadiusButton);

        var label = Label(go.transform, "Label", ButtonSize, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.text = text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        // El clic suena antes de hacer nada; asi tambien suenan los botones sin accion.
        button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        if (onClick != null) button.onClick.AddListener(onClick);

        // Micro-escala al pulsar: feedback táctil consistente en todos los botones construidos aquí.
        ButtonPressFeedback.Attach(go);
        return button;
    }

    // Barra de progreso con su cifra dentro; la usan las fichas de héroe.
    public static Image Bar(Transform parent, string name, Vector2 size, Vector2 position,
                            Color color, out TMP_Text label)
    {
        var fondo = new GameObject(name, typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(parent, false);

        var rt = fondo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        UITheme.Surface(fondo, UITheme.Track, Color.clear, 4f);

        var relleno = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        relleno.transform.SetParent(fondo.transform, false);
        var frt = relleno.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(1f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        var img = UITheme.Surface(relleno, color, Color.clear, 4f);

        label = Label(fondo.transform, "Label", UITheme.SizeSmall, TextAlignmentOptions.Left);
        var lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(10f, 0f);
        lrt.offsetMax = Vector2.zero;

        return img;
    }

    // Ajusta el relleno de una barra ya creada.
    public static void SetBar(Image fill, float ratio)
    {
        if (fill == null) return;

        var rt = fill.rectTransform;
        rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        rt.offsetMax = Vector2.zero;
    }

    // Pixel art del héroe centrado dentro de un marco, sin deformarlo ni teñirlo.
    // Devuelve null si el HeroData todavía no tiene sprite asignado.
    public static Image HeroArt(Transform parent, Sprite sprite, float size)
    {
        var previo = parent.Find("Art");
        if (sprite == null)
        {
            if (previo != null) previo.gameObject.SetActive(false);
            return null;
        }

        var go = previo != null ? previo.gameObject
                                : new GameObject("Art", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.SetActive(true);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;

        // Por encima del fondo del marco pero por debajo de su aro de rareza.
        go.transform.SetAsLastSibling();
        var borde = parent.Find("Border");
        if (borde != null) borde.SetAsLastSibling();

        return image;
    }

    // Botón [X] estándar de cierre, anclado a la esquina superior derecha del modal que lo llama.
    // Único punto de construcción: cualquier ficha/modal que lo use se ve y se comporta igual.
    public static Button CloseButtonTopRight(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.anchoredPosition = new Vector2(-16f, -16f);

        var image = UITheme.Surface(go, UITheme.Neutral, UITheme.BorderCard, UITheme.RadiusButton);

        var label = Label(go.transform, "Label", UITheme.SizeTitle, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.text = "×";
        label.color = UITheme.Text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        if (onClick != null) button.onClick.AddListener(onClick);
        ButtonPressFeedback.Attach(go);
        return button;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
