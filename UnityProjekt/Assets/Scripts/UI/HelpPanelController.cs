using UnityEngine;
using UnityEngine.UI;   // nur nötig, falls du Button-Events hier anbinden willst

public class HelpPanelController : MonoBehaviour
{
    [Header("UI-Referenzen")]
    [SerializeField] private GameObject helpPanel;      // Root des Panels
    [SerializeField] private CanvasGroup canvasGroup;   // Optional: fürs Fading
    [SerializeField] private GameObject backButton;     // Der Zurück-Button

    private const float fadeDuration = 0.25f;

    void Awake()
    {
        HideImmediate();      // alles ausblenden, bevor die Szene loslegt
    }

    /* ---------- Öffnen / Schließen (für Buttons) ---------- */

    public void ShowHelp()
    {
        helpPanel.SetActive(true);
        if (backButton) backButton.SetActive(true);

        if (canvasGroup)
        {
            canvasGroup.alpha = 0;                      // Startwert fürs Fade-In
            Fade(0, 1, fadeDuration);
        }
    }

    public void HideHelp()
    {
        if (canvasGroup)
        {
            Fade(1, 0, fadeDuration, HideImmediate);   // erst ausblenden, dann deaktivieren
        }
        else
        {
            HideImmediate();
        }
    }

    /* ---------- ESC-Shortcut optional ---------- */

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && helpPanel.activeSelf)
            HideHelp();
    }

    /* ---------- Interne Hilfsroutinen ---------- */

    void HideImmediate()
    {
        helpPanel.SetActive(false);
        if (backButton) backButton.SetActive(false);
    }

    void Fade(float from, float to, float time, System.Action onDone = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(from, to, time, onDone));
    }

    System.Collections.IEnumerator FadeRoutine(float a, float b, float t, System.Action cb)
    {
        float e = 0f;
        while (e < t)
        {
            e += Time.unscaledDeltaTime;               // reagiert auch, wenn TimeScale = 0
            canvasGroup.alpha = Mathf.Lerp(a, b, e / t);
            yield return null;
        }
        canvasGroup.alpha = b;
        cb?.Invoke();
    }
}
