using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UI.Pagination;
using Satellites;
using CesiumForUnity;

public class SearchPanelController : MonoBehaviour
{
    /* ─────────── Inspector ─────────── */
    [Header("UI References")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] CanvasGroup panelGroup;
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button loadBlockBtn;
    [SerializeField] GameObject rowPrefab;
    [SerializeField] TMP_InputField searchField;

    [Header("PagedRect")]
    [SerializeField] PagedRect pagedRect;
    [SerializeField] int itemsPerPage = 20;
    [SerializeField] int blockSize = 10;

    [Header("Extern")]
    [SerializeField] SatelliteManager satelliteManager;
    [SerializeField] CesiumZoomController zoomController;
    [SerializeField] CesiumGeoreference georeference;

    [Header("Tracking")]
    [SerializeField] float cameraDistanceOffset = 1e5f;

    /* ─────────── intern ─────────── */
    List<string> allSatNames;
    List<string> satNames;
    int totalPages;
    int pagesBuilt;

    bool isPaused;
    bool isRebuilding;

    private Satellite trackedSatellite;
    private bool isTracking = false;

    /* ==================== Life-Cycle ==================== */
    void Awake()
    {
        if (!panelGroup && panelRoot)
            panelGroup = panelRoot.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        satelliteManager ??= SatelliteManager.Instance;
        zoomController ??= FindObjectOfType<CesiumZoomController>();
        georeference ??= FindObjectOfType<CesiumGeoreference>();

        allSatNames = satelliteManager ? satelliteManager.GetSatelliteNames().ToList()
                                       : new List<string>();
        satNames = new List<string>(allSatNames);
        CalcTotalPages();

        HidePanel();

        if (openButton) openButton.onClick.AddListener(ShowPanel);
        if (closeButton) closeButton.onClick.AddListener(HidePanel);
        if (loadBlockBtn) loadBlockBtn.onClick.AddListener(BuildNextBlock);
        if (searchField) searchField.onEndEdit.AddListener(OnSearchSubmit);

        if (pagedRect?.PageChangedEvent != null)
            pagedRect.PageChangedEvent.AddListener(OnPageChanged);
    }

    /* ==================== Panel-Handling ==================== */
    void ShowPanel()
    {
        if (!panelGroup) return;
        panelGroup.alpha = 1f;
        panelGroup.blocksRaycasts = panelGroup.interactable = true;
        TryInit();
    }

    void HidePanel()
    {
        if (!panelGroup) return;
        panelGroup.alpha = 0f;
        panelGroup.blocksRaycasts = panelGroup.interactable = false;
    }

    /* ---------- Init ---------- */
    void TryInit()
    {
        EnsureSatelliteList();

        if (pagesBuilt == 0 && pagedRect)
        {
            ClearPages(keepTemplate: true);
            BuildNextBlock();
            pagedRect.UpdateDisplay();
            pagedRect.ShowFirstPage();
            SnapScrollToStart();
        }
    }

    void EnsureSatelliteList()
    {
        satelliteManager ??= SatelliteManager.Instance;
        allSatNames = satelliteManager ? satelliteManager.GetSatelliteNames().ToList()
                                       : new List<string>();
        ApplyFilter(searchField ? searchField.text : string.Empty, false);
    }

    /* ---------- Suche ---------- */
    void OnSearchSubmit(string term) => ApplyFilter(term, true);

    void ApplyFilter(string term, bool rebuild)
    {
        term = term?.Trim();
        satNames = string.IsNullOrEmpty(term)
            ? new List<string>(allSatNames)
            : allSatNames.Where(n => n.IndexOf(term,
                                               System.StringComparison.OrdinalIgnoreCase) >= 0)
                         .ToList();
        CalcTotalPages();

        if (!rebuild) return;

        /* ─── Rebuild aller Seiten ─── */
        isRebuilding = true;

        if (pagedRect?.PageChangedEvent != null)
            pagedRect.PageChangedEvent.RemoveListener(OnPageChanged);

        ClearPages(keepTemplate: true);
        pagesBuilt = 0;
        BuildNextBlock();
        pagedRect.UpdateDisplay();

        if (pagedRect?.PageChangedEvent != null)
            pagedRect.PageChangedEvent.AddListener(OnPageChanged);

        pagedRect.ShowFirstPage();
        SnapScrollToStart();

        isRebuilding = false;
    }

    void CalcTotalPages()
        => totalPages = Mathf.CeilToInt(satNames.Count / (float)itemsPerPage);

    /* ---------- Seiten-Aufbau ---------- */
    void BuildNextBlock()
    {
        if (!pagedRect || pagesBuilt >= totalPages) return;

        int toBuild = Mathf.Min(blockSize, totalPages - pagesBuilt);

        for (int n = 0; n < toBuild; n++)
        {
            int pageIdx = pagesBuilt + n;

            Page page;
            if (pageIdx == 0 && pagedRect.Pages.Count > 0)
            {
                page = pagedRect.Pages[0];
            }
            else
            {
                page = pagedRect.AddPageUsingTemplate();
            }

            page.name = $"Page_{pageIdx + 1}";

            foreach (Transform child in page.transform)
                Destroy(child.gameObject);

            var vlg = page.GetComponent<VerticalLayoutGroup>() ??
                      page.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;

            int start = pageIdx * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, satNames.Count);

            for (int i = start; i < end; i++)
            {
                string sat = satNames[i];
                var row = Instantiate(rowPrefab, page.transform, false);
                row.GetComponentInChildren<TextMeshProUGUI>().text = sat;
                row.GetComponent<Button>()
                   .onClick.AddListener(() => OnItemSelected(sat));
            }
        }

        pagesBuilt += toBuild;
        pagedRect.UpdateDisplay();
    }

    /* ---------- Auto-Load Callback ---------- */
    void OnPageChanged(Page prev, Page next)
    {
        if (isRebuilding) return;

        int newPageNumber = pagedRect ? pagedRect.Pages.IndexOf(next) + 1 : 0;
        if (pagesBuilt < totalPages && newPageNumber >= pagesBuilt - 1)
            BuildNextBlock();
    }

    /* ---------- Helpers ---------- */
    void ClearPages(bool keepTemplate)
    {
        if (pagedRect?.Pages == null) return;

        Page template = keepTemplate && pagedRect.Pages.Count > 0
            ? pagedRect.Pages[0]
            : null;

        foreach (var p in pagedRect.Pages.ToArray())
        {
            if (keepTemplate && p == template) continue;
            pagedRect.RemovePage(p, true);
        }

        if (template)
        {
            foreach (Transform child in template.transform)
                Destroy(child.gameObject);
        }

        pagedRect.UpdateDisplay();
    }

    /// <summary>ScrollRect gestoppt und ganz nach links/oben gesetzt.</summary>
    void SnapScrollToStart()
    {
        if (!pagedRect?.ScrollRect) return;

        pagedRect.ScrollRect.StopMovement();
        pagedRect.ScrollRect.horizontalNormalizedPosition = 0f;
        pagedRect.ScrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
    }


    void Update()
    {
        if (isTracking && trackedSatellite != null)
        {
            Vector3 satWorldPos = trackedSatellite.transform.position;

            // 2) Richtung von Erdmitte zum Sat (Erdmitte ist bei Cesium meist (0,0,0) in Unity‐Space)
            Vector3 dir = satWorldPos.normalized;

            // 3) Kamera‐Position = Sat‐Position + dir * Offset
            Vector3 camPos = satWorldPos + dir * cameraDistanceOffset;

            // 4) Übernehme Position & richte die Kamera auf den Sat aus
            Camera.main.transform.position = camPos;
            Camera.main.transform.LookAt(satWorldPos);
        }
    }

    public void StopTracking()
    {
        isTracking = false;
        trackedSatellite = null;
    }

    public void OnItemSelected(string itemName)
    {
        StartCoroutine(zoomController.FadeToBlack());
        panelRoot.SetActive(false);

        StartCoroutine(TheLoop(itemName));
    }

    public IEnumerator TheLoop(string itemName)
    {
        yield return new WaitForSeconds(1.2f);

        // gewählten SatelliteController speichern
        trackedSatellite = satelliteManager.GetSatelliteByName(itemName);
        if (trackedSatellite == null)
        {
            isTracking = false;
        }

        // sofortiger Sprung zur aktuellen Position
        Vector3 sphVec = trackedSatellite.transform.position;

        georeference.latitude = sphVec.y;
        georeference.longitude = sphVec.x;
        georeference.height = sphVec.z;

        // ZoomToEarth erwartet (lon, lat, height)
        zoomController.SnapToSatellit(trackedSatellite);

        isTracking = true;
    }
}
