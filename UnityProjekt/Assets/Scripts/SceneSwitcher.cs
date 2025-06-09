using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [Header("Name der Zielszene (muss in Build Settings stehen)")]
    public string sceneName;

    public void SwitchScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Kein Szenenname angegeben!");
        }
    }

    public void EndProgramm()
    { 
        Application.Quit();
    }
}
