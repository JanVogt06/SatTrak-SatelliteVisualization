using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using CesiumForUnity;
using Newtonsoft.Json;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using Unity.Mathematics;

public class SatelliteManager : MonoBehaviour
{
    [Header("TLE Source")]
    public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

    [Header("Prefabs & References")]
    public GameObject satellitePrefab;
    public Transform orbitParent;
    public CesiumGeoreference cesiumGeoreference;
    public CesiumZoomController zoomController;
    
    [Header("Movement Settings")]
    [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
    public float fovThreshold = 70f;
    [Tooltip("Dauer der Interpolation im Erde-Modus (in Sekunden)")]
    public float interpolationDuration = 0.5f;

    private readonly List<SatelliteController> satellites = new();
    private Dictionary<uint, Queue<double3>> satellitePositions = new();
    private Dictionary<uint, double3> currentPositions = new();
    private Dictionary<uint, Coroutine> activeRoutines = new();
    
    private const int MaxPositionCache = 50;
    private const int UpdateBatchSize = 250;
    private float TotalUpdateDelay = 1;

    void Start()
    {
        Debug.Log("SatelliteManager: Start");
        FetchTleData();
        StartCoroutine(PositionUpdateCoroutine());
        StartPositionCalculation();
    }
    
    public void OnSliderValueChanged(float newValue)
    {
        TotalUpdateDelay = newValue;
    }
    
    void FetchTleData()
    {
        try
        {
            var provider = new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(tleUrl));
            var data = provider.GetTles();
            Debug.Log($"TLE Data: Gefunden: {data.Count} Satelliten");
            
            foreach (var tle in data.Values)
            {
                GameObject sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                sat.name = tle.NoradNumber + " " + tle.Name;
                var con = sat.GetComponent<SatelliteController>();
                con.Initialize(tle);
                satellites.Add(con);
                satellitePositions[con.Tle.NoradNumber] = new Queue<double3>();
                currentPositions[con.Tle.NoradNumber] = new double3(0, 0, 0);
                activeRoutines[con.Tle.NoradNumber] = null;
            }
            
            Debug.Log($"Initialisiert: {satellites.Count} Satelliten");
        }
        catch (Exception e)
        {
            Debug.LogError("Parsing-Fehler: " + e.Message);
        }
    }

    private void StartPositionCalculation()
    {
        Task.Run(() =>
        {
            while (true)
            {
                DateTime nextTime = DateTime.Now;
                for (var i = 0; i < satellites.Count; i++)
                {
                    var satellite = satellites[i];
                    try
                    {
                        if (satellitePositions[satellite.Tle.NoradNumber].Count >= MaxPositionCache)
                        {
                            continue;
                        }

                        var positions = MaxPositionCache - satellitePositions[satellite.Tle.NoradNumber].Count;
                        
                        DateTime futureTime = nextTime;
                        for (int j = 0; j < positions; j++)
                        {
                            futureTime = futureTime.AddMinutes(1);
                            EciCoordinate result = null;
                            try
                            {
                                result = satellite.OrbitPropagator.FindPosition(futureTime);
                            }
                            catch (DecayedException _)
                            {
                                // Behandle abgestürzte Satelliten
                                continue;
                            }
                            catch (Exception _)
                            {
                                continue;
                            }

                            var pos = result.ToGeodetic();
                            var newPosition = new double3(pos.Longitude.Degrees, pos.Latitude.Degrees,
                                pos.Altitude * 1000);

                            satellitePositions[satellite.Tle.NoradNumber].Enqueue(newPosition);

                            if (i == satellites.Count - 1 && j == positions - 1)
                            {
                                nextTime = futureTime;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }

                Task.Delay((int)(TotalUpdateDelay * 1000));
            }
        });
    }

    private IEnumerator PositionUpdateCoroutine()
    {
        int startIndex = 0;
        int updateCount = 0;

        while (true)
        { 
            updateCount++;
            if (updateCount % 100 == 0)
            {
                Debug.Log($"Position Update: Frame {updateCount}");
            }
            
            var delay = CalculateDelay(satellites.Count, TotalUpdateDelay);
            int endIndex = Mathf.Min(startIndex + UpdateBatchSize, satellites.Count);
            
            // Standard-Modus ist Space (false)
            bool isEarthMode = false;
            if (zoomController != null && zoomController.targetCamera != null)
            {
                isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                if (i >= satellites.Count || satellites[i] == null) continue;
                
                uint noradNumber = satellites[i].Tle.NoradNumber;
                
                if (satellitePositions.ContainsKey(noradNumber) && 
                    satellitePositions[noradNumber].Count > 0)
                {
                    var nextPosition = satellitePositions[noradNumber].Dequeue();
                    
                    // WICHTIG: Wir überspringen den Visibility-Check, damit die Satelliten immer sichtbar sind
                    // Später können wir ihn wieder aktivieren, wenn alles funktioniert
                    
                    if (isEarthMode)
                    {
                        // Im Erde-Modus: Smooth interpolieren
                        if (activeRoutines.ContainsKey(noradNumber) && 
                            activeRoutines[noradNumber] != null)
                        {
                            StopCoroutine(activeRoutines[noradNumber]);
                        }
                        
                        activeRoutines[noradNumber] = 
                            StartCoroutine(InterpolatePosition(satellites[i], nextPosition));
                    }
                    else
                    {
                        // Im Space-Modus: Direkt setzen
                        satellites[i].Anchor.longitudeLatitudeHeight = nextPosition;
                        currentPositions[noradNumber] = nextPosition;
                    }
                }
            }

            yield return new WaitForSeconds(delay);

            startIndex = endIndex;
            if (startIndex >= satellites.Count)
            {
                startIndex = 0;
            }
        }
    }
    
    private IEnumerator InterpolatePosition(SatelliteController satellite, double3 targetPosition)
    {
        uint noradNumber = satellite.Tle.NoradNumber;
        
        // Initialisiere aktuelle Position falls noch nicht vorhanden
        if (!currentPositions.ContainsKey(noradNumber) ||
            (currentPositions[noradNumber].x == 0 && 
             currentPositions[noradNumber].y == 0 && 
             currentPositions[noradNumber].z == 0))
        {
            currentPositions[noradNumber] = satellite.Anchor.longitudeLatitudeHeight;
        }
        
        double3 startPosition = currentPositions[noradNumber];
        float elapsedTime = 0;
        
        while (elapsedTime < interpolationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / interpolationDuration);
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            double3 newPosition = new double3(
                Mathf.Lerp((float)startPosition.x, (float)targetPosition.x, smoothT),
                Mathf.Lerp((float)startPosition.y, (float)targetPosition.y, smoothT),
                Mathf.Lerp((float)startPosition.z, (float)targetPosition.z, smoothT)
            );
            
            satellite.Anchor.longitudeLatitudeHeight = newPosition;
            
            yield return null;
        }
        
        satellite.Anchor.longitudeLatitudeHeight = targetPosition;
        currentPositions[noradNumber] = targetPosition;
    }

    private float CalculateDelay(int totalItems, float targetIterationDuration)
    {
        if (UpdateBatchSize <= 0 || totalItems <= 0)
            return 0;

        int numberOfBatches = (int)Math.Ceiling((double)totalItems / UpdateBatchSize);
        return (float)targetIterationDuration / (float)numberOfBatches;
    }
}