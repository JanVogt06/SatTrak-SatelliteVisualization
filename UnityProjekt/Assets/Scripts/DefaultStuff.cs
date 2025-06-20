using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DefaultStuff : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float timer;

    public GameObject ButtonOpenObject;

    public Animator uiAnimator;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 0.2f)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            fpsText.text = $"{Mathf.RoundToInt(fps)} FPS";
            timer = 0;
        }
    }

    void Start()
    {
        ButtonOpenObject.SetActive(true);
        Application.targetFrameRate = 240;
    }

    public void EndGame()
    {
        Application.Quit();
    }

    public void PlayAnimation()
    {
        ButtonOpenObject.SetActive(false);
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("Play");
        }
    }

    public void PlayBackAnimation()
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("Back");
            StartCoroutine(AnimationDelay());
        }
    }

    public IEnumerator AnimationDelay()
    {
        yield return new WaitForSeconds(0.3f);
        ButtonOpenObject.SetActive(true);
    }
}
