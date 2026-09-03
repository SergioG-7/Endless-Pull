using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Paginado reutilizable con flechas ("< Página X/Y >"). No crea ni destruye tarjetas: reposiciona
// en los mismos huecos fijos las que tocan en la página activa y oculta el resto.
public class UIPager
{
    private readonly GameObject row;
    private readonly TMP_Text indicator;
    private readonly Button prevButton;
    private readonly Button nextButton;

    private IReadOnlyList<GameObject> cards;
    private float[] slotX;
    private int pageSize = 1;
    private int currentPage;
    private bool tabActive = true;

    public UIPager(Transform parent, Vector2 anchoredPositionFromBottom)
    {
        row = new GameObject("Pagination", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(360f, 40f);
        rt.anchoredPosition = anchoredPositionFromBottom;

        prevButton = UIBuild.Button(row.transform, "Btn_PagePrev", "<", UITheme.Neutral,
            new Vector2(48f, 40f), new Vector2(-156f, 0f), OnPrevPressed);
        nextButton = UIBuild.Button(row.transform, "Btn_PageNext", ">", UITheme.Neutral,
            new Vector2(48f, 40f), new Vector2(156f, 0f), OnNextPressed);

        indicator = UIBuild.Label(row.transform, "Label", UITheme.SizeBody, TextAlignmentOptions.Center);
        var lrt = indicator.rectTransform;
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(220f, 40f);
        lrt.anchoredPosition = Vector2.zero;

        row.SetActive(false);
    }

    // slotXPositions define cuantas tarjetas caben por página (su longitud) y donde va cada hueco.
    public void Setup(IReadOnlyList<GameObject> allCards, float[] slotXPositions)
    {
        cards = allCards;
        slotX = slotXPositions;
        pageSize = Mathf.Max(1, slotXPositions.Length);
        currentPage = 0;
        Refresh();
    }

    // La llama el panel dueño al cambiar de pestaña: oculta esta fila entera si no es la activa,
    // y al volver a activarse reaplica la página en la que se había quedado.
    public void SetTabActive(bool active)
    {
        tabActive = active;
        if (!active)
        {
            if (cards != null)
                foreach (var card in cards)
                    if (card != null) card.SetActive(false);
            row.SetActive(false);
            return;
        }

        Refresh();
    }

    // La llama el panel dueño en OnLanguageChanged: solo el texto "Página X/Y" necesita releerse.
    public void RefreshLocalization() => Refresh();

    private void OnPrevPressed()
    {
        currentPage--;
        Refresh();
    }

    private void OnNextPressed()
    {
        currentPage++;
        Refresh();
    }

    private void Refresh()
    {
        if (cards == null || slotX == null) return;

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(cards.Count / (float)pageSize));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
        int start = currentPage * pageSize;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;

            bool onPage = tabActive && i >= start && i < start + pageSize;
            cards[i].SetActive(onPage);
            if (onPage)
            {
                var rt = cards[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(slotX[i - start], rt.anchoredPosition.y);
            }
        }

        bool needsPagination = tabActive && totalPages > 1;
        row.SetActive(needsPagination);
        if (needsPagination)
        {
            indicator.text = string.Format(LocalizationManager.Get("UI_PAGE_INDICATOR"), currentPage + 1, totalPages);
            prevButton.interactable = currentPage > 0;
            nextButton.interactable = currentPage < totalPages - 1;
        }
    }
}
