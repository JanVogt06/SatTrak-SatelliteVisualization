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
    [Header("UI References")]
    public GameObject panel;
    public Button openButton;
    public RectTransform contentParent; 
    public GameObject rowPrefab;
    public Button nextPageButton;
    public Button prevPageButton;
    public TextMeshProUGUI pageLabel;

    [Header("External References")]
    public SatelliteManager satelliteManager;
    public CesiumZoomController zoomController;
    public CesiumGeoreference georeference;

    [Header("Settings")]
    private int itemsPerPage = 20; 

    [Header("Camera Tracking Settings")]
    public float cameraDistanceOffset = 100000f;

    private Satellite trackedSatellite;
    private bool isTracking = false;

    private List<string> allSatelliteNames = new List<string>();
    private int currentPage = 0;
    private int totalPages = 0;

    [Header("Suche")]
    public TMP_InputField searchInputField;
    private List<string> filteredSatelliteNames = new List<string>();

    [Header("Info Panel")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoText;
    public Toggle OrbitToggle;

    [Header("Zoom-Slider-Einstellungen")]
    public Slider zoomSlider;                 
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
        "TERRA",
        "AQUA",
        "TERRASAR-X",
        "SKYTERRA 1",
        "LANDSAT 8",
        "INSAT-3D",
        "SENTINEL-2A",
        "JASON-3",
        "INSAT-3DR",
        "SENTINEL-2B",
        "NOAA 20",
        "BEIDOU-3 G1",
        "LANDSAT 9",
        "INSAT-3DS",
        "ROBUSTA-3A",
        "28485 SWIFT",
        "43435 TESS",
        "37849 SUOMI NPP",
        "39634 SENTINEL-1A"
    };

    void Start()
    {
        if (satelliteManager == null)
            satelliteManager = SatelliteManager.Instance;
        if (zoomController == null)
            zoomController = FindObjectOfType<CesiumZoomController>();
        if (georeference == null)
            georeference = FindObjectOfType<CesiumGeoreference>();

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

        totalPages = Mathf.Max(1,
            Mathf.CeilToInt((float)filteredSatelliteNames.Count / itemsPerPage));
        ShowPage(0);
    }

    public void OpenButtonPanel()
    {
        var satellites = SatelliteManager.Instance.GetAllSatellites();
        foundSatText.text = $"{satellites.Count}";

        bool nowOpen = !panel.activeInHierarchy;
        panel.SetActive(nowOpen);

        openButton.GetComponent<Image>().color =
            nowOpen ? new Color(1f, 0.7058824f, 0f, 1f)
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
        OrbitToggle.isOn = satellite.orbit.shouldCalculateOrbit;
        if (satellite == null)
        {
            infoPanel.SetActive(false);
            return;
        }

        Orbit orbit = satellite.OrbitPropagator.Orbit;

        string info =
                "<b>Satellite Data</b>\n\n" +
                $"<b>Name:</b> {satellite.gameObject.name}\n" +
                $"<b>Epoch (UTC):</b> {orbit.Epoch:yyyy-MM-dd HH:mm:ss}\n" +

                "\n<b>SGP4 Orbital Elements</b>\n" +
                $"<b>Inclination:</b> {orbit.Inclination.Degrees:F4}°\n" +
                $"<b>RAAN:</b> {orbit.AscendingNode.Degrees:F4}°\n" +
                $"<b>Argument of Perigee:</b> {orbit.ArgumentPerigee.Degrees:F4}°\n" +
                $"<b>Mean Anomaly:</b> {orbit.MeanAnomoly.Degrees:F4}°\n" +
                $"<b>Eccentricity:</b> {orbit.Eccentricity:F6}\n" +
                $"<b>Mean Motion:</b> {orbit.MeanMotion:F6} rad/min\n" +
                $"<b>Mean Motion (recov.):</b> {orbit.RecoveredMeanMotion:F6} rad/min\n" +

                $"<b>Semi-major Axis:</b> {orbit.SemiMajorAxis:F2} km\n" +
                $"<b>Apogee:</b> {orbit.Apogee:F2} km\n" +
                $"<b>Perigee:</b> {orbit.Perigee:F2} km\n" +
                $"<b>Period:</b> {orbit.Period:F2} min\n" +

                $"<b>BStar (drag):</b> {orbit.BStar:E2}\n";

        infoText.text = info;
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
        totalPages             = Mathf.Max(1,
                                Mathf.CeilToInt((float)filteredSatelliteNames.Count
                                                / itemsPerPage));

        currentPage                = 0;
        prevPageButton.interactable = false;
        firstPageButton.interactable = false;

        bool multiplePages = totalPages > 1;
        nextPageButton.interactable  = multiplePages;
        lastPageButton.interactable  = multiplePages;

        pageLabel.text = $"Seite 1 / {totalPages}";

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int end = Mathf.Min(itemsPerPage, filteredSatelliteNames.Count);
        for (int i = 0; i < end; i++)
        {
            string name  = filteredSatelliteNames[i];
            Satellite sat = satelliteManager.GetSatelliteByName(name);

            var row   = Instantiate(rowPrefab, contentParent);
            var tmps  = row.GetComponentsInChildren<TextMeshProUGUI>();

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

        foundSatText.text = $"{allSatelliteNames.Count}";

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
        infoPanel.SetActive(false);
    }

    public void ToggleOrbit(bool visible)
    {
        trackedSatellite.orbit.shouldCalculateOrbit = visible;
    }

    private void LateUpdate()
    {
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
        foreach (Satellite sat in satelliteManager.GetAllSatellites())
            sat.orbit.shouldCalculateOrbit = false;

        if (trackedSatellite != null)
            OrbitToggle.isOn = trackedSatellite.orbit.shouldCalculateOrbit;

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseTheWindow()
    {
        panel.SetActive(false);

        openButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);

        EventSystem.current.SetSelectedGameObject(null);
    }
}
