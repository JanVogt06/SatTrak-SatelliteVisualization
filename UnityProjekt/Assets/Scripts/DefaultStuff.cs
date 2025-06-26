using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefaultStuff : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float timer;

    public GameObject ButtonOpenObject;

    public Animator uiAnimator;

    public Animator uiOneAnimator;

    public Animator uiTwoAnimator;

    public SearchPanelController spc;

    public GeoNamesSearchFromJSON gns;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 0.2f)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            fpsText.text = $"Framerate: {Mathf.RoundToInt(fps)} FPS";
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
            spc.openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            spc.ResetSearchPanel();
            spc.panel.SetActive(false);

            gns.openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            gns.ResetPanel();
            gns.panel.SetActive(false);

            uiAnimator.SetTrigger("Back");
            StartCoroutine(AnimationDelay());
        }
    }

    public void PlayUIOneOUT()
    {
        spc.openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        spc.ResetSearchPanel();
        spc.panel.SetActive(false);

        gns.openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        gns.ResetPanel();
        gns.panel.SetActive(false);

        StartCoroutine(UIOneToTwo());
    }

    public void PlayUITwoOUT()
    {
        StartCoroutine(UITwoToOne());
    }

    public IEnumerator UIOneToTwo()
    {
        if (uiOneAnimator != null)
        {
            uiOneAnimator.SetTrigger("GoOut");
        }

        yield return new WaitForSeconds(0.2f);

        if (uiTwoAnimator != null)
        {
            uiTwoAnimator.SetTrigger("GetIn");
        }
    }

    public IEnumerator UITwoToOne()
    {
        spc.openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        spc.ResetSearchPanel();
        spc.panel.SetActive(false);

        gns.openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        gns.ResetPanel();
        gns.panel.SetActive(false);

        if (uiTwoAnimator != null)
        {
            uiTwoAnimator.SetTrigger("GetOut");
        }

        yield return new WaitForSeconds(0.2f);

        if (uiOneAnimator != null)
        {
            uiOneAnimator.SetTrigger("GoIn");
        }
    }

    public IEnumerator AnimationDelay()
    {
        yield return new WaitForSeconds(0.3f);
        ButtonOpenObject.SetActive(true);
    }
}
