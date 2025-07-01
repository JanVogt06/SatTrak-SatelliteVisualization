using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(LayoutElement))]
public class TOCButton : MonoBehaviour
{
    [SerializeField] float horizontalPadding = 24f;   // 12 px links + rechts
    [SerializeField] float scrollOffset      = 60f;   // Platz oberhalb der Überschrift

    RectTransform target;
    ScrollRect    scroll;
    LayoutElement le;
    TMP_Text      label;

    public void Init(RectTransform section, ScrollRect s)
    {
        target = section;
        scroll = s;

        le    = GetComponent<LayoutElement>();
        label = GetComponentInChildren<TMP_Text>(true);

        /* Breite = Textbreite + Padding */
        float w = label.GetPreferredValues(label.text).x + horizontalPadding;
        le.preferredWidth = w;
        le.flexibleWidth  = 0;

        /* keine Zeilenumbrüche im Chip */
        label.enableWordWrapping = false;

        GetComponent<Button>().onClick.AddListener(ScrollToSection);
    }

    void ScrollToSection()
    {
        float viewH   = scroll.viewport.rect.height;
        float contH   = scroll.content.rect.height;
        float secY    = Mathf.Abs(target.anchoredPosition.y);

        /* zusätzlicher Abstand oberhalb der Überschrift */
        float adjustedY = Mathf.Max(0, secY - scrollOffset);

        float normPos = Mathf.Clamp01(adjustedY / (contH - viewH));
        scroll.verticalNormalizedPosition = 1f - normPos;
    }
}
