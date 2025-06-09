using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreloadScene : MonoBehaviour
{
    [Header("Szenen & UI")]
    public List<string> scenesToPreload;
    public string mainMenuScene = "MainMenu";
    public Slider loadingSlider;
    public TextMeshProUGUI mytext;

    private void Start()
    {
        StartCoroutine(PreloadAllScenes());
    }

    IEnumerator PreloadAllScenes()
    {
        float totalProgress = 0f;
        int scenesCount = scenesToPreload.Count;

        // Alle Szenen asynchron laden, aber nicht aktivieren
        List<AsyncOperation> operations = new();

        foreach (string sceneName in scenesToPreload)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = false;
            operations.Add(op);
        }

        // Ladebalken aktualisieren
        while (true)
        {
            float sumProgress = 0f;

            foreach (var op in operations)
                sumProgress += Mathf.Clamp01(op.progress / 0.9f); // max. 0.9

            float average = sumProgress / scenesCount;
            loadingSlider.value = average;
            mytext.text = $"{Mathf.RoundToInt(average * 100)}%";

            if (average >= 0.99f)
                break;

            yield return null;
        }

        // Kurze Pause für Eindruck von "100%"
        loadingSlider.value = 1f;
        mytext.text = "100%";
        yield return new WaitForSeconds(0.5f);

        // Szenen aktivieren (flackert nicht, weil sie sofort entladen werden)
        foreach (var op in operations)
            op.allowSceneActivation = true;

        // Warten, bis alle Szenen aktiv wurden
        foreach (var op in operations)
        {
            while (!op.isDone)
                yield return null;
        }

        // Direkt entladen – keine visuelle Aktivierung sichtbar
        foreach (string scene in scenesToPreload)
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        // Hauptmenü starten
        SceneManager.LoadScene(mainMenuScene);
    }
}
