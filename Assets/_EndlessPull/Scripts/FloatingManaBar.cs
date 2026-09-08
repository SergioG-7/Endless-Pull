using UnityEngine;

// Barra fina de maná justo debajo de la de vida. Se construye clonando la de vida, así hereda
// sprites, anchura y orden de dibujado sin tener que montar nada aparte en el prefab.
public class FloatingManaBar : MonoBehaviour
{
    [Tooltip("Barra de vida de la que se copia la geometría; vacío la busca en este objeto.")]
    [SerializeField] private FloatingHealthBar healthBar;

    [Tooltip("Altura de la barra de maná respecto a la de vida; más fina para no saturar.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float heightFactor = 0.5f;

    [Tooltip("Separación vertical entre la barra de vida y la de maná.")]
    [SerializeField] private float gap = 0.11f;

    [Tooltip("Color del maná.")]
    [SerializeField] private Color manaColor = new Color(0.35f, 0.6f, 1f);

    private HeroController hero;
    private Transform pivot;
    private float lastRatio = -1f;

    void Start()
    {
        hero = GetComponent<HeroController>();
        if (healthBar == null) healthBar = GetComponent<FloatingHealthBar>();

        // Sin maná que enseñar no se monta nada: ni barra ni coste por frame.
        if (hero == null || healthBar == null || healthBar.FillPivot == null || hero.MaxMP <= 0)
        {
            enabled = false;
            return;
        }

        Build();
        Redraw();
    }

    private void Build()
    {
        Transform origen = healthBar.FillPivot.parent;

        var copia = Instantiate(origen.gameObject, origen.parent);
        copia.name = "ManaBar";

        var t = copia.transform;
        t.localPosition = origen.localPosition + Vector3.down * gap;
        t.localScale = new Vector3(origen.localScale.x, origen.localScale.y * heightFactor, 1f);

        // El pivote es el segundo hijo, igual que en la barra de vida; el primero es el fondo.
        pivot = t.Find(healthBar.FillPivot.name);

        var relleno = pivot != null ? pivot.GetComponentInChildren<SpriteRenderer>() : null;
        if (relleno != null) relleno.color = manaColor;
    }

    void Update()
    {
        if (pivot == null) return;

        Redraw();
    }

    private void Redraw()
    {
        if (pivot == null || hero.MaxMP <= 0) return;

        float ratio = Mathf.Clamp01((float)hero.CurrentMP / hero.MaxMP);
        if (Mathf.Approximately(ratio, lastRatio)) return;

        lastRatio = ratio;

        Vector3 escala = pivot.localScale;
        escala.x = ratio;
        pivot.localScale = escala;
    }
}
