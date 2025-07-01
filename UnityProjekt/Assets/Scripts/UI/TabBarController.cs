using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Steuert Tabs (Toggles) + zugehörige Panels.
/// Leg es auf dein TabBar-GameObject.
/// </summary>
public class TabBarController : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        [Tooltip("Toggle-Reiter dieses Tabs")]
        public Toggle toggle;

        [Tooltip("Content-Panel, das zu diesem Tab gehört")]
        public GameObject panel;

        [Tooltip("(Optional) Graphic, das farblich hervorgehoben wird – z. B. Background-Image oder Underline-Image")]
        public Graphic highlight;
    }

    [Header("Tabs in derselben Reihenfolge wie in der TabBar")]
    [SerializeField] private Tab[] tabs;

    [Header("Highlight-Farben")]
    [SerializeField] private Color activeColor   = new Color(0f, 0.88f, 1f, 1f);   // Cyan
    [SerializeField] private Color inactiveColor = new Color(0.16f, 0.16f, 0.16f); // Dunkelgrau

    private void Awake()
    {
        // Callback für jeden Toggle registrieren
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i; // lokaler Cache für Lambda
            tabs[i].toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn) ActivateTab(index);
            });
        }

        // Standard-Tab: Index 0 (Panel_Kamera) aktivieren
        tabs[0].toggle.isOn = true;   // löst onValueChanged aus
        ActivateTab(0);               // falls ToggleGroup noch nicht initialisiert war
    }

    private void ActivateTab(int activeIndex)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = (i == activeIndex);

            // Panel sichtbar / unsichtbar
            if (tabs[i].panel != null)
                tabs[i].panel.SetActive(isActive);

            // Highlight-Graphic einfärben (falls gesetzt)
            if (tabs[i].highlight != null)
                tabs[i].highlight.color = isActive ? activeColor : inactiveColor;
        }
    }
}
