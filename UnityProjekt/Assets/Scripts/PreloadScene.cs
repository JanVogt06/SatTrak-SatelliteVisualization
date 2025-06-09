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

        for (int i = 0; i < scenesCount; i++)
        {
            string sceneName = scenesToPreload[i];
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                float currentSceneProgress = Mathf.Clamp01(op.progress / 0.9f);
                totalProgress = (i + currentSceneProgress) / scenesCount;
                loadingSlider.value = totalProgress;

                mytext.text = $"{Mathf.RoundToInt(totalProgress * 100)}%";

                yield return null;
            }
        }

        foreach (string scene in scenesToPreload)
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        loadingSlider.value = 1f;

        yield return new WaitForSeconds(0.3f);

        SceneManager.LoadScene(mainMenuScene);
    }
}
