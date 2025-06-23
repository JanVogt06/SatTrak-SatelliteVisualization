using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreloadScene : MonoBehaviour
{
    [Header("Szenen & UI")]
    [SerializeField] private List<string> scenesToPreload;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    private const float smoothSpeed = 0.5f; // Annäherungsgeschwindigkeit des Sliders

    private void Start()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        StartCoroutine(PreloadSequentially());
    }

    private IEnumerator PreloadSequentially()
    {
        int sceneCount = scenesToPreload.Count;
        float displayedProgress = 0f;

        for (int i = 0; i < sceneCount; i++)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scenesToPreload[i], LoadSceneMode.Additive);
            op.allowSceneActivation = false;

            // Fortschritt hochrechnen, bis die Szene bereit ist (Unity liefert max. 0.9f)
            while (op.progress < 0.9f)
            {
                float targetProgress = (i + op.progress) / sceneCount;
                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, smoothSpeed * Time.deltaTime);

                UpdateUI(displayedProgress);
                yield return null;
            }

            // Szene aktivieren, danach Abschluss abwarten
            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
        }

        // Letzten Rest auf 1.0f glätten
        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, smoothSpeed * Time.deltaTime);
            UpdateUI(displayedProgress);
            yield return null;
        }

        yield return new WaitForSeconds(2.3f); // kurzer Moment bei 100 %

        // Geladene Szenen unmittelbar entladen (nur Assets bleiben im Speicher)
        foreach (string scene in scenesToPreload)
            yield return SceneManager.UnloadSceneAsync(scene);

        SceneManager.LoadScene(mainMenuScene);
    }

    private void UpdateUI(float progress)
    {
        loadingSlider.value = progress;
        progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }
}
