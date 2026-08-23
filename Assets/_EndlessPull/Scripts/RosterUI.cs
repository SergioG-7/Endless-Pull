using System.Text;
using TMPro;
using UnityEngine;

// Panel modal que lista los héroes vivos de la base.
public class RosterUI : MonoBehaviour
{
    [Tooltip("Raíz del panel; se activa y desactiva al abrir y cerrar.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Contenedor donde se generan las filas de héroes.")]
    [SerializeField] private RectTransform content;

    [Tooltip("Texto que se muestra cuando no queda ningún héroe.")]
    [SerializeField] private TMP_Text emptyLabel;

    [Tooltip("Segundos entre refrescos mientras el panel está abierto.")]
    [SerializeField] private float refreshInterval = 0.5f;

    [Tooltip("Alto de cada fila de héroe, en píxeles de UI.")]
    [SerializeField] private float rowHeight = 56f;

    private float refreshTimer;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;

        refreshTimer = refreshInterval;
        Rebuild();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        panel.SetActive(true);
        refreshTimer = refreshInterval;
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Regenera la lista entera; con una decena de héroes sale más barato que diferenciar.
    private void Rebuild()
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);

        if (emptyLabel != null) emptyLabel.gameObject.SetActive(heroes.Length == 0);

        foreach (var hero in heroes)
            CreateRow(hero);
    }

    private void CreateRow(HeroController hero)
    {
        var go = new GameObject($"Row_{hero.name}", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, rowHeight);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.text = BuildRowText(hero);
    }

    private string BuildRowText(HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;

        var stars = new StringBuilder();
        for (int i = 0; i < hero.Data.starRank; i++) stars.Append('★');

        return $"{hero.Data.heroName}  {stars}   Nv. {level}   " +
               $"{HeroTraits.DisplayName(hero.Trait)}   " +
               $"HP {hero.CurrentHealth}/{hero.MaxHealth}   ATK {hero.Attack}   [{hero.State}]";
    }
}
