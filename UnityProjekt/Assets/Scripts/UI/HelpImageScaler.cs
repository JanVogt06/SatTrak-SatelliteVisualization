using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps an Image scaled to a max % of its parent width (no stretch, centred).
/// Safe against execution order & missing components.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class HelpImageScaler : MonoBehaviour, ILayoutSelfController
{
    [Range(0.1f, 1f)]
    [SerializeField] float maxRelativeWidth = 0.8f;  // 80 %

    Image         img;
    LayoutElement le;
    RectTransform rt;

    void Awake()
    {
        img = GetComponent<Image>();
        le  = GetComponent<LayoutElement>();
        rt  = GetComponent<RectTransform>();

        if (img) img.preserveAspect = true;
    }

    /* Called by the UI-system during a layout pass */
    public void SetLayoutHorizontal() => Adjust();
    public void SetLayoutVertical()   => Adjust();

    void Adjust()
    {
        /* **Guard-clauses** : bail out if something is missing */
        if (img == null || le == null || rt == null || img.sprite == null)
            return;

        /* Parent width – if parent is disabled, return */
        RectTransform parentRT = rt.parent as RectTransform;
        if (parentRT == null) return;

        float maxW = parentRT.rect.width * maxRelativeWidth;

        float spriteW = img.sprite.rect.width;
        float spriteH = img.sprite.rect.height;
        float ratio   = spriteH / spriteW;

        float targetW = Mathf.Min(maxW, spriteW);
        float targetH = targetW * ratio;

        le.preferredWidth  = targetW;
        le.preferredHeight = targetH;
    }
}
