using UnityEngine;
using UnityEngine.UI;   // nur UGUI
using System;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class HelpImageScaler : MonoBehaviour, ILayoutSelfController   // ILayoutSelfController ist hier in UnityEngine.UI
{
    [Range(0f, 1f)]
    [SerializeField] private float maxRelativeWidth = 0.8f;   // 80 %

    Image         img;
    LayoutElement le;

    void Awake()
    {
        img = GetComponent<Image>();
        le  = GetComponent<LayoutElement>();

        // sicherstellen, dass Aspect-Schalter aktiv ist
        img.preserveAspect = true;
    }

    /* Unity ruft diese beiden Methoden beim Layouten auf */
    public void SetLayoutHorizontal() => Adjust();
    public void SetLayoutVertical()   => Adjust();

    void Adjust()
    {
        if (img.sprite == null) { le.preferredWidth = le.preferredHeight = 0; return; }

        RectTransform parentRT = transform.parent as RectTransform;
        if (parentRT == null) return;

        float parentW  = parentRT.rect.width;
        float maxW     = parentW * maxRelativeWidth;

        float sprW = img.sprite.rect.width;
        float sprH = img.sprite.rect.height;
        float ratio = sprH / sprW;

        float targetW = Mathf.Min(maxW, sprW);   // nicht größer skalieren als Original
        float targetH = targetW * ratio;

        le.preferredWidth  = targetW;
        le.preferredHeight = targetH;
    }
}
