using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject settingsMenu;
    public GameObject creditsMenu;

    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;
    public Button muteButton;
    public Image muteButtonImage;
    public Sprite muteOnSprite;
    public Sprite muteOffSprite;

    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;

    private Resolution[] resolutions;
    private List<string> resolutionOptions = new();
    private int currentResolutionIndex = 0;

    private MusicManager musicManager => MusicManager.Instance;

    void Start()
    {
        InitializeQualityDropdown();
        InitializeResolutionDropdown();

        UpdateAudioUI();
        BindEvents();
        OpenMainMenu();
    }

    void BindEvents()
    {
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        muteButton.onClick.AddListener(OnMuteClicked);
    }

    public void OnVolumeChanged(float value)
    {
        musicManager.isMuted = false;
        musicManager.SetVolume(value);
        UpdateAudioUI();
    }

    public void OnMuteClicked()
    {
        musicManager.ToggleMute();
        UpdateAudioUI();
    }

    public void UpdateAudioUI()
    {
        float vol = musicManager.isMuted ? 0f : musicManager.volume;

        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.value = vol;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        volumeText.text = Mathf.RoundToInt(vol * 100f) + "%";

        bool muted = musicManager.isMuted;

        muteButtonImage.sprite = muted ? muteOffSprite : muteOnSprite;
        muteButtonImage.color = muted ? Color.red : Color.white;

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        creditsMenu.SetActive(false);
    }

    public void OpenMainMenuFromCredits()
    {
        settingsMenu.SetActive(false);
        creditsMenu.SetActive(false);

        StartCoroutine(OpenMainMenuDelay());
    }

    public void OpenSettingsMenu()
    {
        mainMenu.SetActive(false);
        creditsMenu.SetActive(false);
        settingsMenu.SetActive(true);
        UpdateAudioUI();
    }

    public void OpenCreditsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        StartCoroutine(OpenCreditsDelay());
    }

    public IEnumerator OpenCreditsDelay()
    {
        yield return new WaitForSeconds(4f);
        creditsMenu.SetActive(true);
    }

    public IEnumerator OpenMainMenuDelay()
    {
        yield return new WaitForSeconds(4f);
        mainMenu.SetActive(true);
    }

    void InitializeQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        List<string> options = new();

        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            options.Add(QualitySettings.names[i]);
        }

        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        qualityDropdown.onValueChanged.AddListener(SetQualityLevel);
    }

    void SetQualityLevel(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
    }


    void InitializeResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();
        resolutions = Screen.resolutions;
        resolutionOptions.Clear();

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution res = resolutions[i];
            string option = $"{res.width}x{res.height} @ {Mathf.RoundToInt((float)res.refreshRateRatio.value)}Hz";
            resolutionOptions.Add(option);

            // Aktuelle Auflösung finden
            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height &&
                Mathf.Approximately((float)res.refreshRateRatio.value, (float)Screen.currentResolution.refreshRateRatio.value))
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow, res.refreshRateRatio);
    }
}
