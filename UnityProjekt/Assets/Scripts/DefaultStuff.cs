using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DefaultStuff : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 0.2f)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            timer = 0;
        }
    }

    void Start()
    {
        Application.targetFrameRate = 240; // Max FPS setzen
    }

    public void EndGame()
    {
        Application.Quit();
    }
}
