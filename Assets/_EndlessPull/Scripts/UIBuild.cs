using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Constructores de UI compartidos por los paneles que se montan por código.
public static class UIBuild
{
    // Un solo tamaño para todos los nombres, para que ninguna ficha desentone.
    public const float TitleSize = 34f;
    public const float NameSize = 24f;
    public const float BodySize = 20f;
    public const float ButtonSize = 22f;

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

        go.GetComponent<Image>().color = color;
        return go;
    }

    public static TMP_Text Label(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
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

        var image = go.GetComponent<Image>();
        image.color = color;

        var label = Label(go.transform, "Label", ButtonSize, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.text = text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null) button.onClick.AddListener(onClick);
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
        fondo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.9f);

        var relleno = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        relleno.transform.SetParent(fondo.transform, false);
        var frt = relleno.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(1f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        var img = relleno.GetComponent<Image>();
        img.color = color;

        label = Label(fondo.transform, "Label", BodySize, TextAlignmentOptions.Left);
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

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
