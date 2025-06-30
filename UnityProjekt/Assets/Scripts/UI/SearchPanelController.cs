using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Satellites;
using CesiumForUnity;
using Satellites.SGP.Propagation;
using Unity.Mathematics;
using UnityEngine.EventSystems;
using System;

public class SearchPanelController : MonoBehaviour
{
    [Header("UI References")] public GameObject panel;
    public Button openButton;
    public RectTransform contentParent;
    public GameObject rowPrefab;
    public Button nextPageButton;
    public Button prevPageButton;
    public TextMeshProUGUI pageLabel;

    [Header("External References")] public SatelliteManager satelliteManager;
    public CesiumZoomController zoomController;
    public CesiumGeoreference georeference;
    public GridLayoutGroup trackedLayoutGroup;
    public GameObject SatelliteTrackPrefab;
    public GameObject SatelliteTrackList;

    [Header("Settings")] private int itemsPerPage = 20;

    [Header("Camera Tracking Settings")] public float cameraDistanceOffset = 100000f;

    private Satellite trackedSatellite;
    private Dictionary<Satellite, GameObject> _allTrackedSatellites = new();
    private bool isTracking = false;

    private List<string> allSatelliteNames = new List<string>();
    private int currentPage = 0;
    private int totalPages = 0;

    [Header("Suche")] public TMP_InputField searchInputField;
    private List<string> filteredSatelliteNames = new List<string>();

    [Header("Info Panel")] public GameObject infoPanel;
    public Toggle OrbitToggle;

    [Header("Zoom-Slider-Einstellungen")] public Slider zoomSlider;
    public float minDistance;
    public float maxDistance;

    private float defaultSliderPos = 0.03f;

    private float _targetDistance;
    private float _currentVelocity;

    private Satellite _highlightedSatellite;

    public Sprite normalSatButton;
    public Sprite disabledSatButton;
    public Sprite normalCityButton;
    public Sprite disabledCityButton;

    public GameObject theUI1;
    public GameObject theUI2;

    public Button firstPageButton;
    public Button lastPageButton;

    public TextMeshProUGUI foundSatText;

    public TMP_Dropdown filterDropdown;

    private Dictionary<string, double> perigeeByName = new Dictionary<string, double>();

    public Button disableAllOrbitsButton;

    [SerializeField] private TextMeshProUGUI nameValue;
    [SerializeField] private TextMeshProUGUI epochValue;
    [SerializeField] private TextMeshProUGUI inclinationValue;
    [SerializeField] private TextMeshProUGUI raanValue;
    [SerializeField] private TextMeshProUGUI argPerigeeValue;
    [SerializeField] private TextMeshProUGUI meanAnomalyValue;
    [SerializeField] private TextMeshProUGUI eccentricityValue;
    [SerializeField] private TextMeshProUGUI meanMotionValue;
    [SerializeField] private TextMeshProUGUI semiMajorAxisValue;
    [SerializeField] private TextMeshProUGUI apogeeValue;
    [SerializeField] private TextMeshProUGUI perigeeValue;
    [SerializeField] private TextMeshProUGUI periodValue;
    [SerializeField] private TextMeshProUGUI bStarValue;

    public DefaultStuff infoPanelAnimation;

    public GameObject famousInfoPanel;               
    public TextMeshProUGUI famousInfoText;

    private enum FilterMode
    {
        All,
        Famous,
        NameAscending,
        NameDescending,
        PerigeeAscending,
        PerigeeDescending
    }

    private FilterMode currentMode = FilterMode.All;

    private static readonly string[] FAMOUS_SATELLITES =
    {
        "25544",
        "20580",
        "56217",
        "46984",
		"63147"
    };

    private readonly Dictionary<string, string> famousInfoTexts = new()
    {
        { "25544",
          "<u>International Space Station (ISS)</u> \n\nIn service since 1998, the ISS circles Earth every 90 minutes at roughly 400 km altitude. It is a joint venture of NASA, Roscosmos, ESA, JAXA and CSA and has been permanently inhabited since November 2000. More than 3 000 microgravity, biology, technology and Earth-observation experiments have been conducted on board. The station offers about 930 m³ of pressurised volume—comparable to a six-bedroom house—and is visible to the naked eye from the ground. Current plans call for operation until at least 2030, after which commercial outposts are expected to take over." },

        { "20580",
          "<u>Hubble Space Telescope (HST)</u> \n\nDeployed on 24 April 1990 by Space Shuttle Discovery, Hubble’s 2.4 m mirror has provided diffraction-limited imaging above Earth’s atmosphere. Five shuttle servicing missions between 1993 and 2009 corrected optics and upgraded instruments, enabling landmark deep-field views and precise measurements of cosmic expansion. Hubble continues to operate at ~540 km altitude, working in tandem with newer telescopes such as JWST. Its observations span ultraviolet to near-infrared wavelengths and have contributed to over 20 000 peer-reviewed papers. Without an end-of-life boost or de-orbit mission, re-entry is projected for the late 2030s." },

        { "46984",
          "<u>Sentinel-6 Michael Freilich</u> \n\nLaunched on 21 November 2020, this altimetry satellite continues the precise sea-level record begun by TOPEX/Poseidon in 1992. Operating in a 1 336 km sun-synchronous orbit, its Poseidon-4 radar measures global mean sea level to millimetre accuracy, supporting climate studies and weather forecasting. Sentinel-6 is a collaboration among ESA, EUMETSAT, NASA and NOAA, with a twin (Sentinel-6B) planned for 2025. An onboard GNSS radio-occultation payload also derives atmospheric temperature and humidity profiles. The mission is designed to extend high-quality ocean-topography data through at least 2030." },

        { "56217",
          "<u>Kepler-20 (Kepler Communications)</u> \n\nThis 6U CubeSat was released on 15 April 2023 during SpaceX’s Transporter-7 rideshare and augments Kepler’s growing Gen-1 constellation. Each satellite, built in Toronto, combines Ku-band broadband with narrowband IoT links to create a global store-and-forward data network. Future iterations will add optical inter-satellite lasers for real-time relay services to other spacecraft. Weighing about 10 kg, Kepler-20 orbits in a 97° sun-synchronous track at 575 km altitude. The company’s long-term goal is a space-based “Internet for things”—enabling continuous connectivity for remote sensors, ships and satellites." },

        { "63147",
          "<u>Starlink-32936</u> \n\nPart of SpaceX’s large-scale Starlink network, this satellite was launched on 2 March 2024 and operates in a 550 km low-Earth orbit. Phased-array antennas deliver broadband internet with latencies below 30 ms, while optical laser links route data between satellites without ground hops. The vehicle uses electric propulsion for orbit-keeping and end-of-life disposal, limiting lifetime to about five years before controlled de-orbit. Starlink has already surpassed 7 300 active satellites, forming the largest constellation in history. Mitigation measures such as darker coatings, attitude adjustments and coordination protocols aim to reduce brightness and collision risk." }
    };

    private List<Color> PossibleTrackColors = new List<Color>
    {
        Color.blue,
        Color.green,
        Color.magenta,
        Color.red,
        Color.cyan,
        Color.yellow,
        new(1.0f, 0.6470588235f, 0.0f),
        new(0.616f, 0.0f, 1.0f),
        new(0.6f, 0.3f, 0.0f)
    };

    void Start()
    {
        if (satelliteManager == null)
            satelliteManager = SatelliteManager.Instance;
        if (zoomController == null)
            zoomController = FindObjectOfType<CesiumZoomController>();
        if (georeference == null)
            georeference = FindObjectOfType<CesiumGeoreference>();

        infoPanel.SetActive(false);
        panel.SetActive(false);
        openButton.onClick.AddListener(() => OpenButtonPanel());

        nextPageButton.onClick.AddListener(() => ShowPage(currentPage + 1));
        nextPageButton.onClick.AddListener(() => ResetHighlight());
        prevPageButton.onClick.AddListener(() => ShowPage(currentPage - 1));
        prevPageButton.onClick.AddListener(() => ResetHighlight());

        firstPageButton.onClick.AddListener(() => ShowPage(0));
        firstPageButton.onClick.AddListener(() => ResetHighlight());

        lastPageButton.onClick.AddListener(() => ShowPage(totalPages - 1));
        lastPageButton.onClick.AddListener(() => ResetHighlight());

        disableAllOrbitsButton.onClick.AddListener(DisableAllOrbits);

        satelliteManager.OnSatellitesLoaded += list =>
        {
            allSatelliteNames = list.Select(s => s.gameObject.name).ToList();
            ApplySearchFilter(searchInputField.text);
        };

        searchInputField.onValueChanged.AddListener(ApplySearchFilter);

        filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        searchInputField.onValueChanged.AddListener(ApplySearchFilter);

        satelliteManager.OnSatellitesLoaded += list =>
        {
            allSatelliteNames = list.Select(s => s.gameObject.name).ToList();
            ApplyCurrentFilter();
        };
    }

    public void ResetHighlight()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnFilterChanged(int index)
    {
        currentMode = (FilterMode)index;
        searchInputField.SetTextWithoutNotify(string.Empty);
        ApplyCurrentFilter();
    }

    private void ApplyCurrentFilter()
    {
        switch (currentMode)
        {
            case FilterMode.All:
                filteredSatelliteNames = new List<string>(allSatelliteNames);
                break;

            case FilterMode.Famous:
                filteredSatelliteNames = allSatelliteNames
                    .Where(n => FAMOUS_SATELLITES.Any(f =>
                        n.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
                break;

            case FilterMode.NameAscending:
                filteredSatelliteNames = allSatelliteNames
                    .OrderBy(n => StripLeadingDigits(n).ToUpperInvariant())
                    .ToList();
                break;

            case FilterMode.NameDescending:
                filteredSatelliteNames = allSatelliteNames
                    .OrderByDescending(n => StripLeadingDigits(n).ToUpperInvariant())
                    .ToList();
                break;

            case FilterMode.PerigeeAscending:
                filteredSatelliteNames = allSatelliteNames
                    .OrderBy(n => perigeeByName[n])
                    .ToList();
                break;

            case FilterMode.PerigeeDescending:
                filteredSatelliteNames = allSatelliteNames
                    .OrderByDescending(n => perigeeByName[n])
                    .ToList();
                break;
        }

        UpdateFoundLabel();

        totalPages = Mathf.Max(1,
            Mathf.CeilToInt((float)filteredSatelliteNames.Count / itemsPerPage));
        ShowPage(0);
    }

    public void OpenButtonPanel()
    {
        UpdateFoundLabel();

        bool nowOpen = !panel.activeInHierarchy;
        panel.SetActive(nowOpen);

        openButton.GetComponent<Image>().color =
            nowOpen
                ? new Color(1f, 0.7058824f, 0f, 1f)
                : new Color(1f, 1f, 1f, 1f);

        if (nowOpen)
            ResetSearchPanel();
    }


    private void Awake()
    {
        satelliteManager.OnSatellitesLoaded += list =>
        {
            allSatelliteNames = list.Select(s => s.gameObject.name).ToList();
            perigeeByName = list.ToDictionary(
                sat => sat.gameObject.name,
                sat => sat.OrbitPropagator.Orbit.Perigee);

            ApplyCurrentFilter();
        };

        theUI1.SetActive(true);
        theUI2.SetActive(true);

        zoomSlider.minValue = 0f;
        zoomSlider.maxValue = 1f;
        zoomSlider.wholeNumbers = false;
        zoomSlider.value = defaultSliderPos;
        zoomSlider.onValueChanged.AddListener(UpdateCameraDistance);

        _targetDistance = Mathf.Lerp(minDistance, maxDistance, defaultSliderPos);
        cameraDistanceOffset = _targetDistance;

        filterDropdown.ClearOptions();
        filterDropdown.AddOptions(new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("All satellites"),
            new TMP_Dropdown.OptionData("Famous satellites"),
            new TMP_Dropdown.OptionData("Name ascending"),
            new TMP_Dropdown.OptionData("Name descending"),
            new TMP_Dropdown.OptionData("Distance ascending"),
            new TMP_Dropdown.OptionData("Distance descending")
        });
        filterDropdown.RefreshShownValue();
    }

    private static string StripLeadingDigits(string input)
    {
        int index = 0;
        while (index < input.Length && char.IsDigit(input[index]))
            index++;
        return input.Substring(index);
    }

    private void UpdateCameraDistance(float t)
    {
        _targetDistance = Mathf.Lerp(minDistance, maxDistance, t);

        if (isTracking && trackedSatellite != null)
            trackedSatellite.modelController.SetHighlight(t >= 0.4f);
    }


    public void ResetZoomSlider()
    {
        zoomSlider.SetValueWithoutNotify(defaultSliderPos);
        _targetDistance = Mathf.Lerp(minDistance, maxDistance, defaultSliderPos);
        cameraDistanceOffset = _targetDistance;
        _currentVelocity = 0f;

        if (trackedSatellite != null)
            trackedSatellite.modelController.SetHighlight(false);
    }


    private void ShowSatelliteInfo(Satellite satellite)
    {
        OrbitToggle.isOn = satellite.orbit.ShouldCalculateOrbit;
        if (satellite == null)
        {
            infoPanelAnimation.ResetPanelAnimation();
            infoPanel.SetActive(false);
            famousInfoPanel.SetActive(false);   
            return;
        }

        Orbit orbit = satellite.OrbitPropagator.Orbit;

        nameValue.text = satellite.gameObject.name;
        epochValue.text = orbit.Epoch.ToString("yyyy-MM-dd HH:mm:ss");
        inclinationValue.text = $"{orbit.Inclination.Degrees:F4}°";
        raanValue.text = $"{orbit.AscendingNode.Degrees:F4}°";
        argPerigeeValue.text = $"{orbit.ArgumentPerigee.Degrees:F4}°";
        meanAnomalyValue.text = $"{orbit.MeanAnomoly.Degrees:F4}°";
        eccentricityValue.text = $"{orbit.Eccentricity:F6}";
        meanMotionValue.text = $"{orbit.MeanMotion:F6} rad/min";
        semiMajorAxisValue.text = $"{orbit.SemiMajorAxis:F2} km";
        apogeeValue.text = $"{orbit.Apogee:F2} km";
        perigeeValue.text = $"{orbit.Perigee:F2} km";
        periodValue.text = $"{orbit.Period:F2} min";
        bStarValue.text = $"{orbit.BStar:E2}";

        string satName = satellite.gameObject.name;
        bool isFamous = famousInfoTexts.Keys.Any(id => satName.Contains(id, StringComparison.OrdinalIgnoreCase));

        if (isFamous)
        {
            string id = famousInfoTexts.Keys.First(k => satName.Contains(k, StringComparison.OrdinalIgnoreCase));
            famousInfoText.text = famousInfoTexts[id];
            famousInfoPanel.SetActive(true);
        }
        else
        {
            famousInfoPanel.SetActive(false);
        }

        infoPanel.SetActive(true);
    }

    private void ApplySearchFilter(string query)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            if (currentMode != FilterMode.All)
            {
                currentMode = FilterMode.All;
                filterDropdown.SetValueWithoutNotify(0);
                filterDropdown.RefreshShownValue();
            }

            filteredSatelliteNames = allSatelliteNames
                .Where(n => n.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            totalPages = Mathf.Max(1,
                Mathf.CeilToInt((float)filteredSatelliteNames.Count / itemsPerPage));

            UpdateFoundLabel();

            ShowPage(0);
            return;
        }

        ApplyCurrentFilter();
    }


    private void ShowPage(int pageIndex)
    {
        if (filteredSatelliteNames.Count == 0)
        {
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            pageLabel.text = "No results";
            prevPageButton.interactable =
                nextPageButton.interactable =
                    firstPageButton.interactable =
                        lastPageButton.interactable = false;
            return;
        }

        currentPage = Mathf.Clamp(pageIndex, 0, totalPages - 1);

        bool onFirst = currentPage == 0;
        bool onLast = currentPage == totalPages - 1;

        prevPageButton.interactable = !onFirst;
        firstPageButton.interactable = !onFirst;
        nextPageButton.interactable = !onLast;
        lastPageButton.interactable = !onLast;

        pageLabel.text = $"Page {currentPage + 1} / {totalPages}";

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int start = currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, filteredSatelliteNames.Count);

        for (int i = start; i < end; i++)
        {
            string satName = filteredSatelliteNames[i];
            Satellite sat = satelliteManager.GetSatelliteByName(satName);

            var row = Instantiate(rowPrefab, contentParent);
            var tmps = row.GetComponentsInChildren<TextMeshProUGUI>();

            TextMeshProUGUI nameTxt = tmps[0];
            TextMeshProUGUI distTxt = tmps[1];

            nameTxt.text = satName;
            double perigeeKm = sat.OrbitPropagator.Orbit.Perigee;
            distTxt.text = $"{perigeeKm:F0} km";

            row.GetComponent<Button>().onClick.AddListener(() => OnItemSelected(satName));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
    }


    public void OnItemSelected(string itemName)
    {
        StartCoroutine(zoomController.FadeToBlack());
        ResetSearchPanel();
        panel.SetActive(false);

        openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);

        StartCoroutine(TheLoop(itemName));
    }

    public IEnumerator TheLoop(string itemName)
    {
        yield return new WaitForSeconds(1.2f);

        if (_highlightedSatellite != null)
            _highlightedSatellite.modelController.SetHighlight(false);

        trackedSatellite = satelliteManager.GetSatelliteByName(itemName);
        if (trackedSatellite == null)
        {
            isTracking = false;
            infoPanelAnimation.ResetPanelAnimation();
            infoPanel.SetActive(false);
            yield break;
        }

        Vector3 sphVec = trackedSatellite.transform.position;

        georeference.latitude = sphVec.y;
        georeference.longitude = sphVec.x;
        georeference.height = sphVec.z;

        zoomController.SnapToSatellit(trackedSatellite);

        ShowSatelliteInfo(trackedSatellite);
        isTracking = true;

        ResetZoomSlider();

        trackedSatellite.modelController.SetHighlight(true);
        _highlightedSatellite = trackedSatellite;
    }

    public void ResetSearchPanel()
    {
        filterDropdown.SetValueWithoutNotify(0);
        currentMode = FilterMode.All;

        searchInputField.SetTextWithoutNotify(string.Empty);

        filteredSatelliteNames = new List<string>(allSatelliteNames);
        totalPages = Mathf.Max(1,
            Mathf.CeilToInt((float)filteredSatelliteNames.Count
                            / itemsPerPage));

        currentPage = 0;
        prevPageButton.interactable = false;
        firstPageButton.interactable = false;

        bool multiplePages = totalPages > 1;
        nextPageButton.interactable = multiplePages;
        lastPageButton.interactable = multiplePages;

        pageLabel.text = $"Page 1 / {totalPages}";

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int end = Mathf.Min(itemsPerPage, filteredSatelliteNames.Count);
        for (int i = 0; i < end; i++)
        {
            string name = filteredSatelliteNames[i];
            Satellite sat = satelliteManager.GetSatelliteByName(name);

            var row = Instantiate(rowPrefab, contentParent);
            var tmps = row.GetComponentsInChildren<TextMeshProUGUI>();

            tmps[0].text = name;

            double perigeeKm = sat.OrbitPropagator.Orbit.Perigee;
            tmps[1].text = $"{perigeeKm:F0} km";

            var btn = row.GetComponent<Button>();
            btn.onClick.AddListener(() => OnItemSelected(name));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);

        var scroll = contentParent.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;

        UpdateFoundLabel();

        pageLabel.text = $"Page 1 / {totalPages}";
        filterDropdown.RefreshShownValue();
    }

    public void StopTracking()
    {
        isTracking = false;
        if (_highlightedSatellite != null)
            _highlightedSatellite.modelController.SetHighlight(false);
        _highlightedSatellite = null;

        trackedSatellite = null;
        infoPanelAnimation.ResetPanelAnimation();
        infoPanel.SetActive(false);
    }

    public void ToggleOrbit(bool visible)
    {
        if (trackedSatellite == null) return;

        bool hasEntry = _allTrackedSatellites.TryGetValue(trackedSatellite, out GameObject trackGo);

        if (visible && !hasEntry)
        {
            if (_allTrackedSatellites.Count == PossibleTrackColors.Count)
            {
                var firstKey = _allTrackedSatellites.Keys.First();
                Destroy(_allTrackedSatellites[firstKey]);
                _allTrackedSatellites.Remove(firstKey);
            }

            Color color = PossibleTrackColors[_allTrackedSatellites.Count];

            trackGo = Instantiate(SatelliteTrackPrefab, trackedLayoutGroup.transform);
            trackGo.GetComponentsInChildren<Image>().Single(child => child.CompareTag("Color")).color = color;
            trackGo.GetComponentInChildren<TMP_Text>().text = trackedSatellite.name;
            trackGo.name = trackedSatellite.name;

            _allTrackedSatellites.Add(trackedSatellite, trackGo);
            trackedSatellite.orbit.ToggleCalculateOrbit(true, color);
        }
        else if (!visible && hasEntry)           
        {
            Destroy(trackGo);
            _allTrackedSatellites.Remove(trackedSatellite);
            trackedSatellite.orbit.ToggleCalculateOrbit(false);
        }

        OrbitToggle.SetIsOnWithoutNotify(trackedSatellite.orbit.ShouldCalculateOrbit);
    }


    private void LateUpdate()
    {
        if (_allTrackedSatellites.Count != 0)
        {
            SatelliteTrackList.SetActive(true);
        }
        else
        {
            SatelliteTrackList.SetActive(false);
        }
        if (!isTracking || trackedSatellite == null) return;

        Vector3 satPos = trackedSatellite.transform.position;

        double4x4 ecefToLocal = georeference.ecefToLocalMatrix;
        double3 earthD = math.transform(ecefToLocal, double3.zero);
        Vector3 earth = new Vector3((float)earthD.x, (float)earthD.y, (float)earthD.z);

        Vector3 radial = (satPos - earth).normalized;

        cameraDistanceOffset = Mathf.SmoothDamp(
            cameraDistanceOffset,
            _targetDistance,
            ref _currentVelocity,
            0.25f);

        Vector3 camPos = satPos + radial * cameraDistanceOffset;
        Camera.main.transform.position = camPos;

        Vector3 up = Vector3.Cross(radial, Vector3.right);
        if (up.sqrMagnitude < 1e-6f)
            up = Vector3.Cross(radial, Vector3.forward);
        up.Normalize();

        Camera.main.transform.rotation = Quaternion.LookRotation(satPos - camPos, up);
    }

    public void DisableAllOrbits()
    {
        foreach (var (satellite, go) in _allTrackedSatellites)
        {
            Destroy(go);
            satellite.orbit.ToggleCalculateOrbit(false);   
        }
        _allTrackedSatellites.Clear();

        if (trackedSatellite != null)
            OrbitToggle.SetIsOnWithoutNotify(trackedSatellite.orbit.ShouldCalculateOrbit);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseTheWindow()
    {
        panel.SetActive(false);

        openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void UpdateFoundLabel()
    {
        if (foundSatText != null)
            foundSatText.text = filteredSatelliteNames.Count.ToString();
    }
}