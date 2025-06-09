using System;
using System.Collections.Generic;
using System.Linq;                   
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Satellites;                     
using CesiumForUnity;                 
using DefaultNamespace;               
using Unity.Mathematics;
using System.Collections;

public class SearchPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject     panel;
    public Button         openButton;
    public Button         closeButton;
    public RectTransform  contentParent;
    public GameObject     rowPrefab;

    [Header("External References")]
    public SatelliteManager     satelliteManager;
    public CesiumZoomController zoomController;
    public CesiumGeoreference   georeference;

    [Header("Settings")]
    public int maxInitialItems = 50;    // Anzahl initialer Buttons

    [Header("Camera Tracking Settings")]
    [Tooltip("Wie weit hinter dem Sat auf der Erd-Sat-Kameraachse die Kamera stehen soll (in Metern)")]
    public float cameraDistanceOffset = 100000f;

    private Satellite trackedSatellite;
    private bool isTracking = false;

    void Start()
    {
        // Falls im Inspector leer, per Code füllen:
        if (satelliteManager == null)
            satelliteManager = SatelliteManager.Instance;
        if (zoomController   == null)
            zoomController   = FindObjectOfType<CesiumZoomController>();
        if (georeference     == null)
            georeference     = FindObjectOfType<CesiumGeoreference>();

        panel.SetActive(false);
        openButton.onClick.AddListener(() => panel.SetActive(true));
        closeButton.onClick.AddListener(() => panel.SetActive(false));

        // Nur die ersten maxInitialItems laden
        var allNames     = satelliteManager.GetSatelliteNames();
        var initialNames = allNames.Take(maxInitialItems).ToList();
        Debug.Log($"SearchPanelController: Zeige {initialNames.Count} von {allNames.Count} Satelliten");
        
        PopulateList(initialNames);
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

    public void PopulateList(IEnumerable<string> items)
    {
        // Alte Einträge entfernen
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Neue Buttons anlegen
        foreach (var name in items)
        {
            var row = Instantiate(rowPrefab, contentParent);
            var txt = row.GetComponentInChildren<TextMeshProUGUI>();
            var btn = row.GetComponent<Button>();

            // ISS hervorheben
            if (name.Contains("25544"))
            {
                txt.text = "🛸 " + name + " (ISS)";
                txt.color = Color.yellow;
            }
            else
            {
                txt.text = name;
            }
    
            btn.onClick.AddListener(() => OnItemSelected(name));
        }

        // Layout aktualisieren
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

    public void StopTracking()
    {
        isTracking = false;
        trackedSatellite = null;
    }
}
