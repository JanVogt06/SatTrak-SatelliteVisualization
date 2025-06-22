using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Satellites;
using CesiumForUnity;
using Satellites.SGP.Propagation;
using UnityEngine.Serialization;
using Unity.Mathematics;          // notwendig für double4x4, math

public class SearchPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Button openButton;
    public Button closeButton;
    public RectTransform contentParent; // Grid mit LayoutGroup
    public GameObject rowPrefab;
    public Button nextPageButton;
    public Button prevPageButton;
    public TextMeshProUGUI pageLabel;

    [Header("External References")]
    public SatelliteManager satelliteManager;
    public CesiumZoomController zoomController;
    public CesiumGeoreference georeference;

    [Header("Settings")]
    public int itemsPerPage = 25; 

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

    void Start()
    {
        if (satelliteManager == null)
            satelliteManager = SatelliteManager.Instance;
        if (zoomController == null)
            zoomController = FindObjectOfType<CesiumZoomController>();
        if (georeference == null)
            georeference = FindObjectOfType<CesiumGeoreference>();

        panel.SetActive(false);
        openButton.onClick.AddListener(() => panel.SetActive(true));
        closeButton.onClick.AddListener(() => panel.SetActive(false));
        nextPageButton.onClick.AddListener(() => ShowPage(currentPage + 1));
        prevPageButton.onClick.AddListener(() => ShowPage(currentPage - 1));

        satelliteManager.OnSatellitesLoaded += list =>
        {
            allSatelliteNames = list.Select(s => s.gameObject.name).ToList();
            ApplySearchFilter(searchInputField.text);
        };

        searchInputField.onValueChanged.AddListener(ApplySearchFilter);
    }

    private void Awake()
    {
        theUI1.SetActive(true);
        theUI2.SetActive(true);

        zoomSlider.minValue = 0f;
        zoomSlider.maxValue = 1f;
        zoomSlider.wholeNumbers = false;
        zoomSlider.value = defaultSliderPos;
        zoomSlider.onValueChanged.AddListener(UpdateCameraDistance);

        _targetDistance = Mathf.Lerp(minDistance, maxDistance, defaultSliderPos);
        cameraDistanceOffset = _targetDistance;
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

        string info = $"<b>🛰 Satelliten-Informationen</b>\n\n" +
                      $"<b>Name:</b> {satellite.gameObject.name}\n" +
                      $"<b>Epoch:</b> {orbit.Epoch:yyyy-MM-dd HH:mm:ss}\n" +

                      "\n<b>📈 Bahnelemente (SGP4)</b>\n" +
                      $"<b>Inklination:</b> {orbit.Inclination.Degrees:F4}°\n" +
                      $"<b>RAAN (Aufsteigender Knoten):</b> {orbit.AscendingNode.Degrees:F4}°\n" +
                      $"<b>Argument des Perigäums:</b> {orbit.ArgumentPerigee.Degrees:F4}°\n" +
                      $"<b>Mean Anomaly:</b> {orbit.MeanAnomoly.Degrees:F4}°\n" +
                      $"<b>Eccentricity:</b> {orbit.Eccentricity:F6}\n" +
                      $"<b>Mean Motion:</b> {orbit.MeanMotion:F6} rad/min\n" +
                      $"<b>Recovered Mean Motion:</b> {orbit.RecoveredMeanMotion:F6} rad/min\n" +

                      $"<b>Halbachse (a):</b> {orbit.SemiMajorAxis:F2} km\n" +
                      $"<b>Apogäum:</b> {orbit.Apogee:F2} km\n" +
                      $"<b>Perigäum:</b> {orbit.Perigee:F2} km\n" +
                      $"<b>Periode:</b> {orbit.Period:F2} min\n" +

                      $"<b>BStar (Drag-Term):</b> {orbit.BStar:E2}\n";

        infoText.text = info;
        infoPanel.SetActive(true);
    }

    private void ApplySearchFilter(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            filteredSatelliteNames = new List<string>(allSatelliteNames);
        }
        else
        {
            filteredSatelliteNames = allSatelliteNames
                .Where(name => name.ToLower().Contains(query.ToLower()))
                .ToList();
        }

        totalPages = Mathf.CeilToInt((float)filteredSatelliteNames.Count / itemsPerPage);
        ShowPage(0);
    }


    private void ShowPage(int pageIndex)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, totalPages - 1);

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int start = currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, filteredSatelliteNames.Count);
        var itemsToShow = filteredSatelliteNames.GetRange(start, end - start);

        foreach (var name in itemsToShow)
        {
            var row = Instantiate(rowPrefab, contentParent);
            var txt = row.GetComponentInChildren<TextMeshProUGUI>();
            var btn = row.GetComponent<Button>();

            txt.text = name.Contains("25544") ? name + " (ISS)" : name;
            txt.color = name.Contains("25544") ? Color.red : Color.black;

            btn.onClick.AddListener(() => OnItemSelected(name));
        }

        prevPageButton.interactable = currentPage > 0;
        nextPageButton.interactable = currentPage < totalPages - 1;

        if (pageLabel != null)
            pageLabel.text = $"Seite {currentPage + 1} / {totalPages}";

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
    }


    public void OnItemSelected(string itemName)
    {
        StartCoroutine(zoomController.FadeToBlack());
        panel.SetActive(false);
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

        // 1) Positionen
        Vector3 satPos = trackedSatellite.transform.position;

        double4x4 ecefToLocal = georeference.ecefToLocalMatrix;
        double3 earthD = math.transform(ecefToLocal, double3.zero);
        Vector3 earth = new Vector3((float)earthD.x, (float)earthD.y, (float)earthD.z);

        // 2) Radialvektor Erdmittelpunkt → Satellit
        Vector3 radial = (satPos - earth).normalized;

        // 3) Kameradistanz weich an Zielwert angleichen
        cameraDistanceOffset = Mathf.SmoothDamp(
                                  cameraDistanceOffset,
                                  _targetDistance,
                                  ref _currentVelocity,
                                  0.25f);                     // Zeitkonstante [s]

        // 4) Kameraposition
        Vector3 camPos = satPos + radial * cameraDistanceOffset;
        Camera.main.transform.position = camPos;

        // 5) Up-Vektor robust bestimmen
        Vector3 up = Vector3.Cross(radial, Vector3.right);
        if (up.sqrMagnitude < 1e-6f)
            up = Vector3.Cross(radial, Vector3.forward);
        up.Normalize();

        // 6) Ausrichtung: Blick auf den Satelliten
        Camera.main.transform.rotation = Quaternion.LookRotation(satPos - camPos, up);
    }
}
