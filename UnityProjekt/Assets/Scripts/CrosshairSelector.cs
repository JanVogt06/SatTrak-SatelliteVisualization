using UnityEngine;
using UnityEngine.UI;

public class CrosshairSelector : MonoBehaviour
{
    [Header("Crosshair Auswahl")]
    public Button[] crosshairButtons;            // Buttons zur Auswahl des Crosshairs
    public Image[] crosshairImages;              // Die Crosshair-Bildvorschau-Images

    [Header("Crosshair Farb-Auswahl")]
    public Button[] colorButtons;                // Farbbuttons für Crosshair
    public Color[] availableColors;              // Farbliste für beide Systeme

    [Header("Cursor Auswahl")]
    public Button[] cursorButtons;               // Zwei Buttons für Cursor-Formen
    public Image[] cursorPreviewImages;          // Die kleinen Vorschau-Cursorbilder (UI)
    public Texture2D[] cursorTextures;           // Die echten Texturen für Cursor.SetCursor()

    [Header("Cursor Farb-Auswahl")]
    public Button[] cursorColorButtons;          // Farbbuttons speziell für den Cursor

    public CustomCursor custCursor;

    private int currentCrosshair = 0;

    private void Start()
    {
        // Vorschau-Sprites für Cursor aus Texturen erstellen
        for (int i = 0; i < cursorPreviewImages.Length && i < cursorTextures.Length; i++)
        {
            Sprite previewSprite = Sprite.Create(
                cursorTextures[i],
                new Rect(0, 0, cursorTextures[i].width, cursorTextures[i].height),
                new Vector2(0.5f, 0.5f)
            );
            cursorPreviewImages[i].sprite = previewSprite;
        }

        // Initialauswahl
        SelectCrosshair(0);
        SelectColor(0);
        SelectCursorTexture(0);
        SelectCursorColor(0);

        // Crosshair-Button-Bindings
        for (int i = 0; i < crosshairButtons.Length; i++)
        {
            int index = i;
            crosshairButtons[i].onClick.AddListener(() => SelectCrosshair(index));
        }

        // Crosshair-Farbwahl-Bindings
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int index = i;
            colorButtons[i].onClick.AddListener(() => SelectColor(index));
        }

        // Cursor-Formwahl-Bindings
        for (int i = 0; i < cursorButtons.Length; i++)
        {
            int index = i;
            cursorButtons[i].onClick.AddListener(() => SelectCursorTexture(index));
        }

        // Cursor-Farbwahl-Bindings
        for (int i = 0; i < cursorColorButtons.Length; i++)
        {
            int index = i;
            cursorColorButtons[i].onClick.AddListener(() => SelectCursorColor(index));
        }
    }

    void SelectCrosshair(int index)
    {
        currentCrosshair = index;
        CrosshairSettings.selectedSprite = crosshairImages[index].sprite;

        for (int i = 0; i < crosshairButtons.Length; i++)
        {
            crosshairButtons[i].transform.GetChild(1).gameObject.SetActive(i == index);
        }
    }

    void SelectColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= availableColors.Length)
            return;

        Color chosenColor = availableColors[colorIndex];
        chosenColor.a = 1f;

        CrosshairSettings.selectedColor = chosenColor;

        foreach (var img in crosshairImages)
        {
            img.color = chosenColor;
        }
    }

    public void SelectCursorTexture(int index)
    {
        if (index < 0 || index >= cursorTextures.Length)
            return;

        CrosshairSettings.cursorTexture = cursorTextures[index];

        for (int i = 0; i < cursorButtons.Length; i++)
        {
            cursorButtons[i].transform.GetChild(1).gameObject.SetActive(i == index);
        }

        custCursor.ApplyCursor();
    }

    public void SelectCursorColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= availableColors.Length)
            return;

        Color chosenColor = availableColors[colorIndex];
        chosenColor.a = 1f;

        CrosshairSettings.cursorColor = chosenColor;

        // Vorschau einfärben
        foreach (var img in cursorPreviewImages)
        {
            img.color = chosenColor;
        }

        custCursor.ApplyCursor();
    }
}
